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

public class TestLuceneDev7000_PrivateFieldNameAnalyzer
{
    [Test]
    public async Task TestEmptyFile()
    {
        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7000_PrivateFieldNameAnalyzer())
        {
            TestCode = ""
        };
        await test.RunAsync();
    }

    [Test]
    [TestCase("value")]
    [TestCase("_value")]
    [TestCase("MAX_VALUE")]
    public async Task ValidNames_NoDiagnostic(string fieldName)
    {
        string testCode =
            $$"""
            public class MyClass
            {
                private int {{fieldName}};
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7000_PrivateFieldNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PascalCasePrivateField_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                private int FieldValue;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7000_PrivateFieldName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("FieldValue")
            .WithLocation(3, 17);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7000_PrivateFieldNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task InternalField_AlsoChecked_Diagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                internal int FieldValue;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7000_PrivateFieldName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("FieldValue")
            .WithLocation(3, 18);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7000_PrivateFieldNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task PublicField_NotChecked_NoDiagnostic()
    {
        const string testCode =
            """
            public class MyClass
            {
                public int FieldValue;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7000_PrivateFieldNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
