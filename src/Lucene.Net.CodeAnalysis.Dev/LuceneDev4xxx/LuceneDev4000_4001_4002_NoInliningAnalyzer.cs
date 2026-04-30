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
    /// Analyzer for [MethodImpl(MethodImplOptions.NoInlining)] usage rules:
    ///  - LuceneDev4000: NoInlining has no effect on interface or abstract methods.
    ///  - LuceneDev4001: NoInlining on empty-bodied methods provides no benefit.
    ///  - LuceneDev4002: Methods referenced by StackTraceHelper.DoesStackTraceContainMethod
    ///                   (the 2-argument overload) should be marked NoInlining when the
    ///                   method body is non-empty.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev4000_4001_4002_NoInliningAnalyzer : DiagnosticAnalyzer
    {
        private const string StackTraceHelperFullName = "Lucene.Net.Support.ExceptionHandling.StackTraceHelper";
        private const string DoesStackTraceContainMethodName = "DoesStackTraceContainMethod";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev4000_NoInliningHasNoEffect,
                Descriptors.LuceneDev4001_NoInliningOnEmptyMethod,
                Descriptors.LuceneDev4002_MissingNoInlining);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationCtx =>
            {
                var methodImplAttrSymbol = compilationCtx.Compilation.GetTypeByMetadataName(
                    "System.Runtime.CompilerServices.MethodImplAttribute");

                compilationCtx.RegisterSyntaxNodeAction(
                    ctx => AnalyzeMethodForNoInliningAttribute(ctx, methodImplAttrSymbol),
                    SyntaxKind.MethodDeclaration);

                compilationCtx.RegisterSyntaxNodeAction(
                    ctx => AnalyzeStackTraceHelperInvocation(ctx, methodImplAttrSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        // -----------------------------------------------------------------
        // 4000 / 4001 — examine method declarations carrying NoInlining
        // -----------------------------------------------------------------
        private static void AnalyzeMethodForNoInliningAttribute(
            SyntaxNodeAnalysisContext ctx,
            INamedTypeSymbol? methodImplAttrSymbol)
        {
            if (methodImplAttrSymbol is null)
                return;

            var methodDecl = (MethodDeclarationSyntax)ctx.Node;

            var attribute = FindNoInliningAttribute(methodDecl, ctx.SemanticModel, methodImplAttrSymbol);
            if (attribute is null)
                return;

            // 4000: interface or abstract method
            if (IsInterfaceOrAbstractMethod(methodDecl))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev4000_NoInliningHasNoEffect,
                    attribute.GetLocation(),
                    methodDecl.Identifier.ValueText));
                return;
            }

            // 4001: empty-bodied method
            if (HasEmptyBody(methodDecl))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Descriptors.LuceneDev4001_NoInliningOnEmptyMethod,
                    attribute.GetLocation(),
                    methodDecl.Identifier.ValueText));
            }
        }

        // -----------------------------------------------------------------
        // 4002 — examine StackTraceHelper.DoesStackTraceContainMethod calls
        // -----------------------------------------------------------------
        private static void AnalyzeStackTraceHelperInvocation(
            SyntaxNodeAnalysisContext ctx,
            INamedTypeSymbol? methodImplAttrSymbol)
        {
            if (methodImplAttrSymbol is null)
                return;

            var invocation = (InvocationExpressionSyntax)ctx.Node;

            // Quick syntactic filter
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                return;
            if (memberAccess.Name.Identifier.ValueText != DoesStackTraceContainMethodName)
                return;

            // Only the 2-argument overload (className, methodName) is in scope per the issue.
            if (invocation.ArgumentList.Arguments.Count != 2)
                return;

            // Resolve & verify it is the right method.
            var symbol = ctx.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol is null)
                return;
            if (symbol.ContainingType?.ToDisplayString() != StackTraceHelperFullName)
                return;

            // Identify the referenced method symbol(s) from the (className, methodName) arguments.
            // We prefer the most precise resolution: a `nameof(Type.Method)` expression yields a
            // method-group symbol-info with candidate symbols; a string literal we resolve by name.
            var classArg = invocation.ArgumentList.Arguments[0].Expression;
            var methodArg = invocation.ArgumentList.Arguments[1].Expression;

            var (classNameValue, classTypeFromNameof) = ResolveClassReference(classArg, ctx.SemanticModel);
            if (classNameValue is null)
                return;

            var methodNameValue = ResolveMethodNameValue(methodArg, ctx.SemanticModel);
            if (methodNameValue is null)
                return;

            // Find the target type. Prefer the type resolved from nameof(Type), otherwise look up by
            // simple name within the compilation's source assembly.
            var targetType = classTypeFromNameof
                ?? FindSourceTypeByName(ctx.SemanticModel.Compilation, classNameValue);
            if (targetType is null)
                return;

            // Examine matching methods in the target type (we check all overloads).
            foreach (var member in targetType.GetMembers(methodNameValue).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

                // Walk to the method declaration syntax (only consider source-defined methods).
                foreach (var declRef in member.DeclaringSyntaxReferences)
                {
                    if (declRef.GetSyntax(ctx.CancellationToken) is not MethodDeclarationSyntax methodDecl)
                        continue;

                    // Skip if the method already carries NoInlining.
                    if (FindNoInliningAttribute(methodDecl, ctx.SemanticModel, methodImplAttrSymbol) is not null)
                        continue;

                    // Skip empty-bodied methods (no benefit; see issue rationale).
                    if (HasEmptyBody(methodDecl))
                        continue;

                    // Skip interface/abstract — nothing to inline.
                    if (IsInterfaceOrAbstractMethod(methodDecl))
                        continue;

                    ctx.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.LuceneDev4002_MissingNoInlining,
                        methodDecl.GetLocation(),
                        methodDecl.Identifier.ValueText));
                }
            }
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static AttributeSyntax? FindNoInliningAttribute(
            MethodDeclarationSyntax methodDecl,
            SemanticModel semantic,
            INamedTypeSymbol methodImplAttrSymbol)
        {
            foreach (var attrList in methodDecl.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var attrType = semantic.GetTypeInfo(attr).Type as INamedTypeSymbol;
                    if (attrType is null)
                    {
                        // Sometimes GetTypeInfo on AttributeSyntax doesn't resolve cleanly;
                        // fall back to symbol info on the attribute name.
                        attrType = semantic.GetSymbolInfo(attr).Symbol?.ContainingType;
                    }
                    if (!SymbolEqualityComparer.Default.Equals(attrType, methodImplAttrSymbol))
                        continue;

                    if (AttributeSpecifiesNoInlining(attr, semantic))
                        return attr;
                }
            }
            return null;
        }

        private static bool AttributeSpecifiesNoInlining(AttributeSyntax attr, SemanticModel semantic)
        {
            // [MethodImpl(MethodImplOptions.NoInlining)]
            // [MethodImpl((MethodImplOptions)8)]
            // [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveInlining)]  // pathological, still flag
            if (attr.ArgumentList is null || attr.ArgumentList.Arguments.Count == 0)
                return false;

            // Only the first positional argument controls MethodImplOptions; the second optional
            // argument is MethodCodeType. Skip named arguments.
            var firstPositional = attr.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals is null && a.NameColon is null);
            if (firstPositional is null)
                return false;

            var constant = semantic.GetConstantValue(firstPositional.Expression);
            if (constant.HasValue && constant.Value is int intValue)
            {
                const int NoInlining = 0x0008;
                return (intValue & NoInlining) == NoInlining;
            }

            // Fall back to syntactic textual check ("NoInlining" appears in the expression).
            return firstPositional.Expression.ToString().Contains("NoInlining");
        }

        private static bool IsInterfaceOrAbstractMethod(MethodDeclarationSyntax methodDecl)
        {
            if (methodDecl.Parent is InterfaceDeclarationSyntax)
                return true;
            if (methodDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
                return true;
            return false;
        }

        private static bool HasEmptyBody(MethodDeclarationSyntax methodDecl)
        {
            // Abstract / interface declarations have no body — handled separately.
            if (methodDecl.Body is null && methodDecl.ExpressionBody is null)
                return false;

            if (methodDecl.ExpressionBody is not null)
                return false; // Expression-bodied is by definition non-empty.

            return methodDecl.Body!.Statements.Count == 0;
        }

        private static (string? Name, INamedTypeSymbol? TypeFromNameof) ResolveClassReference(
            ExpressionSyntax expr,
            SemanticModel semantic)
        {
            // nameof(SomeType) — preferred form, also lets us resolve the type symbol.
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

                // nameof can also wrap a member access — fall through to literal extraction.
            }

            // String literal "ClassName"
            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return (literal.Token.ValueText, null);

            // Constant-folded expression (e.g., a const string field)
            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return (s, null);

            return (null, null);
        }

        private static string? ResolveMethodNameValue(ExpressionSyntax expr, SemanticModel semantic)
        {
            // nameof(Type.Method) or nameof(Method) — extract textual identifier
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                return ExtractRightmostIdentifier(inner);
            }

            // String literal "MethodName"
            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            // Constant-folded
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
            // Look for a type with this simple name within the current compilation's source assembly.
            // Exact-name lookup; if multiple match, return the first found.
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
