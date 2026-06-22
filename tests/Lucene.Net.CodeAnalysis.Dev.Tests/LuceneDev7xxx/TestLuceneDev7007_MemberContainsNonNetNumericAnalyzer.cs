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

public class TestLuceneDev7007_MemberContainsNonNetNumericAnalyzer
{
    [Test]
    [TestCase("ReadInt32")]
    [TestCase("Intern")]
    [TestCase("GetPoint")]
    public async Task NetNumericOrInnocuousNames_NoDiagnostic(string methodName)
    {
        string testCode =
            $$"""
            public class MyClass
            {
                public int {{methodName}}() => 0;
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7007_MemberContainsNonNetNumericAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    [TestCase("ReadLong", 16)]
    [TestCase("ReadInt", 16)]
    [TestCase("ReadFloat", 16)]
    [TestCase("ReadShort", 16)]
    public async Task NonNetNumericMethodName_Diagnostic(string methodName, int column)
    {
        string testCode =
            $$"""
            public class MyClass
            {
                public int {{methodName}}() => 0;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7007_MemberContainsNonNetNumeric)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(methodName)
            .WithLocation(3, column);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7007_MemberContainsNonNetNumericAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }
}
