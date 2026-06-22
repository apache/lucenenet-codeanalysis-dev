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
    /// Base class for the member-name analyzers that mirror the LUCENENET TestApiConsistency
    /// rules which scan all members of all types (regardless of accessibility) and report when a
    /// member's name matches a predicate. Like the original tests, these examine ordinary methods
    /// (excluding property/event accessors), properties and events. Derived classes provide the
    /// descriptor and the name predicate, and may override member-kind handling.
    /// </summary>
    public abstract class MemberNamePredicateAnalyzerBase : DiagnosticAnalyzer
    {
        private readonly DiagnosticDescriptor descriptor;

        protected MemberNamePredicateAnalyzerBase(DiagnosticDescriptor descriptor)
        {
            this.descriptor = descriptor;
        }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(descriptor);

        public sealed override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
            context.RegisterSymbolAction(AnalyzeProperty, SymbolKind.Property);
            if (IncludeEvents)
                context.RegisterSymbolAction(AnalyzeEvent, SymbolKind.Event);
        }

        /// <summary>Returns <c>true</c> when the given name violates this rule.</summary>
        protected abstract bool IsViolation(string name);

        /// <summary>
        /// Allows derived classes to further restrict which ordinary methods are considered
        /// (e.g. only parameterless methods). Defaults to all ordinary methods.
        /// </summary>
        protected virtual bool IncludeMethod(IMethodSymbol method) => true;

        /// <summary>
        /// Whether event members participate in this rule. Defaults to <c>true</c>; the
        /// 'Size' rule excludes events to match the original test.
        /// </summary>
        protected virtual bool IncludeEvents => true;

        /// <summary>
        /// Allows derived classes to skip a member entirely (e.g. when it carries a
        /// LUCENENET suppression attribute). Defaults to never skipping.
        /// </summary>
        protected virtual bool ShouldSkipMember(ISymbol member) => false;

        private void AnalyzeMethod(SymbolAnalysisContext context)
        {
            var method = (IMethodSymbol)context.Symbol;

            // Original tests examine ordinary methods, excluding get_/set_ accessors.
            if (method.MethodKind != MethodKind.Ordinary)
                return;

            if (!ShouldExamine(method, method.ContainingType))
                return;

            if (!IncludeMethod(method))
                return;

            ReportIfViolation(context, method);
        }

        private void AnalyzeProperty(SymbolAnalysisContext context)
        {
            var property = (IPropertySymbol)context.Symbol;
            if (!ShouldExamine(property, property.ContainingType))
                return;

            ReportIfViolation(context, property);
        }

        private void AnalyzeEvent(SymbolAnalysisContext context)
        {
            var @event = (IEventSymbol)context.Symbol;
            if (!ShouldExamine(@event, @event.ContainingType))
                return;

            ReportIfViolation(context, @event);
        }

        private static bool ShouldExamine(ISymbol member, INamedTypeSymbol? containingType)
        {
            if (containingType is null)
                return false;

            if (ApiConventionHelper.IsGeneratedType(containingType))
                return false;

            if (member.IsImplicitlyDeclared || ApiConventionHelper.IsCompilerGeneratedName(member.Name))
                return false;

            return true;
        }

        private void ReportIfViolation(SymbolAnalysisContext context, ISymbol member)
        {
            if (ShouldSkipMember(member))
                return;

            if (!IsViolation(member.Name))
                return;

            foreach (var location in member.Locations)
            {
                if (location.IsInSource)
                {
                    context.ReportDiagnostic(Diagnostic.Create(descriptor, location, member.Name));
                }
            }
        }
    }
}
