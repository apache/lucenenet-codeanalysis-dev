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
    public class TestLuceneDev1002_FloatingPointArithmeticCSCodeAnalyzer
    {

        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            var testCode = @"";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1002_FloatingPointArithmeticCSCodeAnalyzer())
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
        using System.Linq;
        using System.Text;
        using System.Threading.Tasks;
        using System.Diagnostics;

        public class MyClass
        {
            private readonly float float1 = 1f;
            private readonly float float2 = 3.14f;

            public void MyMethod()
            {
                long foo = 33;
                var result = ((double)float1 * (double)float2) / foo;
            }
       }
       ";

            var expected1 = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1002_FloatingPointArithmetic.Id)
                .WithMessageFormat(Descriptors.LuceneDev1002_FloatingPointArithmetic.MessageFormat)
                .WithArguments("((double)float1 * (double)float2) / foo")
                .WithLocation("/0/Test0.cs", line: 17, column: 30);

            var expected2 = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1002_FloatingPointArithmetic.Id)
                .WithMessageFormat(Descriptors.LuceneDev1002_FloatingPointArithmetic.MessageFormat)
                .WithArguments("(double)float1 * (double)float2")
                .WithLocation("/0/Test0.cs", line: 17, column: 31);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1002_FloatingPointArithmeticCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected1, expected2 }
            };

            await test.RunAsync();
        }
    }
}
