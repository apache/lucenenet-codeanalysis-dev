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
    /// Ports TestApiConsistency.TestClassNames: class names must be PascalCase and must not
    /// follow the interface naming convention (capital 'I' followed by another capital letter).
    /// Classes decorated with [ExceptionToClassNameConvention] are excluded.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7004_ClassNameAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7004_ClassName);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            // The original test scanned reflection's t.IsClass, which is true for both classes and
            // delegates (delegates are CLR classes). Delegate names follow the same convention.
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Delegate))
                return;

            if (ApiConventionHelper.IsGeneratedType(type))
                return;

            // Ignore compiler-produced classes (e.g. anonymous types) whose names start with '<'.
            if (ApiConventionHelper.IsCompilerGeneratedName(type.MetadataName))
                return;

            if (ApiConventionHelper.HasAttribute(type, ApiConventionHelper.ExceptionToClassNameConventionAttribute))
                return;

            // Original test: invalid when it is not a valid class name OR it looks like an interface name.
            if (!NamingHelper.ClassName.IsMatch(type.MetadataName) || NamingHelper.InterfaceName.IsMatch(type.MetadataName))
            {
                foreach (var location in type.Locations)
                {
                    if (location.IsInSource)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            Descriptors.LuceneDev7004_ClassName, location, type.Name));
                    }
                }
            }
        }
    }
}
