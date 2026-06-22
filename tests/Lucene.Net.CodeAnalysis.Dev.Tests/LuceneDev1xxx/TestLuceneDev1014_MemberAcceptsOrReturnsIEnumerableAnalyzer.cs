/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev;

// Note: LuceneDev1014 is disabled by default, but the analyzer test harness runs the analyzer
// directly, so its diagnostics are still observed here.
public class TestLuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer
{
    [Test]
    public async Task MethodReturnsIEnumerable_Diagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                public IEnumerable<int> GetItems() => null;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1014_MemberAcceptsOrReturnsIEnumerable)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("GetItems")
            .WithLocation(4, 29);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task MethodAcceptsIEnumerableParameter_Diagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                public void SetItems(IEnumerable<int> items) { }
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1014_MemberAcceptsOrReturnsIEnumerable)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("items")
            .WithLocation(4, 43);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task MethodReturnsIList_NoDiagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                public IList<int> GetItems() => null;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1014_MemberAcceptsOrReturnsIEnumerableAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
