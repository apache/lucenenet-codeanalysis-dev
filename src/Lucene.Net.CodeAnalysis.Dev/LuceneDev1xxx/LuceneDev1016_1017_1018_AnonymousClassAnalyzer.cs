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
    /// Checks types that represent Java anonymous classes, which could not be used outside the
    /// code that declared them or be inherited from:
    /// <list type="bullet">
    ///     <item><description>LuceneDev1016: the type should be private.</description></item>
    ///     <item><description>LuceneDev1017: the type should be sealed.</description></item>
    ///     <item><description>LuceneDev1018: instance fields should be private.</description></item>
    /// </list>
    /// Static and const fields are not checked, because a singleton instance such as
    /// DefaultSimilarityAnonymousClass.Default must be readable by the enclosing type.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1016_1017_1018_AnonymousClassAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.LuceneDev1016_AnonymousClassShouldBePrivate,
                Descriptors.LuceneDev1017_AnonymousClassShouldBeSealed,
                Descriptors.LuceneDev1018_AnonymousClassFieldShouldBePrivate);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (!AnonymousClassHelper.IsAnonymousClass(type))
                return;

            if (type.DeclaredAccessibility != Accessibility.Private)
                Report(context, Descriptors.LuceneDev1016_AnonymousClassShouldBePrivate, type, type.Name);

            // A static class cannot also be declared sealed.
            if (!type.IsSealed && !type.IsStatic)
                Report(context, Descriptors.LuceneDev1017_AnonymousClassShouldBeSealed, type, type.Name);
        }

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;

            // IsStatic is also true for const fields.
            if (field.IsStatic || field.IsImplicitlyDeclared)
                return;

            if (field.DeclaredAccessibility == Accessibility.Private
                || !AnonymousClassHelper.IsAnonymousClass(field.ContainingType))
                return;

            Report(context, Descriptors.LuceneDev1018_AnonymousClassFieldShouldBePrivate, field, field.Name, field.ContainingType.Name);
        }

        private static void Report(
            SymbolAnalysisContext context,
            DiagnosticDescriptor descriptor,
            ISymbol symbol,
            params object[] messageArgs)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource)
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
            }
        }
    }
}
