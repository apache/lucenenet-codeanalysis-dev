/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev6xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6003_6004_SpanComparisonCodeFixProvider)), Shared]
    public sealed class LuceneDev6003_6004_SpanComparisonCodeFixProvider : CodeFixProvider
    {
        private const string TitleRemoveOrdinal = "Remove redundant StringComparison.Ordinal";
        private const string TitleOptimizeToDefaultOrdinal = "Optimize to default Ordinal comparison (remove argument)";
        private const string TitleReplaceWithOrdinalIgnoreCase = "Use StringComparison.OrdinalIgnoreCase";

        // Integer values for StringComparison Enum members (used for semantic analysis)
        private const int CurrentCulture = 0;
        private const int CurrentCultureIgnoreCase = 1;
        private const int InvariantCulture = 2;
        private const int InvariantCultureIgnoreCase = 3;
        private const int Ordinal = 4;
        private const int OrdinalIgnoreCase = 5;

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev6003_RedundantOrdinal.Id,
                Descriptors.LuceneDev6004_InvalidComparison.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindNode(diagnosticSpan).FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation == null)
                return;

            // Skip char literals and single-character string literals when safe (LuceneDev6005 handles conversion).
            var firstArgExpr = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (firstArgExpr is LiteralExpressionSyntax lit)
            {
                if (lit.IsKind(SyntaxKind.CharacterLiteralExpression))
                    return;

                if (lit.IsKind(SyntaxKind.StringLiteralExpression) && lit.Token.ValueText.Length == 1)
                {
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    if (semanticModel == null)
                        return;

                    bool hasStringComparisonArg = invocation.ArgumentList.Arguments.Any(arg =>
                        (semanticModel.GetTypeInfo(arg.Expression).Type is INamedTypeSymbol t &&
                            t.ToDisplayString() == "System.StringComparison")
                        || (semanticModel.GetSymbolInfo(arg.Expression).Symbol is IFieldSymbol f &&
                            f.ContainingType?.ToDisplayString() == "System.StringComparison"));

                    if (!hasStringComparisonArg)
                        return;
                }
            }
            switch (diagnostic.Id)
            {
                case var id when id == Descriptors.LuceneDev6003_RedundantOrdinal.Id:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: TitleRemoveOrdinal,
                            createChangedDocument: c => RemoveStringComparisonArgumentAsync(context.Document, invocation, c),
                            equivalenceKey: "RemoveRedundantOrdinal"),
                        diagnostic);
                    break;

                case var id when id == Descriptors.LuceneDev6004_InvalidComparison.Id:
                    var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    if (semanticModel == null)
                        return;

                    var comparisonArg = invocation.ArgumentList.Arguments.FirstOrDefault(arg =>
                        semanticModel.GetTypeInfo(arg.Expression).Type?.ToDisplayString() == "System.StringComparison");

                    if (comparisonArg == null)
                        return;

                    var originalComparisonValue = semanticModel.GetConstantValue(comparisonArg.Expression);

                    if (originalComparisonValue.HasValue && originalComparisonValue.Value is int intValue)
                    {
                        // Check if the original comparison was case-insensitive
                        bool wasCaseInsensitive = intValue == CurrentCultureIgnoreCase ||
                                                  intValue == InvariantCultureIgnoreCase;

                        if (wasCaseInsensitive)
                        {
                            // Fix 1: Case-Insensitive Invalid -> OrdinalIgnoreCase (Single, targeted fix)
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: TitleReplaceWithOrdinalIgnoreCase,
                                    createChangedDocument: c => ReplaceWithStringComparisonAsync(context.Document, invocation, "OrdinalIgnoreCase", c),
                                    equivalenceKey: "ReplaceWithOrdinalIgnoreCase"),
                                diagnostic);

                            // Optionally, still offer the case-sensitive fix for completeness
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: "Use StringComparison.Ordinal", // Offer Ordinal as second choice
                                    createChangedDocument: c => ReplaceWithStringComparisonAsync(context.Document, invocation, "Ordinal", c),
                                    equivalenceKey: "ReplaceWithOrdinal"),
                                diagnostic);
                        }
                        else
                        {
                            // Fix 1: Case-Sensitive Invalid (CurrentCulture/InvariantCulture) -> Optimal Default (Remove argument)
                            // This skips the redundant intermediate step (Ordinal)
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: TitleOptimizeToDefaultOrdinal,
                                    createChangedDocument: c => RemoveStringComparisonArgumentAsync(context.Document, invocation, c),
                                    equivalenceKey: "OptimizeToDefaultOrdinal"),
                                diagnostic);

                            // Optionally, still offer the case-insensitive fix for completeness
                            context.RegisterCodeFix(
                                CodeAction.Create(
                                    title: TitleReplaceWithOrdinalIgnoreCase,
                                    createChangedDocument: c => ReplaceWithStringComparisonAsync(context.Document, invocation, "OrdinalIgnoreCase", c),
                                    equivalenceKey: "ReplaceWithOrdinalIgnoreCase"),
                                diagnostic);
                        }
                    }
                    break;
            }
        }

        private static async Task<Document> RemoveStringComparisonArgumentAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            var compilation = semanticModel.Compilation;
            var stringComparisonType = compilation.GetTypeByMetadataName("System.StringComparison");
            if (stringComparisonType == null)
                return document;

            // Find the StringComparison argument
            ArgumentSyntax? argumentToRemove = null;
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                var argType = semanticModel.GetTypeInfo(arg.Expression, cancellationToken).Type;
                if (argType != null && SymbolEqualityComparer.Default.Equals(argType, stringComparisonType))
                {
                    argumentToRemove = arg;
                    break;
                }

                // fallback: check if it's a member access of StringComparison.*
                if (argumentToRemove == null && arg.Expression is MemberAccessExpressionSyntax member &&
                    member.Expression is IdentifierNameSyntax idName &&
                    idName.Identifier.ValueText == "StringComparison")
                {
                    argumentToRemove = arg;
                    break;
                }

            }

            if (argumentToRemove == null)
                return document;

            // Remove the argument
            var newArguments = invocation.ArgumentList.Arguments.Remove(argumentToRemove);
            var newArgumentList = invocation.ArgumentList.WithArguments(newArguments);

            // CRITICAL FIX: Removed NormalizeWhitespace() which causes test instability
            var newInvocation = invocation.WithArgumentList(newArgumentList)
                                            .WithTriviaFrom(invocation); // Preserving trivia on the outer node is usually fine

            var newRoot = root.ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> ReplaceWithStringComparisonAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            string comparisonMember,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel == null)
                return document;

            var compilation = semanticModel.Compilation;
            var stringComparisonType = compilation.GetTypeByMetadataName("System.StringComparison");
            if (stringComparisonType == null)
                return document;

            // Find the StringComparison argument
            ArgumentSyntax? argumentToReplace = null;
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                var argType = semanticModel.GetTypeInfo(arg.Expression, cancellationToken).Type;
                if (argType != null && SymbolEqualityComparer.Default.Equals(argType, stringComparisonType))
                {
                    argumentToReplace = arg;
                    break;
                }

                // fallback: check if it's a member access of StringComparison.*
                if (argumentToReplace == null && arg.Expression is MemberAccessExpressionSyntax member &&
                    member.Expression is IdentifierNameSyntax idName &&
                    idName.Identifier.ValueText == "StringComparison")
                {
                    argumentToReplace = arg;
                    break;
                }

            }

            if (argumentToReplace == null)
                return document;

            // Check if argument already uses System.StringComparison
            bool isFullyQualified = argumentToReplace.Expression.ToString().StartsWith("System.StringComparison");

            // Create new StringComparison expression
            var baseExpression = isFullyQualified
                ? (ExpressionSyntax)SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("StringComparison"))
                : SyntaxFactory.IdentifierName("StringComparison");

            var newExpression = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                baseExpression,
                SyntaxFactory.IdentifierName(comparisonMember));


            var newArgument = argumentToReplace.WithExpression(newExpression);

            // CRITICAL FIX: Removed WithTriviaFrom(invocation) and NormalizeWhitespace() which cause test instability
            var newInvocation = invocation.ReplaceNode(argumentToReplace, newArgument);

            var newRoot = root;
            if (!isFullyQualified)
            {
                newRoot = EnsureSystemUsing(newRoot);
            }
            newRoot = newRoot.ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

        // EnsureSystemUsing remains unchanged as it looks correct for adding a using directive
        private static SyntaxNode EnsureSystemUsing(SyntaxNode root)
        {
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var hasSystemUsing = compilationUnit.Usings.Any(u =>
                    u.Name is IdentifierNameSyntax id && id.Identifier.ValueText == "System");

                // only add if missing
                if (!hasSystemUsing)
                {
                    var systemUsing = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"))
                        .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);

                    return compilationUnit.AddUsings(systemUsing);
                }
            }

            return root;
        }
    }
}
