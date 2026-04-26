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
 * distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS
 * OF ANY KIND, either express or implied.  See the License for
 * the specific language governing permissions and limitations
 * under the License.
 */

using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Lucene.Net.CodeAnalysis.Dev.Utility.Category;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    public static partial class Descriptors
    {
        // IMPORTANT: Do not make these into properties!
        // The AnalyzerReleases release management analyzers do not recognize them
        // and will report RS2002 warnings if it cannot read the DiagnosticDescriptor
        // instance through a field.

        // 2000: BCL numeric Parse/TryParse without IFormatProvider
        public static readonly DiagnosticDescriptor LuceneDev2000_BclNumericParseMissingFormatProvider =
            Diagnostic(
                "LuceneDev2000",
                Globalization,
                Warning
            );

        // 2001: BCL numeric ToString/TryFormat without IFormatProvider
        public static readonly DiagnosticDescriptor LuceneDev2001_BclNumericToStringMissingFormatProvider =
            Diagnostic(
                "LuceneDev2001",
                Globalization,
                Warning
            );

        // 2002: System.Convert numeric to/from string without IFormatProvider
        public static readonly DiagnosticDescriptor LuceneDev2002_ConvertNumericMissingFormatProvider =
            Diagnostic(
                "LuceneDev2002",
                Globalization,
                Warning
            );

        // 2003: string.Format without IFormatProvider where any argument is numeric
        public static readonly DiagnosticDescriptor LuceneDev2003_StringFormatNumericMissingFormatProvider =
            Diagnostic(
                "LuceneDev2003",
                Globalization,
                Warning
            );

        // 2004: J2N.Numerics.* member without IFormatProvider
        public static readonly DiagnosticDescriptor LuceneDev2004_J2NNumericMissingFormatProvider =
            Diagnostic(
                "LuceneDev2004",
                Globalization,
                Warning
            );

        // 2005: Implicit numeric formatting via string concatenation
        public static readonly DiagnosticDescriptor LuceneDev2005_NumericStringConcatenation =
            Diagnostic(
                "LuceneDev2005",
                Globalization,
                Warning
            );

        // 2006: Implicit numeric formatting via string interpolation
        public static readonly DiagnosticDescriptor LuceneDev2006_NumericStringInterpolation =
            Diagnostic(
                "LuceneDev2006",
                Globalization,
                Warning
            );

        // 2007: Explicit IFormatProvider passed to numeric API, but it is not InvariantCulture
        public static readonly DiagnosticDescriptor LuceneDev2007_NumericNonInvariantFormatProvider =
            Diagnostic(
                "LuceneDev2007",
                Globalization,
                Warning
            );

        // 2008: Explicit IFormatProvider passed to numeric API, and it IS InvariantCulture
        // (review-sweep aid; off by default)
        public static readonly DiagnosticDescriptor LuceneDev2008_NumericInvariantFormatProvider =
            Diagnostic(
                "LuceneDev2008",
                Globalization,
                Info,
                isEnabledByDefault: false
            );
    }
}
