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

        // 6000: Non-generic IDictionary indexer usage — may return null for missing keys
        public static readonly DiagnosticDescriptor LuceneDev6000_NonGenericDictionaryIndexer =
            Diagnostic(
                "LuceneDev6000",
                Usage,
                Info
            );

        // 6001: Missing StringComparison argument on String overload
        public static readonly DiagnosticDescriptor LuceneDev6001_MissingStringComparison =
            Diagnostic(
                "LuceneDev6001",
                Usage,
                Error
            );

        // 6002: Invalid StringComparison value on String overload (not Ordinal or OrdinalIgnoreCase)
        public static readonly DiagnosticDescriptor LuceneDev6002_InvalidStringComparison =
            Diagnostic(
                "LuceneDev6002",
                Usage,
                Error
            );

        // 6003: Redundant StringComparison.Ordinal on span-like overload
        public static readonly DiagnosticDescriptor LuceneDev6003_RedundantOrdinal =
            Diagnostic(
                "LuceneDev6003",
                Usage,
                Warning
            );

        // 6004: Invalid StringComparison value on span-like overload
        public static readonly DiagnosticDescriptor LuceneDev6004_InvalidComparison =
            Diagnostic(
                "LuceneDev6004",
                Usage,
                Error
            );

        // 6005: Single-character string argument should use the char overload
        public static readonly DiagnosticDescriptor LuceneDev6005_SingleCharString =
            Diagnostic(
                "LuceneDev6005",
                Usage,
                Info
            );
    }
}
