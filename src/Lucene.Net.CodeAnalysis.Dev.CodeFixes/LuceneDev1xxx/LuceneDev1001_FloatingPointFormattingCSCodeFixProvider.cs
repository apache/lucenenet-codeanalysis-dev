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
            Diagnostic? diagnostic = context.Diagnostics.FirstOrDefault();
            if (diagnostic is null)
                return;

            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            // the diagnostic in the analyzer is reported on the member access (e.g. "x.ToString")
            // but we need the whole invocation (e.g. "x.ToString(...)").  So find the invocation
            // by walking ancestors if needed.
            SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node is null)
                return;

            if (node is not ExpressionSyntax exprNode)
                return;

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return;

            if (!TryGetJ2NTypeAndMember(semanticModel, exprNode, out var j2nTypeName, out var memberAccess))
                return;

            string codeElement = $"J2N.Numerics.{j2nTypeName}.ToString(...)";

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.UseX,
                    c => ReplaceWithJ2NToStringAsync(context.Document, memberAccess, c),
                    "UseJ2NToString",
                    codeElement),
                diagnostic);
        }

        private async Task<Document> ReplaceWithJ2NToStringAsync(
            Document document,
            MemberAccessExpressionSyntax memberAccess,
            CancellationToken cancellationToken)
        {
            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return document;

            if (!TryGetJ2NTypeAndMember(semanticModel, memberAccess, out var j2nTypeName, out _))
                return document;

            // Build J2N.Numerics.Single/Double.ToString
            MemberAccessExpressionSyntax j2nToStringAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("J2N"),
                    SyntaxFactory.IdentifierName("Numerics")),
                SyntaxFactory.IdentifierName(j2nTypeName))
                .WithAdditionalAnnotations(Formatter.Annotation);

            MemberAccessExpressionSyntax fullAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                j2nToStringAccess,
                SyntaxFactory.IdentifierName("ToString"));

            // Build invocation: J2N.Numerics.<Single|Double>.ToString(<expr>, <original args...>)
            if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
                return document;

            var newArgs = new List<ArgumentSyntax> { SyntaxFactory.Argument(memberAccess.Expression) };
            if (invocation.ArgumentList != null)
                newArgs.AddRange(invocation.ArgumentList.Arguments);

            InvocationExpressionSyntax newInvocation = SyntaxFactory.InvocationExpression(
                fullAccess,
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(newArgs)))
                .WithTriviaFrom(invocation) // safe now
                .WithAdditionalAnnotations(Formatter.Annotation);

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(memberAccess.Parent, newInvocation);

            return editor.GetChangedDocument();
        }

        private static bool TryGetJ2NTypeAndMember(
            SemanticModel semanticModel,
            ExpressionSyntax expr,
            out string j2nTypeName,
            out MemberAccessExpressionSyntax memberAccess)
        {
            memberAccess = expr as MemberAccessExpressionSyntax
                ?? expr.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();

            if (memberAccess is null)
            {
                j2nTypeName = null!; // we always return false when the value is null, so we can ignore it here.
                return false;
            }

            var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
            var type = typeInfo.Type;

            j2nTypeName = type?.SpecialType switch
            {
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                _ => null! // we always return false when the value is null, so we can ignore it here.
            };

            if (j2nTypeName is null)
                return false;

            return true;
        }
    }
}
