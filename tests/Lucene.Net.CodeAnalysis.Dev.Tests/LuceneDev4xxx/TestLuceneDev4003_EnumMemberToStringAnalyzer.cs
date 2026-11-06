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

using Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev4xxx
{
    [TestFixture]
    public class TestLuceneDev4003_EnumMemberToStringAnalyzer
    {
        [Test]
        public async Task LuceneDev4003_Reports_EnumMember_ToString()
        {
            var testCode = @"
public enum LegacyDocValuesType
{
    NONE,
    BYTES_FIXED_DEREF
}

public class Sample
{
    public string M()
    {
        return {|#0:LegacyDocValuesType.BYTES_FIXED_DEREF.ToString()|};
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("LegacyDocValuesType.BYTES_FIXED_DEREF");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_Reports_NamespaceQualified_EnumMember_ToString()
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
        return {|#0:My.Codecs.Kind.B.ToString()|};
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("My.Codecs.Kind.B");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_Reports_UsingStatic_EnumMember_ToString()
        {
            var testCode = @"
using static Kind;

public enum Kind { A, B }

public class Sample
{
    public string M()
    {
        return {|#0:B.ToString()|};
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("B");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_Reports_MetadataEnumMember_ToString()
        {
            var testCode = @"
using System;

public class Sample
{
    public string M()
    {
        return {|#0:DayOfWeek.Monday.ToString()|};
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DayOfWeek.Monday");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_Reports_FlagsEnum_DefinedMember_ToString()
        {
            // A defined member's name is returned by ToString() even for [Flags]
            // enums (including composite members), so nameof is equivalent.
            var testCode = @"
using System;

[Flags]
public enum Access
{
    None = 0,
    Read = 1,
    Write = 2,
    ReadWrite = Read | Write
}

public class Sample
{
    public string M()
    {
        return {|#0:Access.ReadWrite.ToString()|};
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4003_EnumMemberToString)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("Access.ReadWrite");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_NoDiagnostic_For_EnumMember_With_DuplicateValue()
        {
            // When two members share a value, ToString() may return the other
            // member's name, so nameof would change behavior.
            var testCode = @"
public enum Kind
{
    First = 1,
    Default = 1,
    Other = 2
}

public class Sample
{
    public string M1() => Kind.First.ToString();
    public string M2() => Kind.Default.ToString();
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_NoDiagnostic_For_EnumTypedVariables()
        {
            var testCode = @"
public enum Kind { A, B }

public class Sample
{
    private Kind kind = Kind.A;

    public Kind Current { get; set; }

    public string M(Kind parameter)
    {
        Kind local = Kind.B;
        return local.ToString() + parameter.ToString() + kind.ToString() + this.kind.ToString() + Current.ToString();
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_NoDiagnostic_For_ToString_With_Arguments()
        {
            var testCode = @"
public enum Kind { A, B }

public class Sample
{
    public string M()
    {
        return Kind.A.ToString(""D"") + Kind.B.ToString(""G"");
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4003_NoDiagnostic_For_NonEnum_Constant_ToString()
        {
            var testCode = @"
public class Sample
{
    private const int Size = 4;
    private const string Name = ""x"";

    public string M()
    {
        return Size.ToString() + Name.ToString();
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4003_EnumMemberToStringAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
