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
using System.Linq;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev2xxx
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev2007_2008_NumericExplicitCultureAnalyzer : DiagnosticAnalyzer
    {
        // Methods this analyzer cares about, across BCL numerics, J2N numerics, System.Convert, and string.Format.
        private static readonly ImmutableHashSet<string> TargetMethodNames =
            ImmutableHashSet.Create(
                "Parse", "TryParse", "ToString", "TryFormat", "Format",
                "ToByte", "ToSByte", "ToInt16", "ToUInt16", "ToInt32", "ToUInt32",
                "ToInt64", "ToUInt64", "ToSingle", "ToDouble", "ToDecimal");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider,
                Descriptors.LuceneDev2008_NumericInvariantFormatProvider);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            string? methodName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => null
            };
            if (methodName is null || !TargetMethodNames.Contains(methodName))
                return;

            var semantic = ctx.SemanticModel;
            var method = semantic.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (method is null) return;

            // Only consider methods that actually accept an IFormatProvider; otherwise 2000-2004 covers it.
            if (!NumericTypeHelper.HasFormatProviderParameter(method, semantic.Compilation))
                return;

            // Restrict to numeric scenarios: containing type is BCL numeric, J2N numeric, System.Convert,
            // or System.String (string.Format with at least one numeric argument).
            var containing = method.ContainingType;
            bool isNumericScenario = false;

            if (NumericTypeHelper.IsBclNumericSpecialType(containing)
                || NumericTypeHelper.IsJ2NNumericType(containing, semantic.Compilation))
            {
                isNumericScenario = true;
            }
            else if (containing?.ToDisplayString() == "System.Convert")
            {
                isNumericScenario = IsConvertNumericMethod(method);
            }
            else if (containing?.SpecialType == SpecialType.System_String && methodName == "Format")
            {
                isNumericScenario = invocation.ArgumentList.Arguments.Skip(1).Any(arg =>
                {
                    var t = semantic.GetTypeInfo(arg.Expression).Type;
                    return NumericTypeHelper.IsBclNumericSpecialType(t)
                        || NumericTypeHelper.IsJ2NNumericType(t, semantic.Compilation);
                });
            }

            if (!isNumericScenario)
                return;

            var providerArg = NumericTypeHelper.GetFormatProviderArgument(invocation, semantic);
            if (providerArg is null)
                return;

            bool isInvariant = NumericTypeHelper.IsInvariantCultureAccess(providerArg, semantic);

            var location = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.GetLocation(),
                _ => invocation.Expression.GetLocation()
            };

            if (isInvariant)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev2008_NumericInvariantFormatProvider,
                    location,
                    methodName));
            }
            else
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider,
                    location,
                    methodName));
            }
        }

        private static bool IsConvertNumericMethod(IMethodSymbol method)
        {
            var name = method.Name;
            if (name == "ToString")
            {
                var first = method.Parameters.FirstOrDefault();
                return NumericTypeHelper.IsBclNumericSpecialType(first?.Type);
            }
            // ToByte/ToInt32/etc. — match by name.
            return name is "ToByte" or "ToSByte"
                or "ToInt16" or "ToUInt16"
                or "ToInt32" or "ToUInt32"
                or "ToInt64" or "ToUInt64"
                or "ToSingle" or "ToDouble" or "ToDecimal";
        }
    }
}
