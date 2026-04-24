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
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6003_6004_SpanComparisonAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> TargetMethodNames =
            ImmutableHashSet.Create("StartsWith", "EndsWith", "IndexOf", "LastIndexOf");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev6003_RedundantOrdinal,
                Descriptors.LuceneDev6004_InvalidComparison);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            if (!(invocation.Expression is MemberAccessExpressionSyntax memberAccess))
                return;

            var methodName = memberAccess.Name.Identifier.ValueText;
            if (!TargetMethodNames.Contains(methodName))
                return;

            var semantic = ctx.SemanticModel;
            var compilation = semantic.Compilation;
            var stringComparisonType = compilation.GetTypeByMetadataName("System.StringComparison");

            if (stringComparisonType == null)
                return;

            // Get symbol info
            var symbolInfo = semantic.GetSymbolInfo(memberAccess);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            var candidateSymbols = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToImmutableArray();

            // Determine if this is a span-like type
            var receiverType = semantic.GetTypeInfo(memberAccess.Expression).Type;

            // Check if calling on System.String - if so, skip (handled by LuceneDev6001)
            if (receiverType != null && receiverType.SpecialType == SpecialType.System_String)
                return;

            // Check if receiver is span-like
            bool isSpanLike = IsSpanLikeReceiver(receiverType);

            // If not span-like based on receiver, check method symbol
            if (!isSpanLike && methodSymbol != null)
            {
                isSpanLike = IsSpanLikeReceiver(methodSymbol.ContainingType);
            }

            // Check candidates if still not determined
            if (!isSpanLike && candidateSymbols.Length > 0)
            {
                isSpanLike = candidateSymbols.Any(c => IsSpanLikeReceiver(c.ContainingType));
            }

            if (!isSpanLike)
                return;

            // Check if this is a char overload - ignore those
            if (methodSymbol != null && IsCharOverload(methodSymbol))
                return;

            if (candidateSymbols.Length > 0 && candidateSymbols.All(c => IsCharOverload(c)))
                return;

            // Skip char literals and single-character string literals when safe ---
            var firstArgExpr = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
            if (firstArgExpr is LiteralExpressionSyntax lit)
            {
                if (lit.IsKind(SyntaxKind.CharacterLiteralExpression))
                    return; // already char overload; no diagnostic

                if (lit.IsKind(SyntaxKind.StringLiteralExpression) && lit.Token.ValueText.Length == 1)
                {
                    // Check if a StringComparison argument is present
                    bool hasStringComparisonArgForLiteral = invocation.ArgumentList.Arguments.Any(arg =>
                        semantic.GetTypeInfo(arg.Expression).Type is INamedTypeSymbol t &&
                        t.ToDisplayString() == "System.StringComparison"
                        || (semantic.GetSymbolInfo(arg.Expression).Symbol is IFieldSymbol f &&
                            f.ContainingType?.ToDisplayString() == "System.StringComparison"));

                    if (!hasStringComparisonArgForLiteral)
                    {
                        // Safe to convert to char (LuceneDev6005 handles it); skip 6003/6004 here.
                        return;
                    }
                    // Has StringComparison -> do not skip; 6003/6004 validation continues.
                }
            }


            // Check for StringComparison argument
            var (hasComparison, comparisonValue, argLocation) =
                CheckStringComparisonArgument(invocation, semantic, stringComparisonType);

            if (!hasComparison)
            {
                // No StringComparison argument - this is OK for span types (default is Ordinal)
                return;
            }

            // Has StringComparison argument - validate it
            if (comparisonValue == "Ordinal")
            {
                // Redundant - suggest removal (Warning)
                var diag = Diagnostic.Create(
                    Descriptors.LuceneDev6003_RedundantOrdinal,
                    argLocation ?? memberAccess.Name.GetLocation(),
                    methodName);
                ctx.ReportDiagnostic(diag);
            }
            else if (comparisonValue == "OrdinalIgnoreCase")
            {
                // Valid - no warning
                return;
            }
            else
            {
                // Invalid comparison (CurrentCulture, InvariantCulture, etc.) - Error
                var diag = Diagnostic.Create(
                    Descriptors.LuceneDev6004_InvalidComparison,
                    argLocation ?? memberAccess.Name.GetLocation(),
                    methodName,
                    comparisonValue ?? "non-ordinal comparison");
                ctx.ReportDiagnostic(diag);
            }
        }

        private static bool IsSpanLikeReceiver(ITypeSymbol? type)
        {
            if (type == null) return false;

            // Check for Span<char> or ReadOnlySpan<char>
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var constructedFrom = namedType.ConstructedFrom.ToDisplayString();
                if (constructedFrom == "System.Span<T>" || constructedFrom == "System.ReadOnlySpan<T>")
                {
                    // Verify it's char
                    var typeArg = namedType.TypeArguments.FirstOrDefault();
                    if (typeArg != null && typeArg.SpecialType == SpecialType.System_Char)
                        return true;
                }
            }

            // Check for custom span-like types
            var fullname = type.ToDisplayString();
            return fullname == "J2N.Text.OpenStringBuilder" ||
                   fullname == "Lucene.Net.Text.ValueStringBuilder";
        }

        private static bool IsCharOverload(IMethodSymbol? method)
        {
            if (method == null) return false;
            // Check if the first parameter (value parameter) is char
            return method.Parameters.Length > 0 &&
                   method.Parameters[0].Type.SpecialType == SpecialType.System_Char;
        }

        private static (bool hasArgument, string? value, Location? location) CheckStringComparisonArgument(
            InvocationExpressionSyntax invocation,
            SemanticModel semantic,
            INamedTypeSymbol stringComparisonType)
        {
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                var argType = semantic.GetTypeInfo(arg.Expression).Type;

                if (argType != null && SymbolEqualityComparer.Default.Equals(argType, stringComparisonType))
                {
                    // Try to get the enum member name
                    var symbol = semantic.GetSymbolInfo(arg.Expression).Symbol as IFieldSymbol;
                    if (symbol != null && SymbolEqualityComparer.Default.Equals(symbol.ContainingType, stringComparisonType))
                    {
                        return (true, symbol.Name, arg.Expression.GetLocation());
                    }

                    // Check constant value
                    var constantValue = semantic.GetConstantValue(arg.Expression);
                    if (constantValue.HasValue && constantValue.Value is int intValue)
                    {
                        string? name = intValue switch
                        {
                            4 => "Ordinal",
                            5 => "OrdinalIgnoreCase",
                            0 => "CurrentCulture",
                            1 => "CurrentCultureIgnoreCase",
                            2 => "InvariantCulture",
                            3 => "InvariantCultureIgnoreCase",
                            _ => null
                        };
                        return (true, name, arg.Expression.GetLocation());
                    }

                    // Has StringComparison but can't determine value
                    return (true, null, arg.Expression.GetLocation());
                }
            }

            return (false, null, null);
        }
    }
}
