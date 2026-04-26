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
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
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
using Microsoft.CodeAnalysis.Formatting;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev1xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev1007_1008_DictionaryIndexerCodeFixProvider)), Shared]
    public sealed class LuceneDev1007_1008_DictionaryIndexerCodeFixProvider : CodeFixProvider
    {
        private const string TitleReturn = "Use TryGetValue and return default on missing key";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType.Id,
                Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel == null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var elementAccess = root.FindToken(diagnostic.Location.SourceSpan.Start)
                                        .Parent?
                                        .AncestorsAndSelf()
                                        .OfType<ElementAccessExpressionSyntax>()
                                        .FirstOrDefault(e => e.Span.Contains(diagnostic.Location.SourceSpan));
                if (elementAccess == null)
                    continue;

                // Only handle the "return dict[key];" pattern automatically.
                if (elementAccess.Parent is not ReturnStatementSyntax returnStmt
                    || returnStmt.Expression != elementAccess)
                {
                    continue;
                }

                // If the receiver type doesn't expose an accessible TryGetValue method
                // (e.g. only via explicit interface implementation), skip — the rewrite would not compile.
                if (!HasAccessibleTryGetValue(semanticModel, elementAccess))
                    continue;

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: TitleReturn,
                        createChangedDocument: c => ConvertReturnAsync(context.Document, returnStmt, elementAccess, c),
                        equivalenceKey: TitleReturn),
                    diagnostic);
            }
        }

        private static bool HasAccessibleTryGetValue(SemanticModel semanticModel, ElementAccessExpressionSyntax elementAccess)
        {
            var receiverType = semanticModel.GetTypeInfo(elementAccess.Expression).Type;
            if (receiverType == null)
                return false;

            foreach (var member in receiverType.GetMembers("TryGetValue"))
            {
                if (member is not IMethodSymbol method)
                    continue;
                if (method.IsStatic)
                    continue;
                if (method.DeclaredAccessibility != Accessibility.Public)
                    continue;
                if (method.Parameters.Length != 2)
                    continue;
                if (method.Parameters[1].RefKind != RefKind.Out)
                    continue;
                return true;
            }

            return false;
        }

        private static async Task<Document> ConvertReturnAsync(
            Document document,
            ReturnStatementSyntax returnStmt,
            ElementAccessExpressionSyntax elementAccess,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var receiver = elementAccess.Expression;
            var keyArg = elementAccess.ArgumentList.Arguments.FirstOrDefault();
            if (keyArg == null) return document;

            var outName = PickLocalName(returnStmt);

            // receiver.TryGetValue(key, out var <outName>)
            var tryGetValueInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver.WithoutTrivia(),
                    SyntaxFactory.IdentifierName("TryGetValue")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(new[]
                {
                    keyArg.WithoutTrivia(),
                    SyntaxFactory.Argument(
                        SyntaxFactory.DeclarationExpression(
                            SyntaxFactory.IdentifierName(
                                SyntaxFactory.Identifier("var")),
                            SyntaxFactory.SingleVariableDesignation(SyntaxFactory.Identifier(outName))))
                        .WithRefOrOutKeyword(SyntaxFactory.Token(SyntaxKind.OutKeyword))
                })));

            // tryGetValueInvocation ? <outName> : default
            var ternary = SyntaxFactory.ConditionalExpression(
                tryGetValueInvocation,
                SyntaxFactory.IdentifierName(outName),
                SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression,
                    SyntaxFactory.Token(SyntaxKind.DefaultKeyword)));

            var newReturn = returnStmt.WithExpression(ternary).WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(returnStmt, newReturn);
            return document.WithSyntaxRoot(newRoot);
        }

        private static string PickLocalName(SyntaxNode context)
        {
            // Avoid collisions with identifiers in the enclosing member.
            var member = context.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            var names = member == null
                ? ImmutableHashSet<string>.Empty
                : member.DescendantTokens()
                        .Where(t => t.IsKind(SyntaxKind.IdentifierToken))
                        .Select(t => t.ValueText)
                        .ToImmutableHashSet();

            if (!names.Contains("value"))
                return "value";
            for (int i = 1; i < 100; i++)
            {
                var candidate = "value" + i;
                if (!names.Contains(candidate))
                    return candidate;
            }
            return "value";
        }
    }
}
