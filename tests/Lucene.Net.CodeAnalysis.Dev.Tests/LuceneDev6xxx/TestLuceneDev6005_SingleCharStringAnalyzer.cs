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
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6005_SingleCharStringAnalyzer
    {
        [Test]
        public async Task Detects_SingleCharacter_StringLiteral()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""H"");
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithSpan(9, 34, 9, 37)
                .WithArguments("IndexOf", "\"H\"");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6005_SingleCharStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_EscapedCharacter_StringLiteral()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""\"""");  // Added missing semicolon
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithSpan(9, 34, 9, 38)
                .WithArguments("IndexOf", "\"\\\"\"");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6005_SingleCharStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_For_MultiCharacterString()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""He"");
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6005_SingleCharStringAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
