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
    public class TestLuceneDev1003_ArrayMethodParameterCSCodeAnalyzer
    {
        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            var testCode = @"";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_ParseChar_String_Int32Array_Char()
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
            public static bool ParseChar(string id, int[] pos, char ch)
            {
                int start = pos[0];
                //pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
                if (pos[0] == id.Length ||
                    id[pos[0]] != ch)
                {
                    pos[0] = start;
                    return false;
                }
                ++pos[0];
                return true;
            }
        }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1003_ArrayMethodParameter.Id)
                .WithMessageFormat(Descriptors.LuceneDev1003_ArrayMethodParameter.MessageFormat)
                .WithArguments("int[] pos")
                .WithLocation("/0/Test0.cs", line: 11, column: 53);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_ParseChar_String_CharArray_Char()
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
            public static bool ParseChar(string id, char[] pos, char ch)
            {
                char start = pos[0];
                //pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
                if (pos[0] == id.Length ||
                    id[pos[0]] != ch)
                {
                    pos[0] = start;
                    return false;
                }
                ++pos[0];
                return true;
            }
        }
       ";
            // We shouldn't trigger a warning on char[]
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
