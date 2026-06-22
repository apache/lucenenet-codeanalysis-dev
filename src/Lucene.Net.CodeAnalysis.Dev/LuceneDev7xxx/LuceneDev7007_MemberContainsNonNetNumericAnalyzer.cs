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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx
{
    /// <summary>
    /// Ports TestApiConsistency.TestForPublicMembersContainingNonNetNumeric: reports ordinary
    /// methods, properties and events whose name contains a Java-style numeric term ('Int' not
    /// followed by 16/32/64, 'Long', 'Short' or 'Float'). Members decorated with
    /// [ExceptionToNetNumericConvention] are excluded.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev7007_MemberContainsNonNetNumericAnalyzer : MemberNamePredicateAnalyzerBase
    {
        public LuceneDev7007_MemberContainsNonNetNumericAnalyzer()
            : base(Descriptors.LuceneDev7007_MemberContainsNonNetNumeric)
        {
        }

        protected override bool IsViolation(string name) =>
            NamingHelper.ContainsNonNetNumeric.IsMatch(name);

        protected override bool ShouldSkipMember(ISymbol member) =>
            ApiConventionHelper.HasAttribute(member, ApiConventionHelper.ExceptionToNetNumericConventionAttribute);
    }
}
