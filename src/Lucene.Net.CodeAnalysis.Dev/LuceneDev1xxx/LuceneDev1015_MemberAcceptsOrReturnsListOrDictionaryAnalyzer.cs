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

using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestForMembersAcceptingOrReturningListOrDictionary: reports
    /// non-private members (methods, constructors, properties) that accept or return
    /// <c>List&lt;T&gt;</c> or <c>Dictionary&lt;K, V&gt;</c>; these should be exposed as
    /// <c>IList&lt;T&gt;</c> and <c>IDictionary&lt;K, V&gt;</c> respectively.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer : MembersUsingTypeAnalyzerBase
    {
        private const string ListMetadataName = "System.Collections.Generic.List`1";
        private const string DictionaryMetadataName = "System.Collections.Generic.Dictionary`2";

        public LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer()
            // Original test passes publiclyVisibleOnly: true (non-private members only).
            : base(Descriptors.LuceneDev1015_MemberAcceptsOrReturnsListOrDictionary, publiclyVisibleOnly: true)
        {
        }

        protected override bool IsTargetType(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named)
                return false;

            // Compare the unbound generic definition by its full metadata name, which is stable
            // across type arguments (e.g. "System.Collections.Generic.List`1").
            var metadataName = named.OriginalDefinition.ContainingNamespace?.ToDisplayString() + "." + named.OriginalDefinition.MetadataName;
            return metadataName == ListMetadataName || metadataName == DictionaryMetadataName;
        }

        protected override string DescribeType(ITypeSymbol type) =>
            type.OriginalDefinition.Name; // "List" or "Dictionary"
    }
}
