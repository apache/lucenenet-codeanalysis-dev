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

    [Test]
    public async Task PublicPartialClassInSupport_FileScopedNamespace_MakeInternalFix()
    {
        const string testCode1 =
            """
            namespace Lucene.Net.Support;

            public partial class MyPartialClass
            {
                public void Method1() { }
            }
            """;

        const string testCode2 =
            """
            namespace Lucene.Net.Support;

            public partial class MyPartialClass
            {
                public void Method2() { }
            }
            """;

        const string fixedCode1 =
            """
            namespace Lucene.Net.Support;

            internal partial class MyPartialClass
            {
                public void Method1() { }
            }
            """;

        const string fixedCode2 =
            """
            namespace Lucene.Net.Support;

            internal partial class MyPartialClass
            {
                public void Method2() { }
            }
            """;

        var expected1 = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyPartialClass")
            .WithLocation("/0/Test0.cs", line: 3, column: 1);

        var expected2 = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyPartialClass")
            .WithLocation("/0/Test1.cs", line: 3, column: 1);

        var test = new InjectableCodeFixTest(
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer(),
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider())
        {
            ExpectedDiagnostics = { expected1, expected2 },
            // Skip FixAll to test single fix application only
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
        };

        test.TestState.Sources.Add(testCode1.ReplaceLineEndings());
        test.TestState.Sources.Add(testCode2.ReplaceLineEndings());
        test.FixedState.Sources.Add(fixedCode1.ReplaceLineEndings());
        test.FixedState.Sources.Add(fixedCode2.ReplaceLineEndings());

        await test.RunAsync();
    }

    [Test]
    public async Task PublicPartialClassInSupport_BlockScopedNamespace_MakeInternalFix()
    {
        const string testCode1 =
            """
            namespace Lucene.Net.Support
            {
                public partial class MyPartialClass
                {
                    public void Method1() { }
                }
            }
            """;

        const string testCode2 =
            """
            namespace Lucene.Net.Support
            {
                public partial class MyPartialClass
                {
                    public void Method2() { }
                }
            }
            """;

        const string fixedCode1 =
            """
            namespace Lucene.Net.Support
            {
                internal partial class MyPartialClass
                {
                    public void Method1() { }
                }
            }
            """;

        const string fixedCode2 =
            """
            namespace Lucene.Net.Support
            {
                internal partial class MyPartialClass
                {
                    public void Method2() { }
                }
            }
            """;

        var expected1 = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyPartialClass")
            .WithLocation("/0/Test0.cs", line: 3, column: 5);

        var expected2 = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyPartialClass")
            .WithLocation("/0/Test1.cs", line: 3, column: 5);

        var test = new InjectableCodeFixTest(
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer(),
            () => new LuceneDev1005_LuceneNetSupportPublicTypesCSCodeFixProvider())
        {
            ExpectedDiagnostics = { expected1, expected2 },
            // Skip FixAll to test single fix application only
            CodeFixTestBehaviors = CodeFixTestBehaviors.FixOne
        };

        test.TestState.Sources.Add(testCode1.ReplaceLineEndings());
        test.TestState.Sources.Add(testCode2.ReplaceLineEndings());
        test.FixedState.Sources.Add(fixedCode1.ReplaceLineEndings());
        test.FixedState.Sources.Add(fixedCode2.ReplaceLineEndings());

        await test.RunAsync();
    }

    [Test]
    public async Task PublicTypeInSupport_WithLicenseHeader_PreservesHeader()
    {
        const string testCode =
            """
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

            namespace Lucene.Net.Support;

            public class MyClass
            {
            }
            """;

        const string fixedCode =
            """
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

            namespace Lucene.Net.Support;

            internal class MyClass
            {
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyClass")
            .WithLocation(20, 1);

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
    public async Task PublicTypeInSupport_WithLicenseHeaderInsideNamespace_PreservesHeader()
    {
        const string testCode =
            """
            namespace Lucene.Net.Support
            {
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

                public class MyClass
                {
                }
            }
            """;

        const string fixedCode =
            """
            namespace Lucene.Net.Support
            {
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

                internal class MyClass
                {
                }
            }
            """;

        var expected = new DiagnosticResult(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes)
            .WithSeverity(DiagnosticSeverity.Warning)
            .WithMessageFormat(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes.MessageFormat)
            .WithArguments("Class", "MyClass")
            .WithLocation(20, 5);

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
    public async Task PublicTypeInSupport_WithTrailingTrivia_PreservesTrailingTrivia()
    {
        const string testCode =
            """
            namespace Lucene.Net.Support
            {
                public class MyClass
                {
                } // Important trailing comment
            }
            """;

        const string fixedCode =
            """
            namespace Lucene.Net.Support
            {
                internal class MyClass
                {
                } // Important trailing comment
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
