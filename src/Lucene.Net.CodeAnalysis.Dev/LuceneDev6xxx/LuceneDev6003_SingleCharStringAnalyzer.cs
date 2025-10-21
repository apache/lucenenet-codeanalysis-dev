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
    /// Analyzer to detect single-character string literals (including escaped characters)
    /// that should use char overload instead for better performance.
    /// Applies to String, Span, and custom span-like types.
    ///
    /// Examples of violations:
    /// - text.IndexOf("H") -> should use text.IndexOf('H')
    /// - text.IndexOf("\n") -> should use text.IndexOf('\n')  // Escaped newline
    /// - text.IndexOf("\"") -> should use text.IndexOf('\"')  // Escaped quote
    /// - span.StartsWith("a") -> should use span.StartsWith('a')
    ///
    /// Severity: Info (suggestion only, not enforced)
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6003_SingleCharStringAnalyzer : DiagnosticAnalyzer
    {
        private static readonly ImmutableHashSet<string> TargetMethodNames =
            ImmutableHashSet.Create("StartsWith", "EndsWith", "IndexOf", "LastIndexOf");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptors.LuceneDev6003_SingleCharStringAnalyzer);

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

            // Check if invocation has arguments
            if (invocation.ArgumentList.Arguments.Count == 0)
                return;

            // Get the first argument (the value to search for)
            var firstArg = invocation.ArgumentList.Arguments[0];

            // Must be a string literal
            if (!(firstArg.Expression is LiteralExpressionSyntax literal))
                return;

            if (!literal.IsKind(SyntaxKind.StringLiteralExpression))
                return;

            // Get the actual character value (handles escape sequences automatically)
            var token = literal.Token;
            var valueText = token.ValueText; // This is the unescaped string value

            // Check if it's exactly one character after unescaping
            if (valueText.Length != 1)
                return;

            // Get the method symbol to verify it's called on a valid type
            var symbolInfo = semantic.GetSymbolInfo(memberAccess);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;
            var candidateSymbols = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().ToImmutableArray();

            // Determine the receiver type
            var receiverType = semantic.GetTypeInfo(memberAccess.Expression).Type;

            // Check if this is a valid target type (String, Span, or custom span-like)
            bool isSpanLike = IsSpanLikeReceiver(receiverType);
            bool isValidTarget = IsValidTargetType(receiverType)
                                    || (methodSymbol != null && IsValidTargetType(methodSymbol.ContainingType))
                                    || candidateSymbols.Any(c => IsValidTargetType(c.ContainingType));

            if (!isValidTarget)
                return;

            // 🌟 CRITICAL FIX: Handle Span/ReadOnlySpan differences
            // For Span<char> and ReadOnlySpan<char>:
            // 1. StartsWith/EndsWith only take ReadOnlySpan<char>, NOT a single char, so we must skip the diagnostic.
            // 2. IndexOf/LastIndexOf only have single-argument overloads for the 'char' (or 'value span') overload.
            if (isSpanLike)
            {
                if (methodName == "StartsWith" || methodName == "EndsWith")
                {
                    // Span/ReadOnlySpan do not have 'char' overloads for StartsWith/EndsWith.
                    // The string literal "a" is correctly resolved to the ReadOnlySpan<char> overload.
                    return;
                }

                // For IndexOf/LastIndexOf on spans, if the invocation has more than 1 argument,
                // it's likely a custom extension method or an invalid call, and it won't resolve
                // to the simple `IndexOf(char value)` or `IndexOf(ReadOnlySpan<char> value)` methods.
                // We only target the simplest case for replacement.
                if (invocation.ArgumentList.Arguments.Count != 1)
                    return;
            }
            else
            {
                // For System.String and custom types, we allow multiple arguments (e.g., IndexOf("a", 5))
                // because the char overloads like IndexOf('a', 5) exist.
                // We rely on the `HasCharOverload` check below to validate that the char overload exists.
            }
            // -----------------------------------------------------

            // Check if a char overload exists
            bool hasCharOverload = HasCharOverload(methodSymbol, candidateSymbols, receiverType, methodName);

            if (!hasCharOverload)
                return;

            // Report diagnostic with Info severity
            // token.Text shows the ORIGINAL text as written in code (with escaping)
            // For example: "\"" shows as "\""
            //              "\n" shows as "\n"
            var diag = Diagnostic.Create(
                Descriptors.LuceneDev6003_SingleCharStringAnalyzer,
                literal.GetLocation(),
                methodName,
                literal.Token.Text); // Show the original escaped text in the message

            ctx.ReportDiagnostic(diag);
        }

        /// <summary>
        /// Determines if the given type is a valid target for this analyzer.
        /// Valid types: System.String, Span&lt;char&gt;, ReadOnlySpan&lt;char&gt;,
        /// J2N.Text.OpenStringBuilder, Lucene.Net.Text.ValueStringBuilder
        /// </summary>
        private static bool IsValidTargetType(ITypeSymbol? type)
        {
            if (type == null) return false;

            // System.String
            if (type.SpecialType == SpecialType.System_String)
                return true;

            // Span<char> or ReadOnlySpan<char>
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var constructedFrom = namedType.ConstructedFrom.ToDisplayString();
                if (constructedFrom == "System.Span<T>" || constructedFrom == "System.ReadOnlySpan<T>")
                {
                    // Verify it's specifically Span<char> or ReadOnlySpan<char>
                    var typeArg = namedType.TypeArguments.FirstOrDefault();
                    if (typeArg != null && typeArg.SpecialType == SpecialType.System_Char)
                        return true;
                }
            }

            // Custom span-like types from Lucene.NET and J2N
            var fullname = type.ToDisplayString();
            return fullname == "J2N.Text.OpenStringBuilder" ||
                   fullname == "Lucene.Net.Text.ValueStringBuilder";
        }

        /// <summary>
        /// Determines if the receiver type is Span&lt;char&gt; or ReadOnlySpan&lt;char&gt;.
        /// </summary>
        private static bool IsSpanLikeReceiver(ITypeSymbol? type)
        {
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                var constructedFrom = namedType.ConstructedFrom.ToDisplayString();
                if ((constructedFrom == "System.Span<T>" || constructedFrom == "System.ReadOnlySpan<T>") &&
                    namedType.TypeArguments.FirstOrDefault()?.SpecialType == SpecialType.System_Char)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a char overload exists for the given method.
        /// A char overload is a method with the same name where the first parameter is System.Char.
        /// </summary>
        private static bool HasCharOverload(
            IMethodSymbol? methodSymbol,
            ImmutableArray<IMethodSymbol> candidateSymbols,
            ITypeSymbol? receiverType,
            string methodName)
        {
            ImmutableArray<IMethodSymbol> methodsToCheck = ImmutableArray<IMethodSymbol>.Empty;

            // Strategy 1: Get all methods with the same name from the resolved method's containing type
            if (methodSymbol != null && methodSymbol.ContainingType != null)
            {
                methodsToCheck = methodSymbol.ContainingType
                    .GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .ToImmutableArray();
            }
            // Strategy 2: Use candidate symbols if method couldn't be resolved
            else if (candidateSymbols.Length > 0)
            {
                methodsToCheck = candidateSymbols;

                // Also try to get more methods from the first candidate's containing type
                var containingType = candidateSymbols.FirstOrDefault()?.ContainingType;
                if (containingType != null)
                {
                    var additionalMethods = containingType.GetMembers(methodName).OfType<IMethodSymbol>();
                    methodsToCheck = methodsToCheck.Concat(additionalMethods).ToImmutableArray();
                }
            }
            // Strategy 3: Use receiver type if nothing else worked
            else if (receiverType != null)
            {
                methodsToCheck = receiverType
                    .GetMembers(methodName)
                    .OfType<IMethodSymbol>()
                    .ToImmutableArray();
            }

            // Look for a char overload
            // The char overload should have System.Char as the first parameter (the value parameter)
            foreach (var method in methodsToCheck)
            {
                if (method.Parameters.Length > 0 &&
                    method.Parameters[0].Type.SpecialType == SpecialType.System_Char)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
