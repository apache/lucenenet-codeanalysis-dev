/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6003_6004_SpanComparisonCodeFixProvider
    {
        [Test]
        public async Task TestFix_RemoveRedundantOrdinal()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"", StringComparison.Ordinal);
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_RedundantOrdinal)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(9, 42, 9, 66)
                .WithArguments("IndexOf");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_6004_SpanComparisonAnalyzer(),
                () => new LuceneDev6003_6004_SpanComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_InvalidToOptimalRemoval_CaseSensitive()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"", StringComparison.CurrentCulture);
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(9, 42, 9, 73)
                .WithArguments("IndexOf", "CurrentCulture");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_6004_SpanComparisonAnalyzer(),
                () => new LuceneDev6003_6004_SpanComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                // The new logic offers "Optimize to default Ordinal" as CodeActionIndex = 0
                CodeActionIndex = 0,
                // CRITICAL FIX: The smarter fix takes only 1 iteration now.
                NumberOfFixAllIterations = 1
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_InvalidToOptimalOrdinalIgnoreCase_CaseInsensitive()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"", StringComparison.CurrentCultureIgnoreCase);
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"", StringComparison.OrdinalIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithSpan(9, 42, 9, 83)
                .WithArguments("IndexOf", "CurrentCultureIgnoreCase");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_6004_SpanComparisonAnalyzer(),
                () => new LuceneDev6003_6004_SpanComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                // The new logic should offer "OrdinalIgnoreCase" as CodeActionIndex = 0 for case-insensitive inputs
                CodeActionIndex = 0,
                // The fixed code does not trigger RedundantOrdinal, so 1 iteration is sufficient.
                NumberOfFixAllIterations = 1
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_RemoveRedundantOrdinal_Simple()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hi World"".AsSpan();
        int index = span.IndexOf(""x"", StringComparison.Ordinal);
    }
}";

            var fixedCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hi World"".AsSpan();
        int index = span.IndexOf(""x"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_RedundantOrdinal)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(9, 39, 9, 63)
                .WithArguments("IndexOf");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_6004_SpanComparisonAnalyzer(),
                () => new LuceneDev6003_6004_SpanComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_For_CharOverload()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        Span<char> span = stackalloc char[5];
        int index = span.IndexOf('t');
    }
}";

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6003_6004_SpanComparisonAnalyzer(),
                () => new LuceneDev6003_6004_SpanComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = testCode,
                ExpectedDiagnostics = { } // no diagnostics expected
            };

            await test.RunAsync();
        }
    }
}
