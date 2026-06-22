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

using System;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestForPublicMembersNamedSize: reports parameterless methods and
    /// properties named "Size" (case-insensitive). In .NET these should be "Count" or "Length".
    /// Events are not considered, matching the original test.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7006_MemberNamedSizeAnalyzer : MemberNamePredicateAnalyzerBase
    {
        public LuceneDev7006_MemberNamedSizeAnalyzer()
            : base(Descriptors.LuceneDev7006_MemberNamedSize)
        {
        }

        protected override bool IsViolation(string name) =>
            "Size".Equals(name, StringComparison.OrdinalIgnoreCase);

        // Original test only flags parameterless methods named Size.
        protected override bool IncludeMethod(IMethodSymbol method) => method.Parameters.Length == 0;

        // Original test does not consider events.
        protected override bool IncludeEvents => false;
    }
}
