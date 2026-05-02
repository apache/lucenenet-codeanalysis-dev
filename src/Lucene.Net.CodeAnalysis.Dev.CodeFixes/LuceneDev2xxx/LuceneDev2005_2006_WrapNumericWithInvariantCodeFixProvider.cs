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

/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file for additional information.
 * The ASF licenses this file under the Apache License, Version 2.0.
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

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev2xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev2005_2006_WrapNumericWithInvariantCodeFixProvider)), Shared]
    public sealed class LuceneDev2005_2006_WrapNumericWithInvariantCodeFixProvider : CodeFixProvider
    {
        private const string TitleInvariant = "Wrap with .ToString(CultureInfo.InvariantCulture)";
        private const string TitleCurrent = "Wrap with .ToString(CultureInfo.CurrentCulture)";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev2005_NumericStringConcatenation.Id,
                Descriptors.LuceneDev2006_NumericStringInterpolation.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var expression = token.Parent?
                    .AncestorsAndSelf()
                    .OfType<ExpressionSyntax>()
                    .FirstOrDefault(e => e.Span == diagnostic.Location.SourceSpan);

                if (expression is null) continue;

                Register(context, expression, "InvariantCulture", TitleInvariant, diagnostic);
                Register(context, expression, "CurrentCulture", TitleCurrent, diagnostic);
            }
        }

        private static void Register(
            CodeFixContext context,
            ExpressionSyntax expression,
            string cultureMember,
            string title,
            Diagnostic diagnostic)
        {
            context.RegisterCodeFix(CodeAction.Create(
                title: title,
                createChangedDocument: c => WrapAsync(context.Document, expression, cultureMember, c),
                equivalenceKey: title),
                diagnostic);
        }

        private static async Task<Document> WrapAsync(
            Document document,
            ExpressionSyntax expression,
            string cultureMember,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            // Parenthesize complex expressions so we don't accidentally bind .ToString to part of a larger expression.
            ExpressionSyntax receiver = expression is IdentifierNameSyntax || expression is LiteralExpressionSyntax || expression is InvocationExpressionSyntax || expression is MemberAccessExpressionSyntax
                ? expression.WithoutTrivia()
                : SyntaxFactory.ParenthesizedExpression(expression.WithoutTrivia());

            var cultureExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("CultureInfo"),
                SyntaxFactory.IdentifierName(cultureMember));

            var toStringCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("ToString")),
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(cultureExpr))))
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());

            var newRoot = LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider
                .EnsureGlobalizationUsing(root)
                .ReplaceNode(expression, toStringCall);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
