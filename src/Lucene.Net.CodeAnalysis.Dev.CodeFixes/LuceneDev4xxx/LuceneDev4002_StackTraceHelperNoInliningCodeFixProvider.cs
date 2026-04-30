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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev4xxx
{
    /// <summary>
    /// Code fix for LuceneDev4002: adds [MethodImpl(MethodImplOptions.NoInlining)]
    /// to the target method declaration referenced by the
    /// StackTraceHelper.DoesStackTraceContainMethod call. Adds
    /// `using System.Runtime.CompilerServices;` to the target's compilation unit
    /// if missing.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider)), Shared]
    public sealed class LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add [MethodImpl(MethodImplOptions.NoInlining)] to the referenced method";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.LuceneDev4002_MissingNoInlining.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var invocation = node as InvocationExpressionSyntax
                ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => AddNoInliningToTargetAsync(context.Document, invocation, ct),
                    equivalenceKey: nameof(Title)),
                diagnostic);
        }

        private static async Task<Solution> AddNoInliningToTargetAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null || invocation.ArgumentList.Arguments.Count != 2)
                return solution;

            var classArg = invocation.ArgumentList.Arguments[0].Expression;
            var methodArg = invocation.ArgumentList.Arguments[1].Expression;

            var (classNameValue, classTypeFromNameof) = ResolveClassReference(classArg, semanticModel);
            if (classNameValue is null)
                return solution;

            var methodNameValue = ResolveMethodNameValue(methodArg, semanticModel);
            if (methodNameValue is null)
                return solution;

            var compilation = semanticModel.Compilation;
            var targetType = classTypeFromNameof
                ?? FindSourceTypeByName(compilation, classNameValue);
            if (targetType is null)
                return solution;

            var methodImplAttrSymbol = compilation.GetTypeByMetadataName(
                "System.Runtime.CompilerServices.MethodImplAttribute");
            if (methodImplAttrSymbol is null)
                return solution;

            // Find the first ordinary method that needs the attribute.
            MethodDeclarationSyntax? targetDecl = null;
            foreach (var member in targetType.GetMembers(methodNameValue).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

                foreach (var declRef in member.DeclaringSyntaxReferences)
                {
                    if (declRef.GetSyntax(cancellationToken) is not MethodDeclarationSyntax methodDecl)
                        continue;

                    var declSemantic = compilation.GetSemanticModel(methodDecl.SyntaxTree);
                    if (NoInliningAttributeHelper.FindNoInliningAttribute(methodDecl, declSemantic, methodImplAttrSymbol) is not null)
                        continue;
                    if (NoInliningAttributeHelper.HasEmptyBody(methodDecl))
                        continue;
                    if (NoInliningAttributeHelper.IsInterfaceOrAbstractMethod(methodDecl))
                        continue;

                    targetDecl = methodDecl;
                    break;
                }

                if (targetDecl is not null)
                    break;
            }

            if (targetDecl is null)
                return solution;

            var targetTree = targetDecl.SyntaxTree;
            var targetDocument = solution.GetDocument(targetTree);
            if (targetDocument is null)
                return solution;

            var targetRoot = await targetTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Build [MethodImpl(MethodImplOptions.NoInlining)] as its own attribute
            // list. Place it ahead of any existing lists, copying the method's
            // leading trivia onto our new list and re-attaching one indent's worth
            // of trivia between the list and the original method position.
            var attribute = SyntaxFactory.Attribute(
                SyntaxFactory.IdentifierName("MethodImpl"),
                SyntaxFactory.AttributeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.AttributeArgument(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("MethodImplOptions"),
                                SyntaxFactory.IdentifierName("NoInlining"))))));

            var leadingIndent = ExtractLeadingIndentation(targetDecl);
            var endOfLine = DetectEndOfLine(targetRoot);
            var newAttributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute))
                .WithLeadingTrivia(targetDecl.GetLeadingTrivia())
                .WithTrailingTrivia(endOfLine, leadingIndent);

            var newAttributeLists = SyntaxFactory.List<AttributeListSyntax>(
                new[] { newAttributeList }.Concat(targetDecl.AttributeLists));

            var newMethodDecl = targetDecl
                .WithLeadingTrivia(SyntaxFactory.TriviaList())
                .WithAttributeLists(newAttributeLists);

            var newTargetRoot = targetRoot.ReplaceNode(targetDecl, newMethodDecl);

            // Add the using if missing.
            if (newTargetRoot is CompilationUnitSyntax compilationUnit)
            {
                const string requiredNs = "System.Runtime.CompilerServices";
                bool hasUsing = compilationUnit.Usings.Any(u => u.Name?.ToString() == requiredNs);
                if (!hasUsing)
                {
                    var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(requiredNs))
                        .WithTrailingTrivia(endOfLine);
                    compilationUnit = compilationUnit.AddUsings(usingDirective);
                    newTargetRoot = compilationUnit;
                }
            }

            return solution.WithDocumentSyntaxRoot(targetDocument.Id, newTargetRoot);
        }

        private static SyntaxTrivia DetectEndOfLine(SyntaxNode root)
        {
            // Match the source's existing line-ending convention so the fixed
            // output doesn't mix CRLF and LF.
            foreach (var trivia in root.DescendantTrivia())
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    return trivia;
            }
            return SyntaxFactory.EndOfLine("\n");
        }

        private static SyntaxTrivia ExtractLeadingIndentation(SyntaxNode node)
        {
            // Indentation = trailing whitespace of the leading trivia (after the
            // last newline). Used to align the new attribute list with the method.
            foreach (var t in node.GetLeadingTrivia().Reverse())
            {
                if (t.IsKind(SyntaxKind.WhitespaceTrivia))
                    return t;
                if (t.IsKind(SyntaxKind.EndOfLineTrivia))
                    break;
            }
            return SyntaxFactory.Whitespace("");
        }

        // ---- Argument resolution (mirrors the analyzer) ----

        private static (string? Name, INamedTypeSymbol? TypeFromNameof) ResolveClassReference(
            ExpressionSyntax expr,
            SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                var typeSymbol = semantic.GetTypeInfo(inner).Type as INamedTypeSymbol
                    ?? semantic.GetSymbolInfo(inner).Symbol as INamedTypeSymbol;
                if (typeSymbol is not null)
                    return (typeSymbol.Name, typeSymbol);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return (literal.Token.ValueText, null);

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return (s, null);

            return (null, null);
        }

        private static string? ResolveMethodNameValue(ExpressionSyntax expr, SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                return ExtractRightmostIdentifier(inner);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return s;

            return null;
        }

        private static string? ExtractRightmostIdentifier(ExpressionSyntax expr)
        {
            return expr switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                _ => null,
            };
        }

        private static INamedTypeSymbol? FindSourceTypeByName(Compilation compilation, string typeName)
        {
            foreach (var type in EnumerateAllTypes(compilation.Assembly.GlobalNamespace))
            {
                if (type.Name == typeName)
                    return type;
            }
            return null;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamedTypeSymbol type)
                {
                    yield return type;
                    foreach (var nested in EnumerateNestedTypes(type))
                        yield return nested;
                }
                else if (member is INamespaceSymbol child)
                {
                    foreach (var t in EnumerateAllTypes(child))
                        yield return t;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (var deeper in EnumerateNestedTypes(nested))
                    yield return deeper;
            }
        }
    }
}
