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
    public class TestLuceneDev2005_2006_WrapNumericWithInvariantCodeFixProvider
    {
        [Test]
        public async Task IntPlusString_WrapsWithInvariant()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => ""x="" + i;
}";

            var fixedCode = @"using System.Globalization;

public class Sample
{
    public string M(int i) => ""x="" + i.ToString(CultureInfo.InvariantCulture);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2005_NumericStringConcatenation)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2005_NumericStringConcatenation.MessageFormat)
                .WithArguments("Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 38);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2005_NumericConcatenationAnalyzer(),
                () => new LuceneDev2005_2006_WrapNumericWithInvariantCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Wrap with .ToString(CultureInfo.InvariantCulture)",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IntInInterpolation_WrapsWithInvariant()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => $""value={i}"";
}";

            var fixedCode = @"using System.Globalization;

public class Sample
{
    public string M(int i) => $""value={i.ToString(CultureInfo.InvariantCulture)}"";
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2006_NumericStringInterpolation)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2006_NumericStringInterpolation.MessageFormat)
                .WithArguments("Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 40);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev2006_NumericInterpolationAnalyzer(),
                () => new LuceneDev2005_2006_WrapNumericWithInvariantCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected },
                CodeActionEquivalenceKey = "Wrap with .ToString(CultureInfo.InvariantCulture)",
                NumberOfIncrementalIterations = 2,
                NumberOfFixAllIterations = 2
            };
            await test.RunAsync();
        }
    }
}
