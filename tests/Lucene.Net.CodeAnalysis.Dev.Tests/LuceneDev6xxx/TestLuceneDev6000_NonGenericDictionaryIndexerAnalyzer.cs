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

using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6000_NonGenericDictionaryIndexerAnalyzer
    {
        [Test]
        public async Task Detects_NonGenericIDictionary_Indexer()
        {
            var testCode = @"
using System.Collections;

public class Sample
{
    public object M(IDictionary dict)
    {
        return dict[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev6000_NonGenericDictionaryIndexer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithMessageFormat(Descriptors.LuceneDev6000_NonGenericDictionaryIndexer.MessageFormat)
                .WithArguments("dict[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6000_NonGenericDictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task Detects_Hashtable_Indexer()
        {
            var testCode = @"
using System.Collections;

public class Sample
{
    public object M(Hashtable table)
    {
        return table[""key""];
    }
}";
            var expected = new DiagnosticResult(Descriptors.LuceneDev6000_NonGenericDictionaryIndexer)
                .WithSeverity(DiagnosticSeverity.Info)
                .WithMessageFormat(Descriptors.LuceneDev6000_NonGenericDictionaryIndexer.MessageFormat)
                .WithArguments("table[\"key\"]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6000_NonGenericDictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_On_Generic_Dictionary()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public string M(Dictionary<string, string> dict)
    {
        return dict[""key""];
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6000_NonGenericDictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { }
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_On_SetterAssignment()
        {
            var testCode = @"
using System.Collections;

public class Sample
{
    public void M(IDictionary dict)
    {
        dict[""key""] = ""value"";
    }
}";
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6000_NonGenericDictionaryIndexerAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { }
            };
            await test.RunAsync();
        }
    }
}
