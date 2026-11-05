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
using Lucene.Net.CodeAnalysis.Dev.CodeFixes;
using Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev3xxx
{
    /// <summary>
    /// Code fixes for <see cref="Descriptors.LuceneDev3000_CrefToNonPublicSymbol"/>: replaces the
    /// <c>see</c>/<c>seealso</c> tag with a <c>&lt;c&gt;</c> tag or plain text (using the link
    /// text if present, otherwise the cref text), or makes the referenced symbol public.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider))]
    [Shared]
    public sealed class LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider : CodeFixProvider
    {
        public const string ReplaceWithCodeTagEquivalenceKey = "LuceneDev3000_ReplaceWithCodeTag";
        public const string ReplaceWithTextEquivalenceKey = "LuceneDev3000_ReplaceWithText";
        public const string MakePublicEquivalenceKey = "LuceneDev3000_MakePublic";

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(Descriptors.LuceneDev3000_CrefToNonPublicSymbol.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var crefAttribute = root?.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true)
                .FirstAncestorOrSelf<XmlCrefAttributeSyntax>();

            XmlNodeSyntax? element = crefAttribute?.Parent switch
            {
                XmlEmptyElementSyntax emptyElement => emptyElement,
                XmlElementStartTagSyntax { Parent: XmlElementSyntax fullElement } => fullElement,
                _ => null,
            };

            if (crefAttribute is null || element is null)
                return;

            var text = GetReplacementText(element, crefAttribute);

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.ReplaceWithCodeTagX,
                    c => ReplaceAsync(context.Document, element.Span, $"<c>{text}</c>", c),
                    ReplaceWithCodeTagEquivalenceKey,
                    text),
                diagnostic);

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.ReplaceWithTextX,
                    c => ReplaceAsync(context.Document, element.Span, text, c),
                    ReplaceWithTextEquivalenceKey,
                    text),
                diagnostic);

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var referencedSymbol = semanticModel?.GetSymbolInfo(crefAttribute.Cref, context.CancellationToken).Symbol;

            if (referencedSymbol is not null && CanMakePublic(referencedSymbol))
            {
                var name = referencedSymbol is IMethodSymbol { MethodKind: MethodKind.Constructor }
                    ? referencedSymbol.ContainingType.Name
                    : referencedSymbol.Name;

                context.RegisterCodeFix(
                    CodeActionHelper.CreateFromResource(
                        CodeFixResources.MakeXPublic,
                        c => MakePublicAsync(context.Document.Project.Solution, referencedSymbol, c),
                        MakePublicEquivalenceKey,
                        name),
                    diagnostic);
            }
        }

        /// <summary>
        /// Returns the link text of a <c>&lt;see cref="..."&gt;link text&lt;/see&gt;</c> element,
        /// or the cref text if the element has no link text.
        /// </summary>
        private static string GetReplacementText(XmlNodeSyntax element, XmlCrefAttributeSyntax crefAttribute)
        {
            if (element is XmlElementSyntax fullElement)
            {
                var content = string.Concat(fullElement.Content.Select(n => n.ToFullString()));
                if (!string.IsNullOrWhiteSpace(content))
                    return content;
            }

            return crefAttribute.Cref.ToString();
        }

        private static async Task<Document> ReplaceAsync(Document document, TextSpan span, string replacement, CancellationToken cancellationToken)
        {
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return document.WithText(text.Replace(span, replacement));
        }

        /// <summary>
        /// Making the symbol public only fixes the broken link if its containing types are already
        /// publicly visible. Field declarations with multiple variables are excluded, since changing
        /// the accessibility would also make the other variables public.
        /// </summary>
        private static bool CanMakePublic(ISymbol symbol)
        {
            if (symbol is INamespaceSymbol or ITypeParameterSymbol or IParameterSymbol)
                return false;

            if (symbol.DeclaringSyntaxReferences.IsEmpty)
                return false;

            if (symbol.ContainingType is not null && !SymbolVisibilityHelper.IsPubliclyVisible(symbol.ContainingType))
                return false;

            return symbol.DeclaringSyntaxReferences.All(r =>
                r.GetSyntax() is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: > 1 } });
        }

        private static async Task<Solution> MakePublicAsync(Solution solution, ISymbol symbol, CancellationToken cancellationToken)
        {
            var solutionEditor = new SolutionEditor(solution);

            foreach (var reference in symbol.DeclaringSyntaxReferences)
            {
                var node = await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
                var document = solution.GetDocument(node.SyntaxTree);
                if (document is null)
                    continue;

                // Fields and field-like events are declared by a variable declarator, but the
                // accessibility modifiers are on the enclosing declaration.
                var declaration = node is VariableDeclaratorSyntax
                    ? node.FirstAncestorOrSelf<BaseFieldDeclarationSyntax>() ?? node
                    : node;

                var editor = await solutionEditor.GetDocumentEditorAsync(document.Id, cancellationToken).ConfigureAwait(false);
                editor.SetAccessibility(declaration, Accessibility.Public);
            }

            return solutionEditor.GetChangedSolution();
        }
    }
}
