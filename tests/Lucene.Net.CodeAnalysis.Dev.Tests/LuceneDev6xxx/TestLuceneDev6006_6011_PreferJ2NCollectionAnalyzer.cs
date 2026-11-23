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
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6006_6011_PreferJ2NCollectionAnalyzer
    {
        private static readonly MetadataReference J2NReference =
            MetadataReference.CreateFromFile(typeof(J2N.Collections.Generic.List<>).Assembly.Location);

        // Simulates a J2N version that predates OrderedHashSet<T> and OrderedDictionary<TKey, TValue>.
        private const string J2NLinkedCollectionStubs =
            """
            namespace J2N.Collections.Generic
            {
                public class LinkedHashSet<T> { }
                public class LinkedDictionary<TKey, TValue> { }
            }
            """;

        private static InjectableCSharpAnalyzerTest CreateTest(string testCode, bool referenceJ2N = true)
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6006_6011_PreferJ2NCollectionAnalyzer())
            {
                TestCode = testCode,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
            };
            if (referenceJ2N)
                test.TestState.AdditionalReferences.Add(J2NReference);
            return test;
        }

        [Test]
        public async Task SCGList_ReportsDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    private readonly {|#0:List<int>|} values = new {|#1:List<int>|}();
                }
                """;

            var test = CreateTest(testCode);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6006_PreferJ2NList)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.List<T>", "System.Collections.Generic.List<T>")
                .WithLocation(0));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6006_PreferJ2NList)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.List<T>", "System.Collections.Generic.List<T>")
                .WithLocation(1));
            await test.RunAsync();
        }

        [Test]
        public async Task SCGHashSetAndDictionary_ReportsDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    public void M()
                    {
                        var set = new {|#0:HashSet<string>|}();
                        var map = new {|#1:Dictionary<string, int>|}();
                    }
                }
                """;

            var test = CreateTest(testCode);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6007_PreferJ2NHashSet)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.HashSet<T>", "System.Collections.Generic.HashSet<T>")
                .WithLocation(0));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6008_PreferJ2NDictionary)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.Dictionary<TKey, TValue>", "System.Collections.Generic.Dictionary<TKey, TValue>")
                .WithLocation(1));
            await test.RunAsync();
        }

        [Test]
        public async Task SCGTypes_InVariousPositions_ReportsDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;
                using SCG = System.Collections.Generic;

                public class Derived : {|LuceneDev6006:List<int>|}
                {
                }

                public class Sample
                {
                    public IList<int> Property { get; } = new {|LuceneDev6006:System.Collections.Generic.List<int>|}();

                    public IDictionary<string, ISet<int>> M(IList<int> input)
                    {
                        {|LuceneDev6006:SCG.List<int>|} copy = new(input);
                        var nested = new {|LuceneDev6006:List<{|LuceneDev6007:HashSet<int>|}>|}();
                        var global = new {|LuceneDev6008:global::System.Collections.Generic.Dictionary<string, ISet<int>>|}();
                        var enumerator = default({|LuceneDev6006:List<int>|}.Enumerator);
                        return global;
                    }
                }
                """;

            await CreateTest(testCode).RunAsync();
        }

        [Test]
        public async Task SCGTypeInUsingAlias_ReportsDiagnostic()
        {
            const string testCode =
                """
                using IntList = {|LuceneDev6006:System.Collections.Generic.List<int>|};

                public class Sample
                {
                    private readonly IntList values = new IntList();
                }
                """;

            await CreateTest(testCode).RunAsync();
        }

        [Test]
        public async Task DisablingOneRule_OnlySuppressesThatCollectionType()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    public void M()
                    {
                        var list = new List<int>();
                        var set = new {|LuceneDev6007:HashSet<int>|}();
                        var map = new {|LuceneDev6008:Dictionary<string, int>|}();
                    }
                }
                """;

            var test = CreateTest(testCode);
            test.DisabledDiagnostics.Add("LuceneDev6006");
            await test.RunAsync();
        }

        [Test]
        public async Task JCGTypes_NoDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    private readonly IList<int> list = new JCG.List<int>();
                    private readonly ISet<int> set = new JCG.HashSet<int>();
                    private readonly IDictionary<string, int> map = new JCG.Dictionary<string, int>();
                }
                """;

            await CreateTest(testCode).RunAsync();
        }

        [Test]
        public async Task WithoutJ2NReference_NoDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    private readonly List<int> values = new List<int>();
                }
                """;

            await CreateTest(testCode, referenceJ2N: false).RunAsync();
        }

        [Test]
        public async Task UnboundGenericAndDocComment_NoDiagnostic()
        {
            const string testCode =
                """
                using System;
                using System.Collections.Generic;

                /// <summary>Not a <see cref="List{T}"/>.</summary>
                public class Sample
                {
                    public Type ListType => typeof(List<>);
                    public Type DictionaryType => typeof(Dictionary<,>);
                }
                """;

            await CreateTest(testCode).RunAsync();
        }

        [Test]
        public async Task NonCollectionGenericNames_NoDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class List<T> { }

                public class Sample
                {
                    public T Create<T>() where T : new() => new T();

                    public void M()
                    {
                        var local = new List<int>();
                        var created = Create<object>();
                        IEnumerable<int> values = new int[0];
                    }
                }
                """;

            await CreateTest(testCode).RunAsync();
        }

        [Test]
        public async Task JCGLinkedTypes_WithOrderedTypesAvailable_ReportsDiagnostic()
        {
            const string testCode =
                """
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    public void M()
                    {
                        var set = new {|#0:JCG.LinkedHashSet<int>|}();
                        var map = new {|#1:JCG.LinkedDictionary<string, int>|}();
                    }
                }
                """;

            var test = CreateTest(testCode);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6010_PreferOrderedHashSetOverLinkedHashSet)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.OrderedHashSet<T>", "J2N.Collections.Generic.LinkedHashSet<T>")
                .WithLocation(0));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6011_PreferOrderedDictionaryOverLinkedDictionary)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.OrderedDictionary<TKey, TValue>", "J2N.Collections.Generic.LinkedDictionary<TKey, TValue>")
                .WithLocation(1));
            await test.RunAsync();
        }

        [Test]
        public async Task JCGLinkedTypes_WithoutOrderedTypesAvailable_NoDiagnostic()
        {
            const string testCode =
                """
                using JCG = J2N.Collections.Generic;

                public class Sample
                {
                    public void M()
                    {
                        var set = new JCG.LinkedHashSet<int>();
                        var map = new JCG.LinkedDictionary<string, int>();
                    }
                }
                """;

            var test = CreateTest(testCode, referenceJ2N: false);
            test.TestState.Sources.Add(J2NLinkedCollectionStubs);
            await test.RunAsync();
        }

        [Test]
        public async Task SCGOrderedDictionary_WithJCGOrderedDictionaryAvailable_ReportsDiagnostic()
        {
            const string testCode =
                """
                using System.Collections.Generic;

                public class Sample
                {
                    public void M()
                    {
                        var map = new {|#0:OrderedDictionary<string, int>|}();
                    }
                }
                """;

            var test = CreateTest(testCode);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Descriptors.LuceneDev6009_PreferJ2NOrderedDictionary)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithArguments("J2N.Collections.Generic.OrderedDictionary<TKey, TValue>", "System.Collections.Generic.OrderedDictionary<TKey, TValue>")
                .WithLocation(0));
            await test.RunAsync();
        }
    }
}
