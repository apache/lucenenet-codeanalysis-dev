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
    public sealed class LuceneDev2002_ConvertNumericAnalyzer : DiagnosticAnalyzer
    {
        // Conversions between numeric types and string. Other Convert.* methods aren't culture-sensitive.
        private static readonly ImmutableHashSet<string> StringToNumberMethods =
            ImmutableHashSet.Create(
                "ToByte", "ToSByte",
                "ToInt16", "ToUInt16",
                "ToInt32", "ToUInt32",
                "ToInt64", "ToUInt64",
                "ToSingle", "ToDouble", "ToDecimal");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;

            var methodName = memberAccess.Name.Identifier.ValueText;
            bool isToString = methodName == "ToString";
            if (!isToString && !StringToNumberMethods.Contains(methodName))
                return;

            var semantic = ctx.SemanticModel;
            var method = semantic.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
            if (method is null) return;

            if (method.ContainingType?.SpecialType != SpecialType.System_Object
                && method.ContainingType?.ToDisplayString() != "System.Convert")
                return;

            if (NumericTypeHelper.HasFormatProviderParameter(method, semantic.Compilation))
                return;

            if (isToString)
            {
                // Only flag Convert.ToString(<numeric>); skip non-numeric overloads (bool, char, DateTime, object).
                var firstParamType = method.Parameters.FirstOrDefault()?.Type;
                if (!NumericTypeHelper.IsBclNumericSpecialType(firstParamType))
                    return;
            }
            else
            {
                // Only flag Convert.ToXxx(string …); skip overloads that don't take a string source.
                var firstParam = method.Parameters.FirstOrDefault();
                if (firstParam is null || firstParam.Type.SpecialType != SpecialType.System_String)
                    return;
            }

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider,
                memberAccess.Name.GetLocation(),
                methodName));
        }
    }
}
