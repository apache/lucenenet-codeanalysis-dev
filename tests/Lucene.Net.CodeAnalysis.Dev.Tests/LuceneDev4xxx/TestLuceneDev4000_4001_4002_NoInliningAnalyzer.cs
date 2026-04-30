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
    public class TestLuceneDev4000_4001_4002_NoInliningAnalyzer
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

        // ---------------------------------------------------------------------
        // LuceneDev4000: NoInlining on interface / abstract methods
        // ---------------------------------------------------------------------

        [Test]
        public async Task LuceneDev4000_Reports_When_NoInlining_On_InterfaceMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public interface ISample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    void DoWork();
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4000_NoInliningHasNoEffect)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(6, 6, 6, 46)
                .WithArguments("DoWork");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4000_Reports_When_NoInlining_On_AbstractMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public abstract class Sample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public abstract void DoWork();
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4000_NoInliningHasNoEffect)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(6, 6, 6, 46)
                .WithArguments("DoWork");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4000_NoDiagnostic_When_NoInlining_On_RegularMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DoWork()
    {
        var x = 1 + 2;
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        // ---------------------------------------------------------------------
        // LuceneDev4001: NoInlining on empty-bodied methods
        // ---------------------------------------------------------------------

        [Test]
        public async Task LuceneDev4001_Reports_When_NoInlining_On_EmptyBodiedMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DoWork()
    {
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4001_NoInliningOnEmptyMethod)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(6, 6, 6, 46)
                .WithArguments("DoWork");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4001_NoDiagnostic_When_NoInlining_On_ExpressionBodiedThrow()
        {
            // An expression-bodied method that throws is not "empty" — has a real expression body.
            var testCode = @"
using System;
using System.Runtime.CompilerServices;

public class Sample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void DoWork() => throw new InvalidOperationException();
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task LuceneDev4001_NoDiagnostic_When_NoInlining_On_NonEmptyMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int DoWork()
    {
        return 42;
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        // ---------------------------------------------------------------------
        // LuceneDev4002: Methods referenced in StackTraceHelper.DoesStackTraceContainMethod
        // (2-arg form) should have NoInlining when their body is non-empty.
        // ---------------------------------------------------------------------

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

            // Diagnostic is reported on the Target.Merge declaration.
            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithSpan(4, 5, 7, 6)
                .WithArguments("Merge");

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
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

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
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

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode
            };

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

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4000_4001_4002_NoInliningAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
