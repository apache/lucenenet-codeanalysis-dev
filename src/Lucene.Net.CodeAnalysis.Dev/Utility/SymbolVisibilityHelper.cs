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
    public static class SymbolVisibilityHelper
    {
        /// <summary>
        /// Returns <c>true</c> if the symbol and all of its containing types are visible outside
        /// of the assembly (public, protected, or protected internal), and therefore appear in the
        /// API documentation.
        /// </summary>
        public static bool IsPubliclyVisible(ISymbol symbol)
        {
            switch (symbol)
            {
                case INamespaceSymbol:
                case ITypeParameterSymbol:
                case IParameterSymbol:
                    return true;
                case IMethodSymbol { ExplicitInterfaceImplementations.IsEmpty: false }:
                case IPropertySymbol { ExplicitInterfaceImplementations.IsEmpty: false }:
                case IEventSymbol { ExplicitInterfaceImplementations.IsEmpty: false }:
                    // Explicit interface implementations are declared private, but are part of the
                    // public API surface when their containing type is visible.
                    return symbol.ContainingType is null || IsPubliclyVisible(symbol.ContainingType);
            }

            for (var current = symbol; current is not null and not INamespaceSymbol; current = current.ContainingSymbol)
            {
                if (!IsPubliclyAccessible(current.DeclaredAccessibility))
                    return false;
            }

            return true;
        }

        private static bool IsPubliclyAccessible(Accessibility accessibility) =>
            accessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal;
    }
}
