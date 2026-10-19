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
    public class TestLuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider
    {
        // -----------------------------------------------------------------
        // 4000: remove attribute on interface / abstract method
        // -----------------------------------------------------------------

        [Test]
        public async Task Fix_LuceneDev4000_RemovesAttribute_From_InterfaceMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public interface ISample
{
    [{|#0:MethodImpl(MethodImplOptions.NoInlining)|}]
    void DoWork();
}";

            var fixedCode = @"
using System.Runtime.CompilerServices;

public interface ISample
{
    void DoWork();
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4000_NoInliningHasNoEffect)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DoWork");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4000_4001_NoInliningOnNoOpAnalyzer(),
                () => new LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        // -----------------------------------------------------------------
        // 4001: remove attribute on empty-bodied method
        // -----------------------------------------------------------------

        [Test]
        public async Task Fix_LuceneDev4001_RemovesAttribute_From_EmptyBodiedMethod()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    [{|#0:MethodImpl(MethodImplOptions.NoInlining)|}]
    public void DoWork()
    {
    }
}";

            var fixedCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    public void DoWork()
    {
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4001_NoInliningOnEmptyMethod)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DoWork");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4000_4001_NoInliningOnNoOpAnalyzer(),
                () => new LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        // -----------------------------------------------------------------
        // Regression: removing the attribute must preserve comments and
        // blank lines that precede it.
        // -----------------------------------------------------------------

        [Test]
        public async Task Fix_PreservesLeadingCommentBeforeAttribute()
        {
            var testCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    // Important comment about the method.
    [{|#0:MethodImpl(MethodImplOptions.NoInlining)|}]
    public void DoWork()
    {
    }
}";

            var fixedCode = @"
using System.Runtime.CompilerServices;

public class Sample
{
    // Important comment about the method.
    public void DoWork()
    {
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4001_NoInliningOnEmptyMethod)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DoWork");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4000_4001_NoInliningOnNoOpAnalyzer(),
                () => new LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        // -----------------------------------------------------------------
        // Multiple attributes: only the [MethodImpl(NoInlining)] attribute
        // should be removed; siblings must remain intact.
        // -----------------------------------------------------------------

        [Test]
        public async Task Fix_RemovesOnlyTargetAttribute_WithinSingleAttributeList()
        {
            // [A, MethodImpl(NoInlining), B] → [A, B]
            var testCode = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FooAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class BarAttribute : Attribute { }

public class Sample
{
    [Foo, {|#0:MethodImpl(MethodImplOptions.NoInlining)|}, Bar]
    public void DoWork()
    {
    }
}";

            var fixedCode = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FooAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class BarAttribute : Attribute { }

public class Sample
{
    [Foo, Bar]
    public void DoWork()
    {
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4001_NoInliningOnEmptyMethod)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DoWork");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4000_4001_NoInliningOnNoOpAnalyzer(),
                () => new LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_RemovesOnlyTargetAttributeList_AmongMultipleLists()
        {
            // [A] [MethodImpl(NoInlining)] [B] → [A] [B]
            var testCode = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FooAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class BarAttribute : Attribute { }

public class Sample
{
    [Foo]
    [{|#0:MethodImpl(MethodImplOptions.NoInlining)|}]
    [Bar]
    public void DoWork()
    {
    }
}";

            var fixedCode = @"
using System;
using System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class FooAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class BarAttribute : Attribute { }

public class Sample
{
    [Foo]
    [Bar]
    public void DoWork()
    {
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev4001_NoInliningOnEmptyMethod)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments("DoWork");

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4000_4001_NoInliningOnNoOpAnalyzer(),
                () => new LuceneDev4000_4001_NoInliningOnNoOpCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
