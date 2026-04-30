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
using System.Linq;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx
{
    /// <summary>
    /// Reports cases where [MethodImpl(MethodImplOptions.NoInlining)] is applied but
    /// has no useful effect:
    ///  - LuceneDev4000: on an interface or abstract method (the attribute is not
    ///                   inherited, so it has no effect on the implementation).
    ///  - LuceneDev4001: on an empty-bodied method (it cannot appear above any
    ///                   stack frame, so preventing inlining gives no benefit).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev4000_4001_NoInliningOnNoOpAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev4000_NoInliningHasNoEffect,
                Descriptors.LuceneDev4001_NoInliningOnEmptyMethod);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationCtx =>
            {
                var methodImplAttrSymbol = compilationCtx.Compilation.GetTypeByMetadataName(
                    "System.Runtime.CompilerServices.MethodImplAttribute");
                if (methodImplAttrSymbol is null)
                    return;

                compilationCtx.RegisterSyntaxNodeAction(
                    ctx => Analyze(ctx, methodImplAttrSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void Analyze(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol methodImplAttrSymbol)
        {
            var methodDecl = (MethodDeclarationSyntax)ctx.Node;

            var attribute = NoInliningAttributeHelper.FindNoInliningAttribute(
                methodDecl, ctx.SemanticModel, methodImplAttrSymbol);
            if (attribute is null)
                return;

            // 4000: interface or abstract method
            if (NoInliningAttributeHelper.IsInterfaceOrAbstractMethod(methodDecl))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev4000_NoInliningHasNoEffect,
                    attribute.GetLocation(),
                    methodDecl.Identifier.ValueText));
                return;
            }

            // 4001: empty-bodied method
            if (NoInliningAttributeHelper.HasEmptyBody(methodDecl))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev4001_NoInliningOnEmptyMethod,
                    attribute.GetLocation(),
                    methodDecl.Identifier.ValueText));
            }
        }
    }
}
