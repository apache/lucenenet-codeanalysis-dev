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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev2003_AddInvariantCultureToStringFormatCodeFixProvider)), Shared]
    public sealed class LuceneDev2003_AddInvariantCultureToStringFormatCodeFixProvider : CodeFixProvider
    {
        private const string TitleInvariant = "Add CultureInfo.InvariantCulture";
        private const string TitleCurrent = "Add CultureInfo.CurrentCulture";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.LuceneDev2003_StringFormatNumericMissingFormatProvider.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                var invocation = root.FindToken(diagnostic.Location.SourceSpan.Start)
                                     .Parent?
                                     .AncestorsAndSelf()
                                     .OfType<InvocationExpressionSyntax>()
                                     .FirstOrDefault();
                if (invocation is null) continue;

                Register(context, invocation, "InvariantCulture", TitleInvariant, diagnostic);
                Register(context, invocation, "CurrentCulture", TitleCurrent, diagnostic);
            }
        }

        private static void Register(
            CodeFixContext context,
            InvocationExpressionSyntax invocation,
            string cultureMember,
            string title,
            Diagnostic diagnostic)
        {
            context.RegisterCodeFix(CodeAction.Create(
                title: title,
                createChangedDocument: c => InsertProviderAsync(context.Document, invocation, cultureMember, c),
                equivalenceKey: title),
                diagnostic);
        }

        private static async Task<Document> InsertProviderAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            string cultureMember,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root is null) return document;

            var cultureExpr = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("CultureInfo"),
                SyntaxFactory.IdentifierName(cultureMember));
            var providerArg = SyntaxFactory.Argument(cultureExpr);

            // Insert as the new first argument; existing args shift right.
            var newArgs = invocation.ArgumentList.Arguments.Insert(0, providerArg);
            var newArgList = invocation.ArgumentList.WithArguments(newArgs);
            var newInvocation = invocation.WithArgumentList(newArgList);

            var newRoot = LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider
                .EnsureGlobalizationUsing(root)
                .ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
