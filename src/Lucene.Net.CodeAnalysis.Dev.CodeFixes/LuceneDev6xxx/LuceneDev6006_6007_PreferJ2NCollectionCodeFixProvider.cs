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
using Lucene.Net.CodeAnalysis.Dev.CodeFixes;
using Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx.LuceneDev6006_6007_PreferJ2NCollectionAnalyzer;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    /// <summary>
    /// Code fix for <see cref="Descriptors.LuceneDev6006_PreferJ2NCollection"/> and
    /// <see cref="Descriptors.LuceneDev6007_PreferJ2NOrderedCollection"/>: replaces the collection type
    /// with its J2N equivalent, qualified with the <c>JCG</c> alias (e.g. <c>JCG.List&lt;T&gt;</c>),
    /// and adds <c>using JCG = J2N.Collections.Generic;</c> if it is not already in scope.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6006_6007_PreferJ2NCollectionCodeFixProvider))]
    [Shared]
    public sealed class LuceneDev6006_6007_PreferJ2NCollectionCodeFixProvider : CodeFixProvider
    {
        private const string JcgAlias = "JCG";

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(
                Descriptors.LuceneDev6006_PreferJ2NCollection.Id,
                Descriptors.LuceneDev6007_PreferJ2NOrderedCollection.Id);

        // Nested type names (e.g. List<List<int>>) produce overlapping diagnostics, which the
        // batch fixer cannot merge, so all diagnostics in a document are fixed in a single pass.
        public override FixAllProvider GetFixAllProvider() => FixAll.Instance;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            if (!diagnostic.Properties.TryGetValue(ReplacementMetadataNameKey, out var metadataName) || metadataName is null)
                return;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null || FindTypeName(root, diagnostic) is null)
                return;

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.UseX,
                    createChangedDocument: c => FixDocumentAsync(context.Document, ImmutableArray.Create(diagnostic), c),
                    equivalenceKey: $"PreferJ2NCollection_{diagnostic.Id}",
                    $"{JcgAlias}.{GetDisplayName(metadataName)}"),
                diagnostic);
        }

        private static async Task<Document> FixDocumentAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (root is not CompilationUnitSyntax compilationUnit || semanticModel is null)
                return document;

            var replacements = new Dictionary<SyntaxNode, string>();
            foreach (var diagnostic in diagnostics)
            {
                if (!diagnostic.Properties.TryGetValue(ReplacementMetadataNameKey, out var metadataName) || metadataName is null)
                    continue;

                var typeName = FindTypeName(root, diagnostic);
                if (typeName is not null)
                    replacements[typeName] = GetSimpleName(metadataName);
            }

            if (replacements.Count == 0)
                return document;

            var firstNode = replacements.Keys.OrderBy(n => n.SpanStart).First();
            var aliasState = GetAliasState(semanticModel, firstNode.SpanStart, cancellationToken);

            var newRoot = root.ReplaceNodes(replacements.Keys, (original, rewritten) =>
            {
                var genericName = GetGenericName(rewritten);
                if (genericName is null)
                    return rewritten;

                // Using aliases cannot refer to other aliases declared in the same scope,
                // and a conflicting 'JCG' symbol means the alias cannot be used.
                var qualifier = aliasState == AliasState.Conflict || original.FirstAncestorOrSelf<UsingDirectiveSyntax>() is not null
                    ? SyntaxFactory.ParseName(J2NCollectionsGenericNamespace)
                    : SyntaxFactory.IdentifierName(JcgAlias);

                var newGenericName = SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(replacements[original]),
                    genericName.TypeArgumentList);

                return SyntaxFactory.QualifiedName(qualifier, newGenericName)
                    .WithTriviaFrom(rewritten);
            });

            if (aliasState == AliasState.Missing
                && replacements.Keys.Any(n => n.FirstAncestorOrSelf<UsingDirectiveSyntax>() is null))
            {
                newRoot = AddJcgAlias((CompilationUnitSyntax)newRoot, firstNode.SpanStart);
            }

            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Finds the reported (possibly qualified) type name node for the diagnostic. A plain
        /// <see cref="SyntaxNode.FindNode"/> may return an enclosing node with the same span
        /// (such as a base type), so the outermost name node with a matching span is chosen.
        /// </summary>
        private static SyntaxNode? FindTypeName(SyntaxNode root, Diagnostic diagnostic)
        {
            var span = diagnostic.Location.SourceSpan;
            return root.FindNode(span)
                .DescendantNodesAndSelf()
                .FirstOrDefault(n => n.Span == span
                    && (n is NameSyntax || n is MemberAccessExpressionSyntax)
                    && GetGenericName(n) is not null);
        }

        private static GenericNameSyntax? GetGenericName(SyntaxNode node) => node switch
        {
            GenericNameSyntax genericName => genericName,
            QualifiedNameSyntax qualified => qualified.Right as GenericNameSyntax,
            AliasQualifiedNameSyntax aliasQualified => aliasQualified.Name as GenericNameSyntax,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
            _ => null,
        };

        private enum AliasState
        {
            Available,
            Missing,
            Conflict,
        }

        private static AliasState GetAliasState(SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var symbols = semanticModel.LookupNamespacesAndTypes(position, name: JcgAlias);
            if (symbols.IsEmpty)
                return AliasState.Missing;

            var j2nNamespace = semanticModel.Compilation
                .GetTypeByMetadataName($"{J2NCollectionsGenericNamespace}.List`1")?.ContainingNamespace;

            foreach (var symbol in symbols)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (symbol is IAliasSymbol alias
                    && alias.Target is INamespaceSymbol target
                    && (SymbolEqualityComparer.Default.Equals(target, j2nNamespace)
                        || target.ToDisplayString() == J2NCollectionsGenericNamespace))
                {
                    return AliasState.Available;
                }
            }

            return AliasState.Conflict;
        }

        /// <summary>
        /// Appends <c>using JCG = J2N.Collections.Generic;</c> after the existing using directives,
        /// in the namespace that declares them if the file only has namespace-level usings.
        /// </summary>
        private static CompilationUnitSyntax AddJcgAlias(CompilationUnitSyntax root, int position)
        {
            var endOfLine = root.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (endOfLine == default)
                endOfLine = SyntaxFactory.CarriageReturnLineFeed;

            var usingDirective = SyntaxFactory.UsingDirective(
                    SyntaxFactory.NameEquals(JcgAlias),
                    SyntaxFactory.ParseName(J2NCollectionsGenericNamespace))
                .NormalizeWhitespace()
                .WithTrailingTrivia(endOfLine);

            if (root.Usings.Count == 0)
            {
                var namespaceWithUsings = root.FindToken(position).Parent?
                    .AncestorsAndSelf()
                    .OfType<BaseNamespaceDeclarationSyntax>()
                    .LastOrDefault(ns => ns.Usings.Count > 0);

                if (namespaceWithUsings is not null)
                {
                    var indentation = namespaceWithUsings.Usings.Last().GetLeadingTrivia()
                        .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
                    return root.ReplaceNode(
                        namespaceWithUsings,
                        namespaceWithUsings.AddUsings(usingDirective.WithLeadingTrivia(indentation)));
                }

                if (root.Externs.Count == 0)
                {
                    // Keep any file header comment above the new using directive.
                    var firstToken = root.GetFirstToken(includeZeroWidth: true);
                    var header = firstToken.LeadingTrivia;
                    root = root.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(SyntaxFactory.TriviaList()));
                    return root.AddUsings(usingDirective
                        .WithLeadingTrivia(header)
                        .WithTrailingTrivia(endOfLine, endOfLine));
                }
            }

            return root.AddUsings(usingDirective);
        }

        private static string GetSimpleName(string metadataName)
        {
            var tick = metadataName.IndexOf('`');
            return tick < 0 ? metadataName : metadataName.Substring(0, tick);
        }

        private static string GetDisplayName(string metadataName)
        {
            var name = GetSimpleName(metadataName);
            return metadataName.EndsWith("`2", System.StringComparison.Ordinal)
                ? $"{name}<TKey, TValue>"
                : $"{name}<T>";
        }

        private sealed class FixAll : DocumentBasedFixAllProvider
        {
            public static readonly FixAll Instance = new FixAll();

            protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
                => await FixDocumentAsync(document, diagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
        }
    }
}
