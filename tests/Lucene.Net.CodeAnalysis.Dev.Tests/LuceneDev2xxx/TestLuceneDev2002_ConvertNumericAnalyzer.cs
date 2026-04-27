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
    public class TestLuceneDev2002_ConvertNumericAnalyzer
    {
        [Test]
        public async Task ConvertToInt32_String_ReportsDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public int M() => Convert.ToInt32(""1"");
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider.MessageFormat)
                .WithArguments("ToInt32")
                .WithLocation("/0/Test0.cs", line: 6, column: 31);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2002_ConvertNumericAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task ConvertToString_Numeric_ReportsDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public string M(int i) => Convert.ToString(i);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev2002_ConvertNumericMissingFormatProvider.MessageFormat)
                .WithArguments("ToString")
                .WithLocation("/0/Test0.cs", line: 6, column: 39);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2002_ConvertNumericAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task ConvertToString_Bool_NoDiagnostic()
        {
            var testCode = @"
using System;

public class Sample
{
    public string M(bool b) => Convert.ToString(b);
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2002_ConvertNumericAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }

        [Test]
        public async Task ConvertToInt32_WithProvider_NoDiagnostic()
        {
            var testCode = @"
using System;
using System.Globalization;

public class Sample
{
    public int M() => Convert.ToInt32(""1"", CultureInfo.InvariantCulture);
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev2002_ConvertNumericAnalyzer())
            {
                TestCode = testCode
            };
            await test.RunAsync();
        }
    }
}
