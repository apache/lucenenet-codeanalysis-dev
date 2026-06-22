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
    /// Ports TestApiConsistency.TestProtectedFieldNames: protected (or protected internal)
    /// instance fields of classes must use the 'm_' prefix followed by camelCase, or be an
    /// UPPER_CASE constant. Fields in the Lucene.Net.Support namespace and the AssemblyKeys
    /// type are excluded, mirroring the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7001_ProtectedFieldNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7001_ProtectedFieldName);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;
            var containingType = field.ContainingType;

            if (containingType?.TypeKind != TypeKind.Class)
                return;

            if (ApiConventionHelper.IsGeneratedType(containingType))
                return;

            if (ApiConventionHelper.IsInSupportNamespace(containingType))
                return;

            if (containingType.Name == "AssemblyKeys")
                return;

            // Original test binds Instance fields only (static protected fields are not scanned).
            if (field.IsStatic)
                return;

            if (field.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(field.Name))
                return;

            // Original test condition: field.IsFamily || field.IsFamilyOrAssembly.
            if (field.DeclaredAccessibility is not (Accessibility.Protected or Accessibility.ProtectedOrInternal))
                return;

            if (!NamingHelper.ProtectedFieldName.IsMatch(field.Name))
            {
                foreach (var location in field.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev7001_ProtectedFieldName, location, field.Name));
                    }
                }
            }
        }
    }
}
