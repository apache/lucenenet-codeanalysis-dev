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
    public class TestLuceneDev2007_2008_NumericExplicitCultureAnalyzer
    {
        [Test]
        public async Task NonInvariantCulture_Reports2007()
        {
            var testCode = @"
using System.Globalization;

public class Sample
{
    public string M(int i) => i.ToString(CultureInfo.CurrentCulture);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider.MessageFormat)
                .WithArguments("ToString")
                .WithLocation("/0/Test0.cs", line: 6, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2007_2008_NumericExplicitCultureAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task InvariantCulture_Reports2008WhenEnabled()
        {
            // 2008 is disabled by default in production but the analyzer test framework
            // enables every supported diagnostic of an injected analyzer regardless,
            // so we verify here that the analyzer raises it on InvariantCulture call sites.
            var testCode = @"
using System.Globalization;

public class Sample
{
    public string M(int i) => i.ToString(CultureInfo.InvariantCulture);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2008_NumericInvariantFormatProvider)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithMessageFormat(Descriptors.LuceneDev2008_NumericInvariantFormatProvider.MessageFormat)
                .WithArguments("ToString")
                .WithLocation("/0/Test0.cs", line: 6, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2007_2008_NumericExplicitCultureAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NullProviderLiteral_Reports2007()
        {
            // Regression: passing `null` for IFormatProvider should be treated as a non-invariant
            // explicit provider (it's effectively current-culture). The argument's TypeInfo.Type
            // is null for the `null` literal, so detection must fall back to ConvertedType.
            var testCode = @"
public class Sample
{
    public string M(int i) => i.ToString((string)null, null);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider.MessageFormat)
                .WithArguments("ToString")
                .WithLocation("/0/Test0.cs", line: 4, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2007_2008_NumericExplicitCultureAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task IsEnabledByDefault_2007True_2008False()
        {
            Assert.That(Descriptors.LuceneDev2007_NumericNonInvariantFormatProvider.IsEnabledByDefault, Is.True);
            Assert.That(Descriptors.LuceneDev2008_NumericInvariantFormatProvider.IsEnabledByDefault, Is.False);
            await Task.CompletedTask;
        }
    }
}
