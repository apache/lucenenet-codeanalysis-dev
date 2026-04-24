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
    public sealed class LuceneDev6001_6002_StringComparisonAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> TargetMethodNames =
            ImmutableHashSet.Create("StartsWith", "EndsWith", "IndexOf", "LastIndexOf");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev6001_MissingStringComparison,
                Descriptors.LuceneDev6002_InvalidStringComparison);

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

            // Skip char literals and single-character string literals when safe ---
            // early in AnalyzeInvocation, after verifying target method & span/string scope
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
                        // Safe to convert to char (LuceneDev6005 handles it); skip 6001/6002 here.
                        return;
                    }
                    // Has StringComparison -> do not skip; 6001/6002 validation continues.
                }
            }


            // Get symbol info
            var symbolInfo = semantic.GetSymbolInfo(memberAccess);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            var candidateSymbols = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToImmutableArray();

            // Determine if containing type qualifies: System.String or J2N.StringBuilderExtensions variants
            static bool ContainingTypeIsStringOrJ2N(INamedTypeSymbol? containingType)
            {
                if (containingType == null) return false;
                if (containingType.SpecialType == SpecialType.System_String)
                    return true;

                // Accept both "J2N.Text.StringBuilderExtensions" and "J2N.StringBuilderExtensions"
                var fullname = containingType.ToDisplayString();
                return fullname == "J2N.Text.StringBuilderExtensions" || fullname == "J2N.StringBuilderExtensions";
            }

            // Check if method has StringComparison parameter
            static bool HasStringComparisonParameter(IMethodSymbol? m, INamedTypeSymbol scType)
            {
                if (m == null) return false;
                return m.Parameters.Any(p => SymbolEqualityComparer.Default.Equals(p.Type, scType));
            }

            // Check if invocation has StringComparison argument and validate it
            var (hasStringComparisonArg, isValidValue, invalidArgLocation, comparisonValueName) =
                CheckStringComparisonArgument(invocation, semantic, stringComparisonType);

            // If resolved symbol available
            if (methodSymbol != null)
            {
                // Only apply rule to System.String or J2N.StringBuilderExtensions containing type
                if (!ContainingTypeIsStringOrJ2N(methodSymbol.ContainingType))
                    return;

                // If the method has StringComparison parameter in signature
                bool methodHasComparisonParam = HasStringComparisonParameter(methodSymbol, stringComparisonType);

                if (hasStringComparisonArg)
                {
                    // Argument is present - check if it's valid
                    if (!isValidValue)
                    {
                        var diag = Diagnostic.Create(
                            Descriptors.LuceneDev6002_InvalidStringComparison,
                            invalidArgLocation ?? memberAccess.Name.GetLocation(),
                            methodName,
                            comparisonValueName ?? "non-ordinal comparison");
                        ctx.ReportDiagnostic(diag);
                    }
                    return;
                }

                // No StringComparison argument provided
                if (!methodHasComparisonParam)
                {
                    // Method doesn't have StringComparison parameter - report error
                    var diag = Diagnostic.Create(
                        Descriptors.LuceneDev6001_MissingStringComparison,
                        memberAccess.Name.GetLocation(),
                        methodName);
                    ctx.ReportDiagnostic(diag);
                }

                return;
            }

            // Handle ambiguous candidates
            if (candidateSymbols.Length > 0)
            {
                // Check if any candidate is from String or J2N types
                var relevantCandidates = candidateSymbols
                    .Where(c => ContainingTypeIsStringOrJ2N(c.ContainingType))
                    .ToImmutableArray();

                if (relevantCandidates.Length == 0)
                    return;

                // If StringComparison argument is provided
                if (hasStringComparisonArg)
                {
                    if (!isValidValue)
                    {
                        var diag = Diagnostic.Create(
                            Descriptors.LuceneDev6002_InvalidStringComparison,
                            invalidArgLocation ?? memberAccess.Name.GetLocation(),
                            methodName,
                            comparisonValueName ?? "non-ordinal comparison");
                        ctx.ReportDiagnostic(diag);
                    }
                    return;
                }

                // No StringComparison argument - check if any candidate has it
                bool anyCandidateHasComparison = relevantCandidates
                    .Any(c => HasStringComparisonParameter(c, stringComparisonType));

                if (!anyCandidateHasComparison)
                {
                    // None of the candidates have StringComparison parameter
                    var diag = Diagnostic.Create(
                        Descriptors.LuceneDev6001_MissingStringComparison,
                        memberAccess.Name.GetLocation(),
                        methodName);
                    ctx.ReportDiagnostic(diag);
                }
            }
        }

        private static (bool hasArgument, bool isValid, Location? location, string? valueName) CheckStringComparisonArgument(
            InvocationExpressionSyntax invocation,
            SemanticModel semantic,
            INamedTypeSymbol stringComparisonType)
        {
            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                var argType = semantic.GetTypeInfo(arg.Expression).Type;

                bool typeMatches = argType != null && SymbolEqualityComparer.Default.Equals(argType, stringComparisonType);
                var argSymbol = semantic.GetSymbolInfo(arg.Expression).Symbol as IFieldSymbol;
                bool symbolMatches = argSymbol != null && SymbolEqualityComparer.Default.Equals(argSymbol.ContainingType, stringComparisonType);

                if (typeMatches || symbolMatches)
                {
                    bool isValid = IsValidStringComparisonValue(semantic, arg.Expression, stringComparisonType);
                    string? name = argSymbol?.Name ?? GetStringComparisonNameFromConstant(semantic, arg.Expression);
                    return (true, isValid, arg.Expression.GetLocation(), name);
                }
            }

            return (false, true, null, null);
        }

        private static string? GetStringComparisonNameFromConstant(SemanticModel semantic, ExpressionSyntax expression)
        {
            var constantValue = semantic.GetConstantValue(expression);
            if (constantValue.HasValue && constantValue.Value is int intValue)
            {
                return intValue switch
                {
                    0 => "CurrentCulture",
                    1 => "CurrentCultureIgnoreCase",
                    2 => "InvariantCulture",
                    3 => "InvariantCultureIgnoreCase",
                    4 => "Ordinal",
                    5 => "OrdinalIgnoreCase",
                    _ => null
                };
            }
            return null;
        }

        private static bool IsValidStringComparisonValue(
            SemanticModel semantic,
            ExpressionSyntax expression,
            INamedTypeSymbol stringComparisonType)
        {
            // Get the constant value if available
            var constantValue = semantic.GetConstantValue(expression);
            if (constantValue.HasValue && constantValue.Value is int intValue)
            {
                // StringComparison.Ordinal = 4, OrdinalIgnoreCase = 5
                return intValue == 4 || intValue == 5;
            }

            // Try to get field symbol
            var symbolInfo = semantic.GetSymbolInfo(expression);
            var fieldSymbol = symbolInfo.Symbol as IFieldSymbol;

            if (fieldSymbol != null && SymbolEqualityComparer.Default.Equals(fieldSymbol.ContainingType, stringComparisonType))
            {
                var memberName = fieldSymbol.Name;
                return memberName == "Ordinal" || memberName == "OrdinalIgnoreCase";
            }

            // If we can't determine, be conservative and allow it
            return true;
        }
    }
}
