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

public class TestLuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer
{
    [Test]
    public async Task MethodReturnsList_Diagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                public List<int> GetItems() => null;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1015_MemberAcceptsOrReturnsListOrDictionary)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("GetItems", "List")
            .WithLocation(4, 22);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task MethodAcceptsDictionaryParameter_Diagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                public void SetItems(Dictionary<int, string> items) { }
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1015_MemberAcceptsOrReturnsListOrDictionary)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("items", "Dictionary")
            .WithLocation(4, 50);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer())
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

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PrivateMethodReturnsList_NoDiagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                private List<int> GetItems() => null;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PropertyWithOnlyNonPublicAccessor_NoDiagnostic()
    {
        // The original IsNonPrivateProperty requires a public accessor; a property whose only
        // accessors are internal/private must not be flagged.
        const string testCode =
            """
            using System.Collections.Generic;
            public class MyClass
            {
                internal List<int> Items { get; set; }
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1015_MemberAcceptsOrReturnsListOrDictionaryAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
