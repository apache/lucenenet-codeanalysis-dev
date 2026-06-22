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
    /// Ports TestApiConsistency.TestForPropertiesThatReturnArray: instance properties of classes
    /// with a public getter that returns an array type are reported. Properties decorated with
    /// [WritableArray] are excluded, matching the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1011_PropertyReturnsArrayAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev1011_PropertyReturnsArray);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        private static void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            var containingType = property.ContainingType;

            if (containingType?.TypeKind != TypeKind.Class)
                return;

            if (ApiConventionHelper.IsGeneratedType(containingType))
                return;

            if (property.IsStatic)
                return;

            if (ApiConventionHelper.HasAttribute(property, ApiConventionHelper.WritableArrayAttribute))
                return;

            // Original test uses GetGetMethod() (public getter only).
            var getMethod = property.GetMethod;
            if (getMethod is null || getMethod.DeclaredAccessibility != Accessibility.Public)
                return;

            if (property.Type.TypeKind == TypeKind.Array)
            {
                foreach (var location in property.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev1011_PropertyReturnsArray, location, property.Name));
                    }
                }
            }
        }
    }
}
