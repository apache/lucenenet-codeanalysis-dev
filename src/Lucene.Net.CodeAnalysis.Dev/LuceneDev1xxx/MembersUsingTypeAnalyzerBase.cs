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
    /// Base class for the analyzers that mirror TestApiConsistency.GetMembersAcceptingOrReturningType:
    /// they report ordinary methods (return value and parameters), constructors (parameters), and
    /// properties whose type is one of a set of concrete generic types. Derived classes specify the
    /// metadata names of the target open generic types and whether only non-private members are
    /// considered.
    /// </summary>
    public abstract class MembersUsingTypeAnalyzerBase : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor descriptor;
        private readonly bool publiclyVisibleOnly;

        protected MembersUsingTypeAnalyzerBase(DiagnosticDescriptor descriptor, bool publiclyVisibleOnly)
        {
            this.descriptor = descriptor;
            this.publiclyVisibleOnly = publiclyVisibleOnly;
        }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(descriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
        }

        /// <summary>
        /// Returns <c>true</c> when the type matches one of the concrete generic types this rule
        /// targets (compared by its unbound generic definition).
        /// </summary>
        protected abstract bool IsTargetType(ITypeSymbol type);

        /// <summary>The display name of the matched type, used in the diagnostic message.</summary>
        protected virtual string DescribeType(ITypeSymbol type) => type.OriginalDefinition.Name;

        private bool MemberPreconditions(ISymbol member)
        {
            var containingType = member.ContainingType;
            if (containingType is null || ApiConventionHelper.IsGeneratedType(containingType))
                return false;

            return !member.IsImplicitlyDeclared && !ApiConventionHelper.IsCompilerGeneratedName(member.Name);
        }

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor))
                return;

            if (!MemberPreconditions(method))
                return;

            if (publiclyVisibleOnly && method.DeclaredAccessibility == Accessibility.Private)
                return;

            // Constructors have no meaningful return type to flag; only ordinary methods do.
            if (method.MethodKind == MethodKind.Ordinary && IsTargetType(method.ReturnType))
            {
                Report(context, method, method.ReturnType);
            }

            foreach (var parameter in method.Parameters)
            {
                if (IsTargetType(parameter.Type))
                {
                    ReportParameter(context, parameter, parameter.Type);
                }
            }
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (!MemberPreconditions(property))
                return;

            if (publiclyVisibleOnly && !ApiConventionHelper.HasPublicAccessor(property))
                return;

            if (IsTargetType(property.Type))
            {
                Report(context, property, property.Type);
            }
        }

        private void Report(SymbolAnalysisContext context, ISymbol member, ITypeSymbol matchedType)
        {
            foreach (var location in member.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor, location, member.Name, DescribeType(matchedType)));
                }
            }
        }

        private void ReportParameter(SymbolAnalysisContext context, IParameterSymbol parameter, ITypeSymbol matchedType)
        {
            foreach (var location in parameter.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor, location, parameter.Name, DescribeType(matchedType)));
                }
            }
        }
    }
}
