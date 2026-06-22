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
using Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev;

public class TestLuceneDev7005_MemberContainsComparerAnalyzer
{
    [Test]
    public async Task MethodNamedComparer_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public int GetComparer() => 0;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7005_MemberContainsComparerAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task MethodContainingComparator_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public int GetComparator() => 0;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7005_MemberContainsComparer)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("GetComparator")
            .WithLocation(3, 16);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7005_MemberContainsComparerAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task TypeNamedComparator_Diagnostic()
    {
        const string testCode =
            """
            public class MyComparator
            {
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7005_MemberContainsComparer)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("MyComparator")
            .WithLocation(1, 14);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7005_MemberContainsComparerAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ProtectedFieldContainingComparator_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                protected int m_comparatorState;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7005_MemberContainsComparer)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("m_comparatorState")
            .WithLocation(3, 19);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7005_MemberContainsComparerAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }
}
