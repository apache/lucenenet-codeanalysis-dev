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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    /// <summary>
    /// Enforces correct use of the exception helper methods in catch clauses:
    /// <list type="bullet">
    ///   <item><description>LuceneDev6015: a catch clause filtered by a helper called on the caught
    ///   exception must catch <see cref="System.Exception"/>. The helpers match exceptions that do
    ///   not derive from the type they are named after, so a more specific type hides some of them.</description></item>
    ///   <item><description>LuceneDev6016: a catch clause with no filter should use one. A catch clause
    ///   with an existing filter of any kind is treated as a deliberate choice. Code in the
    ///   Lucene.Net.Support namespace is exempt.</description></item>
    /// </list>
    /// Only compilations that can access the helpers are analyzed.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6015_6016_CatchHelperAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException,
                Descriptors.LuceneDev6016_CatchShouldUseHelperFilter);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(startContext =>
            {
                var compilation = startContext.Compilation;
                var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
                var helperType = compilation.GetTypeByMetadataName(ExceptionHelperMethods.ProductionTypeName);

                if (exceptionType is null
                    || helperType is null
                    || !compilation.IsSymbolAccessibleWithin(helperType, compilation.Assembly))
                {
                    return;
                }

                startContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeCatchClause(ctx, exceptionType),
                    SyntaxKind.CatchClause);
            });
        }

        private static void AnalyzeCatchClause(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol exceptionType)
        {
            var catchClause = (CatchClauseSyntax)ctx.Node;

            if (catchClause.Filter is null)
            {
                if (ctx.ContainingSymbol?.ContainingType is not { } containingType
                    || !ApiConventionHelper.IsInSupportNamespace(containingType))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev6016_CatchShouldUseHelperFilter,
                        catchClause.CatchKeyword.GetLocation()));
                }

                return;
            }

            var declaration = catchClause.Declaration;
            if (declaration is null)
                return;

            var caughtType = ctx.SemanticModel.GetTypeInfo(declaration.Type, ctx.CancellationToken).Type;
            if (caughtType is null
                || caughtType.TypeKind == TypeKind.Error
                || SymbolEqualityComparer.Default.Equals(caughtType, exceptionType))
            {
                return;
            }

            var caughtVariable = ctx.SemanticModel.GetDeclaredSymbol(declaration, ctx.CancellationToken);
            if (caughtVariable is null)
                return;

            foreach (var invocation in catchClause.Filter.FilterExpression
                .DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>())
            {
                if (ctx.SemanticModel.GetSymbolInfo(invocation, ctx.CancellationToken).Symbol is IMethodSymbol method
                    && ExceptionHelperMethods.IsHelperFilterMethod(method.ReducedFrom ?? method)
                    && IsCalledOn(invocation, method, caughtVariable, ctx))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException,
                        declaration.Type.GetLocation(),
                        method.Name,
                        caughtType.Name));
                    return;
                }
            }
        }

        private static bool IsCalledOn(
            InvocationExpressionSyntax invocation,
            IMethodSymbol method,
            ILocalSymbol variable,
            SyntaxNodeAnalysisContext ctx)
        {
            ExpressionSyntax? target = null;

            if (method.ReducedFrom is not null)
            {
                target = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
            }
            else if (invocation.ArgumentList.Arguments.Count == 1)
            {
                target = invocation.ArgumentList.Arguments[0].Expression;
            }

            return target is not null
                && SymbolEqualityComparer.Default.Equals(
                    ctx.SemanticModel.GetSymbolInfo(target, ctx.CancellationToken).Symbol,
                    variable);
        }
    }
}
