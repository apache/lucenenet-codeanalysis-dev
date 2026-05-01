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
    /// LuceneDev4002: Reports methods referenced by the 2-argument
    /// StackTraceHelper.DoesStackTraceContainMethod(className, methodName) overload
    /// that lack [MethodImpl(MethodImplOptions.NoInlining)]. Without it the JIT may
    /// inline the method out of the stack trace, silently breaking the check.
    /// The diagnostic is reported on the invocation so the IDE surfaces it as a
    /// local diagnostic and a code fix can apply the attribute to the target method.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev4002_StackTraceHelperNoInliningAnalyzer : DiagnosticAnalyzer
    {
        private const string StackTraceHelperFullName = "Lucene.Net.Support.ExceptionHandling.StackTraceHelper";
        private const string DoesStackTraceContainMethodName = "DoesStackTraceContainMethod";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptors.LuceneDev4002_MissingNoInlining);

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
                    ctx => AnalyzeInvocation(ctx, methodImplAttrSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol methodImplAttrSymbol)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;
            if (memberAccess.Name.Identifier.ValueText != DoesStackTraceContainMethodName)
                return;

            // Only the 2-argument overload (className, methodName) is in scope.
            if (invocation.ArgumentList.Arguments.Count != 2)
                return;

            var symbol = ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol is null)
                return;
            if (symbol.ContainingType?.ToDisplayString() != StackTraceHelperFullName)
                return;

            var classArg = invocation.ArgumentList.Arguments[0].Expression;
            var methodArg = invocation.ArgumentList.Arguments[1].Expression;

            var (classNameValue, classTypeFromNameof) = ResolveClassReference(classArg, ctx.SemanticModel);
            if (classNameValue is null)
                return;

            var methodNameValue = ResolveMethodNameValue(methodArg, ctx.SemanticModel);
            if (methodNameValue is null)
                return;

            var targetType = classTypeFromNameof
                ?? FindSourceTypeByName(ctx.SemanticModel.Compilation, classNameValue);
            if (targetType is null)
                return;

            // Report once per call site if any matching method declaration in source
            // is missing NoInlining. Locating the diagnostic on the invocation makes
            // it a "local" diagnostic relative to the syntax tree the analyzer is
            // visiting, which means the IDE surfaces it (compilation-wide non-local
            // diagnostics are filtered out of live IDE analysis) and a code fix can
            // be wired up in the future.
            foreach (var member in targetType.GetMembers(methodNameValue).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

                if (NoInliningAttributeHelper.HasNoInliningAttribute(member, methodImplAttrSymbol))
                    continue;

                foreach (var declRef in member.DeclaringSyntaxReferences)
                {
                    if (declRef.GetSyntax(ctx.CancellationToken) is not MethodDeclarationSyntax methodDecl)
                        continue;

                    if (NoInliningAttributeHelper.HasEmptyBody(methodDecl))
                        continue;

                    if (NoInliningAttributeHelper.IsInterfaceOrAbstractMethod(methodDecl))
                        continue;

                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev4002_MissingNoInlining,
                        invocation.GetLocation(),
                        $"{targetType.Name}.{methodDecl.Identifier.ValueText}"));
                    return;
                }
            }
        }

        private static (string? Name, INamedTypeSymbol? TypeFromNameof) ResolveClassReference(
            ExpressionSyntax expr,
            SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                var typeSymbol = semantic.GetTypeInfo(inner).Type as INamedTypeSymbol
                    ?? semantic.GetSymbolInfo(inner).Symbol as INamedTypeSymbol;
                if (typeSymbol is not null)
                    return (typeSymbol.Name, typeSymbol);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return (literal.Token.ValueText, null);

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return (s, null);

            return (null, null);
        }

        private static string? ResolveMethodNameValue(ExpressionSyntax expr, SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                return ExtractRightmostIdentifier(inner);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return s;

            return null;
        }

        private static string? ExtractRightmostIdentifier(ExpressionSyntax expr)
        {
            return expr switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                _ => null,
            };
        }

        private static INamedTypeSymbol? FindSourceTypeByName(Compilation compilation, string typeName)
        {
            // Use Roslyn's symbol-name index instead of walking every namespace.
            // Restrict to the source assembly so we don't match metadata types.
            foreach (var symbol in compilation.GetSymbolsWithName(n => n == typeName, SymbolFilter.Type))
            {
                if (symbol is INamedTypeSymbol type
                    && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly))
                {
                    return type;
                }
            }
            return null;
        }
    }
}
