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

using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1003_ArrayMethodParameter];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.MethodDeclaration, SyntaxKind.ParameterList, SyntaxKind.Parameter, SyntaxKind.ArrayType, SyntaxKind.PredefinedType);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
            {
                foreach (var parameter in methodDeclaration.ParameterList.Parameters)
                {
                    if (parameter.Type is ArrayTypeSyntax arrayTypeSyntax)
                    {
                        if (arrayTypeSyntax.ElementType is PredefinedTypeSyntax predefinedTypeSyntax)
                        {
                            if (predefinedTypeSyntax.Keyword.ValueText != "char")
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1003_ArrayMethodParameter, parameter.GetLocation(), parameter.ToString()));
                        }
                    }
                }
            }
        }
    }
}
