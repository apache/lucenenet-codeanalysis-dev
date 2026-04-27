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

using Lucene.Net.CodeAnalysis.Dev.LuceneDev2xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev2xxx
{
    [TestFixture]
    public class TestLuceneDev2001_BclNumericToStringAnalyzer
    {
        [Test]
        public async Task IntToString_Parameterless_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => i.ToString();
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider.MessageFormat)
                .WithArguments("ToString", "Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntToString_FormatStringOnly_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => i.ToString(""D"");
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider.MessageFormat)
                .WithArguments("ToString", "Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntTryFormat_WithoutProvider_ReportsDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public bool M(int i, Span<char> buffer) => i.TryFormat(buffer, out _);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider.MessageFormat)
                .WithArguments("TryFormat", "Int32")
                .WithLocation("/0/Test0.cs", line: 6, column: 50);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntTryFormat_WithProvider_NoDiagnostic()
        {
            var testCode = @"
using System;
using System.Globalization;

public class Sample
{
    public bool M(int i, Span<char> buffer) => i.TryFormat(buffer, out _, provider: CultureInfo.InvariantCulture);
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntToString_WithProvider_NoDiagnostic()
        {
            var testCode = @"
using System.Globalization;

public class Sample
{
    public string M(int i) => i.ToString(CultureInfo.InvariantCulture);
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntToString_InsideToStringOverride_NoDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public int Value { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }

        [Test]
        public async Task EnumToString_NoDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public string M(DayOfWeek d) => d.ToString();
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2001_BclNumericToStringAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }
    }
}
