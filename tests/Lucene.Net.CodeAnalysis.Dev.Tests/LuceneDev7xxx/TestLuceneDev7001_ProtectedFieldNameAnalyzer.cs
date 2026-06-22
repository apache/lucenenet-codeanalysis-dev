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

public class TestLuceneDev7001_ProtectedFieldNameAnalyzer
{
    [Test]
    [TestCase("m_value")]
    [TestCase("MAX_VALUE")]
    public async Task ValidNames_NoDiagnostic(string fieldName)
    {
        string testCode =
            $$"""
            public class MyClass
            {
                protected int {{fieldName}};
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7001_ProtectedFieldNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ProtectedFieldWithoutPrefix_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                protected int count;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7001_ProtectedFieldName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("count")
            .WithLocation(3, 19);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7001_ProtectedFieldNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ProtectedInternalField_AlsoChecked_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                protected internal int count;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7001_ProtectedFieldName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("count")
            .WithLocation(3, 28);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7001_ProtectedFieldNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PrivateField_NotChecked_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                private int count;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7001_ProtectedFieldNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ProtectedFieldInSupportNamespace_Skipped_NoDiagnostic()
    {
        const string testCode =
            """
            namespace Lucene.Net.Support
            {
                public class MyClass
                {
                    protected int count;
                }
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7001_ProtectedFieldNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
