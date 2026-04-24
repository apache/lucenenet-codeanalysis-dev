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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev6005_SingleCharStringCodeFixProvider))]
    [Shared]
    public sealed class LuceneDev6005_SingleCharStringCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(Descriptors.LuceneDev6005_SingleCharString.Id);

        public override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root == null)
                return;

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

            var literal = node as LiteralExpressionSyntax
                ?? node.DescendantNodesAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault(l => l.Span == diagnosticSpan);

            if (literal != null && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Use char literal",
                        c => ReplaceWithCharLiteralAsync(context.Document, literal, c),
                        nameof(LuceneDev6005_SingleCharStringCodeFixProvider)),
                    diagnostic);
            }
        }

        private static async Task<Document> ReplaceWithCharLiteralAsync(
            Document document,
            LiteralExpressionSyntax stringLiteral,
            CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            // Get the original escaped token text (e.g., "\"", "\n", "H")
            var token = stringLiteral.Token;

            // Get unescaped value
            var valueText = token.ValueText;
            if (string.IsNullOrEmpty(valueText) || valueText.Length != 1)
                return document;

            char ch = valueText[0];

            // Escape it properly as a char literal
            string escapedCharText = EscapeCharLiteral(ch);
            var charLiteral = SyntaxFactory.LiteralExpression(
                SyntaxKind.CharacterLiteralExpression,
                SyntaxFactory.Literal(escapedCharText, ch));

            var newRoot = root.ReplaceNode(stringLiteral, charLiteral);
            return document.WithSyntaxRoot(newRoot);
        }

        private static string EscapeCharLiteral(char ch)
        {
            switch (ch)
            {
                case '\'':
                    return @"'\''"; // escape single quote
                case '\\':
                    return @"'\\'"; // escape backslash
                case '\n':
                    return @"'\n'";
                case '\r':
                    return @"'\r'";
                case '\t':
                    return @"'\t'";
                case '\0':
                    return @"'\0'";
                case '\b':
                    return @"'\b'";
                case '\f':
                    return @"'\f'";
                case '\v':
                    return @"'\v'";
                default:
                    // Printable character or Unicode escape
                    if (char.IsControl(ch) || char.IsSurrogate(ch))
                    {
                        // Unicode escape sequence
                        return $"'\\u{((int)ch).ToString("X4", CultureInfo.InvariantCulture)}'";
                    }
                    return $"'{ch}'";
            }
        }
    }
}
