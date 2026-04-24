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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev6xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6001_6002_StringComparisonCodeFixProvider)), Shared]
    public sealed class LuceneDev6001_6002_StringComparisonCodeFixProvider : CodeFixProvider
    {
        private const string Ordinal = "Ordinal";
        private const string OrdinalIgnoreCase = "OrdinalIgnoreCase";
        private const string TitleOrdinal = "Use StringComparison.Ordinal";
        private const string TitleOrdinalIgnoreCase = "Use StringComparison.OrdinalIgnoreCase";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev6001_MissingStringComparison.Id,
                Descriptors.LuceneDev6002_InvalidStringComparison.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <summary>
        /// Registers available code fixes for all diagnostics in the context.
        /// </summary>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null) return;

            // Iterate over ALL diagnostics in the context to ensure all issues are offered a fix.
            foreach (var diagnostic in context.Diagnostics)
            {
                var invocation = root.FindToken(diagnostic.Location.SourceSpan.Start)
                                     .Parent?
                                     .AncestorsAndSelf()
                                     .OfType<InvocationExpressionSyntax>()
                                     .FirstOrDefault();
                if (invocation == null) continue;

                var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                if (semanticModel == null) continue;

                // Skip char literals and single-character string literals when safe (LuceneDev6005 handles conversion).
                var firstArgExpr = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (firstArgExpr is LiteralExpressionSyntax lit)
                {
                    if (lit.IsKind(SyntaxKind.CharacterLiteralExpression))
                        continue;

                    if (lit.IsKind(SyntaxKind.StringLiteralExpression) && lit.Token.ValueText.Length == 1)
                    {
                        bool hasStringComparisonArg = invocation.ArgumentList.Arguments.Any(arg =>
                            (semanticModel.GetTypeInfo(arg.Expression).Type is INamedTypeSymbol t &&
                                t.ToDisplayString() == "System.StringComparison")
                            || (semanticModel.GetSymbolInfo(arg.Expression).Symbol is IFieldSymbol f &&
                                f.ContainingType?.ToDisplayString() == "System.StringComparison"));

                        if (!hasStringComparisonArg)
                            continue;
                    }
                }

                // --- Fix Registration Logic ---

                if (diagnostic.Id == Descriptors.LuceneDev6001_MissingStringComparison.Id)
                {
                    // Case 1: Argument is missing. Only offer Ordinal as the safe, conservative default.
                    RegisterFix(context, invocation, Ordinal, TitleOrdinal, diagnostic);
                }
                else if (diagnostic.Id == Descriptors.LuceneDev6002_InvalidStringComparison.Id)
                {
                    // Case 2: Invalid argument is present. Determine the best replacement.
                    if (TryDetermineReplacement(invocation, semanticModel, out string? targetComparison))
                    {
                        var title = (targetComparison!) == Ordinal ? TitleOrdinal : TitleOrdinalIgnoreCase;
                        RegisterFix(context, invocation, targetComparison!, title, diagnostic);
                    }
                    // If TryDetermineReplacement returns false, the argument is an invalid non-constant
                    // expression (e.g., a variable). We skip the fix to avoid arbitrary changes.
                }
            }
        }

        private static void RegisterFix(
            CodeFixContext context,
            InvocationExpressionSyntax invocation,
            string comparisonMember,
            string title,
            Diagnostic diagnostic)
        {
            context.RegisterCodeFix(CodeAction.Create(
                title: title,
                createChangedDocument: c => FixInvocationAsync(context.Document, invocation, comparisonMember, c),
                equivalenceKey: title),
                diagnostic);
        }

        /// <summary>
        /// Determines the appropriate ordinal replacement (Ordinal or OrdinalIgnoreCase)
        /// for an existing culture-sensitive StringComparison argument.
        /// Only operates on constant argument values.
        /// </summary>
        /// <returns>True if a valid replacement was determined, false otherwise (e.g., if argument is non-constant).</returns>
        private static bool TryDetermineReplacement(InvocationExpressionSyntax invocation, SemanticModel semanticModel, out string? targetComparison)
        {
            targetComparison = null;
            var stringComparisonType = semanticModel.Compilation.GetTypeByMetadataName("System.StringComparison");
            var existingArg = invocation.ArgumentList.Arguments.FirstOrDefault(arg =>
                SymbolEqualityComparer.Default.Equals(
                    semanticModel.GetTypeInfo(arg.Expression).Type, stringComparisonType));

            if (existingArg != null)
            {
                var constVal = semanticModel.GetConstantValue(existingArg.Expression);
                if (constVal.HasValue && constVal.Value is int intVal)
                {
                    // Map original comparison to corresponding ordinal variant for constant values
                    switch ((System.StringComparison)intVal)
                    {
                        case System.StringComparison.CurrentCulture:
                        case System.StringComparison.InvariantCulture:
                            targetComparison = Ordinal;
                            return true;
                        case System.StringComparison.CurrentCultureIgnoreCase:
                        case System.StringComparison.InvariantCultureIgnoreCase:
                            targetComparison = OrdinalIgnoreCase;
                            return true;
                        case System.StringComparison.Ordinal:
                        case System.StringComparison.OrdinalIgnoreCase:
                            return false; // Already correct
                    }
                }
                // Argument exists, but is not a constant value (e.g., a variable). We skip the fix.
                return false;
            }

            // Should not be called for missing arguments by the caller.
            return false;
        }

        /// <summary>
        /// Creates the new document by either replacing an existing StringComparison argument
        /// or adding a new one, based on the fix action.
        /// </summary>
        private static async Task<Document> FixInvocationAsync(Document document, InvocationExpressionSyntax invocation, string comparisonMember, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null) return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var stringComparisonType = semanticModel?.Compilation.GetTypeByMetadataName("System.StringComparison");

            // 1. Create the new StringComparison argument expression
            var stringComparisonExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("StringComparison"),
                SyntaxFactory.IdentifierName(comparisonMember));

            var newArg = SyntaxFactory.Argument(stringComparisonExpr);

            // 2. Find existing argument for replacement/addition check
            var existingArg = invocation.ArgumentList.Arguments.FirstOrDefault(arg =>
                semanticModel != null &&
                SymbolEqualityComparer.Default.Equals(semanticModel.GetTypeInfo(arg.Expression).Type, stringComparisonType));

            // 3. Perform the syntax replacement/addition
            InvocationExpressionSyntax newInvocation;
            if (existingArg != null)
            {
                // Argument exists (Replacement case: InvalidComparison)
                // Preserve leading/trailing trivia (spaces/comma) from the expression being replaced
                var newExprWithTrivia = stringComparisonExpr
                    .WithLeadingTrivia(existingArg.Expression.GetLeadingTrivia())
                    .WithTrailingTrivia(existingArg.Expression.GetTrailingTrivia());

                var newArgWithTrivia = existingArg.WithExpression(newExprWithTrivia);

                newInvocation = invocation.ReplaceNode(existingArg, newArgWithTrivia);
            }
            else
            {
                // Argument is missing (Addition case: MissingComparison)
                // Use AddArguments, relying on Roslyn to correctly handle comma/spacing trivia.
                newInvocation = invocation.WithArgumentList(
                    invocation.ArgumentList.AddArguments(newArg)
                );
            }

            // 4. Update the document root (Ensure using statement is present and replace invocation)
            var newRoot = EnsureSystemUsing(root).ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Ensures a 'using System;' directive is present in the document.
        /// </summary>
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
