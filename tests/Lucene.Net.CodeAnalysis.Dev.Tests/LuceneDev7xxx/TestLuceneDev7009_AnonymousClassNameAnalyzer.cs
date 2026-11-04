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
using Lucene.Net.CodeAnalysis.Dev.LuceneDev7xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev;

public class TestLuceneDev7009_AnonymousClassNameAnalyzer
{
    private static Task VerifyNoDiagnosticAsync(string testCode)
    {
        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7009_AnonymousClassNameAnalyzer())
        {
            TestCode = testCode
        };
        return test.RunAsync();
    }

    private static Task VerifyDiagnosticAsync(string testCode, string typeName)
    {
        var expected = new DiagnosticResult(Descriptors.LuceneDev7009_AnonymousClassName)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments(typeName)
            .WithLocation(0);

        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev7009_AnonymousClassNameAnalyzer())
        {
            TestCode = testCode,
            ExpectedDiagnostics = { expected }
        };
        return test.RunAsync();
    }

    [TestCase("ThreadAnonymousClass")]
    [TestCase("ThreadAnonymousClass1")]
    [TestCase("ThreadAnonymousClass123")]
    [TestCase("AnonymousClass")]
    [TestCase("AnonymousClassThreadAnonymousClass")]
    public Task ConventionalName_NoDiagnostic(string name) =>
        VerifyNoDiagnosticAsync($$"""
            public class Outer
            {
                private sealed class {{name}} { }
            }
            """);

    [Test]
    public Task GenericConventionalName_NoDiagnostic() =>
        VerifyNoDiagnosticAsync("""
            public class Outer
            {
                private sealed class ThreadAnonymousClass1<T> { }
            }
            """);

    [TestCase("AnonymousClassThread")]
    [TestCase("ThreadAnonymousClassForSomeTest")]
    [TestCase("RandomAnonymousClassHelper")]
    [TestCase("ThreadAnonymousClass1A")]
    [TestCase("ThreadAnonymousClass_1")]
    public Task UnconventionalName_Diagnostic(string name) =>
        VerifyDiagnosticAsync($$"""
            public class Outer
            {
                private sealed class {|#0:{{name}}|} { }
            }
            """, name);

    [Test]
    public Task GenericUnconventionalName_Diagnostic() =>
        VerifyDiagnosticAsync("""
            public class Outer
            {
                private sealed class {|#0:AnonymousClassThread|}<T> { }
            }
            """, "AnonymousClassThread");

    [Test]
    public Task Struct_Diagnostic() =>
        VerifyDiagnosticAsync("""
            public class Outer
            {
                private struct {|#0:AnonymousClassThread|} { }
            }
            """, "AnonymousClassThread");

    [Test]
    public Task NameWithoutMarker_NoDiagnostic() =>
        VerifyNoDiagnosticAsync("""
            public class AnonymousCollector { }
            public class ThreadAnonymousInnerClassHelper { }
            public class Anonymousclasshelper { }
            """);

    [Test]
    public Task OtherKindsOfType_NoDiagnostic() =>
        VerifyNoDiagnosticAsync("""
            public interface IAnonymousClassFactory { }
            public enum AnonymousClassKind { One }
            public delegate void AnonymousClassCallback();
            """);

    [Test]
    public Task GeneratedCode_NoDiagnostic() =>
        VerifyNoDiagnosticAsync("""
            [System.CodeDom.Compiler.GeneratedCode("tool", "1.0")]
            public class AnonymousClassThread { }
            """);
}
