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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6001_StringComparisonCodeFixProvider)), Shared]
    public sealed class LuceneDev6001_StringComparisonCodeFixProvider : CodeFixProvider
    {
        private const string TitleOrdinal = "Use StringComparison.Ordinal";
        private const string TitleOrdinalIgnoreCase = "Use StringComparison.OrdinalIgnoreCase";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev6001_MissingStringComparison.Id,
                Descriptors.LuceneDev6001_InvalidStringComparison.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var invocation = root.FindToken(diagnosticSpan.Start)
                .Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault();
            if (invocation == null) return;

            // Offer both Ordinal and OrdinalIgnoreCase fixes
            context.RegisterCodeFix(CodeAction.Create(
                title: TitleOrdinal,
                createChangedDocument: c => FixInvocationAsync(context.Document, invocation, "Ordinal", c),
                equivalenceKey: TitleOrdinal),
                diagnostic);

            context.RegisterCodeFix(CodeAction.Create(
                title: TitleOrdinalIgnoreCase,
                createChangedDocument: c => FixInvocationAsync(context.Document, invocation, "OrdinalIgnoreCase", c),
                equivalenceKey: TitleOrdinalIgnoreCase),
                diagnostic);
        }

        private static async Task<Document> FixInvocationAsync(Document document, InvocationExpressionSyntax invocation, string comparisonMember, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            // Create the StringComparison expression
            var stringComparisonExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("StringComparison"),
                SyntaxFactory.IdentifierName(comparisonMember));

            var newArg = SyntaxFactory.Argument(stringComparisonExpr);

            // Check if a StringComparison argument already exists
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var stringComparisonType = semanticModel?.Compilation.GetTypeByMetadataName("System.StringComparison");
            var existingArg = invocation.ArgumentList.Arguments.FirstOrDefault(arg =>
                semanticModel != null &&
                (SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arg.Expression).Type, stringComparisonType) ||
                 (semanticModel.GetSymbolInfo(arg.Expression).Symbol is IFieldSymbol f && SymbolEqualityComparer.Default.Equals(f.ContainingType, stringComparisonType))));

            // Replace existing argument or add new one
            var newInvocation = existingArg != null
                ? invocation.ReplaceNode(existingArg, newArg)
                : invocation.WithArgumentList(invocation.ArgumentList.AddArguments(newArg));

            // Combine adding 'using System;' and replacing invocation in a single root
            var newRoot = EnsureSystemUsing(root).ReplaceNode(invocation, newInvocation);

            return document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode EnsureSystemUsing(SyntaxNode root)
        {
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var hasSystemUsing = compilationUnit.Usings.Any(u =>
                    u.Name is IdentifierNameSyntax id && id.Identifier.ValueText == "System");

                if (!hasSystemUsing)
                {
                    var systemUsing = SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System"))
                        .WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);
                    return compilationUnit.AddUsings(systemUsing);
                }
            }

            return root;
        }
    }
}
