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
    /// Ports TestApiConsistency.TestForPublicMembersContainingComparer: reports protected fields,
    /// externally visible types, and members (ordinary methods, properties and events) whose name
    /// contains the Java term "Comparator". In .NET these should use "Comparer".
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7005_MemberContainsComparerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev7005_MemberContainsComparer);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
        }

        private static bool Matches(string name) => NamingHelper.ContainsComparer.IsMatch(name);

        private void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (ApiConventionHelper.IsGeneratedType(type))
                return;

            // Original test: only externally visible types are flagged (t.IsVisible).
            if (Matches(type.MetadataName) && IsVisible(type))
                Report(context, type);
        }

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;
            var containingType = field.ContainingType;

            if (containingType?.TypeKind != TypeKind.Class || ApiConventionHelper.IsGeneratedType(containingType))
                return;

            if (field.IsStatic || field.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(field.Name))
                return;

            // Original test scans protected (or protected internal) instance fields.
            if (field.DeclaredAccessibility is not (Accessibility.Protected or Accessibility.ProtectedOrInternal))
                return;

            if (Matches(field.Name))
                Report(context, field);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;
            if (method.MethodKind != MethodKind.Ordinary)
                return;

            if (ShouldExamineMember(method) && Matches(method.Name))
                Report(context, method);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (ShouldExamineMember(property) && Matches(property.Name))
                Report(context, property);
        }

        private void AnalyzeEvent(SymbolAnalysisContext context)
        {
            var @event = (IEventSymbol)context.Symbol;
            if (ShouldExamineMember(@event) && Matches(@event.Name))
                Report(context, @event);
        }

        private static bool ShouldExamineMember(ISymbol member)
        {
            var containingType = member.ContainingType;
            if (containingType is null || ApiConventionHelper.IsGeneratedType(containingType))
                return false;

            return !member.IsImplicitlyDeclared && !ApiConventionHelper.IsCompilerGeneratedName(member.Name);
        }

        private static bool IsVisible(INamedTypeSymbol type)
        {
            for (var current = type; current is not null; current = current.ContainingType)
            {
                if (current.DeclaredAccessibility != Accessibility.Public)
                    return false;
            }

            return true;
        }

        private void Report(SymbolAnalysisContext context, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev7005_MemberContainsComparer, location, symbol.Name));
                }
            }
        }
    }
}
