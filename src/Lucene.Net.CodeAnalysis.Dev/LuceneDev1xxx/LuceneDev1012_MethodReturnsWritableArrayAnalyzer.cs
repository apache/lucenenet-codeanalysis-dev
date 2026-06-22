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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestForMethodsThatReturnWritableArray. The original test inspected
    /// the compiled IL to detect a method body that simply returns a field (so the consumer can
    /// mutate the array). Because a source analyzer has no IL, this analyzer inspects the method
    /// body syntax and reports when an array-returning method returns a field reference directly,
    /// without cloning it (e.g. <c>return m_values;</c>). Methods decorated with [WritableArray]
    /// are excluded, matching the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1012_MethodReturnsWritableArrayAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev1012_MethodReturnsWritableArray);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Must be declared on a class.
            if (methodDeclaration.Parent is not ClassDeclarationSyntax)
                return;

            // Return type must be an array.
            if (methodDeclaration.ReturnType is not ArrayTypeSyntax)
                return;

            var semanticModel = context.SemanticModel;
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration, context.CancellationToken);
            if (methodSymbol is null)
                return;

            if (methodSymbol.ReturnType.TypeKind != TypeKind.Array)
                return;

            if (ApiConventionHelper.HasAttribute(methodSymbol, ApiConventionHelper.WritableArrayAttribute))
                return;

            foreach (var returnedExpression in GetReturnedExpressions(methodDeclaration))
            {
                if (ReturnsFieldDirectly(returnedExpression, semanticModel, context.CancellationToken))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev1012_MethodReturnsWritableArray,
                        methodDeclaration.Identifier.GetLocation(),
                        methodSymbol.Name));
                    return; // one diagnostic per method
                }
            }
        }

        private static IEnumerable<ExpressionSyntax> GetReturnedExpressions(MethodDeclarationSyntax method)
        {
            // Expression-bodied member: e.g. int[] GetValues() => m_values;
            if (method.ExpressionBody?.Expression is { } arrow)
            {
                yield return arrow;
                yield break;
            }

            if (method.Body is null)
                yield break;

            foreach (var statement in method.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
            {
                if (statement.Expression is { } expression)
                {
                    yield return expression;
                }
            }
        }

        private static bool ReturnsFieldDirectly(ExpressionSyntax expression, SemanticModel semanticModel, System.Threading.CancellationToken cancellationToken)
        {
            // Unwrap parentheses.
            var unwrapped = expression;
            while (unwrapped is ParenthesizedExpressionSyntax parenthesized)
            {
                unwrapped = parenthesized.Expression;
            }

            // A direct field reference: 'field' or 'this.field'. Anything else (a method call such
            // as arr.ToArray(), a 'new[]' allocation, etc.) returns a fresh array and is safe.
            if (unwrapped is not (IdentifierNameSyntax or MemberAccessExpressionSyntax))
                return false;

            var symbol = semanticModel.GetSymbolInfo(unwrapped, cancellationToken).Symbol;
            return symbol is IFieldSymbol;
        }
    }
}
