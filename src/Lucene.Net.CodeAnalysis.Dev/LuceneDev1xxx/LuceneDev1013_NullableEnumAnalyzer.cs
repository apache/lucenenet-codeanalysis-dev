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
    /// Ports TestApiConsistency.TestForPublicMembersWithNullableEnum: reports non-private methods
    /// (return value and parameters), non-private constructors (parameters), non-private properties,
    /// and protected fields whose type is a nullable enum (<c>Nullable&lt;TEnum&gt;</c>). Members
    /// decorated with [ExceptionToNullableEnumConvention] are excluded, matching the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1013_NullableEnumAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev1013_NullableEnum);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
        }

        private static bool IsNullableEnum(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named
                && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                && named.TypeArguments.Length == 1)
            {
                return named.TypeArguments[0].TypeKind == TypeKind.Enum;
            }

            return false;
        }

        private static bool MemberPreconditions(ISymbol member)
        {
            var containingType = member.ContainingType;
            if (containingType is null || ApiConventionHelper.IsGeneratedType(containingType))
                return false;

            if (member.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(member.Name))
                return false;

            return !ApiConventionHelper.HasAttribute(member, ApiConventionHelper.ExceptionToNullableEnumConventionAttribute);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Original test handles ordinary methods (excluding accessors) and constructors.
            if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
                return;

            if (!MemberPreconditions(method))
                return;

            // Original test only considers non-private methods/constructors.
            if (method.DeclaredAccessibility == Accessibility.Private)
                return;

            // Return value (ordinary methods only; constructors have no return type to flag).
            if (method.MethodKind == MethodKind.Ordinary && IsNullableEnum(method.ReturnType))
            {
                ReportSymbol(context, method);
            }

            foreach (var parameter in method.Parameters)
            {
                if (IsNullableEnum(parameter.Type))
                {
                    ReportParameter(context, parameter);
                }
            }
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (!MemberPreconditions(property))
                return;

            // Original IsNonPrivateProperty: requires a public getter or setter.
            if (!ApiConventionHelper.HasPublicAccessor(property))
                return;

            if (IsNullableEnum(property.Type))
            {
                ReportSymbol(context, property);
            }
        }

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var field = (IFieldSymbol)context.Symbol;
            if (!MemberPreconditions(field))
                return;

            // Original test flags protected (or protected internal) fields.
            if (field.DeclaredAccessibility is not (Accessibility.Protected or Accessibility.ProtectedOrInternal))
                return;

            if (IsNullableEnum(field.Type))
            {
                ReportSymbol(context, field);
            }
        }

        private static void ReportSymbol(SymbolAnalysisContext context, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev1013_NullableEnum, location, symbol.Name));
                }
            }
        }

        private static void ReportParameter(SymbolAnalysisContext context, IParameterSymbol parameter)
        {
            foreach (var location in parameter.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev1013_NullableEnum, location, parameter.Name));
                }
            }
        }
    }
}
