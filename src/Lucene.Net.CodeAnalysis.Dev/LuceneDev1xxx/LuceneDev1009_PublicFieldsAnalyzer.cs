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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestPublicFields: public instance fields of classes are reported;
    /// consider using a public property instead. Mirrors the original test, which scans only
    /// instance fields (so const and static fields are not considered) and skips the
    /// Lucene.Net.Support namespace.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1009_PublicFieldsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev1009_PublicFields);

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

            if (ApiConventionHelper.IsCompilerGeneratedName(containingType.MetadataName))
                return;

            if (ApiConventionHelper.IsInSupportNamespace(containingType))
                return;

            // Original test binds Public | Instance only: static and const fields are not scanned.
            if (field.IsStatic || field.IsConst)
                return;

            if (field.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(field.Name))
                return;

            if (field.DeclaredAccessibility != Accessibility.Public)
                return;

            foreach (var location in field.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev1009_PublicFields, location, field.Name));
                }
            }
        }
    }
}
