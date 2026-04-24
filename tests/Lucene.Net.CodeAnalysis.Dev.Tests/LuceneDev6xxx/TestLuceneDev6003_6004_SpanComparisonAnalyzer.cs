/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to you under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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
    public class TestLuceneDev6003_6004_SpanComparisonAnalyzer
    {
        [Test]
        public async Task Detects_RedundantOrdinal_OnReadOnlySpan_IndexOf()
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_RedundantOrdinal)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("IndexOf")
                .WithSpan(9, 42, 9, 66);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_RedundantOrdinal_OnSpan_StartsWith()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = stackalloc char[5];
        bool starts = span.StartsWith(""test"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6003_RedundantOrdinal)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("StartsWith")
                .WithSpan(9, 47, 9, 71);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }


        [Test]
        public async Task Detects_InvalidComparison_CurrentCulture()
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("IndexOf", "CurrentCulture")
                .WithSpan(9, 42, 9, 73);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidComparison_CurrentCultureIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.LastIndexOf(""test"", StringComparison.CurrentCultureIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("LastIndexOf", "CurrentCultureIgnoreCase")
                .WithSpan(9, 46, 9, 87);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidComparison_InvariantCulture()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        bool ends = span.EndsWith(""test"", StringComparison.InvariantCulture);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("EndsWith", "InvariantCulture")
                .WithSpan(9, 43, 9, 76);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidComparison_InvariantCultureIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        bool ends = span.EndsWith(""test"", StringComparison.InvariantCultureIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("EndsWith", "InvariantCultureIgnoreCase")
                .WithSpan(9, 43, 9, 86);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoWarning_WithoutStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""test"");
        bool starts = span.StartsWith(""Hello"");
        bool ends = span.EndsWith(""lo"");
        int lastIndex = span.LastIndexOf(""ll"");
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoWarning_WithOrdinalIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""TEST"", StringComparison.OrdinalIgnoreCase);
        bool starts = span.StartsWith(""HELLO"", StringComparison.OrdinalIgnoreCase);
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoWarning_OnStringType()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        // String types are handled by LuceneDev6001, not 6002
        int index = text.IndexOf(""test"", StringComparison.Ordinal);
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
        [Test]
        public async Task NoWarning_CharOverloads()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index = span.IndexOf(""H"");
        bool starts = span.StartsWith(""H"");
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_MultipleViolations()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        ReadOnlySpan<char> span = ""Hello"".AsSpan();
        int index1 = span.IndexOf(""test"", StringComparison.Ordinal);
        int index2 = span.LastIndexOf(""test"", StringComparison.CurrentCulture);
    }
}";

            var expected1 = new DiagnosticResult(Descriptors.LuceneDev6003_RedundantOrdinal)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("IndexOf")
                .WithSpan(9, 43, 9, 67);

            var expected2 = new DiagnosticResult(Descriptors.LuceneDev6004_InvalidComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments("LastIndexOf", "CurrentCulture")
                .WithSpan(10, 47, 10, 78);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6003_6004_SpanComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected1, expected2 }
            };

            await test.RunAsync();
        }
    }
}
