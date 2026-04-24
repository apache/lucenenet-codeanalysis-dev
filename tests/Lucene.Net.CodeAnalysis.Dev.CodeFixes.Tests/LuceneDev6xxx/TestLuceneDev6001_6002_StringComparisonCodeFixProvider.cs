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

using Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6001_6002_StringComparisonCodeFixProvider
    {
        [Test]
        public async Task TestFix_IndexOf_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"");
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_StartsWith_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""Hello"");
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""Hello"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("StartsWith")
                .WithLocation("/0/Test0.cs", line: 9, column: 28);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_EndsWith_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""World"");
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""World"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("EndsWith")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_LastIndexOf_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World Hello"";
        int index = text.LastIndexOf(""Hello"");
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World Hello"";
        int index = text.LastIndexOf(""Hello"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("LastIndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_IndexOf_WithStartIndex_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 5);
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 5, StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_IndexOf_WithStartIndexAndCount_MissingStringComparison()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 0, 11);
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 0, 11, StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_IndexOf_InvalidStringComparison_CurrentCulture()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.CurrentCulture);
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("IndexOf", "CurrentCulture")
                .WithLocation("/0/Test0.cs", line: 9, column: 43);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_StartsWith_InvalidStringComparison_InvariantCulture()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""Hello"", StringComparison.InvariantCulture);
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""Hello"", StringComparison.Ordinal);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("StartsWith", "InvariantCulture")
                .WithLocation("/0/Test0.cs", line: 9, column: 48);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_EndsWith_InvalidStringComparison_CurrentCultureIgnoreCase()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""WORLD"", StringComparison.CurrentCultureIgnoreCase);
    }
}";

            var fixedCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""WORLD"", StringComparison.OrdinalIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("EndsWith", "CurrentCultureIgnoreCase")
                .WithLocation("/0/Test0.cs", line: 9, column: 44);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestNoError_WithOrdinal()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.Ordinal);
        bool starts = text.StartsWith(""Hello"", StringComparison.Ordinal);
        bool ends = text.EndsWith(""World"", StringComparison.Ordinal);
        int lastIndex = text.LastIndexOf(""World"", StringComparison.Ordinal);
    }
}";

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics expected
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestNoError_WithOrdinalIgnoreCase()
        {
            var testCode = @"
using System;

public class MyClass
{
    public void MyMethod()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""hello"", StringComparison.OrdinalIgnoreCase);
        bool starts = text.StartsWith(""HELLO"", StringComparison.OrdinalIgnoreCase);
        bool ends = text.EndsWith(""WORLD"", StringComparison.OrdinalIgnoreCase);
        int lastIndex = text.LastIndexOf(""world"", StringComparison.OrdinalIgnoreCase);
    }
}";

            var test = new InjectableCodeFixTest(
                () => new LuceneDev6001_6002_StringComparisonAnalyzer(),
                () => new LuceneDev6001_6002_StringComparisonCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics expected
            };

            await test.RunAsync();
        }

    }
}
