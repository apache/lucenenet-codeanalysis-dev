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
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
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
    public class TestLuceneDev6012_6013_6014_StackTraceHelperArgumentsAnalyzer
    {
        // Stub of StackTraceHelper appended to test sources, matching the
        // Lucene.Net.Util.StackTraceHelper API surface.
        private const string Stub = @"
namespace Lucene.Net.Util
{
    public static class StackTraceHelper
    {
        public static bool DoesStackTraceContainMethod(string methodName) => false;
        public static bool DoesStackTraceContainMethod(string className, string methodName) => false;
    }
}
";

        private const string Target = @"
public class Target
{
    public int Count => 0;
    public void Merge() { }
}
";

        private static string InCaller(string statements) => @"
using System;
using Lucene.Net.Util;

public class Caller
{
    private const string ConstName = ""Target"";

    public void Check(string name)
    {
        " + statements + @"
    }
}
" + Target + Stub;

        private static DiagnosticResult ClassName(int location, string argument) =>
            new DiagnosticResult(Descriptors.LuceneDev6012_StackTraceHelperClassNameShouldUseNameof)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(location)
                .WithArguments(argument);

        private static DiagnosticResult MethodName(int location, string argument) =>
            new DiagnosticResult(Descriptors.LuceneDev6013_StackTraceHelperMethodNameShouldUseNameof)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(location)
                .WithArguments(argument);

        private static DiagnosticResult NotDeclared(int location, string typeName, string methodName) =>
            new DiagnosticResult(Descriptors.LuceneDev6014_StackTraceHelperMethodNotDeclared)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithLocation(location)
                .WithArguments(typeName, methodName);

        private static async Task VerifyAsync(string testCode, params DiagnosticResult[] expected)
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6012_6013_6014_StackTraceHelperArgumentsAnalyzer())
            {
                TestCode = testCode
            };
            test.ExpectedDiagnostics.AddRange(expected);

            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_When_BothArguments_Use_Nameof()
        {
            await VerifyAsync(InCaller(
                "StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge));"));
        }

        [Test]
        public async Task NoDiagnostic_When_SingleArgument_Uses_Nameof()
        {
            await VerifyAsync(InCaller(
                "StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge));"));
        }

        [Test]
        public async Task NoDiagnostic_When_Arguments_Are_Not_Constant()
        {
            // The value isn't known at compile time, so it can't be replaced with nameof or
            // checked. lucenenet has call sites like this that pass a field set from nameof.
            await VerifyAsync(InCaller(@"
                StackTraceHelper.DoesStackTraceContainMethod(name);
                StackTraceHelper.DoesStackTraceContainMethod(name, nameof(Target.Merge));
                StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), name);"));
        }

        [Test]
        public async Task NoDiagnostic_For_Same_Named_Method_On_Other_Type()
        {
            await VerifyAsync(@"
public static class OtherHelper
{
    public static bool DoesStackTraceContainMethod(string className, string methodName) => false;

    public static bool Check() => DoesStackTraceContainMethod(""Target"", ""Missing"");
}");
        }

        [Test]
        public async Task LuceneDev6012_Reports_Literal_ClassName()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod({|#0:""Target""|}, nameof(Target.Merge));"),
                ClassName(0, "\"Target\""));
        }

        [Test]
        public async Task LuceneDev6012_Reports_Constant_ClassName()
        {
            await VerifyAsync(
                InCaller("StackTraceHelper.DoesStackTraceContainMethod({|#0:ConstName|}, nameof(Target.Merge));"),
                ClassName(0, "ConstName"));
        }

        [Test]
        public async Task LuceneDev6012_Reports_Correct_Argument_When_Named_Out_Of_Order()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod(methodName: nameof(Target.Merge), className: {|#0:""Target""|});"),
                ClassName(0, "\"Target\""));
        }

        [Test]
        public async Task LuceneDev6013_Reports_Literal_MethodName()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), {|#0:""Merge""|});"),
                MethodName(0, "\"Merge\""));
        }

        [Test]
        public async Task LuceneDev6013_Reports_Literal_MethodName_On_SingleArgument_Overload()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod({|#0:""Merge""|});"),
                MethodName(0, "\"Merge\""));
        }

        [Test]
        public async Task LuceneDev6012_6013_Report_Both_Literals_Without_6014_When_Method_Is_Declared()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod({|#0:""Target""|}, {|#1:""Merge""|});"),
                ClassName(0, "\"Target\""),
                MethodName(1, "\"Merge\""));
        }

        [Test]
        public async Task LuceneDev6014_Reports_Method_Missing_From_Nameof_Type()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), {|#0:{|#1:""Missing""|}|});"),
                MethodName(0, "\"Missing\""),
                NotDeclared(1, "Target", "Missing"));
        }

        [Test]
        public async Task LuceneDev6014_Reports_Method_Missing_From_Literal_Type()
        {
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod({|#0:""Target""|}, {|#1:{|#2:""Missing""|}|});"),
                ClassName(0, "\"Target\""),
                MethodName(1, "\"Missing\""),
                NotDeclared(2, "Target", "Missing"));
        }

        [Test]
        public async Task LuceneDev6014_Reports_Method_Name_From_Another_Type()
        {
            await VerifyAsync(@"
using Lucene.Net.Util;

public class Other
{
    public void Run() { }

    public bool Check() => StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), {|#0:nameof(Other.Run)|});
}
" + Target + Stub,
                NotDeclared(0, "Target", "Run"));
        }

        [Test]
        public async Task LuceneDev6014_Reports_Inherited_Method()
        {
            // StackTraceHelper compares the frame's DeclaringType name, which is Base here, so
            // this compiles but can never match.
            await VerifyAsync(@"
using Lucene.Net.Util;

public class Base
{
    public void Merge() { }
}

public class Derived : Base
{
    public bool Check() => StackTraceHelper.DoesStackTraceContainMethod(nameof(Derived), {|#0:nameof(Derived.Merge)|});
}
" + Stub,
                NotDeclared(0, "Derived", "Merge"));
        }

        [Test]
        public async Task NoDiagnostic_For_Overridden_Method()
        {
            await VerifyAsync(@"
using Lucene.Net.Util;

public class Base
{
    public virtual void Merge() { }
}

public class Derived : Base
{
    public override void Merge() { }

    public bool Check() => StackTraceHelper.DoesStackTraceContainMethod(nameof(Derived), nameof(Derived.Merge));
}
" + Stub);
        }

        [Test]
        public async Task LuceneDev6014_Reports_Property_Name()
        {
            // The stack frame for a property is get_Count or set_Count, not Count.
            await VerifyAsync(
                InCaller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), {|#0:nameof(Target.Count)|});"),
                NotDeclared(0, "Target", "Count"));
        }

        [Test]
        public async Task NoDiagnostic_6014_When_Any_Type_With_Literal_Name_Declares_Method()
        {
            // StackTraceHelper compares simple names, so either Target could match at runtime.
            await VerifyAsync(@"
using Lucene.Net.Util;

namespace A { public class Target { } }
namespace B { public class Target { public void Merge() { } } }

public class Caller
{
    public bool Check() => StackTraceHelper.DoesStackTraceContainMethod({|#0:""Target""|}, {|#1:""Merge""|});
}
" + Stub,
                ClassName(0, "\"Target\""),
                MethodName(1, "\"Merge\""));
        }

        [Test]
        public async Task NoDiagnostic_6014_When_Literal_Type_Is_Not_In_Source()
        {
            // The type may be non-public in a referenced assembly, so its absence proves nothing.
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod({|#0:""Unknown""|}, {|#1:""Missing""|});"),
                ClassName(0, "\"Unknown\""),
                MethodName(1, "\"Missing\""));
        }

        [Test]
        public async Task NoDiagnostic_When_Method_Declared_On_Referenced_Assembly_Type()
        {
            await VerifyAsync(InCaller(
                "StackTraceHelper.DoesStackTraceContainMethod(nameof(String), nameof(String.Concat));"));
        }

        [Test]
        public async Task NoDiagnostic_6014_For_Literal_Method_On_Referenced_Assembly_Type()
        {
            // Private members of referenced assemblies aren't visible to the compilation, so
            // this may be a real method. lucenenet does this for IndexWriter's private
            // FinishCommit.
            await VerifyAsync(
                InCaller(@"StackTraceHelper.DoesStackTraceContainMethod(nameof(String), {|#0:""Missing""|});"),
                MethodName(0, "\"Missing\""));
        }

        [Test]
        public async Task LuceneDev6014_Reports_Nameof_Inherited_Method_On_Referenced_Assembly_Type()
        {
            // nameof proves the member is visible, so not finding it declared here is conclusive.
            await VerifyAsync(
                InCaller("StackTraceHelper.DoesStackTraceContainMethod(nameof(ArgumentException), {|#0:nameof(ArgumentException.GetType)|});"),
                NotDeclared(0, "ArgumentException", "GetType"));
        }
    }
}
