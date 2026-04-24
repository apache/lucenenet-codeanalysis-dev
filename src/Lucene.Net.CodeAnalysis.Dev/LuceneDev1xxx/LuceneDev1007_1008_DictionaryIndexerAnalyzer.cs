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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1007_1008_DictionaryIndexerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType,
                Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        }

        private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext ctx)
        {
            var elementAccess = (ElementAccessExpressionSyntax)ctx.Node;

            // Skip assignment targets (setter usage does not throw).
            if (IsAssignmentTarget(elementAccess))
                return;

            var symbolInfo = ctx.SemanticModel.GetSymbolInfo(elementAccess, ctx.CancellationToken);
            var property = symbolInfo.Symbol as IPropertySymbol;
            if (property == null || !property.IsIndexer)
                return;

            var containing = property.ContainingType;
            if (containing == null)
                return;

            if (!DictionaryTypeHelper.IsGenericDictionaryIndexer(property, containing, out var valueType))
                return;

            var receiverText = elementAccess.Expression.ToString();
            var keyText = elementAccess.ArgumentList.ToString();
            var display = receiverText + keyText;

            var descriptor = IsValueTypeForDiagnostic(valueType!)
                ? Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType
                : Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType;

            ctx.ReportDiagnostic(Diagnostic.Create(descriptor, elementAccess.GetLocation(), display));
        }

        private static bool IsAssignmentTarget(ElementAccessExpressionSyntax elementAccess)
        {
            // dict[key] = value  -> skip
            if (elementAccess.Parent is AssignmentExpressionSyntax assignment
                && assignment.Left == elementAccess
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return true;
            }
            return false;
        }

        private static bool IsValueTypeForDiagnostic(ITypeSymbol valueType)
        {
            // Unconstrained type parameters: treat as reference-like (safer — null check may apply).
            if (valueType is ITypeParameterSymbol tp)
            {
                if (tp.HasValueTypeConstraint)
                    return true;
                return false;
            }
            return valueType.IsValueType;
        }
    }
}
