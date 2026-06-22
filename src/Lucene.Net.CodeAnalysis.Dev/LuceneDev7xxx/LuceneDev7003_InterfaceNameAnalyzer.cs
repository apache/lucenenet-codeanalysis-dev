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
    /// Ports TestApiConsistency.TestInterfaceNames: interface names must begin with a capital
    /// 'I' followed by another capital letter (with an optional generic-arity suffix).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7003_InterfaceNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7003_InterfaceName);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind != TypeKind.Interface)
                return;

            if (ApiConventionHelper.IsGeneratedType(type))
                return;

            // The original tests match on the metadata name, which carries the generic arity
            // suffix (e.g. "IComparer`1"); MetadataName preserves that.
            if (!NamingHelper.InterfaceName.IsMatch(type.MetadataName))
            {
                foreach (var location in type.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev7003_InterfaceName, location, type.Name));
                    }
                }
            }
        }
    }
}
