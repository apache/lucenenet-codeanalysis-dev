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

public class TestLuceneDev1009_PublicFieldsAnalyzer
{
    [Test]
    public async Task PublicInstanceField_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public int Value;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1009_PublicFields)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Value")
            .WithLocation(3, 16);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1009_PublicFieldsAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PublicConstField_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public const int Value = 1;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1009_PublicFieldsAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PublicStaticField_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public static int Value;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1009_PublicFieldsAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PrivateField_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                private int value;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1009_PublicFieldsAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
