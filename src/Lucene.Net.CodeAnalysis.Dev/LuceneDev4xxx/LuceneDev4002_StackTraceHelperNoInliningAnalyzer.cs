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

using System.Collections.Generic;
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
    ///
    /// This analyzer has no code fix: the diagnostic is reported on the referenced
    /// method declaration but is triggered by a separate invocation, which Roslyn
    /// treats as a non-local diagnostic and does not allow code fixes for.
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

            foreach (var member in targetType.GetMembers(methodNameValue).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

                foreach (var declRef in member.DeclaringSyntaxReferences)
                {
                    if (declRef.GetSyntax(ctx.CancellationToken) is not MethodDeclarationSyntax methodDecl)
                        continue;

                    if (NoInliningAttributeHelper.FindNoInliningAttribute(methodDecl, ctx.SemanticModel, methodImplAttrSymbol) is not null)
                        continue;

                    if (NoInliningAttributeHelper.HasEmptyBody(methodDecl))
                        continue;

                    if (NoInliningAttributeHelper.IsInterfaceOrAbstractMethod(methodDecl))
                        continue;

                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev4002_MissingNoInlining,
                        methodDecl.GetLocation(),
                        methodDecl.Identifier.ValueText));
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
            foreach (var type in EnumerateAllTypes(compilation.Assembly.GlobalNamespace))
            {
                if (type.Name == typeName)
                    return type;
            }
            return null;
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateAllTypes(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                if (member is INamedTypeSymbol type)
                {
                    yield return type;
                    foreach (var nested in EnumerateNestedTypes(type))
                        yield return nested;
                }
                else if (member is INamespaceSymbol child)
                {
                    foreach (var t in EnumerateAllTypes(child))
                        yield return t;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> EnumerateNestedTypes(INamedTypeSymbol type)
        {
            foreach (var nested in type.GetTypeMembers())
            {
                yield return nested;
                foreach (var deeper in EnumerateNestedTypes(nested))
                    yield return deeper;
            }
        }
    }
}
