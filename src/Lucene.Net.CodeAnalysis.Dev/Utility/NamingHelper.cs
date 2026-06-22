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

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Shared naming-convention helpers ported from the LUCENENET TestApiConsistency
    /// (ApiScanTestBase) rules. Centralizes the regular expressions so the individual
    /// naming analyzers (LuceneDev7000-LuceneDev7008) stay small and consistent.
    /// </summary>
    public static class NamingHelper
    {
        // Private field names: camelCase, optionally with a leading underscore, OR an UPPER_CASE constant.
        public static readonly Regex PrivateFieldName = new Regex(
            "^_?[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        // Protected field names: 'm_' prefix followed by camelCase, OR an UPPER_CASE constant.
        public static readonly Regex ProtectedFieldName = new Regex(
            "^m_[a-z][a-zA-Z0-9_]*$|^[A-Z0-9_]+$", RegexOptions.Compiled);

        // Method parameter names: camelCase.
        public static readonly Regex MethodParameterName = new Regex(
            "^[a-z](?:[a-zA-Z0-9_]*[a-zA-Z0-9])?$", RegexOptions.Compiled);

        // Interface names: 'I' prefix followed by PascalCase (optionally with a generic arity suffix).
        public static readonly Regex InterfaceName = new Regex(
            "^I[A-Z][a-zA-Z0-9_]*(?:`\\d+)?$", RegexOptions.Compiled);

        // Class names: PascalCase (optionally with a generic arity suffix).
        public static readonly Regex ClassName = new Regex(
            "^[A-Z][a-zA-Z0-9_]*(?:`\\d+)?$", RegexOptions.Compiled);

        // Names containing the Java term "comparator" (case-insensitive).
        public static readonly Regex ContainsComparer = new Regex(
            "[Cc]omparator", RegexOptions.Compiled);

        // Names containing a Java-style numeric term (Int/Long/Short/Float) that is not a valid
        // .NET numeric type name (Int16/Int32/Int64 etc.) or an innocuous substring (point, print,
        // join, integer, longest, shortest, ...).
        public static readonly Regex ContainsNonNetNumeric = new Regex(
            "(?<![Pp]o|[Pp]r|[Jj]o)[Ii]nt(?!16|32|64|er|eg|ro)|[Ll]ong(?!est|er)|[Ss]hort(?!est|er)|[Ff]loat",
            RegexOptions.Compiled);
    }
}
