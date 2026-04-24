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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6000_NonGenericDictionaryIndexerAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev6000_NonGenericDictionaryIndexer);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
        }

        private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext ctx)
        {
            var elementAccess = (ElementAccessExpressionSyntax)ctx.Node;

            if (elementAccess.Parent is AssignmentExpressionSyntax assignment
                && assignment.Left == elementAccess
                && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                return;
            }

            var symbolInfo = ctx.SemanticModel.GetSymbolInfo(elementAccess, ctx.CancellationToken);
            var property = symbolInfo.Symbol as IPropertySymbol;
            if (property == null || !property.IsIndexer)
                return;

            var containing = property.ContainingType;
            if (containing == null)
                return;

            if (!DictionaryTypeHelper.IsNonGenericDictionaryIndexer(property, containing))
                return;

            var display = elementAccess.Expression.ToString() + elementAccess.ArgumentList.ToString();
            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev6000_NonGenericDictionaryIndexer,
                elementAccess.GetLocation(),
                display));
        }
    }
}
