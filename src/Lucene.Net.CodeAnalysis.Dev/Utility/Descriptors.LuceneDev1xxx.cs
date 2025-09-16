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
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Lucene.Net.CodeAnalysis.Dev.Utility.Category;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    public static partial class Descriptors
    {
        // IMPORTANT: Do not make these into properties! The AnalyzerReleases release management
        // analyzers do not recognize them and will report RS2002 warnings if it cannot read the
        // DignosticDescriptor instance through a field.

        public static readonly DiagnosticDescriptor LuceneDev1000_FloatingPointEquality =
            Diagnostic(
                "LuceneDev1000",
                Design,
                Warning
            );

        public static readonly DiagnosticDescriptor LuceneDev1001_FloatingPointFormatting =
            Diagnostic(
                "LuceneDev1001",
                Design,
                Warning
            );

        public static readonly DiagnosticDescriptor LuceneDev1002_FloatingPointArithmetic =
            Diagnostic(
                "LuceneDev1002",
                Design,
                Warning
            );

        public static readonly DiagnosticDescriptor LuceneDev1003_ArrayMethodParameter =
            Diagnostic(
                "LuceneDev1003",
                Design,
                Warning
            );

        public static readonly DiagnosticDescriptor LuceneDev1004_ArrayMethodReturnValue =
            Diagnostic(
                "LuceneDev1004",
                Design,
                Warning
            );
    }
}
