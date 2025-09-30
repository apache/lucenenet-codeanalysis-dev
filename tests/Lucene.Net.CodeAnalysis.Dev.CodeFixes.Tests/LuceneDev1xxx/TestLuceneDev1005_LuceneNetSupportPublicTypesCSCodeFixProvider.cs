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

using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes;

public class TestLuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider
{
    [Test]
    public async Task PublicTypeInSupport_FileScopedNamespace_MakeInternalFix()
    {
        const string testCode =
            """
            namespace Lucene.Net.Support;

            public class MyClass
            {
            }
            """;

        const string fixedCode =
            """
            namespace Lucene.Net.Support;

            internal class MyClass
            {
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyClass")
            .WithLocation(3, 1);

        var test = new InjectableCodeFixTest(
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer(),
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider())
        {
            TestCode = testCode.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            ExpectedDiagnostics = { expected }
        };

        await test.RunAsync();
    }

    [Test]
    public async Task PublicTypeInSupport_BlockScopedNamespace_MakeInternalFix()
    {
        const string testCode =
            """
            namespace Lucene.Net.Support
            {
                public class MyClass
                {
                }
            }
            """;

        const string fixedCode =
            """
            namespace Lucene.Net.Support
            {
                internal class MyClass
                {
                }
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyClass")
            .WithLocation(3, 5);

        var test = new InjectableCodeFixTest(
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer(),
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider())
        {
            TestCode = testCode.ReplaceLineEndings(),
            FixedCode = fixedCode.ReplaceLineEndings(),
            ExpectedDiagnostics = { expected }
        };

        await test.RunAsync();
    }
}
