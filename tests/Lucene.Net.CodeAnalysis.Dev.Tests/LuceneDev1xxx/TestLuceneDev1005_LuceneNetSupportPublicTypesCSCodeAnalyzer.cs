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
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev
{
    public class TestLuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer
    {
        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            const string testCode = "";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_FileScopedNamespace_PositiveTest(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $"""
                namespace Lucene.Net.Support;
                public {typeKind} {typeName};
                """;

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.Id)
                .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
                .WithArguments(typeKindDesc, typeName)
                .WithLocation(2, 1);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_FileScopedNamespace_NegativeTest_PublicInAnotherNamespace(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $"""
                 namespace Lucene.Net.SomethingElse;
                 public {typeKind} {typeName};
                 """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_FileScopedNamespace_NegativeTest_NonPublicInSupport(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $"""
                 namespace Lucene.Net.Support.Bar;
                 internal {typeKind} {typeName};
                 """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_FileScopedNamespace_Delegate_PositiveTest()
        {
            const string testCode =
                """
                namespace Lucene.Net.Support;
                public delegate void PublicDelegate();
                """;

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.Id)
                .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
                .WithArguments("Delegate", "PublicDelegate")
                .WithLocation(2, 1);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_FileScopedNamespace_Delegate_NegativeTest_PublicInAnotherNamespace()
        {
            const string testCode =
                """
                namespace Lucene.Net.SomethingElse;
                public delegate void PublicDelegate();
                """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_FileScopedNamespace_Delegate_NegativeTest_NonPublicInSupport()
        {
            const string testCode =
                """
                namespace Lucene.Net.Support;
                internal delegate void PublicDelegate();
                """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_BlockScopedNamespace_PositiveTest(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $$"""
                namespace Lucene.Net.Support
                {
                    public {{typeKind}} {{typeName}};
                }
                """;

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.Id)
                .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
                .WithArguments(typeKindDesc, typeName)
                .WithLocation(3, 5);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_BlockScopedNamespace_NegativeTest_PublicInAnotherNamespace(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $$"""
                namespace Lucene.Net.SomethingElse
                {
                    public {{typeKind}} {{typeName}};
                }
                """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        [TestCase("class")]
        [TestCase("struct")]
        [TestCase("interface")]
        [TestCase("record")]
        [TestCase("record struct")]
        [TestCase("enum")]
        public async Task TestDiagnostic_BlockScopedNamespace_NegativeTest_NonPublicInSupport(string typeKind)
        {
            string typeKindDesc = typeKind[0].ToString().ToUpper() + typeKind[1..];
            string typeName = $"Public{typeKindDesc.Replace(" ", "")}";

            string testCode =
                $$"""
                  namespace Lucene.Net.Support.Foo
                  {
                      internal {{typeKind}} {{typeName}};
                  }
                  """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_BlockScopedNamespace_Delegate_PositiveTest()
        {
            const string testCode =
                """
                namespace Lucene.Net.Support
                {
                    public delegate void PublicDelegate();
                }
                """;

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.Id)
                .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
                .WithArguments("Delegate", "PublicDelegate")
                .WithLocation(3, 5);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_BlockScopedNamespace_Delegate_NegativeTest_PublicInAnotherNamespace()
        {
            const string testCode =
                """
                namespace Lucene.Net.SomethingElse
                {
                    public delegate void PublicDelegate();
                }
                """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_BlockScopedNamespace_Delegate_NegativeTest_NonPublicInSupport()
        {
            const string testCode =
                """
                namespace Lucene.Net.Support
                {
                    internal delegate void PublicDelegate();
                }
                """;

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
