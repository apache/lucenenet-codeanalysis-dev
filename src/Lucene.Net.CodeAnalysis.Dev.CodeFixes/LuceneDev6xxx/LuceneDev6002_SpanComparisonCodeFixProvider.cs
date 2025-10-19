/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6002_SpanComparisonCodeFixProvider)), Shared]
    public sealed class LuceneDev6002_SpanComparisonCodeFixProvider : CodeFixProvider
    {
        private const string TitleRemoveOrdinal = "Remove redundant StringComparison.Ordinal";
        private const string TitleReplaceWithOrdinal = "Replace with StringComparison.Ordinal";
        private const string TitleReplaceWithOrdinalIgnoreCase = "Replace with StringComparison.OrdinalIgnoreCase";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev6002_RedundantOrdinal.Id,
                Descriptors.LuceneDev6002_InvalidComparison.Id);

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

            switch (diagnostic.Id)
            {
                case var id when id == Descriptors.LuceneDev6002_RedundantOrdinal.Id:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Remove redundant StringComparison.Ordinal",
                            createChangedDocument: c => RemoveStringComparisonArgumentAsync(context.Document, invocation, c),
                            equivalenceKey: "RemoveRedundantOrdinal"),
                        diagnostic);
                    break;

                case var id when id == Descriptors.LuceneDev6002_InvalidComparison.Id:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Use StringComparison.Ordinal",
                           createChangedDocument: c => ReplaceWithStringComparisonAsync(context.Document, invocation, "Ordinal", c),
                            equivalenceKey: "ReplaceWithOrdinal"),
                        diagnostic);

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Use StringComparison.OrdinalIgnoreCase",
                            createChangedDocument: c => ReplaceWithStringComparisonAsync(context.Document, invocation, "OrdinalIgnoreCase", c),
                            equivalenceKey: "ReplaceWithOrdinalIgnoreCase"),
                        diagnostic);
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

            // Remove the argument and normalize formatting
            var newArguments = invocation.ArgumentList.Arguments.Remove(argumentToRemove);
            var newArgumentList = invocation.ArgumentList.WithArguments(newArguments);
            var newInvocation = invocation.WithArgumentList(newArgumentList)
                                          .WithTriviaFrom(invocation)                // preserve trivia
                                          .NormalizeWhitespace();                    // clean formatting

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
            var newInvocation = invocation.ReplaceNode(argumentToReplace, newArgument)
                                          .WithTriviaFrom(invocation)
                                          .NormalizeWhitespace();

            var newRoot = root;
            if (!isFullyQualified)
            {
                newRoot = EnsureSystemUsing(newRoot);
            }
            newRoot = newRoot.ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

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
