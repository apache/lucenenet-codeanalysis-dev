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

public class TestLuceneDev1012_MethodReturnsWritableArrayAnalyzer
{
    [Test]
    public async Task ReturnsFieldDirectly_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                private readonly int[] m_values = new int[1];
                public int[] GetValues()
                {
                    return m_values;
                }
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1012_MethodReturnsWritableArray)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("GetValues")
            .WithLocation(4, 18);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1012_MethodReturnsWritableArrayAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ReturnsFieldDirectly_ExpressionBodied_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                private readonly int[] m_values = new int[1];
                public int[] GetValues() => m_values;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1012_MethodReturnsWritableArray)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("GetValues")
            .WithLocation(4, 18);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1012_MethodReturnsWritableArrayAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ReturnsClonedArray_NoDiagnostic()
    {
        const string testCode =
            """
            using System.Linq;
            public class MyClass
            {
                private readonly int[] m_values = new int[1];
                public int[] GetValues() => m_values.ToArray();
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1012_MethodReturnsWritableArrayAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task ReturnsNewArray_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public int[] GetValues() => new int[1];
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1012_MethodReturnsWritableArrayAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
