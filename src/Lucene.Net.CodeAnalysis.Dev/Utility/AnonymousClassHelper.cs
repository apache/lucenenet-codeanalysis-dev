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

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Identifies types that represent Java anonymous classes, which by convention have
    /// "AnonymousClass" in their name.
    /// </summary>
    public static class AnonymousClassHelper
    {
        private const string Marker = "AnonymousClass";

        // Numbered names such as ThreadAnonymousClass2 are allowed.
        private static readonly Regex ConventionalName = new Regex(
            "AnonymousClass[0-9]*$", RegexOptions.Compiled);

        /// <summary>
        /// Returns <c>true</c> for a class or struct with "AnonymousClass" anywhere in its name.
        /// Other kinds of type, such as an enum named AnonymousClassKind, cannot represent one.
        /// </summary>
        public static bool IsAnonymousClass(INamedTypeSymbol? type) =>
            type is { TypeKind: TypeKind.Class or TypeKind.Struct }
            && type.Name.Contains(Marker)
            && !ApiConventionHelper.IsGeneratedType(type);

        /// <summary>
        /// Returns <c>true</c> if <paramref name="typeName"/> ends in "AnonymousClass", optionally
        /// followed by digits. It should be the name without a generic arity suffix.
        /// </summary>
        public static bool HasConventionalName(string typeName) =>
            ConventionalName.IsMatch(typeName);
    }
}
