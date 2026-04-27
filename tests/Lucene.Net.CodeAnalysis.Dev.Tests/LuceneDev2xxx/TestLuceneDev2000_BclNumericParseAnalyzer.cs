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
    public class TestLuceneDev2000_BclNumericParseAnalyzer
    {
        [Test]
        public async Task EmptyFile_NoDiagnostic()
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2000_BclNumericParseAnalyzer())
            {
                TestCode = string.Empty
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntParse_String_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public void M()
    {
        var x = int.Parse(""1"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.MessageFormat)
                .WithArguments("Parse", "Int32")
                .WithLocation("/0/Test0.cs", line: 6, column: 21);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2000_BclNumericParseAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task DoubleTryParse_String_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public void M()
    {
        double.TryParse(""1.5"", out _);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.MessageFormat)
                .WithArguments("TryParse", "Double")
                .WithLocation("/0/Test0.cs", line: 6, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2000_BclNumericParseAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntParse_WithProvider_NoDiagnostic()
        {
            var testCode = @"
using System.Globalization;

public class Sample
{
    public void M()
    {
        var x = int.Parse(""1"", CultureInfo.InvariantCulture);
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2000_BclNumericParseAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }

        [Test]
        public async Task GuidParse_NoDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        var g = Guid.Parse(""00000000-0000-0000-0000-000000000000"");
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2000_BclNumericParseAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }
    }
}
