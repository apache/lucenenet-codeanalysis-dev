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
    public sealed class LuceneDev2006_NumericInterpolationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev2006_NumericStringInterpolation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInterpolatedString, SyntaxKind.InterpolatedStringExpression);
        }

        private static void AnalyzeInterpolatedString(SyntaxNodeAnalysisContext ctx)
        {
            var interpolated = (InterpolatedStringExpressionSyntax)ctx.Node;
            var semantic = ctx.SemanticModel;

            if (NumericTypeHelper.IsInsideInvariantInterpolationContext(interpolated, semantic))
                return;

            foreach (var content in interpolated.Contents)
            {
                if (content is not InterpolationSyntax interp)
                    continue;

                var type = semantic.GetTypeInfo(interp.Expression).Type;
                if (!NumericTypeHelper.IsBclNumericSpecialType(type)
                    && !NumericTypeHelper.IsJ2NNumericType(type, semantic.Compilation))
                {
                    continue;
                }

                var typeName = NumericTypeHelper.GetBclNumericTypeName(type) ?? type!.Name;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev2006_NumericStringInterpolation,
                    interp.Expression.GetLocation(),
                    typeName));
            }
        }
    }
}
