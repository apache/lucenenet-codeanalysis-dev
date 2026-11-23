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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev4xxx
{
    [TestFixture]
    public class TestLuceneDev4002_StackTraceHelperNoInliningCodeFixProvider
    {
        private const string StackTraceHelperStub = @"
namespace Lucene.Net.Util
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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
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
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
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
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
        {
        }
    }
}" + StackTraceHelperStub;

            var fixedCallerSource = @"
public class Caller
{
    public void Check()
    {
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
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
                .WithLocation(0)
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
            test.FixedState.Sources.Add(fixedCallerSource);

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_AddsAttribute_ForSingleArgumentOverload()
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
    public bool Check() => {|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))|};
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
    public bool Check() => Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge));
}" + StackTraceHelperStub;

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { Missing("Target.Merge") }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_AddsAttribute_WhenArgumentsAreNamedOutOfOrder()
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
    public bool Check() => {|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(methodName: nameof(Target.Merge), className: nameof(Target))|};
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
    public bool Check() => Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(methodName: nameof(Target.Merge), className: nameof(Target));
}" + StackTraceHelperStub;

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { Missing("Target.Merge") }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Fix_AddsAttribute_InReferencedProject()
        {
            var targetSource = @"using System.Runtime.CompilerServices;

public class Target
{
    public void Merge()
    {
        var x = 1;
    }
}
";

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

            var callerSource = @"
public class Caller
{
    public bool Check() => {|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|};
}" + StackTraceHelperStub;

            var fixedCallerSource = @"
public class Caller
{
    public bool Check() => Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge));
}" + StackTraceHelperStub;

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                ExpectedDiagnostics = { Missing("Target.Merge") },

                // Roslyn's batch fixer doesn't apply edits to another project, so this only
                // works one fix at a time.
                CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck
            };
            test.TestState.Sources.Add(callerSource);
            test.TestState.AdditionalProjects["TargetLibrary"].Sources.Add(targetSource);
            test.TestState.AdditionalProjectReferences.Add("TargetLibrary");
            test.FixedState.Sources.Add(fixedCallerSource);
            test.FixedState.AdditionalProjects["TargetLibrary"].Sources.Add(fixedTargetSource);
            test.FixedState.AdditionalProjectReferences.Add("TargetLibrary");

            await test.RunAsync();
        }

        [Test]
        public async Task NoFix_WhenTargetHasNoSource()
        {
            // There is nothing to edit, so the fix shouldn't be offered at all.
            var references = await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            var compilation = CSharpCompilation.Create(
                "TargetLibrary",
                new[]
                {
                    CSharpSyntaxTree.ParseText(@"
public class Target
{
    public void Merge()
    {
        var x = 1;
    }
}")
                },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));

            var testCode = @"
public class Caller
{
    public bool Check() => {|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|};
}" + StackTraceHelperStub;

            var test = new InjectableCodeFixTest(
                () => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer(),
                () => new LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = testCode,
                ExpectedDiagnostics = { Missing("Target.Merge") }
            };
            test.TestState.AdditionalReferences.Add(MetadataReference.CreateFromImage(stream.ToArray()));
            test.FixedState.AdditionalReferences.Add(MetadataReference.CreateFromImage(stream.ToArray()));
            test.FixedState.ExpectedDiagnostics.Add(Missing("Target.Merge"));

            await test.RunAsync();
        }

        private static DiagnosticResult Missing(string methodName) =>
            new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(methodName);

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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
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
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
        {
        }
    }
}" + StackTraceHelperStub;

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
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
