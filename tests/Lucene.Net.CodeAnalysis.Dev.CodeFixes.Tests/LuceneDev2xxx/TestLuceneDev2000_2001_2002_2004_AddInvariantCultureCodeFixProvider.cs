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

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev2xxx;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev2xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev2xxx
{
    [TestFixture]
    public class TestLuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider
    {
        [Test]
        public async Task IntParse_AddsInvariantCulture()
        {
            var testCode = @"
public class Sample
{
    public int M() => int.Parse(""1"");
}";

            var fixedCode = @"using System.Globalization;

public class Sample
{
    public int M() => int.Parse(""1"", CultureInfo.InvariantCulture);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.MessageFormat)
                .WithArguments("Parse", "Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 27);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2000_BclNumericParseAnalyzer(),
                () => new LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Add CultureInfo.InvariantCulture",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntToString_AddsInvariantCulture()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => i.ToString();
}";

            var fixedCode = @"using System.Globalization;

public class Sample
{
    public string M(int i) => i.ToString(CultureInfo.InvariantCulture);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2001_BclNumericToStringMissingFormatProvider.MessageFormat)
                .WithArguments("ToString", "Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 33);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2001_BclNumericToStringAnalyzer(),
                () => new LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Add CultureInfo.InvariantCulture",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntParse_PreservesCrlfLineEndings()
        {
            // Repo policy is `*.cs eol=crlf`. The codefix must emit an inserted `using`
            // directive with the same line ending as the rest of the document, otherwise
            // CI (which checks out CRLF) and dev machines (which may not) disagree.
            var testCode = "\r\npublic class Sample\r\n{\r\n    public int M() => int.Parse(\"1\");\r\n}";
            var fixedCode = "using System.Globalization;\r\n\r\npublic class Sample\r\n{\r\n    public int M() => int.Parse(\"1\", CultureInfo.InvariantCulture);\r\n}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.MessageFormat)
                .WithArguments("Parse", "Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 27);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2000_BclNumericParseAnalyzer(),
                () => new LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Add CultureInfo.InvariantCulture",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }

        [Test]
        public async Task DoubleTryParse_InsertsInvariantCultureBeforeOutArg()
        {
            // Regression: TryParse signature is (string, IFormatProvider, out T) — the provider
            // must be inserted BEFORE the trailing `out` parameter, not appended at the end.
            var testCode = @"
public class Sample
{
    public bool M() => double.TryParse(""1.5"", out _);
}";

            var fixedCode = @"using System.Globalization;

public class Sample
{
    public bool M() => double.TryParse(""1.5"", CultureInfo.InvariantCulture, out _);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2000_BclNumericParseMissingFormatProvider.MessageFormat)
                .WithArguments("TryParse", "Double")
                .WithLocation("/0/Test0.cs", line: 4, column: 31);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2000_BclNumericParseAnalyzer(),
                () => new LuceneDev2000_2001_2002_2004_AddInvariantCultureCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Add CultureInfo.InvariantCulture",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }
    }
}
