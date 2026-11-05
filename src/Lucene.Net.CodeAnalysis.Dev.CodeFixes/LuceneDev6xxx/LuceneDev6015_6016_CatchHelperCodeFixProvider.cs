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

using System;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    /// <summary>
    /// Fixes LuceneDev6015 by catching <see cref="Exception"/>, and LuceneDev6016 by offering a
    /// when clause for each helper method, which also catches <see cref="Exception"/> so that the
    /// result does not violate LuceneDev6015.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6015_6016_CatchHelperCodeFixProvider))]
    [Shared]
    public sealed class LuceneDev6015_6016_CatchHelperCodeFixProvider : CodeFixProvider
    {
        private const string ExceptionTypeName = "System.Exception";
        private const string HelperNamespace = "Lucene";

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(
                Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException.Id,
                Descriptors.LuceneDev6016_CatchShouldUseHelperFilter.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var catchClause = root?.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf<CatchClauseSyntax>();
            if (catchClause is null)
                return;

            if (diagnostic.Id == Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException.Id)
            {
                if (catchClause.Declaration is not { } declaration)
                    return;

                context.RegisterCodeFix(
                    CodeActionHelper.CreateFromResource(
                        CodeFixResources.UseX,
                        createChangedDocument: ct => ReplaceCaughtTypeAsync(context.Document, declaration, ct),
                        diagnostic.Id,
                        ExceptionTypeName),
                    diagnostic);
                return;
            }

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var helperType = semanticModel?.Compilation.GetTypeByMetadataName(ExceptionHelperMethods.ProductionTypeName);
            if (helperType is null)
                return;

            var helperNames = helperType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(ExceptionHelperMethods.IsFilterMethod)
                .Select(m => m.Name)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal);

            var variableName = GetVariableName(catchClause);

            foreach (var helperName in helperNames)
            {
                context.RegisterCodeFix(
                    CodeActionHelper.CreateFromResource(
                        CodeFixResources.UseX,
                        createChangedDocument: ct => AddFilterAsync(context.Document, catchClause, variableName, helperName, ct),
                        diagnostic.Id + "_" + helperName,
                        $"when ({variableName}.{helperName}())"),
                    diagnostic);
            }
        }

        private static async Task<Document> ReplaceCaughtTypeAsync(
            Document document,
            CatchDeclarationSyntax declaration,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null)
                return document;

            var newRoot = root.ReplaceNode(declaration.Type, CreateExceptionType().WithTriviaFrom(declaration.Type));
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> AddFilterAsync(
            Document document,
            CatchClauseSyntax catchClause,
            string variableName,
            string helperName,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var exceptionType = semanticModel?.Compilation.GetTypeByMetadataName(ExceptionTypeName);
            if (root is null || semanticModel is null || exceptionType is null)
                return document;

            var declaration = catchClause.Declaration;

            // The filter goes between the declaration and the block, so it takes over the
            // trivia, such as a line ending or comment, that came before the block.
            var trailingTrivia = declaration?.CloseParenToken.TrailingTrivia ?? catchClause.CatchKeyword.TrailingTrivia;
            var space = SyntaxFactory.TriviaList(SyntaxFactory.Space);

            var caughtType = declaration is not null
                && SymbolEqualityComparer.Default.Equals(
                    semanticModel.GetTypeInfo(declaration.Type, cancellationToken).Type,
                    exceptionType)
                ? declaration.Type
                : CreateExceptionType();

            var newDeclaration = SyntaxFactory.CatchDeclaration(
                SyntaxFactory.Token(declaration?.OpenParenToken.LeadingTrivia ?? default, SyntaxKind.OpenParenToken, default),
                caughtType.WithLeadingTrivia(declaration?.Type.GetLeadingTrivia() ?? default).WithTrailingTrivia(space),
                SyntaxFactory.Identifier(variableName),
                SyntaxFactory.Token(default, SyntaxKind.CloseParenToken, space));

            var filter = SyntaxFactory.CatchFilterClause(
                SyntaxFactory.Token(default, SyntaxKind.WhenKeyword, space),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                SyntaxFactory.ParseExpression($"{variableName}.{helperName}()"),
                SyntaxFactory.Token(default, SyntaxKind.CloseParenToken, trailingTrivia));

            var newCatchClause = catchClause
                .WithCatchKeyword(catchClause.CatchKeyword.WithTrailingTrivia(space))
                .WithDeclaration(newDeclaration)
                .WithFilter(filter);

            var newRoot = root.ReplaceNode(catchClause, newCatchClause);

            var isHelperInScope = semanticModel.LookupSymbols(
                catchClause.Block.SpanStart,
                exceptionType,
                helperName,
                includeReducedExtensionMethods: true).Any();

            if (!isHelperInScope && newRoot is CompilationUnitSyntax compilationUnit)
                newRoot = AddHelperUsing(compilationUnit);

            return document.WithSyntaxRoot(newRoot);
        }

        private static CompilationUnitSyntax AddHelperUsing(CompilationUnitSyntax compilationUnit)
        {
            var usingDirective = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(HelperNamespace))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var lastUsing = compilationUnit.Usings.LastOrDefault();
            usingDirective = lastUsing is not null
                ? usingDirective.WithTrailingTrivia(lastUsing.GetTrailingTrivia())
                : usingDirective.WithTrailingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed);

            return compilationUnit.AddUsings(usingDirective);
        }

        private static TypeSyntax CreateExceptionType()
            => SyntaxFactory.ParseTypeName(ExceptionTypeName).WithAdditionalAnnotations(Simplifier.Annotation);

        /// <summary>
        /// Returns the name of the caught variable, or if there is none, a name that isn't
        /// used as an identifier anywhere in the enclosing member.
        /// </summary>
        private static string GetVariableName(CatchClauseSyntax catchClause)
        {
            var identifier = catchClause.Declaration?.Identifier ?? default;
            if (!identifier.IsKind(SyntaxKind.None))
                return identifier.ValueText;

            var member = catchClause.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            SyntaxNode scope = member is null or GlobalStatementSyntax
                ? catchClause.SyntaxTree.GetRoot()
                : member;

            var usedNames = new HashSet<string>(
                scope.DescendantTokens()
                    .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                    .Select(token => token.ValueText),
                StringComparer.Ordinal);

            var name = "e";
            for (var suffix = 1; usedNames.Contains(name); suffix++)
            {
                name = "e" + suffix;
            }

            return name;
        }
    }
}
