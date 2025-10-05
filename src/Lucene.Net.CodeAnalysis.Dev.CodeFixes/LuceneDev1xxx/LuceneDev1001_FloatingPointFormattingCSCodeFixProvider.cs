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
        [
            Descriptors.LuceneDev1001_FloatingPointFormatting.Id,
            Descriptors.LuceneDev1006_FloatingPointFormatting.Id
        ];

        public override FixAllProvider GetFixAllProvider() =>
            WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            SemanticModel? semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel is null)
                return;

            foreach (Diagnostic diagnostic in context.Diagnostics)
            {
                SyntaxNode? node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                if (node is null)
                    continue;

                if (diagnostic.Id == Descriptors.LuceneDev1001_FloatingPointFormatting.Id)
                {
                    RegisterExplicitToStringFix(context, semanticModel, diagnostic, node);
                }
                else if (diagnostic.Id == Descriptors.LuceneDev1006_FloatingPointFormatting.Id)
                {
                    RegisterStringEmbeddingFix(context, semanticModel, diagnostic, node);
                }
            }
        }

        private void RegisterExplicitToStringFix(
            CodeFixContext context,
            SemanticModel semanticModel,
            Diagnostic diagnostic,
            SyntaxNode node)
        {
            if (node is not ExpressionSyntax expression)
                return;

            if (!TryGetJ2NTypeAndMember(semanticModel, expression, out var j2nTypeName, out var memberAccess))
                return;

            string codeElement = $"J2N.Numerics.{j2nTypeName}.ToString(...)";

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.UseX,
                    c => ReplaceExplicitToStringAsync(context.Document, memberAccess, j2nTypeName, c),
                    "UseJ2NToString",
                    codeElement),
                diagnostic);
        }

        private void RegisterStringEmbeddingFix(
            CodeFixContext context,
            SemanticModel semanticModel,
            Diagnostic diagnostic,
            SyntaxNode node)
        {
            ExpressionSyntax? expression = node as ExpressionSyntax ?? node.AncestorsAndSelf().OfType<ExpressionSyntax>().FirstOrDefault();
            if (expression is null)
                return;

            if (!TryGetFloatingPointTypeName(semanticModel.GetTypeInfo(expression, context.CancellationToken), out var j2nTypeName))
                return;

            string codeElement = $"J2N.Numerics.{j2nTypeName}.ToString(...)";

            InterpolationSyntax? interpolation = expression.AncestorsAndSelf().OfType<InterpolationSyntax>().FirstOrDefault();
            if (interpolation is not null)
            {
                context.RegisterCodeFix(
                    CodeActionHelper.CreateFromResource(
                        CodeFixResources.UseX,
                        c => ReplaceInterpolationExpressionAsync(context.Document, interpolation, expression, j2nTypeName, c),
                        "UseJ2NToString",
                        codeElement),
                    diagnostic);

                return;
            }

            context.RegisterCodeFix(
                CodeActionHelper.CreateFromResource(
                    CodeFixResources.UseX,
                    c => ReplaceConcatenationExpressionAsync(context.Document, expression, j2nTypeName, c),
                    "UseJ2NToString",
                    codeElement),
                diagnostic);
        }

        private async Task<Document> ReplaceExplicitToStringAsync(
            Document document,
            MemberAccessExpressionSyntax memberAccess,
            string j2nTypeName,
            CancellationToken cancellationToken)
        {
            if (memberAccess.Parent is not InvocationExpressionSyntax invocation)
                return document;

            var newArguments = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(memberAccess.Expression.WithoutTrivia())
            };

            if (invocation.ArgumentList is not null)
                newArguments.AddRange(invocation.ArgumentList.Arguments);

            InvocationExpressionSyntax replacement = CreateJ2NToStringInvocation(j2nTypeName, newArguments)
                .WithTriviaFrom(invocation);

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(invocation, replacement);

            return editor.GetChangedDocument();
        }

        private async Task<Document> ReplaceConcatenationExpressionAsync(
            Document document,
            ExpressionSyntax expression,
            string j2nTypeName,
            CancellationToken cancellationToken)
        {
            var arguments = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(expression.WithoutTrivia())
            };

            InvocationExpressionSyntax replacement = CreateJ2NToStringInvocation(j2nTypeName, arguments)
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(expression, replacement);

            return editor.GetChangedDocument();
        }

        private async Task<Document> ReplaceInterpolationExpressionAsync(
            Document document,
            InterpolationSyntax interpolation,
            ExpressionSyntax expression,
            string j2nTypeName,
            CancellationToken cancellationToken)
        {
            var arguments = new List<ArgumentSyntax>
            {
                SyntaxFactory.Argument(expression.WithoutTrivia())
            };

            var updatedInterpolation = interpolation;
            var alignmentClause = interpolation.AlignmentClause;

            if (interpolation.FormatClause is not null)
            {
                var formatToken = interpolation.FormatClause.FormatStringToken;
                var formatLiteral = SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression,
                    SyntaxFactory.Literal(formatToken.ValueText));
                arguments.Add(SyntaxFactory.Argument(formatLiteral));
                updatedInterpolation = updatedInterpolation.WithFormatClause(null);
            }

            InvocationExpressionSyntax replacementExpression = CreateJ2NToStringInvocation(j2nTypeName, arguments)
                .WithLeadingTrivia(expression.GetLeadingTrivia())
                .WithTrailingTrivia(expression.GetTrailingTrivia());

            updatedInterpolation = updatedInterpolation.WithExpression(replacementExpression);

            if (alignmentClause is not null)
            {
                updatedInterpolation = updatedInterpolation.WithAlignmentClause(alignmentClause);
            }

            updatedInterpolation = updatedInterpolation.WithAdditionalAnnotations(Formatter.Annotation);

            DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            editor.ReplaceNode(interpolation, updatedInterpolation);

            return editor.GetChangedDocument();
        }

        private static InvocationExpressionSyntax CreateJ2NToStringInvocation(
            string j2nTypeName,
            IEnumerable<ArgumentSyntax> arguments)
        {
            MemberAccessExpressionSyntax j2nTypeAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("J2N"),
                    SyntaxFactory.IdentifierName("Numerics")),
                SyntaxFactory.IdentifierName(j2nTypeName));

            MemberAccessExpressionSyntax toStringAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                j2nTypeAccess,
                SyntaxFactory.IdentifierName("ToString"));

            return SyntaxFactory.InvocationExpression(
                toStringAccess,
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)))
                .WithAdditionalAnnotations(Formatter.Annotation);
        }


        private static bool TryGetFloatingPointTypeName(TypeInfo typeInfo, out string typeName)
        {
            if (TryGetFloatingPointTypeName(typeInfo.Type, out typeName))
                return true;

            if (TryGetFloatingPointTypeName(typeInfo.ConvertedType, out typeName))
                return true;

            typeName = null!;
            return false;
        }

        private static bool TryGetFloatingPointTypeName(ITypeSymbol? typeSymbol, out string typeName)
        {
            typeName = typeSymbol?.SpecialType switch
            {
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                _ => null!
            };

            return typeName is not null;
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
                j2nTypeName = null!;
                return false;
            }

            if (!TryGetFloatingPointTypeName(semanticModel.GetTypeInfo(memberAccess.Expression), out j2nTypeName))
                return false;

            return true;
        }
    }
}
