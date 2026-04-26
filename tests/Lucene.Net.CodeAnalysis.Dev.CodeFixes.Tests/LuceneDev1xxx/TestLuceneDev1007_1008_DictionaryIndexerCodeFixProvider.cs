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

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev1xxx;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes
{
    public class TestLuceneDev1007_1008_DictionaryIndexerCodeFixProvider
    {
        [Test]
        public async Task TestFix_Return_ReferenceType()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public string M(IDictionary<string, string> dict, string key)
    {
        return dict[key];
    }
}";

            var fixedCode = @"
using System.Collections.Generic;

public class Sample
{
    public string M(IDictionary<string, string> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : default;
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1008_GenericDictionaryIndexerReferenceType.MessageFormat)
                .WithArguments("dict[key]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1007_1008_DictionaryIndexerAnalyzer(),
                () => new LuceneDev1007_1008_DictionaryIndexerCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_Return_ValueType()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(IDictionary<string, int> dict, string key)
    {
        return dict[key];
    }
}";

            var fixedCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(IDictionary<string, int> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : default;
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType.MessageFormat)
                .WithArguments("dict[key]")
                .WithLocation("/0/Test0.cs", line: 8, column: 16);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1007_1008_DictionaryIndexerAnalyzer(),
                () => new LuceneDev1007_1008_DictionaryIndexerCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_Return_PicksUniqueLocalName_WhenValueIsInScope()
        {
            var testCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(IDictionary<string, int> dict, string key)
    {
        int value = 42;
        if (value > 0)
        {
            return dict[key];
        }
        return value;
    }
}";

            var fixedCode = @"
using System.Collections.Generic;

public class Sample
{
    public int M(IDictionary<string, int> dict, string key)
    {
        int value = 42;
        if (value > 0)
        {
            return dict.TryGetValue(key, out var value1) ? value1 : default;
        }
        return value;
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1007_GenericDictionaryIndexerValueType.MessageFormat)
                .WithArguments("dict[key]")
                .WithLocation("/0/Test0.cs", line: 11, column: 20);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1007_1008_DictionaryIndexerAnalyzer(),
                () => new LuceneDev1007_1008_DictionaryIndexerCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
