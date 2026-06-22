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
    /// Ports TestApiConsistency.TestForTypesContainingNonNetNumeric: reports types whose name
    /// contains a Java-style numeric term ('Int' not followed by 16/32/64, 'Long', 'Short' or
    /// 'Float'). Types decorated with [ExceptionToNetNumericConvention] are excluded.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7008_TypeContainsNonNetNumericAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7008_TypeContainsNonNetNumeric);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (ApiConventionHelper.IsGeneratedType(type))
                return;

            if (ApiConventionHelper.HasAttribute(type, ApiConventionHelper.ExceptionToNetNumericConventionAttribute))
                return;

            if (NamingHelper.ContainsNonNetNumeric.IsMatch(type.MetadataName))
            {
                foreach (var location in type.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev7008_TypeContainsNonNetNumeric, location, type.Name));
                    }
                }
            }
        }
    }
}
