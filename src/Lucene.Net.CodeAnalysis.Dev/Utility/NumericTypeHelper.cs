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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    internal static class NumericTypeHelper
    {
        // The 11 BCL numeric primitive types covered by the Lucene.NET culture-correctness audit.
        public static bool IsBclNumericSpecialType(ITypeSymbol? type)
        {
            if (type is null) return false;
            switch (type.SpecialType)
            {
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_Decimal:
                    return true;
                default:
                    return false;
            }
        }

        // Returns the simple numeric type name (e.g. "Int32") for a BCL numeric, or null.
        public static string? GetBclNumericTypeName(ITypeSymbol? type)
        {
            if (type is null) return null;
            return type.SpecialType switch
            {
                SpecialType.System_Byte => "Byte",
                SpecialType.System_SByte => "SByte",
                SpecialType.System_Int16 => "Int16",
                SpecialType.System_UInt16 => "UInt16",
                SpecialType.System_Int32 => "Int32",
                SpecialType.System_UInt32 => "UInt32",
                SpecialType.System_Int64 => "Int64",
                SpecialType.System_UInt64 => "UInt64",
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                SpecialType.System_Decimal => "Decimal",
                _ => null
            };
        }

        private static readonly string[] J2NNumericMetadataNames = new[]
        {
            "J2N.Numerics.Int32",
            "J2N.Numerics.Int64",
            "J2N.Numerics.Int16",
            "J2N.Numerics.Byte",
            "J2N.Numerics.SByte",
            "J2N.Numerics.Single",
            "J2N.Numerics.Double",
        };

        // Resolved J2N numeric types are cached per-Compilation so analyzers don't
        // re-run GetTypeByMetadataName for every numeric invocation/concat/interpolation node.
        // ConditionalWeakTable keeps the cache alive only as long as the Compilation is.
        private sealed class J2NTypeBox { public ImmutableArray<INamedTypeSymbol> Types; }
        private static readonly ConditionalWeakTable<Compilation, J2NTypeBox> J2NTypeCache = new();

        public static ImmutableArray<INamedTypeSymbol> GetJ2NNumericTypes(Compilation compilation)
            => J2NTypeCache.GetValue(compilation, ResolveJ2NTypes).Types;

        private static J2NTypeBox ResolveJ2NTypes(Compilation compilation)
        {
            var builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(J2NNumericMetadataNames.Length);
            foreach (var name in J2NNumericMetadataNames)
            {
                var t = compilation.GetTypeByMetadataName(name);
                if (t is not null) builder.Add(t);
            }
            return new J2NTypeBox { Types = builder.ToImmutable() };
        }

        public static bool IsJ2NNumericType(ITypeSymbol? type, Compilation compilation)
        {
            if (type is null) return false;
            if (type is not INamedTypeSymbol named) return false;

            // Fast pre-filter: skip the symbol-equality loop unless the metadata name matches a J2N type.
            // (BCL primitives never sit under J2N.Numerics, so this short-circuits the hot path.)
            if (named.ContainingNamespace?.ToDisplayString() != "J2N.Numerics")
                return false;

            foreach (var j2n in GetJ2NNumericTypes(compilation))
            {
                if (SymbolEqualityComparer.Default.Equals(named, j2n))
                    return true;
            }
            return false;
        }

        public static bool IsNumericType(ITypeSymbol? type, Compilation compilation)
            => IsBclNumericSpecialType(type) || IsJ2NNumericType(type, compilation);

        // True if any parameter on the method's signature is (or implements) System.IFormatProvider.
        public static bool HasFormatProviderParameter(IMethodSymbol? method, Compilation compilation)
        {
            if (method is null) return false;
            var fpType = compilation.GetTypeByMetadataName("System.IFormatProvider");
            if (fpType is null) return false;
            foreach (var p in method.Parameters)
            {
                if (SymbolEqualityComparer.Default.Equals(p.Type, fpType))
                    return true;
                foreach (var iface in p.Type.AllInterfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(iface, fpType))
                        return true;
                }
            }
            return false;
        }

        // Find the IFormatProvider argument expression in an invocation, if any.
        public static ExpressionSyntax? GetFormatProviderArgument(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel)
        {
            var fpType = semanticModel.Compilation.GetTypeByMetadataName("System.IFormatProvider");
            if (fpType is null) return null;

            foreach (var arg in invocation.ArgumentList.Arguments)
            {
                // For literals like `null` or `default`, GetTypeInfo(...).Type is null but
                // ConvertedType reflects the parameter type chosen by overload resolution.
                var typeInfo = semanticModel.GetTypeInfo(arg.Expression);
                var argType = typeInfo.Type ?? typeInfo.ConvertedType;
                if (argType is null) continue;
                if (SymbolEqualityComparer.Default.Equals(argType, fpType))
                    return arg.Expression;
                if (argType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, fpType)))
                    return arg.Expression;
            }
            return null;
        }

        // True when the expression statically resolves to System.Globalization.CultureInfo.InvariantCulture.
        public static bool IsInvariantCultureAccess(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            var symbol = semanticModel.GetSymbolInfo(expression).Symbol;
            if (symbol is IPropertySymbol prop)
            {
                return prop.Name == "InvariantCulture"
                    && prop.ContainingType?.ToDisplayString() == "System.Globalization.CultureInfo";
            }
            return false;
        }

        // True if the given syntax node is lexically inside an `override ToString()` method body.
        public static bool IsInsideToStringOverride(SyntaxNode node)
        {
            for (var current = node.Parent; current is not null; current = current.Parent)
            {
                if (current is MethodDeclarationSyntax method
                    && method.Identifier.ValueText == "ToString"
                    && method.ParameterList.Parameters.Count == 0
                    && method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
                {
                    return true;
                }
            }
            return false;
        }

        // True when the invocation is the FormattableString argument to FormattableString.Invariant(...)
        // or string.Create(IFormatProvider, ...) — in those cases an interpolated numeric is fine.
        public static bool IsInsideInvariantInterpolationContext(SyntaxNode interpolated, SemanticModel semanticModel)
        {
            // Walk up to the enclosing invocation that takes this interpolated string as an argument.
            for (var current = interpolated.Parent; current is not null; current = current.Parent)
            {
                if (current is InvocationExpressionSyntax inv)
                {
                    var symbol = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
                    if (symbol is null) continue;

                    var containing = symbol.ContainingType?.ToDisplayString();
                    var methodName = symbol.Name;

                    // FormattableString.Invariant(FormattableString)
                    if (containing == "System.FormattableString" && methodName == "Invariant")
                        return true;

                    // string.Create(IFormatProvider, …) — only treat as invariant when the provider is InvariantCulture.
                    if (containing == "string" || containing == "System.String")
                    {
                        if (methodName == "Create" && inv.ArgumentList.Arguments.Count >= 1)
                        {
                            var first = inv.ArgumentList.Arguments[0].Expression;
                            if (IsInvariantCultureAccess(first, semanticModel))
                                return true;
                        }
                    }

                    return false;
                }
            }
            return false;
        }
    }
}
