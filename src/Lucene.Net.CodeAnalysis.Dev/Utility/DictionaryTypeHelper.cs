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

using Microsoft.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    internal static class DictionaryTypeHelper
    {
        private const string GenericIDictionary = "System.Collections.Generic.IDictionary`2";
        private const string GenericIReadOnlyDictionary = "System.Collections.Generic.IReadOnlyDictionary`2";
        private const string NonGenericIDictionary = "System.Collections.IDictionary";

        /// <summary>
        /// Returns true when <paramref name="indexer"/> is the indexer declared by (or implementing)
        /// <c>IDictionary&lt;TKey, TValue&gt;</c> or <c>IReadOnlyDictionary&lt;TKey, TValue&gt;</c> on a type
        /// that implements one of those interfaces.
        /// </summary>
        public static bool IsGenericDictionaryIndexer(IPropertySymbol indexer, INamedTypeSymbol containingType, out ITypeSymbol? valueType)
        {
            valueType = null;

            // Must take a single key parameter. (The non-generic IDictionary.this[object] variant is handled elsewhere.)
            if (indexer.Parameters.Length != 1)
                return false;

            // If the containing type is itself IDictionary<,> or IReadOnlyDictionary<,>, this is direct.
            if (containingType.IsGenericType
                && (MatchesConstructedFrom(containingType, GenericIDictionary)
                    || MatchesConstructedFrom(containingType, GenericIReadOnlyDictionary)))
            {
                valueType = containingType.TypeArguments[1];
                return true;
            }

            // Otherwise, containing type must implement one of the generic dictionary interfaces,
            // and the parameter type must match TKey of an implemented interface.
            foreach (var iface in containingType.AllInterfaces)
            {
                if (!iface.IsGenericType)
                    continue;

                var matches = MatchesConstructedFrom(iface, GenericIDictionary)
                              || MatchesConstructedFrom(iface, GenericIReadOnlyDictionary);
                if (!matches)
                    continue;

                var tkey = iface.TypeArguments[0];
                var tvalue = iface.TypeArguments[1];

                if (SymbolEqualityComparer.Default.Equals(indexer.Parameters[0].Type, tkey))
                {
                    valueType = tvalue;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when <paramref name="indexer"/> is the non-generic <c>IDictionary</c> indexer
        /// (takes <see cref="object"/>, returns <see cref="object"/>) declared by or implemented on a type
        /// that implements <c>System.Collections.IDictionary</c>.
        /// </summary>
        public static bool IsNonGenericDictionaryIndexer(IPropertySymbol indexer, INamedTypeSymbol containingType)
        {
            if (indexer.Parameters.Length != 1)
                return false;

            // The non-generic IDictionary indexer returns object and takes object.
            if (indexer.Parameters[0].Type.SpecialType != SpecialType.System_Object)
                return false;
            if (indexer.Type.SpecialType != SpecialType.System_Object)
                return false;

            if (containingType.ToDisplayString() == NonGenericIDictionary)
                return true;

            foreach (var iface in containingType.AllInterfaces)
            {
                if (iface.ToDisplayString() == NonGenericIDictionary)
                    return true;
            }

            return false;
        }

        private static bool MatchesConstructedFrom(INamedTypeSymbol type, string metadataName)
        {
            var constructedFrom = type.ConstructedFrom ?? type;
            return constructedFrom.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                       .Equals("global::" + StripArity(metadataName), System.StringComparison.Ordinal)
                   || MetadataNameEquals(constructedFrom, metadataName);
        }

        private static string StripArity(string metadataName)
        {
            var backtick = metadataName.IndexOf('`');
            return backtick < 0 ? metadataName : metadataName.Substring(0, backtick);
        }

        private static bool MetadataNameEquals(INamedTypeSymbol type, string metadataName)
        {
            var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            var full = string.IsNullOrEmpty(ns) ? type.MetadataName : ns + "." + type.MetadataName;
            return full == metadataName;
        }
    }
}
