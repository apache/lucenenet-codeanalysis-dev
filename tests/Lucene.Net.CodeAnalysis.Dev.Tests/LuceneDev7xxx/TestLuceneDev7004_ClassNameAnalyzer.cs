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

public class TestLuceneDev7004_ClassNameAnalyzer
{
    [Test]
    public async Task PascalCaseClassName_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7004_ClassNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ClassNameLikeInterface_Diagnostic()
    {
        const string testCode =
            """
            public class IMyClass
            {
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7004_ClassName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("IMyClass")
            .WithLocation(1, 14);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7004_ClassNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task LowerCaseClassName_Diagnostic()
    {
        const string testCode =
            """
            public class myClass
            {
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7004_ClassName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("myClass")
            .WithLocation(1, 14);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7004_ClassNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task DelegateNameLikeInterface_Diagnostic()
    {
        // Delegates are CLR classes (reflection t.IsClass), so they follow the class-name rule.
        const string testCode =
            """
            public delegate void IMyHandler();
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7004_ClassName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("IMyHandler")
            .WithLocation(1, 22);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7004_ClassNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ValidDelegateName_NoDiagnostic()
    {
        const string testCode =
            """
            public delegate void MyHandler();
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7004_ClassNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
