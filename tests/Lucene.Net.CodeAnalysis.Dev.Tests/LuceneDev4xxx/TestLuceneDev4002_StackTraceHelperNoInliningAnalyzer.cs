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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev4xxx
{
    [TestFixture]
    public class TestLuceneDev4002_StackTraceHelperNoInliningAnalyzer
    {
        // Stub of StackTraceHelper appended to test sources, matching the
        // Lucene.Net.Util.StackTraceHelper API surface.
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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
        {
        }
    }
}" + StackTraceHelperStub;

            // Diagnostic is now reported on the DoesStackTraceContainMethod invocation
            // (call site) so the IDE can surface it and a future code fix can be hooked
            // up. Argument is the qualified target method name.
            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
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
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
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
        if (Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge)))
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
        if ({|#0:Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|})
        {
        }
    }
}" + StackTraceHelperStub;

            var expected = new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
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
        public async Task LuceneDev4002_Reports_For_SingleArgOverload_With_Nameof()
        {
            await VerifyAsync(
                Target("public void Merge() { var x = 1; }")
                    + Caller("{|#0:StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))|}"),
                references: null,
                Missing("Target.Merge"));
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload_When_Target_Has_NoInlining()
        {
            await VerifyAsync(
                Target("[MethodImpl(MethodImplOptions.NoInlining)] public void Merge() { var x = 1; }")
                    + Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))"),
                references: null);
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload_With_Literal()
        {
            // A name alone doesn't identify a method, and could match any type's method.
            await VerifyAsync(
                Target("public void Merge() { var x = 1; }")
                    + Caller(@"StackTraceHelper.DoesStackTraceContainMethod(""Merge"")"),
                references: null);
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload_When_Target_Has_EmptyBody()
        {
            await VerifyAsync(
                Target("public void Merge() { }")
                    + Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))"),
                references: null);
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload_When_Target_Is_Not_A_Method()
        {
            await VerifyAsync(
                Target("public int Count => 0;")
                    + Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Count))"),
                references: null);
        }

        [Test]
        public async Task LuceneDev4002_Reports_For_SingleArgOverload_When_Any_Overload_Is_Missing_NoInlining()
        {
            // StackTraceHelper compares names, so either overload could be the one that's inlined.
            await VerifyAsync(
                Target(@"
    [MethodImpl(MethodImplOptions.NoInlining)] public void Merge() { var x = 1; }
    public void Merge(int y) { var x = y; }")
                    + Caller("{|#0:StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))|}"),
                references: null,
                Missing("Target.Merge"));
        }

        [Test]
        public async Task LuceneDev4002_Reports_When_Arguments_Are_Named_Out_Of_Order()
        {
            await VerifyAsync(
                Target("public void Merge() { var x = 1; }")
                    + Caller("{|#0:StackTraceHelper.DoesStackTraceContainMethod(methodName: nameof(Target.Merge), className: nameof(Target))|}"),
                references: null,
                Missing("Target.Merge"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_Reports_When_Referenced_Assembly_Target_Is_Missing_NoInlining(bool referenceAssembly)
        {
            await VerifyAsync(
                Caller("{|#0:StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))|}"),
                await EmitAsync(Target("public void Merge() { var x = 1; }"), referenceAssembly),
                Missing("Target.Merge"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_NoDiagnostic_When_Referenced_Assembly_Target_Has_NoInlining(bool referenceAssembly)
        {
            await VerifyAsync(
                Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))"),
                await EmitAsync(Target("[MethodImpl(MethodImplOptions.NoInlining)] public void Merge() { var x = 1; }"), referenceAssembly));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_Reports_For_SingleArgOverload_When_Referenced_Assembly_Target_Is_Missing_NoInlining(bool referenceAssembly)
        {
            await VerifyAsync(
                Caller("{|#0:StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))|}"),
                await EmitAsync(Target("public void Merge() { var x = 1; }"), referenceAssembly),
                Missing("Target.Merge"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_NoDiagnostic_For_SingleArgOverload_When_Referenced_Assembly_Target_Has_NoInlining(bool referenceAssembly)
        {
            await VerifyAsync(
                Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target.Merge))"),
                await EmitAsync(Target("[MethodImpl(MethodImplOptions.NoInlining)] public void Merge() { var x = 1; }"), referenceAssembly));
        }

        [Test]
        public async Task LuceneDev4002_Reports_Literal_Method_On_Referenced_Assembly_Type()
        {
            await VerifyAsync(
                Caller(@"{|#0:StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), ""Merge"")|}"),
                await EmitAsync(Target("public void Merge() { var x = 1; }"), referenceAssembly: true),
                Missing("Target.Merge"));
        }

        [Test]
        public async Task LuceneDev4002_NoDiagnostic_For_Private_Method_On_Referenced_Assembly_Type()
        {
            // Not visible to this compilation, so there is nothing to check.
            await VerifyAsync(
                Caller(@"StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), ""Merge"")"),
                await EmitAsync(Target("private void Merge() { var x = 1; }"), referenceAssembly: true));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_NoDiagnostic_For_Abstract_Method_On_Referenced_Assembly_Type(bool referenceAssembly)
        {
            // NoInlining isn't inherited and has no effect here (LuceneDev4000).
            var library = @"
public abstract class Target
{
    public abstract void Merge();
}";
            await VerifyAsync(
                Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(Target), nameof(Target.Merge))"),
                await EmitAsync(library, referenceAssembly));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task LuceneDev4002_NoDiagnostic_For_Interface_Method_On_Referenced_Assembly_Type(bool referenceAssembly)
        {
            var library = @"
public interface ITarget
{
    void Merge();
}";
            await VerifyAsync(
                Caller("StackTraceHelper.DoesStackTraceContainMethod(nameof(ITarget.Merge))"),
                await EmitAsync(library, referenceAssembly));
        }

        private static string Target(string members) => @"
using System.Runtime.CompilerServices;

public class Target
{
    " + members + @"
}
";

        private static string Caller(string expression) => @"
namespace CallerNamespace
{
    using Lucene.Net.Util;

    public class Caller
    {
        public bool Check() => " + expression + @";
    }
}
" + StackTraceHelperStub;

        private static DiagnosticResult Missing(string methodName) =>
            new DiagnosticResult(Descriptors.LuceneDev4002_MissingNoInlining)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(0)
                .WithArguments(methodName);

        private static async Task VerifyAsync(string testCode, MetadataReference? references, params DiagnosticResult[] expected)
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev4002_StackTraceHelperNoInliningAnalyzer())
            {
                TestCode = testCode
            };
            test.ExpectedDiagnostics.AddRange(expected);
            if (references is not null)
                test.TestState.AdditionalReferences.Add(references);

            await test.RunAsync();
        }

        // Emits a real assembly, because a project reference within a test is an in-memory
        // compilation whose symbols still have their source. A reference assembly is what
        // MSBuild passes the compiler for a project reference.
        private static async Task<MetadataReference> EmitAsync(string source, bool referenceAssembly)
        {
            var references = await ReferenceAssemblies.Default.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            var compilation = CSharpCompilation.Create(
                "TargetLibrary",
                new[] { CSharpSyntaxTree.ParseText(source) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var stream = new MemoryStream();
            var result = compilation.Emit(
                stream,
                options: new EmitOptions(metadataOnly: referenceAssembly, includePrivateMembers: !referenceAssembly));
            Assert.That(result.Success, Is.True, string.Join("\n", result.Diagnostics));

            return MetadataReference.CreateFromImage(stream.ToArray());
        }
    }
}
