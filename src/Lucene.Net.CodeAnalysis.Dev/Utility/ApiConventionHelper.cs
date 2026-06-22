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

using System.Linq;
using Microsoft.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Shared helpers for the analyzers ported from the LUCENENET TestApiConsistency
    /// (ApiScanTestBase) rules. These mirror behaviors of the original reflection-based
    /// tests, including the LUCENENET-specific suppression attributes.
    /// </summary>
    public static class ApiConventionHelper
    {
        // The LUCENENET attributes that suppressed individual TestApiConsistency findings.
        // They are matched by simple name so existing lucenenet suppressions keep working
        // without this analyzer assembly having to reference the attribute types.
        public const string WritableArrayAttribute = "WritableArrayAttribute";
        public const string ExceptionToClassNameConventionAttribute = "ExceptionToClassNameConventionAttribute";
        public const string ExceptionToNetNumericConventionAttribute = "ExceptionToNetNumericConventionAttribute";
        public const string ExceptionToNullableEnumConventionAttribute = "ExceptionToNullableEnumConventionAttribute";

        /// <summary>
        /// Returns <c>true</c> when <paramref name="symbol"/> is decorated with an attribute
        /// whose class is named <paramref name="attributeSimpleName"/> (e.g. "WritableArrayAttribute").
        /// </summary>
        public static bool HasAttribute(ISymbol symbol, string attributeSimpleName) =>
            symbol.GetAttributes().Any(a => a.AttributeClass?.Name == attributeSimpleName);

        /// <summary>
        /// Mirrors the original tests' exclusion of compiler/auto-generated members, which were
        /// identified by a name beginning with '&lt;' (e.g. backing fields and local-function methods).
        /// </summary>
        public static bool IsCompilerGeneratedName(string name) =>
            name.StartsWith("<", System.StringComparison.Ordinal);

        /// <summary>
        /// The original tests ignored types decorated with [GeneratedCode] or [CompilerGenerated].
        /// </summary>
        public static bool IsGeneratedType(INamedTypeSymbol type) =>
            HasAttribute(type, "GeneratedCodeAttribute") || HasAttribute(type, "CompilerGeneratedAttribute");

        /// <summary>
        /// Returns <c>true</c> when the type is in the Lucene.Net.Support namespace (or a child),
        /// which several of the original rules skipped.
        /// </summary>
        public static bool IsInSupportNamespace(INamedTypeSymbol type)
        {
            var ns = type.ContainingNamespace?.ToDisplayString();
            return ns is not null && ns.StartsWith("Lucene.Net.Support", System.StringComparison.Ordinal);
        }

        /// <summary>
        /// Mirrors the original tests' IsNonPrivateProperty helper, which used the parameterless
        /// PropertyInfo.GetGetMethod()/GetSetMethod() overloads. Those return an accessor only when
        /// it is <c>public</c>, so a property "counts" only when it has a public getter or setter.
        /// </summary>
        public static bool HasPublicAccessor(IPropertySymbol property) =>
            property.GetMethod is { DeclaredAccessibility: Accessibility.Public }
            || property.SetMethod is { DeclaredAccessibility: Accessibility.Public };
    }
}
