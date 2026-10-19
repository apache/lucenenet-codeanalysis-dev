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
    public class TestLuceneDev6005_SingleCharStringCodeFixProvider
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
        int index = text.IndexOf({|#0:""H""|});
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"H\"")
                .WithLocation(0);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6005_SingleCharStringAnalyzer(),
                () => new LuceneDev6005_SingleCharStringCodeFixProvider())
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
        int index = text.IndexOf({|#0:""\""""|});
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"\\\"\"")
                .WithLocation(0);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6005_SingleCharStringAnalyzer(),
                () => new LuceneDev6005_SingleCharStringCodeFixProvider())
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
        int i1 = text.IndexOf({|#0:""H""|});
        int i2 = text.IndexOf({|#1:""\n""|});
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

            var expected1 = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"H\"")
                .WithLocation(0);

            var expected2 = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"\\n\"")
                .WithLocation(1);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6005_SingleCharStringAnalyzer(),
                () => new LuceneDev6005_SingleCharStringCodeFixProvider())
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
        int index = span.IndexOf({|#0:""X""|});
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev6005_SingleCharString)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithArguments("IndexOf", "\"X\"")
                .WithLocation(0);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6005_SingleCharStringAnalyzer(),
                () => new LuceneDev6005_SingleCharStringCodeFixProvider())
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
                () => new LuceneDev6005_SingleCharStringAnalyzer(),
                () => new LuceneDev6005_SingleCharStringCodeFixProvider())
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
