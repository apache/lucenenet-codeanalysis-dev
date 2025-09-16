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
    public class TestLuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer
    {

        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            var testCode = @"";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_GetVersionByteArrayFromCompactInt32_ByteArrayReturnType()
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
            public static byte[] GetVersionByteArrayFromCompactInt32(int version) // ICU4N specific - Renamed from GetVersionByteArrayFromCompactInt
            {
                return new byte[] {
                    (byte)(version >> 24),
                    (byte)(version >> 16),
                    (byte)(version >> 8),
                    (byte)(version)
                };
            }
        }
       ";


            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1004_ArrayMethodReturnValue.Id)
                .WithMessageFormat(Descriptors.LuceneDev1004_ArrayMethodReturnValue.MessageFormat)
                .WithArguments("byte[]")
                .WithLocation("/0/Test0.cs", line: 11, column: 27);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_GetVersionCharArrayFromCompactInt32_CharArrayReturnType()
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
            public static char[] GetVersionCharArrayFromCompactInt32(int version)
            {
                return new char[] {
                    (char)(version >> 24),
                    (char)(version >> 16),
                    (char)(version >> 8),
                    (char)(version)
                };
            }
        }
       ";

            // We shouldn't trigger a warning on char[]
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
