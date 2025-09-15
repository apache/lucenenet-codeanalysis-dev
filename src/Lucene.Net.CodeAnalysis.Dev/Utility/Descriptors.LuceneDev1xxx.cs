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
        public static DiagnosticDescriptor LuceneDev1000_FloatingPointEquality { get; } =
            Diagnostic(
                "LuceneDev1000",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1001_FloatingPointFormatting { get; } =
            Diagnostic(
                "LuceneDev1001",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1002_FloatingPointArithmetic { get; } =
            Diagnostic(
                "LuceneDev1002",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1003_ArrayMethodParameter { get; } =
            Diagnostic(
                "LuceneDev1003",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1004_ArrayMethodReturnValue { get; } =
            Diagnostic(
                "LuceneDev1004",
                Design,
                Warning
            );
    }
}
