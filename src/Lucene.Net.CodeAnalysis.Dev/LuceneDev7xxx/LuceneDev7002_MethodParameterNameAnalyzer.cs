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

using System.Collections.Immutable;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestMethodParameterNames: parameters of methods declared on
    /// classes must be camelCase and not begin or end with an underscore. All accessibilities
    /// are scanned, matching the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7002_MethodParameterNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7002_MethodParameterName);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        }

        private static void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            var containingType = method.ContainingType;

            if (containingType?.TypeKind != TypeKind.Class)
                return;

            if (ApiConventionHelper.IsGeneratedType(containingType))
                return;

            // Original test scans c.GetMethods(...), skipping '<'-prefixed (compiler-generated)
            // names. Reflection's GetMethods() also returns property/event accessors and operator
            // methods; we intentionally restrict to ordinary methods. Accessor parameters are
            // always compiler-named (e.g. 'value') and never violate, and operators/conversions
            // do not exist in the ported Java API surface this rule targets.
            if (method.MethodKind != MethodKind.Ordinary)
                return;

            if (method.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(method.Name))
                return;

            foreach (var parameter in method.Parameters)
            {
                if (string.IsNullOrEmpty(parameter.Name))
                    continue;

                if (!NamingHelper.MethodParameterName.IsMatch(parameter.Name))
                {
                    foreach (var location in parameter.Locations)
                    {
                        if (location.IsInSource)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                Descriptors.LuceneDev7002_MethodParameterName, location, parameter.Name));
                        }
                    }
                }
            }
        }
    }
}
