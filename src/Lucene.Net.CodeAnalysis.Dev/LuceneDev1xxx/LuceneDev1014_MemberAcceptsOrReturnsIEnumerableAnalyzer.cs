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
    /// Ports TestApiConsistency.TestForMembersAcceptingOrReturningIEnumerable: reports members
    /// (methods, constructors, properties) that accept or return <c>IEnumerable&lt;T&gt;</c>. This
    /// rule was a one-time porting aid and is disabled by default.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer : MembersUsingTypeAnalyzerBase
    {
        public LuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer()
            // Original test passes publiclyVisibleOnly: false (all members are scanned).
            : base(Descriptors.LuceneDev1014_MemberAcceptsOrReturnsIEnumerable, publiclyVisibleOnly: false)
        {
        }

        protected override bool IsTargetType(ITypeSymbol type) =>
            type is INamedTypeSymbol named
            && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;
    }
}
