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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev2xxx
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev2005_NumericConcatenationAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(Descriptors.LuceneDev2005_NumericStringConcatenation);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeAdd, SyntaxKind.AddExpression);
        }

        private static void AnalyzeAdd(SyntaxNodeAnalysisContext ctx)
        {
            var add = (BinaryExpressionSyntax)ctx.Node;

            // Only consider the topmost AddExpression of a string-concat chain. Children will be
            // visited by the framework; we reach into them ourselves to flag every numeric subexpression.
            if (add.Parent is BinaryExpressionSyntax parentAdd && parentAdd.IsKind(SyntaxKind.AddExpression))
                return;

            var semantic = ctx.SemanticModel;

            // Confirm this is a string concatenation (result is string).
            var resultType = semantic.GetTypeInfo(add).Type;
            if (resultType is null || resultType.SpecialType != SpecialType.System_String)
                return;

            VisitOperand(add, ctx);
        }

        private static void VisitOperand(ExpressionSyntax node, SyntaxNodeAnalysisContext ctx)
        {
            // Walk through nested AddExpressions; flag numeric leaves.
            if (node is BinaryExpressionSyntax bin && bin.IsKind(SyntaxKind.AddExpression))
            {
                VisitOperand(bin.Left, ctx);
                VisitOperand(bin.Right, ctx);
                return;
            }

            // Unwrap parentheses to inspect the underlying expression's type, but flag the outer
            // expression so trailing-trivia/parens land in the diagnostic span.
            var inner = node;
            while (inner is ParenthesizedExpressionSyntax paren)
                inner = paren.Expression;

            var semantic = ctx.SemanticModel;
            var typeInfo = semantic.GetTypeInfo(inner);
            var type = typeInfo.Type;

            if (!NumericTypeHelper.IsBclNumericSpecialType(type)
                && !NumericTypeHelper.IsJ2NNumericType(type, semantic.Compilation))
            {
                return;
            }

            var typeName = NumericTypeHelper.GetBclNumericTypeName(type)
                ?? type!.Name;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev2005_NumericStringConcatenation,
                node.GetLocation(),
                typeName));
        }
    }
}
