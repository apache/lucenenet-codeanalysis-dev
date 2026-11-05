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
using System.Linq;
using System.Threading;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev3xxx
{
    /// <summary>
    /// Flags <c>&lt;see cref="..."/&gt;</c> and <c>&lt;seealso cref="..."/&gt;</c> tags in the XML
    /// doc comment of a publicly visible symbol that reference a symbol that is not publicly
    /// visible. These compile without warning, but produce broken links in the API documentation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev3000_CrefToNonPublicSymbolAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev3000_CrefToNonPublicSymbol);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeCrefAttribute, SyntaxKind.XmlCrefAttribute);
        }

        private static void AnalyzeCrefAttribute(SyntaxNodeAnalysisContext context)
        {
            var crefAttribute = (XmlCrefAttributeSyntax)context.Node;

            if (!IsLinkingElement(crefAttribute.Parent))
                return;

            var documentedSymbol = GetDocumentedSymbol(crefAttribute, context.SemanticModel, context.CancellationToken);
            if (documentedSymbol is null || !SymbolVisibilityHelper.IsPubliclyVisible(documentedSymbol))
                return;

            var symbolInfo = context.SemanticModel.GetSymbolInfo(crefAttribute.Cref, context.CancellationToken);

            // An ambiguous cref (such as an overloaded method group) has no single symbol; only
            // report it when none of the candidates are visible.
            var referencedSymbols = symbolInfo.Symbol is not null
                ? ImmutableArray.Create(symbolInfo.Symbol)
                : symbolInfo.CandidateSymbols;

            if (referencedSymbols.IsEmpty || referencedSymbols.Any(SymbolVisibilityHelper.IsPubliclyVisible))
                return;

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev3000_CrefToNonPublicSymbol,
                crefAttribute.Cref.GetLocation(),
                crefAttribute.Cref.ToString()));
        }

        /// <summary>
        /// Returns <c>true</c> if the cref attribute belongs to a <c>see</c> or <c>seealso</c>
        /// element, which are rendered as links in the API documentation.
        /// </summary>
        private static bool IsLinkingElement(SyntaxNode? node)
        {
            var name = node switch
            {
                XmlEmptyElementSyntax emptyElement => emptyElement.Name,
                XmlElementStartTagSyntax startTag => startTag.Name,
                _ => null,
            };

            return name is { Prefix: null }
                && (name.LocalName.ValueText == "see" || name.LocalName.ValueText == "seealso");
        }

        private static ISymbol? GetDocumentedSymbol(XmlCrefAttributeSyntax crefAttribute, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var docComment = crefAttribute.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();

            // The doc comment is leading trivia of the first token of the declaration it documents.
            var declaration = docComment?.ParentTrivia.Token.Parent?.AncestorsAndSelf()
                .FirstOrDefault(n => n is MemberDeclarationSyntax or StatementSyntax);

            switch (declaration)
            {
                case BaseNamespaceDeclarationSyntax:
                    // Doc comments are not valid on namespaces (CS1587) and do not produce documentation.
                    return null;
                case BaseFieldDeclarationSyntax fieldDeclaration:
                    // All variables in a field declaration share the same accessibility.
                    var variable = fieldDeclaration.Declaration.Variables.FirstOrDefault();
                    return variable is null ? null : semanticModel.GetDeclaredSymbol(variable, cancellationToken);
                case MemberDeclarationSyntax memberDeclaration:
                    return semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
                default:
                    // Local functions and misplaced doc comments are never publicly visible.
                    return null;
            }
        }
    }
}
