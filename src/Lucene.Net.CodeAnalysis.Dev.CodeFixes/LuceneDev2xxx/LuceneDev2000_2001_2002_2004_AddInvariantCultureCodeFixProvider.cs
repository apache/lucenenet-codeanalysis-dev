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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider)), Shared]
    public sealed class LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider : CodeFixProvider
    {
        private const string TitleInvariant = "Add CultureInfo.InvariantCulture";
        private const string TitleCurrent = "Add CultureInfo.CurrentCulture";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.Id,
                Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider.Id,
                Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider.Id,
                Descriptors.LuceneDev2004_J2NNumericMissingFormatProvider.Id);

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

                RegisterFix(context, invocation, "InvariantCulture", TitleInvariant, diagnostic);
                RegisterFix(context, invocation, "CurrentCulture", TitleCurrent, diagnostic);
            }
        }

        private static void RegisterFix(
            CodeFixContext context,
            InvocationExpressionSyntax invocation,
            string cultureMember,
            string title,
            Diagnostic diagnostic)
        {
            context.RegisterCodeFix(CodeAction.Create(
                title: title,
                createChangedDocument: c => AddCultureArgumentAsync(context.Document, invocation, cultureMember, c),
                equivalenceKey: title),
                diagnostic);
        }

        private static async Task<Document> AddCultureArgumentAsync(
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
            var newArg = SyntaxFactory.Argument(cultureExpr);

            // For TryParse-style methods, the IFormatProvider must come BEFORE the trailing
            // `out` argument: bool TryParse(string, IFormatProvider, out T). Otherwise append.
            var args = invocation.ArgumentList.Arguments;
            int insertAt = args.Count;
            if (args.Count > 0 && args[args.Count - 1].RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                insertAt = args.Count - 1;

            var newArgs = args.Insert(insertAt, newArg);
            var newInvocation = invocation.WithArgumentList(invocation.ArgumentList.WithArguments(newArgs));
            var newRoot = EnsureGlobalizationUsing(root).ReplaceNode(invocation, newInvocation);
            return document.WithSyntaxRoot(newRoot);
        }

        internal static SyntaxNode EnsureGlobalizationUsing(SyntaxNode root)
        {
            if (root is CompilationUnitSyntax compilationUnit)
            {
                var hasUsing = compilationUnit.Usings.Any(u => u.Name?.ToString() == "System.Globalization");
                if (!hasUsing)
                {
                    // Match the document's existing line ending so the inserted using directive
                    // doesn't mix CRLF and LF (the .gitattributes for this repo enforces CRLF
                    // for *.cs, but local checkouts may differ).
                    var newline = DetectLineEnding(root);
                    var usingDirective = SyntaxFactory.UsingDirective(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("System"),
                            SyntaxFactory.IdentifierName("Globalization")))
                        .WithTrailingTrivia(SyntaxFactory.ElasticEndOfLine(newline));
                    return compilationUnit.AddUsings(usingDirective);
                }
            }
            return root;
        }

        private static string DetectLineEnding(SyntaxNode root)
        {
            var text = root.GetText();
            if (text.Lines.Count > 1)
            {
                var firstLine = text.Lines[0];
                var endIncludingBreak = text.Lines[1].Start;
                var breakLength = endIncludingBreak - firstLine.End;
                if (breakLength == 2) return "\r\n";
                if (breakLength == 1)
                {
                    var ch = text[firstLine.End];
                    if (ch == '\r') return "\r";
                    return "\n";
                }
            }
            return "\n";
        }
    }
}
