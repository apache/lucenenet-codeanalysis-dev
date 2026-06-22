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
    /// Ports TestApiConsistency.TestPrivateFieldNames: private or internal (assembly) fields of
    /// classes must be camelCase (optionally with a leading underscore) or an UPPER_CASE constant.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7000_PrivateFieldNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7000_PrivateFieldName);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;

            // Original test: only classes.
            if (field.ContainingType?.TypeKind != TypeKind.Class)
                return;

            if (ApiConventionHelper.IsGeneratedType(field.ContainingType))
                return;

            // Ignore compiler-generated backing fields (names starting with '<').
            if (field.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(field.Name))
                return;

            // Original test condition: field.IsPrivate || field.IsAssembly (internal).
            if (field.DeclaredAccessibility is not (Accessibility.Private or Accessibility.Internal))
                return;

            if (!NamingHelper.PrivateFieldName.IsMatch(field.Name))
            {
                foreach (var location in field.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev7000_PrivateFieldName, location, field.Name));
                    }
                }
            }
        }
    }
}
