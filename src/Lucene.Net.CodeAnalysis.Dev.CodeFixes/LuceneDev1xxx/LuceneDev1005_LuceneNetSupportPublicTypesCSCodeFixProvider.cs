/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SyntaxKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Lucene.Net.CodeAnalysis.Dev;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider)), Shared]
public class LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider : CodeFixProvider
{
    // Specify the diagnostic IDs of analyzers that are expected to be linked.
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.Id];

    public override FixAllProvider? GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        // We link only one diagnostic and assume there is only one diagnostic in the context.
        var diagnostic = context.Diagnostics.Single();

        // 'SourceSpan' of 'Location' is the highlighted area. We're going to use this area to find the 'SyntaxNode' to rename.
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Get the root of Syntax Tree that contains the highlighted diagnostic.
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        // Find SyntaxNode corresponding to the diagnostic.
        var diagnosticNode = root?.FindNode(diagnosticSpan);

        if (diagnosticNode is MemberDeclarationSyntax declaration)
        {
            var name = declaration switch
            {
                BaseTypeDeclarationSyntax baseTypeDeclaration => baseTypeDeclaration.Identifier.ToString(),
                DelegateDeclarationSyntax delegateDeclaration => delegateDeclaration.Identifier.ToString(),
                _ => null
            };

            if (name == null)
            {
                return;
            }

            // Register a code action that will invoke the fix.
            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.MakeXInternal,
                    createChangedSolution: c => MakeDeclarationInternal(context.Document, declaration, c),
                    "MakeDeclarationInternal",
                    name),
                diagnostic);
        }
    }

    private static async Task<Solution> MakeDeclarationInternal(Document document,
        MemberDeclarationSyntax memberDeclaration,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return solution;

        // Get the symbol for this type declaration
        var symbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
        if (symbol == null) return solution;

        // Find all partial declarations of this symbol
        var declaringSyntaxReferences = symbol.DeclaringSyntaxReferences;

        // Update all partial declarations across all documents
        foreach (var syntaxReference in declaringSyntaxReferences)
        {
            var declarationSyntax = await syntaxReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
            if (declarationSyntax is not MemberDeclarationSyntax declaration) continue;

            var declarationDocument = solution.GetDocument(syntaxReference.SyntaxTree);
            if (declarationDocument == null) continue;

            var syntaxRoot = await declarationDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxRoot == null) continue;

            // Get leading trivia from the first modifier (which contains license headers/comments)
            var leadingTrivia = declaration.Modifiers.Count > 0
                ? declaration.Modifiers[0].LeadingTrivia
                : SyntaxTriviaList.Empty;

            // Get trailing trivia from the accessibility modifier we're removing (typically whitespace)
            var accessibilityModifier = declaration.Modifiers
                .FirstOrDefault(m => m.IsKind(SyntaxKind.PublicKeyword) ||
                                    m.IsKind(SyntaxKind.InternalKeyword) ||
                                    m.IsKind(SyntaxKind.ProtectedKeyword) ||
                                    m.IsKind(SyntaxKind.PrivateKeyword));
            var trailingTrivia = accessibilityModifier.TrailingTrivia;

            // Remove existing accessibility modifiers
            var newModifiers = SyntaxFactory.TokenList(
                    declaration.Modifiers
                    .Where(modifier => !modifier.IsKind(SyntaxKind.PrivateKeyword) &&
                                       !modifier.IsKind(SyntaxKind.ProtectedKeyword) &&
                                       !modifier.IsKind(SyntaxKind.InternalKeyword) &&
                                       !modifier.IsKind(SyntaxKind.PublicKeyword))
                ).Insert(0, SyntaxFactory.Token(leadingTrivia, SyntaxKind.InternalKeyword, trailingTrivia)); // Ensure 'internal' is the first modifier with preserved trivia

            var newDeclaration = declaration.WithModifiers(newModifiers);
            var newRoot = syntaxRoot.ReplaceNode(declaration, newDeclaration);
            solution = solution.WithDocumentSyntaxRoot(declarationDocument.Id, newRoot);
        }

        return solution;
    }
}
