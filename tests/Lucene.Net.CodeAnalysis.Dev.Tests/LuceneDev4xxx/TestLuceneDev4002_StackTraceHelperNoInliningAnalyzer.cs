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
    public class TestLuceneDev4002_StackTraceHelperNoInliningAnalyzer
    {
        // Stub of StackTraceHelper appended to test sources, matching the
        // Lucene.Net.Support.ExceptionHandling.StackTraceHelper API surface.
        private const string StackTraceHelperStub = @"
namespace Lucene.Net.Support.ExceptionHandling
{
    public static class StackTraceHelper
    {
        public static bool DoesStackTraceContainMethod(string methodName) => false;
        public static bool DoesStackTraceContainMethod(string className, string methodName) => false;
    }
}
";

        [Test]
        public async Task LuceneDev4002_Reports_When_TargetMethod_Missing_NoInlining()
        {
            var testCode = @"
public class Target
{
    public void Merge()
    {
        var x = 1;
    }
}

public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            // Diagnostic is now reported on the DoesStackTraceContainMethod invocation
            // (call site) so the IDE can surface it and a future code fix can be hooked
            // up. Argument is the qualified target method name.
            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(14, 13, 14, 132)
                .WithArguments("Target.Merge");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_When_TargetMethod_Already_Has_NoInlining()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Target
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Merge()
    {
        var x = 1;
    }
}

public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_When_TargetMethod_Has_EmptyBody()
        {
            // Empty-bodied methods can never appear in a stack trace under the relevant
            // call (they call nothing); NoInlining gives no benefit. Don't flag.
            var testCode = @"
public class Target
{
    public void Merge()
    {
    }
}

public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4002_Reports_When_TargetMethod_Is_In_Different_Document()
        {
            // The Target type and the Caller live in separate source files. The analyzer
            // must build a SemanticModel against the Target's syntax tree before
            // inspecting attributes — using the caller's SemanticModel against a node
            // from a different tree throws ArgumentException. Target carries an
            // unrelated attribute so the attribute-walk path actually executes.
            var targetSource = @"
using System;

[AttributeUsage(AttributeTargets.Method)]
public class FooAttribute : Attribute { }

public class Target
{
    [Foo]
    public void Merge()
    {
        var x = 1;
    }
}
";

            var callerSource = @"
public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation("/0/Test1.cs", line: 6, column: 13)
                .WithArguments("Target.Merge");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                ExpectedDiagnostics = { expected }
            };
            test.TestState.Sources.Add(targetSource);
            test.TestState.Sources.Add(callerSource);

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload()
        {
            // The single-arg overload doesn't validate the owning class, so we don't
            // require methods referenced through it to have NoInlining. Per the issue,
            // only the 2-arg form is in scope.
            var testCode = @"
public class Target
{
    public void Merge()
    {
        var x = 1;
    }
}

public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
