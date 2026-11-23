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
using Lucene.Net.CodeAnalysis.Dev.LuceneDev3xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev;

public class TestLuceneDev3000_CrefToNonPublicSymbolAnalyzer
{
    private static Task RunAsync(string testCode, params DiagnosticResult[] expected)
    {
        var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev3000_CrefToNonPublicSymbolAnalyzer())
        {
            TestCode = testCode
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Test]
    public async Task PublicMethod_SeeCrefToPrivateField_Diagnostic()
    {
        const string testCode =
            """
            public class TypeTokenFilter
            {
                private bool useWhiteList;

                /// <summary>
                /// Uses <see cref="{|#0:useWhiteList|}"/> to decide.
                /// </summary>
                public bool Accept() => useWhiteList;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev3000_CrefToNonPublicSymbol)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("useWhiteList")
            .WithLocation(0);

        await RunAsync(testCode, expected);
    }

    [Test]
    public async Task PublicType_SeeCrefToInternalType_Diagnostic()
    {
        const string testCode =
            """
            internal class Helper { }

            /// <summary>
            /// Uses <see cref="{|#0:Helper|}"/>.
            /// </summary>
            public class Sample { }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev3000_CrefToNonPublicSymbol)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("Helper")
            .WithLocation(0);

        await RunAsync(testCode, expected);
    }

    [Test]
    public async Task AllSeeTagForms_Diagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;
                private void Method(int x) { }

                /// <summary>
                /// Self-closing: <see cref="{|LuceneDev3000:field|}"/>
                /// Extra spaces: <see   cref = "{|LuceneDev3000:field|}"   />
                /// Single quotes: <see cref='{|LuceneDev3000:field|}'/>
                /// No space before slash: <see cref="{|LuceneDev3000:field|}"/>
                /// Link text: <see cref="{|LuceneDev3000:field|}">the field</see>
                /// Empty link text: <see cref="{|LuceneDev3000:field|}"></see>
                /// Qualified: <see cref="{|LuceneDev3000:Sample.field|}"/>
                /// Method with parameters: <see cref="{|LuceneDev3000:Method(int)|}"/>
                /// See also: <seealso cref="{|LuceneDev3000:Method|}"/>
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    [TestCase("private")]
    [TestCase("internal")]
    [TestCase("private protected")]
    public async Task NonPublicMemberKinds_Diagnostic(string accessibility)
    {
        string testCode =
            $$"""
            public class Sample
            {
                {{accessibility}} int Field;
                {{accessibility}} int Property { get; set; }
                {{accessibility}} void Method() { }
                {{accessibility}} event System.EventHandler Event;
                {{accessibility}} Sample(int x) { }
                {{accessibility}} class Nested { }

                /// <summary>
                /// <see cref="{|LuceneDev3000:Field|}"/>
                /// <see cref="{|LuceneDev3000:Property|}"/>
                /// <see cref="{|LuceneDev3000:Method|}"/>
                /// <see cref="{|LuceneDev3000:Event|}"/>
                /// <see cref="{|LuceneDev3000:Sample(int)|}"/>
                /// <see cref="{|LuceneDev3000:Nested|}"/>
                /// </summary>
                public Sample() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task PublicMemberOfNonPublicType_Diagnostic()
    {
        const string testCode =
            """
            internal class Helper
            {
                public void DoWork() { }
            }

            public class Sample
            {
                private class Nested
                {
                    public int Value;
                }

                /// <summary>
                /// <see cref="{|LuceneDev3000:Helper.DoWork|}"/>
                /// <see cref="{|LuceneDev3000:Nested.Value|}"/>
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task CrefToGenericNonPublicType_Diagnostic()
    {
        const string testCode =
            """
            internal class Helper<T> { }

            /// <summary>
            /// <see cref="{|LuceneDev3000:Helper{T}|}"/>
            /// </summary>
            public class Sample { }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task CrefToOverloadedNonPublicMethodGroup_Diagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private void Method() { }
                private void Method(int x) { }

                /// <summary>
                /// <see cref="{|LuceneDev3000:Method|}"/>
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task DocCommentElsewhereInComment_Diagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>Runs.</summary>
                /// <param name="x">See <see cref="{|LuceneDev3000:field|}"/>.</param>
                /// <returns>Like <see cref="{|LuceneDev3000:field|}"/>.</returns>
                /// <remarks>
                /// <para>Nested <see cref="{|LuceneDev3000:field|}"/>.</para>
                /// </remarks>
                public int Run(int x) => x;
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task MultiLineDocComment_Diagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /**
                 * <summary>Uses <see cref="{|LuceneDev3000:field|}"/>.</summary>
                 */
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    [TestCase("public class")]
    [TestCase("public struct")]
    [TestCase("public interface")]
    [TestCase("public record")]
    public async Task VisibleTypeKinds_Diagnostic(string typeKind)
    {
        string testCode =
            $$"""
            internal class Helper { }

            /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
            {{typeKind}} Sample { }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task VisibleDocumentedMemberKinds_Diagnostic()
    {
        const string testCode =
            """
            internal class Helper { }

            /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
            public delegate void MyDelegate();

            /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
            public enum MyEnum
            {
                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                A
            }

            public class Sample
            {
                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public int Field1, Field2;

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public int Property { get; set; }

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public int this[int i] => i;

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public event System.EventHandler Event1;

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public event System.EventHandler Event2 { add { } remove { } }

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                protected Sample() { }

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                protected internal void Method() { }

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                [System.Obsolete]
                public void AttributedMethod() { }

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public static Sample operator +(Sample a, Sample b) => a;

                /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                public static implicit operator int(Sample s) => 0;
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task VisibleDocumentedMemberInNamespace_Diagnostic()
    {
        const string testCode =
            """
            namespace Lucene.Net.Analysis
            {
                internal class Helper { }

                public class Outer
                {
                    /// <summary><see cref="{|LuceneDev3000:Helper|}"/></summary>
                    public class Inner { }
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    [TestCase("private")]
    [TestCase("internal")]
    [TestCase("private protected")]
    public async Task NonPublicDocumentedMember_NoDiagnostic(string accessibility)
    {
        string testCode =
            $$"""
            public class Sample
            {
                private int field;

                /// <summary>
                /// <see cref="field"/>
                /// </summary>
                {{accessibility}} void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task MemberOfNonPublicType_NoDiagnostic()
    {
        const string testCode =
            """
            internal class Helper { }

            /// <summary><see cref="Helper"/></summary>
            internal class Sample
            {
                private int field;

                /// <summary><see cref="field"/></summary>
                public void Run() { }

                /// <summary><see cref="field"/></summary>
                public class Nested { }
            }

            public class Outer
            {
                private class Inner
                {
                    private int field;

                    /// <summary><see cref="field"/></summary>
                    public void Run() { }
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task LocalFunction_NoDiagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                public void Run()
                {
                    /// <summary><see cref="field"/></summary>
                    void Local() { }

                    Local();
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task CrefToPublicSymbols_NoDiagnostic()
    {
        const string testCode =
            """
            using System.Collections.Generic;

            namespace Lucene.Net.Analysis
            {
                public class Sample
                {
                    public int Field;
                    protected int ProtectedField;
                    protected internal int ProtectedInternalField;

                    public class Nested
                    {
                        public void DoWork() { }
                    }

                    /// <summary>
                    /// <see cref="Field"/>
                    /// <see cref="ProtectedField"/>
                    /// <see cref="ProtectedInternalField"/>
                    /// <see cref="Nested.DoWork"/>
                    /// <see cref="Sample"/>
                    /// <see cref="Lucene.Net.Analysis"/>
                    /// <see cref="string"/>
                    /// <see cref="System.String.Length"/>
                    /// <see cref="List{T}"/>
                    /// <see cref="IList{T}.Add(T)"/>
                    /// <seealso cref="Run{T}(T)"/>
                    /// <see cref="T"/>
                    /// </summary>
                    public void Run<T>(T value) { }
                }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task ExplicitInterfaceImplementation_NoDiagnostic()
    {
        const string testCode =
            """
            public interface IFoo
            {
                void Bar();
            }

            public class Sample : IFoo
            {
                void IFoo.Bar() { }

                /// <summary>
                /// <see cref="IFoo.Bar"/>
                /// <see cref="IFoo.Bar()"/>
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task NonLinkingCrefTags_NoDiagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// <see langword="null"/>
                /// <see href="https://lucene.apache.org/"/>
                /// <c>field</c>
                /// </summary>
                /// <inheritdoc cref="field"/>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }

    [Test]
    public async Task UnresolvedCref_NoDiagnostic()
    {
        const string testCode =
            """
            public class Sample
            {
                /// <summary>
                /// <see cref="DoesNotExist"/>
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode);
    }
}
