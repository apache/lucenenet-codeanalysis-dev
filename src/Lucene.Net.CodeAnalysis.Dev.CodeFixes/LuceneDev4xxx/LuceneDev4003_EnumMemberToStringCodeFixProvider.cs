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
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev4xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev4003_EnumMemberToStringCodeFixProvider)), Shared]
    public sealed class LuceneDev4003_EnumMemberToStringCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Use nameof instead of ToString()";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.LuceneDev4003_EnumMemberToString.Id);

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
            if (invocation?.Expression is not MemberAccessExpressionSyntax)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => ReplaceWithNameofAsync(context.Document, invocation, ct),
                    equivalenceKey: nameof(Title)),
                diagnostic);
        }

        private static async Task<Document> ReplaceWithNameofAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null || invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return document;

            // MyEnum.Value.ToString() -> nameof(MyEnum.Value)
            var nameofExpression = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.IdentifierName("nameof"),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(memberAccess.Expression.WithoutTrivia()))))
                .WithTriviaFrom(invocation);

            return document.WithSyntaxRoot(root.ReplaceNode(invocation, nameofExpression));
        }
    }
}
