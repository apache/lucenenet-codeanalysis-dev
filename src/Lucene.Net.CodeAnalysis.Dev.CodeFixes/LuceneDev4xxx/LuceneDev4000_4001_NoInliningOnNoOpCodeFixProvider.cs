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

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev4xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider)), Shared]
    public sealed class LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider : CodeFixProvider
    {
        private const string TitleRemoveAttribute = "Remove [MethodImpl(MethodImplOptions.NoInlining)]";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev4000_NoInliningHasNoEffect.Id,
                Descriptors.LuceneDev4001_NoInliningOnEmptyMethod.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            var diagnostic = context.Diagnostics[0];
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            var attribute = node as AttributeSyntax
                ?? node.FirstAncestorOrSelf<AttributeSyntax>();
            if (attribute is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    TitleRemoveAttribute,
                    ct => RemoveAttributeAsync(context.Document, attribute, ct),
                    equivalenceKey: nameof(TitleRemoveAttribute) + diagnostic.Id),
                diagnostic);
        }

        private static async Task<Document> RemoveAttributeAsync(
            Document document,
            AttributeSyntax attribute,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            if (attribute.Parent is not AttributeListSyntax attrList)
                return document;

            SyntaxNode newRoot;
            if (attrList.Attributes.Count > 1)
            {
                // [Foo, MethodImpl(NoInlining), Bar] → [Foo, Bar]
                var newList = attrList.WithAttributes(attrList.Attributes.Remove(attribute));
                newRoot = root.ReplaceNode(attrList, newList);
                return document.WithSyntaxRoot(newRoot);
            }

            // Removing the whole [ ... ] list.
            //
            // Trivia handling: the attribute list's leading trivia is typically
            // (newline)(indent)[comment(newline)(indent)]*. We want to keep any
            // comments (and the newline that ends each one) but drop the final
            // whitespace block — which is just the indentation for the now-removed
            // attribute. The token following the list already carries its own
            // newline+indent, so leaving that whitespace in would double-indent the
            // next line. We move the trimmed trivia onto the next token.
            var leading = attrList.GetLeadingTrivia();
            int trim = leading.Count;
            while (trim > 0 && leading[trim - 1].IsKind(SyntaxKind.WhitespaceTrivia))
            {
                trim--;
            }
            var triviaToKeep = SyntaxFactory.TriviaList(leading.Take(trim));

            // Locate the parent that owns this attribute list. Use the parent's
            // AttributeLists collection (e.g. on MethodDeclarationSyntax) so that
            // removing the list and re-attaching trivia happens in a single step
            // and preserves indentation of the surrounding declaration.
            if (attrList.Parent is MemberDeclarationSyntax member)
            {
                var newAttrLists = member.AttributeLists.Remove(attrList);
                MemberDeclarationSyntax newMember = member.WithAttributeLists(newAttrLists);

                // Prepend the trivia we want to keep (e.g. comments) to the new
                // first token of the member declaration.
                if (triviaToKeep.Count > 0)
                {
                    var firstToken = newMember.GetFirstToken();
                    var combined = triviaToKeep.AddRange(firstToken.LeadingTrivia);
                    newMember = newMember.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(combined));
                }

                newRoot = root.ReplaceNode(member, newMember);
                return document.WithSyntaxRoot(newRoot);
            }

            // Fallback: just remove the list, dropping its trivia.
            newRoot = root.RemoveNode(attrList, SyntaxRemoveOptions.KeepNoTrivia)!;
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
