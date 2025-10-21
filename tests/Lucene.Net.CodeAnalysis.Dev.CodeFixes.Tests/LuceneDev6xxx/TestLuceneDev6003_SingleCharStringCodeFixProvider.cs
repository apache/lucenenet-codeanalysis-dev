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
    public class TestLuceneDev6003_SingleCharStringCodeFixProvider
    {
        [Test]
        public async Task Fix_SingleCharacter_StringLiteral()
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

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf('H');
    }
}";

            // "H" starts at column 39 and ends at column 42 (3 chars wide)
            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_SingleCharStringAnalyzer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"H\"")
                .WithSpan(10, 39, 10, 42);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_SingleCharStringAnalyzer(),
                () => new LuceneDev6003_SingleCharStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionIndex = 0
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_EscapedCharacter_StringLiteral()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""\"""");
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf('""');
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_SingleCharStringAnalyzer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"\\\"\"")
                .WithSpan(10, 39, 10, 43);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_SingleCharStringAnalyzer(),
                () => new LuceneDev6003_SingleCharStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionIndex = 0
            };

            await test.RunAsync();
        }

        [Test]
        public async Task FixAll_SingleCharacterStringLiterals()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int i1 = text.IndexOf(""H"");
        int i2 = text.IndexOf(""\n"");
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int i1 = text.IndexOf('H');
        int i2 = text.IndexOf('\n');
    }
}";

            // First: "H" (line 10, columns 38–41 → 3 chars)
            var expected1 = new DiagnosticResult(Descriptors.LuceneDev6003_SingleCharStringAnalyzer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"H\"")
                .WithSpan(10, 38, 10, 41);

            // Second: "\n" (line 11, columns 38–42 → 4 chars)
            var expected2 = new DiagnosticResult(Descriptors.LuceneDev6003_SingleCharStringAnalyzer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"\\n\"")
                .WithSpan(11, 38, 11, 42);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_SingleCharStringAnalyzer(),
                () => new LuceneDev6003_SingleCharStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected1, expected2 },
                CodeActionIndex = 0
            };

            await test.RunAsync();
        }
        [Test]
        public async Task Fix_Span_IndexOf_SingleCharacter()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M(ReadOnlySpan<char> span)
    {
        int index = span.IndexOf(""X"");
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M(ReadOnlySpan<char> span)
    {
        int index = span.IndexOf('X');
    }
}";

            // "X" starts at column 30 and ends at column 33 (3 chars wide)
            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_SingleCharStringAnalyzer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"X\"")
                .WithSpan(9, 30, 9, 33);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_SingleCharStringAnalyzer(),
                () => new LuceneDev6003_SingleCharStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionIndex = 0
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoFix_Span_StartsWith_SingleCharacter()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M(ReadOnlySpan<char> span)
    {
        bool starts = span.StartsWith(""X"");
    }
}";

            // This test expects NO diagnostic, ensuring the Analyzer correctly skips
            // ReadOnlySpan.StartsWith/EndsWith calls when the argument is a single-character string literal.
            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_SingleCharStringAnalyzer(),
                () => new LuceneDev6003_SingleCharStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = testCode, // Fixed code is the same as test code
                ExpectedDiagnostics = { },
                CodeActionIndex = 0
            };

            await test.RunAsync();
        }
    }
}
