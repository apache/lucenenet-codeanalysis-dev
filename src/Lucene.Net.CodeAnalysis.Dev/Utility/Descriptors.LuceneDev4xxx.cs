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

        // 4000: [MethodImpl(MethodImplOptions.NoInlining)] on an interface or abstract method
        public static readonly DiagnosticDescriptor LuceneDev4000_NoInliningHasNoEffect =
            Diagnostic(
                "LuceneDev4000",
                Performance,
                Warning
            );

        // 4001: [MethodImpl(MethodImplOptions.NoInlining)] on an empty-bodied method
        public static readonly DiagnosticDescriptor LuceneDev4001_NoInliningOnEmptyMethod =
            Diagnostic(
                "LuceneDev4001",
                Performance,
                Warning
            );

        // 4002: Method referenced by StackTraceHelper.DoesStackTraceContainMethod (2-arg)
        // is missing [MethodImpl(MethodImplOptions.NoInlining)]
        public static readonly DiagnosticDescriptor LuceneDev4002_MissingNoInlining =
            Diagnostic(
                "LuceneDev4002",
                Performance,
                Warning
            );
    }
}
