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

        // 6001: Missing StringComparison argument
        public static readonly DiagnosticDescriptor LuceneDev6001_MissingStringComparison =
            Diagnostic(
                "LuceneDev6001_1",
                Usage,
                Error
            );

        // 6001: Invalid StringComparison value (not Ordinal or OrdinalIgnoreCase)
        public static readonly DiagnosticDescriptor LuceneDev6001_InvalidStringComparison =
            Diagnostic(
                "LuceneDev6001_2",
                Usage,
                Warning
            );

        // 6002: Redundant Ordinal (StringComparison.Ordinal on span-like)
        public static readonly DiagnosticDescriptor LuceneDev6002_RedundantOrdinal =
            Diagnostic(
                "LuceneDev6002_1",
                Usage,
                Warning
            );

        // 6002: Invalid comparison on span (e.g., CurrentCulture, InvariantCulture)
        public static readonly DiagnosticDescriptor LuceneDev6002_InvalidComparison =
            Diagnostic(
                "LuceneDev6002_2",
                Usage,
                Error
            );
        public static readonly DiagnosticDescriptor LuceneDev6003_SingleCharStringAnalyzer =
            Diagnostic(
                "LuceneDev6003",
                Usage,
                Info
            );
    }
}
