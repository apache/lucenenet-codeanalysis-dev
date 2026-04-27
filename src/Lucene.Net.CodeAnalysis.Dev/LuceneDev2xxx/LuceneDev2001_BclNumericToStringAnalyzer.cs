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
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev2xxx
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev2001_BclNumericToStringAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> TargetMethodNames =
            ImmutableHashSet.Create("ToString", "TryFormat");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider);

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
            if (!TargetMethodNames.Contains(methodName))
                return;

            var semantic = ctx.SemanticModel;
            var method = semantic.GetSymbolInfo(memberAccess).Symbol as IMethodSymbol;
            if (method is null)
                return;

            // Instance call on a BCL numeric value.
            if (method.IsStatic)
                return;

            var containing = method.ContainingType;
            if (!NumericTypeHelper.IsBclNumericSpecialType(containing))
                return;

            // Bail only when the call site actually supplies a provider argument; methods like
            // TryFormat declare an *optional* IFormatProvider parameter that callers often omit.
            if (NumericTypeHelper.GetFormatProviderArgument(invocation, semantic) is not null)
                return;

            // Exempt parameterless ToString() inside a class's ToString() override —
            // there's no IFormatProvider parameter to forward to in that context.
            if (methodName == "ToString"
                && method.Parameters.Length == 0
                && NumericTypeHelper.IsInsideToStringOverride(invocation))
            {
                return;
            }

            var typeName = NumericTypeHelper.GetBclNumericTypeName(containing) ?? containing.Name;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider,
                memberAccess.Name.GetLocation(),
                methodName,
                typeName));
        }
    }
}
