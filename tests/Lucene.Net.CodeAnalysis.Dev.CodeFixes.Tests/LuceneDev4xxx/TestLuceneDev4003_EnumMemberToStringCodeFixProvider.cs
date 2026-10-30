/*
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev4xxx;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev4xxx
{
    [TestFixture]
    public class TestLuceneDev4003_EnumMemberToStringCodeFixProvider
    {
        [Test]
        public async Task Fix_ReplacesToString_WithNameof()
        {
            var testCode = @"
using System.Collections.Generic;

public enum LegacyDocValuesType
{
    NONE,
    BYTES_FIXED_DEREF
}

public class Sample
{
    public void M(IDictionary<string, string> attributes, string legacyKey)
    {
        attributes[legacyKey] = {|#0:LegacyDocValuesType.BYTES_FIXED_DEREF.ToString()|};
    }
}";

            var fixedCode = @"
using System.Collections.Generic;

public enum LegacyDocValuesType
{
    NONE,
    BYTES_FIXED_DEREF
}

public class Sample
{
    public void M(IDictionary<string, string> attributes, string legacyKey)
    {
        attributes[legacyKey] = nameof(LegacyDocValuesType.BYTES_FIXED_DEREF);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("LegacyDocValuesType.BYTES_FIXED_DEREF");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4003_EnumMemberToStringAnalyzer(),
                () => new LuceneDev4003_EnumMemberToStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_PreservesQualification_And_Trivia()
        {
            var testCode = @"
namespace My.Codecs
{
    public enum Kind { A, B }
}

public class Sample
{
    public string M()
    {
        return Concat(
            {|#0:My.Codecs.Kind.B.ToString()|} /* trailing */,
            ""x"");
    }

    private static string Concat(string a, string b) => a + b;
}";

            var fixedCode = @"
namespace My.Codecs
{
    public enum Kind { A, B }
}

public class Sample
{
    public string M()
    {
        return Concat(
            nameof(My.Codecs.Kind.B) /* trailing */,
            ""x"");
    }

    private static string Concat(string a, string b) => a + b;
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("My.Codecs.Kind.B");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4003_EnumMemberToStringAnalyzer(),
                () => new LuceneDev4003_EnumMemberToStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_UsingStatic_EnumMember()
        {
            var testCode = @"
using static Kind;

public enum Kind { A, B }

public class Sample
{
    public string M() => {|#0:B.ToString()|};
}";

            var fixedCode = @"
using static Kind;

public enum Kind { A, B }

public class Sample
{
    public string M() => nameof(B);
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("B");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4003_EnumMemberToStringAnalyzer(),
                () => new LuceneDev4003_EnumMemberToStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task FixAll_ReplacesAllOccurrences()
        {
            var testCode = @"
public enum Kind { A, B }

public class Sample
{
    public string M()
    {
        string a = {|#0:Kind.A.ToString()|};
        string b = {|#1:Kind.B.ToString()|};
        return a + b;
    }
}";

            var fixedCode = @"
public enum Kind { A, B }

public class Sample
{
    public string M()
    {
        string a = nameof(Kind.A);
        string b = nameof(Kind.B);
        return a + b;
    }
}";

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4003_EnumMemberToStringAnalyzer(),
                () => new LuceneDev4003_EnumMemberToStringCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics =
                {
                    new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                        .WithSeverity(DiagnosticSeverity.Warning)
                        .WithLocation(0)
                        .WithArguments("Kind.A"),
                    new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                        .WithSeverity(DiagnosticSeverity.Warning)
                        .WithLocation(1)
                        .WithArguments("Kind.B"),
                }
            };

            await test.RunAsync();
        }
    }
}
