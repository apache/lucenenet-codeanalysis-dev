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
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1006_FloatingPointFormatting];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAddExpression, SyntaxKind.AddExpression);
            context.RegisterSyntaxNodeAction(AnalyzeInterpolatedStringExpression, SyntaxKind.InterpolatedStringExpression);
        }

        private static void AnalyzeAddExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not BinaryExpressionSyntax addExpression)
                return;

            if (!IsStringConcatenation(addExpression, context.SemanticModel, context.CancellationToken))
                return;

            if (addExpression.Parent is BinaryExpressionSyntax parent &&
                parent.IsKind(SyntaxKind.AddExpression) &&
                IsStringConcatenation(parent, context.SemanticModel, context.CancellationToken))
            {
                // Only analyze the outermost concatenation expression to avoid duplicate diagnostics.
                return;
            }

            foreach (var operand in FlattenConcatenation(addExpression))
            {
                var expression = operand is ParenthesizedExpressionSyntax parenthesized
                    ? parenthesized.Expression
                    : operand;

                var typeInfo = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken);
                if (!FloatingPoint.IsFloatingPointType(typeInfo))
                    continue;

                ReportDiagnostic(context, expression);
            }
        }

        private static void AnalyzeInterpolatedStringExpression(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InterpolatedStringExpressionSyntax interpolatedString)
                return;

            foreach (var interpolation in interpolatedString.Contents.OfType<InterpolationSyntax>())
            {
                if (interpolation.Expression is null)
                    continue;

                var typeInfo = context.SemanticModel.GetTypeInfo(interpolation.Expression, context.CancellationToken);
                if (!FloatingPoint.IsFloatingPointType(typeInfo))
                    continue;

                ReportDiagnostic(context, interpolation.Expression);
            }
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
        {
            var diagnostic = Diagnostic.Create(
                Descriptors.LuceneDev1006_FloatingPointFormatting,
                expression.GetLocation(),
                expression.ToString());

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsStringConcatenation(ExpressionSyntax expression, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
            return typeInfo.Type?.SpecialType == SpecialType.System_String
                || typeInfo.ConvertedType?.SpecialType == SpecialType.System_String;
        }

        private static IEnumerable<ExpressionSyntax> FlattenConcatenation(ExpressionSyntax expression)
        {
            if (expression is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            {
                foreach (var left in FlattenConcatenation(binary.Left))
                    yield return left;

                foreach (var right in FlattenConcatenation(binary.Right))
                    yield return right;
            }
            else
            {
                yield return expression;
            }
        }
    }
}
