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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Helpers for classifying the declared accessibility of a field declaration by its
    /// syntactic modifiers. Shared by the field-related analyzers (LuceneDev7000,
    /// LuceneDev7001 and LuceneDev1009).
    /// </summary>
    public static class FieldNameHelper
    {
        /// <summary>
        /// Returns <c>true</c> when the field has <c>protected</c> accessibility
        /// (including <c>protected internal</c> and <c>private protected</c>).
        /// </summary>
        public static bool IsProtected(FieldDeclarationSyntax field) =>
            field.Modifiers.Any(SyntaxKind.ProtectedKeyword);

        /// <summary>
        /// Returns <c>true</c> when the field has <c>public</c> accessibility.
        /// </summary>
        public static bool IsPublic(FieldDeclarationSyntax field) =>
            field.Modifiers.Any(SyntaxKind.PublicKeyword);

        /// <summary>
        /// Returns <c>true</c> when the field is effectively private, i.e. it has no
        /// accessibility modifier (the C# default for a field) or an explicit
        /// <c>private</c> modifier, and is not <c>protected</c>, <c>internal</c> or
        /// <c>public</c>.
        /// </summary>
        public static bool IsPrivate(FieldDeclarationSyntax field)
        {
            if (field.Modifiers.Any(SyntaxKind.PublicKeyword)
                || field.Modifiers.Any(SyntaxKind.ProtectedKeyword)
                || field.Modifiers.Any(SyntaxKind.InternalKeyword))
            {
                return false;
            }

            return true; // explicit 'private' or no accessibility modifier
        }
    }
}
