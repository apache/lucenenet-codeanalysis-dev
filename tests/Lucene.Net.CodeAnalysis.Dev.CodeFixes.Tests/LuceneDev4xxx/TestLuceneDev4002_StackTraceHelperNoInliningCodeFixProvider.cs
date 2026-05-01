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
    public class TestLuceneDev4002_StackTraceHelperNoInliningCodeFixProvider
    {
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
        public async Task Fix_AddsAttribute_WhenUsingAlreadyPresent()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

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

            var fixedCode = @"
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(16, 13, 16, 132)
                .WithArguments("Target.Merge");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_AddsAttributeAndUsing_WhenUsingMissing()
        {
            // No `using System.Runtime.CompilerServices;` initially — fix must add it.
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

            var fixedCode = @"using System.Runtime.CompilerServices;

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

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(14, 13, 14, 132)
                .WithArguments("Target.Merge");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_AddsAttributeAndUsing_InTargetDocument_WhenTargetIsInDifferentFile()
        {
            // Target lives in /0/Test0.cs, Caller in /0/Test1.cs. The code fix must
            // edit the target's document (adding the attribute and the using) rather
            // than the caller's.
            var targetSource = @"
public class Target
{
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

            var fixedTargetSource = @"using System.Runtime.CompilerServices;

public class Target
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Merge()
    {
        var x = 1;
    }
}
";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation("/0/Test1.cs", line: 6, column: 13)
                .WithArguments("Target.Merge");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                ExpectedDiagnostics = { expected }
            };
            test.TestState.Sources.Add(targetSource);
            test.TestState.Sources.Add(callerSource);
            test.FixedState.Sources.Add(fixedTargetSource);
            test.FixedState.Sources.Add(callerSource);

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_PreservesExistingAttributeOnTarget()
        {
            // Target method has another attribute; the new MethodImpl list should
            // be inserted ahead of it without disturbing it.
            var testCode = @"
using System;
using System.Runtime.CompilerServices;

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

public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Support.ExceptionHandling.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var fixedCode = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method)]
public class FooAttribute : Attribute { }

public class Target
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    [Foo]
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

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(21, 13, 21, 132)
                .WithArguments("Target.Merge");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
