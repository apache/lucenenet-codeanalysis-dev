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

using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev
{
    public class TestLuceneDev1001_FloatingPointFormattingCSCodeAnalyzer
    {
        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            var testCode = @"";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_Float_ToString()
        {
            var testCode = @"
        using System;
        using System.Collections.Generic;
        using System.Globalization;
        using System.Linq;
        using System.Text;
        using System.Diagnostics;

        public class MyClass
        {
            private readonly float float1 = 1f;

            public void MyMethod()
            {
                string result = float1.ToString(CultureInfo.InvariantCulture);
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1001_FloatingPointFormatting.Id)
                .WithMessageFormat(Descriptors.LuceneDev1001_FloatingPointFormatting.MessageFormat)
                .WithArguments("float1.ToString")
                .WithLocation("/0/Test0.cs", line: 15, column: 33);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
