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

using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6006_6007_PreferJ2NCollectionCodeFixProvider
    {
        private static readonly MetadataReference J2NReference =
            MetadataReference.CreateFromFile(typeof(J2N.Collections.Generic.List<>).Assembly.Location);

        // OrderedHashSet<T> and OrderedDictionary<TKey, TValue> are not in the J2N version
        // referenced by the tests, so they are declared in source.
        private const string J2NOrderedCollectionStubs =
            """
            namespace J2N.Collections.Generic
            {
                public class OrderedHashSet<T> { }
                public class OrderedDictionary<TKey, TValue> { }
            }
            """;

        private static InjectableCodeFixTest CreateTest(string testCode, string fixedCode)
        {
            var test = new InjectableCodeFixTest(
                () => new LuceneDev6006_6007_PreferJ2NCollectionAnalyzer(),
                () => new LuceneDev6006_6007_PreferJ2NCollectionCodeFixProvider())
            {
                TestCode = testCode.ReplaceLineEndings(),
                FixedCode = fixedCode.ReplaceLineEndings(),
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };
            test.TestState.AdditionalReferences.Add(J2NReference);
            test.FixedState.AdditionalReferences.Add(J2NReference);
            return test;
        }

        [Test]
        public async Task SCGList_AddsJCGAliasAfterExistingUsings()
        {
            const string testCode =
                """
                using System;
                using System.Collections.Generic;

                public class Sample
                {
                    private readonly IList<int> values = new {|LuceneDev6006:List<int>|}();
                }
                """;

            const string fixedCode =
                """
                using System;
                using System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly IList<int> values = new JCG.List<int>();
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task ExistingJCGAlias_IsReused()
        {
            const string testCode =
                """
                using System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly IDictionary<string, int> map = new {|LuceneDev6006:Dictionary<string, int>|}();
                    private readonly ISet<int> set = new JCG.HashSet<int>();
                }
                """;

            const string fixedCode =
                """
                using System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly IDictionary<string, int> map = new JCG.Dictionary<string, int>();
                    private readonly ISet<int> set = new JCG.HashSet<int>();
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task QualifiedNames_AreReplacedWithJCGAlias()
        {
            const string testCode =
                """
                using SCG = System.Collections.Generic;

                public class Sample
                {
                    private readonly SCG.ISet<int> set = new {|LuceneDev6006:SCG.HashSet<int>|}();
                    private readonly SCG.IList<int> list = new {|LuceneDev6006:System.Collections.Generic.List<int>|}();
                    private readonly SCG.IDictionary<int, int> map = new {|LuceneDev6006:global::System.Collections.Generic.Dictionary<int, int>|}();
                }
                """;

            const string fixedCode =
                """
                using SCG = System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly SCG.ISet<int> set = new JCG.HashSet<int>();
                    private readonly SCG.IList<int> list = new JCG.List<int>();
                    private readonly SCG.IDictionary<int, int> map = new JCG.Dictionary<int, int>();
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task NestedTypes_AreAllReplaced()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    private readonly {|LuceneDev6006:Dictionary<string, {|LuceneDev6006:List<int>|}>|} map = new {|LuceneDev6006:Dictionary<string, {|LuceneDev6006:List<int>|}>|}();
                }
                """;

            const string fixedCode =
                """
                using System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly JCG.Dictionary<string, JCG.List<int>> map = new JCG.Dictionary<string, JCG.List<int>>();
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task NoExistingUsings_AddsJCGAliasBelowFileHeader()
        {
            const string testCode =
                """
                // File header

                namespace Sample;

                public class Derived : {|LuceneDev6006:System.Collections.Generic.List<int>|}
                {
                }
                """;

            const string fixedCode =
                """
                // File header

                using JCG = J2N.Collections.Generic;

                namespace Sample;

                public class Derived : JCG.List<int>
                {
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task NamespaceLevelUsings_AddsJCGAliasInNamespace()
        {
            const string testCode =
                """
                namespace Sample
                {
                    using System.Collections.Generic;

                    public class C
                    {
                        private readonly IList<int> values = new {|LuceneDev6006:List<int>|}();
                    }
                }
                """;

            const string fixedCode =
                """
                namespace Sample
                {
                    using System.Collections.Generic;
                    using JCG = J2N.Collections.Generic;

                    public class C
                    {
                        private readonly IList<int> values = new JCG.List<int>();
                    }
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task UsingAliasDeclaration_UsesFullyQualifiedName()
        {
            const string testCode =
                """
                using IntList = {|LuceneDev6006:System.Collections.Generic.List<int>|};

                public class Sample
                {
                    private readonly IntList values = new IntList();
                }
                """;

            const string fixedCode =
                """
                using IntList = J2N.Collections.Generic.List<int>;

                public class Sample
                {
                    private readonly IntList values = new IntList();
                }
                """;

            await CreateTest(testCode, fixedCode).RunAsync();
        }

        [Test]
        public async Task JCGLinkedTypes_AreReplacedWithOrderedTypes()
        {
            const string testCode =
                """
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly object set = new {|LuceneDev6007:JCG.LinkedHashSet<int>|}();
                    private readonly object map = new {|LuceneDev6007:J2N.Collections.Generic.LinkedDictionary<string, int>|}();
                }
                """;

            const string fixedCode =
                """
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly object set = new JCG.OrderedHashSet<int>();
                    private readonly object map = new JCG.OrderedDictionary<string, int>();
                }
                """;

            var test = CreateTest(testCode, fixedCode);
            test.TestState.Sources.Add(J2NOrderedCollectionStubs.ReplaceLineEndings());
            test.FixedState.Sources.Add(J2NOrderedCollectionStubs.ReplaceLineEndings());
            await test.RunAsync();
        }
    }
}
