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

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes;

public class TestLuceneDev3000_CrefToNonPublicSymbolCodeFixProvider
{
    private static Task RunAsync(string testCode, string fixedCode, string equivalenceKey, params DiagnosticResult[] expected)
    {
        var test = new InjectableCodeFixTest(
            () => new LuceneDev3000_CrefToNonPublicSymbolAnalyzer(),
            () => new LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider())
        {
            TestCode = testCode.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            CodeActionEquivalenceKey = equivalenceKey,
        };
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync();
    }

    [Test]
    public async Task ReplaceWithCodeTag_SelfClosing()
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

        const string fixedCode =
            """
            public class TypeTokenFilter
            {
                private bool useWhiteList;

                /// <summary>
                /// Uses <c>useWhiteList</c> to decide.
                /// </summary>
                public bool Accept() => useWhiteList;
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev3000_CrefToNonPublicSymbol)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithArguments("useWhiteList")
            .WithLocation(0);

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.ReplaceWithCodeTagEquivalenceKey, expected);
    }

    [Test]
    public async Task ReplaceWithCodeTag_LinkText_UsesLinkText()
    {
        const string testCode =
            """
            public class Sample
            {
                private void Method(int x) { }

                /// <summary>
                /// Calls <see cref="{|LuceneDev3000:Method(int)|}">the method</see>.
                /// Calls <see cref="{|LuceneDev3000:Method(int)|}"></see>.
                /// </summary>
                public void Run() { }
            }
            """;

        const string fixedCode =
            """
            public class Sample
            {
                private void Method(int x) { }

                /// <summary>
                /// Calls <c>the method</c>.
                /// Calls <c>Method(int)</c>.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.ReplaceWithCodeTagEquivalenceKey);
    }

    [Test]
    public async Task ReplaceWithCodeTag_AllInDocument()
    {
        const string testCode =
            """
            internal class Helper { }

            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:field|}"/> and <see cref = '{|LuceneDev3000:Helper|}' />.
                /// </summary>
                /// <seealso cref="{|LuceneDev3000:Sample.field|}"/>
                public void Run() { }
            }
            """;

        const string fixedCode =
            """
            internal class Helper { }

            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses <c>field</c> and <c>Helper</c>.
                /// </summary>
                /// <c>Sample.field</c>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.ReplaceWithCodeTagEquivalenceKey);
    }

    [Test]
    public async Task ReplaceWithText_SelfClosing()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:field|}"/> to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        const string fixedCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses field to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.ReplaceWithTextEquivalenceKey);
    }

    [Test]
    public async Task ReplaceWithText_LinkText_UsesLinkText()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:field|}">the <b>field</b></see> to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        const string fixedCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses the <b>field</b> to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.ReplaceWithTextEquivalenceKey);
    }

    [Test]
    public async Task MakePublic_Field()
    {
        const string testCode =
            """
            public class Sample
            {
                private int field;

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:field|}"/> to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        const string fixedCode =
            """
            public class Sample
            {
                public int field;

                /// <summary>
                /// Uses <see cref="field"/> to decide.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }

    [Test]
    [TestCase("internal static")]
    [TestCase("private protected static")]
    [TestCase("static")]
    public async Task MakePublic_Method(string modifiers)
    {
        string testCode =
            $$"""
            public class Sample
            {
                /// <summary>Helps.</summary>
                {{modifiers}} void Helper() { }

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:Helper|}"/>.
                /// </summary>
                public void Run() { }
            }
            """;

        string fixedCode =
            $$"""
            public class Sample
            {
                /// <summary>Helps.</summary>
                public static void Helper() { }

                /// <summary>
                /// Uses <see cref="Helper"/>.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }

    [Test]
    public async Task MakePublic_Type()
    {
        const string testCode =
            """
            internal class Helper { }

            /// <summary>
            /// Uses <see cref="{|LuceneDev3000:Helper|}"/>.
            /// </summary>
            public class Sample { }
            """;

        const string fixedCode =
            """
            public class Helper { }

            /// <summary>
            /// Uses <see cref="Helper"/>.
            /// </summary>
            public class Sample { }
            """;

        await RunAsync(testCode, fixedCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }

    [Test]
    public async Task MakePublic_ContainingTypeNotVisible_NotOffered()
    {
        const string testCode =
            """
            public class Sample
            {
                private class Nested
                {
                    internal int Value;
                }

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:Nested.Value|}"/>.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, testCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }

    [Test]
    public async Task MakePublic_MultipleFieldDeclarators_NotOffered()
    {
        const string testCode =
            """
            public class Sample
            {
                private int a, b;

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:a|}"/>.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, testCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }

    [Test]
    public async Task MakePublic_AmbiguousCref_NotOffered()
    {
        const string testCode =
            """
            public class Sample
            {
                private void Method() { }
                private void Method(int x) { }

                /// <summary>
                /// Uses <see cref="{|LuceneDev3000:Method|}"/>.
                /// </summary>
                public void Run() { }
            }
            """;

        await RunAsync(testCode, testCode, LuceneDev3000_CrefToNonPublicSymbolCodeFixProvider.MakePublicEquivalenceKey);
    }
}
