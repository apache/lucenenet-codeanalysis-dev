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

using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev1xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev;

public class TestLuceneDev1016_1017_1018_AnonymousClassAnalyzer
{
    private static Task VerifyAsync(string testCode, params DiagnosticResult[] expected)
    {
        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1016_1017_1018_AnonymousClassAnalyzer())
        {
            TestCode = testCode
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    private static DiagnosticResult NotPrivate(string typeName) =>
        new DiagnosticResult(Descriptors.LuceneDev1016_AnonymousClassShouldBePrivate)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(typeName)
            .WithLocation(0);

    private static DiagnosticResult NotSealed(string typeName) =>
        new DiagnosticResult(Descriptors.LuceneDev1017_AnonymousClassShouldBeSealed)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(typeName)
            .WithLocation(0);

    private static DiagnosticResult FieldNotPrivate(string fieldName, string typeName, int location = 0) =>
        new DiagnosticResult(Descriptors.LuceneDev1018_AnonymousClassFieldShouldBePrivate)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(fieldName, typeName)
            .WithLocation(location);

    [Test]
    public Task PrivateSealedWithPrivateFields_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                private sealed class ThreadAnonymousClass
                {
                    private int explicitlyPrivate;
                    int implicitlyPrivate;

                    public int Sum() => explicitlyPrivate + implicitlyPrivate;
                }
            }
            """);

    [Test]
    public Task NameWithoutMarker_NoDiagnostic() =>
        VerifyAsync("""
            public class AnonymousCollector
            {
                public int value;
            }
            """);

    [TestCase("public")]
    [TestCase("internal")]
    [TestCase("protected")]
    [TestCase("protected internal")]
    [TestCase("private protected")]
    public Task NestedNotPrivate_1016(string accessibility) =>
        VerifyAsync($$"""
            public class Outer
            {
                {{accessibility}} sealed class {|#0:ThreadAnonymousClass|} { }
            }
            """, NotPrivate("ThreadAnonymousClass"));

    [Test]
    public Task NestedImplicitlyPrivate_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                sealed class ThreadAnonymousClass { }
            }
            """);

    [Test]
    public Task TopLevel_1016(
        [Values("public", "internal")] string accessibility) =>
        VerifyAsync($$"""
            {{accessibility}} sealed class {|#0:ThreadAnonymousClass|} { }
            """, NotPrivate("ThreadAnonymousClass"));

    [Test]
    public Task PrivateNotSealed_1017() =>
        VerifyAsync("""
            public class Outer
            {
                private class {|#0:ThreadAnonymousClass|} { }
            }
            """, NotSealed("ThreadAnonymousClass"));

    [Test]
    public Task PrimaryConstructorNotSealed_1017() =>
        VerifyAsync("""
            public class Outer
            {
                private class {|#0:ThreadAnonymousClass|}(int value)
                {
                    public int Value() => value;
                }
            }
            """, NotSealed("ThreadAnonymousClass"));

    [Test]
    public Task Abstract_1017() =>
        VerifyAsync("""
            public class Outer
            {
                private abstract class {|#0:ThreadAnonymousClass|} { }
            }
            """, NotSealed("ThreadAnonymousClass"));

    [Test]
    public Task StaticClass_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                private static class ThreadAnonymousClass { }
            }
            """);

    [Test]
    public Task PrivateStruct_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                private struct ThreadAnonymousClass { }
            }
            """);

    [Test]
    public Task NotPrivateAndNotSealed_BothReported() =>
        VerifyAsync("""
            public class Outer
            {
                internal class {|#0:ThreadAnonymousClass|} { }
            }
            """, NotPrivate("ThreadAnonymousClass"), NotSealed("ThreadAnonymousClass"));

    [Test]
    public Task UnconventionalName_StillChecked() =>
        VerifyAsync("""
            public class Outer
            {
                private class {|#0:AnonymousClassThread|} { }
            }
            """, NotSealed("AnonymousClassThread"));

    [TestCase("public")]
    [TestCase("internal")]
    [TestCase("protected internal")]
    public Task InstanceFieldNotPrivate_1018(string accessibility) =>
        VerifyAsync($$"""
            public class Outer
            {
                private sealed class ThreadAnonymousClass
                {
                    {{accessibility}} int {|#0:value|};
                }
            }
            """, FieldNotPrivate("value", "ThreadAnonymousClass"));

    [Test]
    public Task MultipleVariables_EachReported() =>
        VerifyAsync("""
            public class Outer
            {
                private sealed class ThreadAnonymousClass
                {
                    internal int {|#0:a|}, {|#1:b|};
                }
            }
            """,
            FieldNotPrivate("a", "ThreadAnonymousClass", 0),
            FieldNotPrivate("b", "ThreadAnonymousClass", 1));

    [Test]
    public Task StructInstanceField_1018() =>
        VerifyAsync("""
            public class Outer
            {
                private struct ThreadAnonymousClass
                {
                    public int {|#0:value|};
                }
            }
            """, FieldNotPrivate("value", "ThreadAnonymousClass"));

    [Test]
    public Task StaticAndConstFields_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                public object Create() => DefaultSimilarityAnonymousClass.Default;

                private sealed class DefaultSimilarityAnonymousClass
                {
                    public static readonly DefaultSimilarityAnonymousClass Default = new DefaultSimilarityAnonymousClass();
                    internal static int count;
                    public const int Max = 1;
                }
            }
            """);

    [Test]
    public Task NonFieldMembers_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                private sealed class ThreadAnonymousClass
                {
                    public int Property { get; set; }
                    internal void Method() { }
                }
            }
            """);

    [Test]
    public Task FieldOfNestedOrdinaryType_NoDiagnostic() =>
        VerifyAsync("""
            public class Outer
            {
                private sealed class ThreadAnonymousClass
                {
                    private sealed class Helper
                    {
                        public int value;
                    }
                }
            }
            """);

    [Test]
    public Task OtherKindsOfType_NoDiagnostic() =>
        VerifyAsync("""
            public interface IAnonymousClassFactory { }
            public enum AnonymousClassKind { One }
            """);

    [Test]
    public Task GeneratedCode_NoDiagnostic() =>
        VerifyAsync("""
            [System.CodeDom.Compiler.GeneratedCode("tool", "1.0")]
            public class ThreadAnonymousClass
            {
                public int value;
            }
            """);
}
