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

using Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev1xxx
{
    [TestFixture]
    public class TestLuceneDev1007_1008_DictionaryIndexerAnalyzer
    {
        [Test]
        public async Task Detects_IDictionary_ValueType_Reports1007()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(IDictionary<string, int> dict)
    {
        return dict[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType.MessageFormat)
                .WithArguments("dict[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task Detects_IDictionary_ReferenceType_Reports1008()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public string M(IDictionary<string, string> dict)
    {
        return dict[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType.MessageFormat)
                .WithArguments("dict[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task Detects_IReadOnlyDictionary_Reports1008()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public string M(IReadOnlyDictionary<string, string> dict)
    {
        return dict[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType.MessageFormat)
                .WithArguments("dict[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task Detects_ConcreteDictionary_Reports1007()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(Dictionary<string, int> dict)
    {
        return dict[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType.MessageFormat)
                .WithArguments("dict[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_On_SetterAssignment()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public void M(Dictionary<string, int> dict)
    {
        dict[""key""] = 42;
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_On_NonDictionaryIndexer()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(List<int> list)
    {
        return list[0];
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1007_1008_DictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { }
            };
            await test.RunAsync();
        }
    }
}
