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
    public class TestLuceneDev2005_NumericConcatenationAnalyzer
    {
        [Test]
        public async Task IntPlusString_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public string M(int i) => ""x="" + i;
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2005_NumericStringConcatenation)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2005_NumericStringConcatenation.MessageFormat)
                .WithArguments("Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 38);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2005_NumericConcatenationAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task EmptyStringPlusInt_ReportsDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public string M(int a, int b) => """" + (a + b);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2005_NumericStringConcatenation)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2005_NumericStringConcatenation.MessageFormat)
                .WithArguments("Int32")
                .WithLocation("/0/Test0.cs", line: 4, column: 43);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2005_NumericConcatenationAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task StringPlusString_NoDiagnostic()
        {
            var testCode = @"
public class Sample
{
    public string M(string a, string b) => a + b;
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2005_NumericConcatenationAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }
    }
}
