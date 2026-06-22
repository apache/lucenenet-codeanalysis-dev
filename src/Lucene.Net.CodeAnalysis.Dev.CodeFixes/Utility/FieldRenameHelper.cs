/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Globalization;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility;

/// <summary>
/// Helpers that compute conventional names used by the naming code fixes.
/// </summary>
internal static class FieldRenameHelper
{
    /// <summary>
    /// Computes the conventional protected field name ('m_' prefix followed by camelCase) for the
    /// given field name, or <c>null</c> when a sensible suggestion cannot be produced (e.g. the name
    /// is an UPPER_CASE constant, which is already an allowed form).
    /// </summary>
    public static string? ToProtectedFieldName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Strip any leading underscores and a leading 'm_' if already present.
        var core = name.TrimStart('_');
        if (core.StartsWith("m_", System.StringComparison.Ordinal))
            core = core.Substring(2);

        core = core.TrimStart('_');
        if (core.Length == 0)
            return null;

        // camelCase the first letter.
        var camel = char.ToLower(core[0], CultureInfo.InvariantCulture) + core.Substring(1);
        return "m_" + camel;
    }

    /// <summary>
    /// Computes the conventional interface name (capital 'I' prefix followed by PascalCase) for the
    /// given interface name, or <c>null</c> when a sensible suggestion cannot be produced.
    /// </summary>
    public static string? ToInterfaceName(string name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        // Drop leading underscores, then PascalCase the first letter.
        var core = name.TrimStart('_');
        if (core.Length == 0)
            return null;

        var pascal = char.ToUpper(core[0], CultureInfo.InvariantCulture) + core.Substring(1);

        // If it already starts with 'I' followed by an uppercase letter it is already valid.
        if (pascal.Length >= 2 && pascal[0] == 'I' && char.IsUpper(pascal[1]))
            return null;

        return "I" + pascal;
    }
}
