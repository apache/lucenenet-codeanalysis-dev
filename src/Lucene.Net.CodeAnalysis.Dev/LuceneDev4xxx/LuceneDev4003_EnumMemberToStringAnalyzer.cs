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
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Immutable;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx
{
    /// <summary>
    /// LuceneDev4003: Reports parameterless ToString() calls on an enum member that
    /// is referenced directly (e.g. <c>MyEnum.Value.ToString()</c>). The name is known
    /// at compile time, so <c>nameof(MyEnum.Value)</c> avoids boxing the value and
    /// looking up its name at runtime.
    /// <para/>
    /// Enum-typed variables, fields, parameters and properties are not reported because
    /// their value is not known at compile time. Members that share their underlying
    /// value with another member are also skipped, since ToString() may return the
    /// other member's name.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev4003_EnumMemberToStringAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptors.LuceneDev4003_EnumMemberToString);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (!TryGetEnumMemberReceiver(invocation, ctx.SemanticModel, out var receiver))
                return;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev4003_EnumMemberToString,
                invocation.GetLocation(),
                receiver.ToString()));
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="invocation"/> is a parameterless
        /// ToString() call on a directly referenced enum member whose name ToString()
        /// is guaranteed to return. <paramref name="receiver"/> is the enum member
        /// expression (e.g. <c>MyEnum.Value</c>).
        /// </summary>
        private static bool TryGetEnumMemberReceiver(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            out ExpressionSyntax receiver)
        {
            receiver = null!;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess
                || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return false;
            if (memberAccess.Name.Identifier.ValueText != nameof(ToString))
                return false;
            if (invocation.ArgumentList.Arguments.Count != 0)
                return false;

            if (semanticModel.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method
                || method.Parameters.Length != 0
                || method.IsExtensionMethod)
                return false;

            if (semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol is not IFieldSymbol field
                || field.ContainingType?.TypeKind != TypeKind.Enum
                || !field.HasConstantValue)
                return false;

            if (HasDuplicateValue(field))
                return false;

            receiver = memberAccess.Expression;
            return true;
        }

        private static bool HasDuplicateValue(IFieldSymbol field)
        {
            foreach (var member in field.ContainingType.GetMembers())
            {
                if (member is IFieldSymbol other
                    && other.HasConstantValue
                    && !SymbolEqualityComparer.Default.Equals(other, field)
                    && Equals(other.ConstantValue, field.ConstantValue))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
