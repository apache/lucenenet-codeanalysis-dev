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

using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes;

public class TestLuceneDev7001_ProtectedFieldNameCodeFixProvider
{
    [Test]
    public async Task RenamesProtectedFieldToMPrefix()
    {
        const string testCode =
            """
            public class MyClass
            {
                protected int count;

                protected int Get() => count;
            }
            """;

        const string fixedCode =
            """
            public class MyClass
            {
                protected int m_count;

                protected int Get() => m_count;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7001_ProtectedFieldName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("count")
            .WithLocation(3, 19);

        var test = new InjectableCodeFixTest(
            () => new LuceneDev7001_ProtectedFieldNameAnalyzer(),
            () => new LuceneDev7001_ProtectedFieldNameCodeFixProvider())
        {
            TestCode = testCode.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            ExpectedDiagnostics = { expected }
        };

        await test.RunAsync();
    }
}
