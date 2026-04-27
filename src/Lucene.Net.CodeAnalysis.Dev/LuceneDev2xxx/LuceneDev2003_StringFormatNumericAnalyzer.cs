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
    public sealed class LuceneDev2003_StringFormatNumericAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev2003_StringFormatNumericMissingFormatProvider);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            // Match Format(...) called as either `string.Format(...)` (member access) or `Format(...)`
            // (identifier — e.g. via `using static System.String;`). Containing-type check below
            // confirms the resolved method really is on System.String.
            string? methodName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.Identifier.ValueText,
                IdentifierNameSyntax id => id.Identifier.ValueText,
                _ => null
            };
            if (methodName != "Format")
                return;

            var semantic = ctx.SemanticModel;
            var method = semantic.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (method is null) return;

            if (method.ContainingType?.SpecialType != SpecialType.System_String)
                return;

            if (NumericTypeHelper.HasFormatProviderParameter(method, semantic.Compilation))
                return;

            // Only flag if at least one argument is a numeric type. Skip the format string itself (first arg).
            bool anyNumeric = invocation.ArgumentList.Arguments.Skip(1).Any(arg =>
            {
                var t = semantic.GetTypeInfo(arg.Expression).Type;
                return NumericTypeHelper.IsBclNumericSpecialType(t)
                    || NumericTypeHelper.IsJ2NNumericType(t, semantic.Compilation);
            });
            if (!anyNumeric)
                return;

            var location = invocation.Expression switch
            {
                MemberAccessExpressionSyntax m => m.Name.GetLocation(),
                _ => invocation.Expression.GetLocation()
            };

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev2003_StringFormatNumericMissingFormatProvider,
                location));
        }
    }
}
