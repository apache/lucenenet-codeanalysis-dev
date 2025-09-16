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

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev1001_FloatingPointFormattingCSCodeFixProvider)), Shared]
    public class LuceneDev1001_FloatingPointFormattingCSCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            [Descriptors.LuceneDev1001_FloatingPointFormatting.Id];

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            // only handle the first diagnostic for this registration
            var diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic == null)
                return;

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            // the diagnostic in the analyzer is reported on the member access (e.g. "x.ToString")
            // but we need the whole invocation (e.g. "x.ToString(...)").  So find the invocation
            // by walking ancestors if needed.
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node is null)
                return;

            var invocation = node as InvocationExpressionSyntax
                         ?? node.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

            if (invocation is null)
                return;

            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return;

            var memberAccess = node as MemberAccessExpressionSyntax
                ?? node.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            if (memberAccess is null)
                return;

            // Determine the type name for J2N (Single or Double)
            var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken);
            var type = typeInfo.Type;

            string? j2nTypeName = type?.SpecialType switch
            {
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                _ => null
            };

            if (j2nTypeName == null)
                return;

            // Build the code element string
            string codeElement = $"J2N.Numerics.{j2nTypeName}.ToString(...)";

            // Use the helper to register the code fix
            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.UseX,
                    c => ReplaceWithJ2NToStringAsync(context.Document, invocation, c),
                    "UseJ2NToString",
                    codeElement),
                diagnostic);

        }

        private async Task<Document> ReplaceWithJ2NToStringAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return document;

            var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression, cancellationToken);
            var type = typeInfo.Type;

            string? j2nTypeName = type?.SpecialType switch
            {
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                _ => null
            };

            if (j2nTypeName == null)
                return document; // unsupported type

            // Build J2N.Numerics.Single.ToString
            var j2n = SyntaxFactory.IdentifierName("J2N");
            var numerics = SyntaxFactory.IdentifierName("Numerics");
            var single = SyntaxFactory.IdentifierName(j2nTypeName);
            var toString = SyntaxFactory.IdentifierName("ToString");

            // Build the chain: J2N.Numerics.Single.ToString
            var j2nNumerics = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, j2n, numerics);
            var j2nNumericsSingle = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, j2nNumerics, single);
            var j2nToStringAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, j2nNumericsSingle, toString);


            // Build invocation: J2N.Numerics.<Single|Double>.ToString(<expr>, <original args...>)
            var newArgs = new List<ArgumentSyntax> { SyntaxFactory.Argument(memberAccess.Expression) };
            if (invocation.ArgumentList != null)
                newArgs.AddRange(invocation.ArgumentList.Arguments);

            var newInvocation = SyntaxFactory.InvocationExpression(j2nToStringAccess, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArgs)))
                                     .WithTriviaFrom(invocation)
                                     .WithAdditionalAnnotations(Formatter.Annotation);

            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(invocation, newInvocation);

            return editor.GetChangedDocument();
        }
    }
}
