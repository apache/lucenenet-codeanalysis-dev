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

using Microsoft.CodeAnalysis;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Identifies the extension methods that Lucene.NET provides for use in catch clause filters.
    /// </summary>
    public static class ExceptionHelperMethods
    {
        /// <summary>
        /// The class containing the helpers, in the Lucene.Net assembly.
        /// </summary>
        public const string ProductionTypeName = "Lucene.ExceptionExtensions";

        /// <summary>
        /// The class containing test-specific overrides of the helpers, in the
        /// Lucene.Net.TestFramework assembly.
        /// </summary>
        public const string TestFrameworkTypeName = "Lucene.Net.ExceptionExtensions";

        /// <summary>
        /// Returns <c>true</c> if the method is a public <c>bool</c> extension method that takes
        /// only the exception. That excludes other members of the same classes that are not
        /// filters, such as <c>PrintStackTrace()</c> and <c>ToTypeMessageString()</c>.
        /// </summary>
        /// <param name="method">The method, in its unreduced form.</param>
        public static bool IsFilterMethod(IMethodSymbol method)
            => method.IsExtensionMethod
                && method.DeclaredAccessibility == Accessibility.Public
                && method.ReturnType.SpecialType == SpecialType.System_Boolean
                && method.Parameters.Length == 1;

        /// <summary>
        /// Returns <c>true</c> if the method is a filter method declared by one of the helper classes.
        /// </summary>
        /// <param name="method">The method, in its unreduced form.</param>
        public static bool IsHelperFilterMethod(IMethodSymbol method)
        {
            var containingType = method.ContainingType?.ToDisplayString();
            return (containingType == ProductionTypeName || containingType == TestFrameworkTypeName)
                && IsFilterMethod(method);
        }
    }
}
