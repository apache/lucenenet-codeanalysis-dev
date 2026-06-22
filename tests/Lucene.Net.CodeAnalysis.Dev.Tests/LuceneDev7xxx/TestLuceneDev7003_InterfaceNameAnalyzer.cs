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

namespace Lucene.Net.CodeAnalysis.Dev;

public class TestLuceneDev7003_InterfaceNameAnalyzer
{
    [Test]
    [TestCase("IComparer")]
    [TestCase("IList")]
    public async Task ValidInterfaceName_NoDiagnostic(string name)
    {
        string testCode =
            $$"""
            public interface {{name}}
            {
                void DoWork();
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7003_InterfaceNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }

    [Test]
    public async Task InterfaceWithoutIPrefix_Diagnostic()
    {
        const string testCode =
            """
            public interface Comparer
            {
                void DoWork();
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev7003_InterfaceName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Comparer")
            .WithLocation(1, 18);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7003_InterfaceNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        await test.RunAsync();
    }

    [Test]
    public async Task GenericInterfaceWithIPrefix_NoDiagnostic()
    {
        const string testCode =
            """
            public interface IComparer<T>
            {
                int Compare(T a, T b);
            }
            """;

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7003_InterfaceNameAnalyzer())
        {
            TestCode = testCode
        };
        await test.RunAsync();
    }
}
