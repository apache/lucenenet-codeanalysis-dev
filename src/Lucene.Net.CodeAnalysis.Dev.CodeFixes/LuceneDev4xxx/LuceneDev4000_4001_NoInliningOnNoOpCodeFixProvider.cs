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

            if (attribute.Parent is AttributeListSyntax attrList)
            {
                SyntaxNode newRoot;
                if (attrList.Attributes.Count == 1)
                {
                    newRoot = root.RemoveNode(attrList, SyntaxRemoveOptions.KeepNoTrivia)!;
                }
                else
                {
                    var newList = attrList.WithAttributes(attrList.Attributes.Remove(attribute));
                    newRoot = root.ReplaceNode(attrList, newList);
                }
                return document.WithSyntaxRoot(newRoot);
            }

            return document;
        }
    }
}
