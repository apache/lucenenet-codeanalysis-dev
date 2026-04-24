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

using Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx;
using Lucene.Net.CodeAnalysis.Dev.TestUtilities;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.Tests.LuceneDev6xxx
{
    [TestFixture]
    public class TestLuceneDev6001_6002_StringComparisonAnalyzer
    {
        [Test]
        public async Task Skips_SingleCharStringLiteral_Alone()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""H""); // Single-character string
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                // Expect no diagnostics because 6001 should skip single-character string literal alone
                ExpectedDiagnostics = { } // No diagnostics expected
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_For_SingleCharString_MissingComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello"";
        int index = text.IndexOf(""H"", 0, 5); // Single-character string with startIndex/count
    }
}";

            // Change the test to use InjectableAnalyzerTest (no CodeFix)
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { } // Asserting NO diagnostics are expected
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_IndexOf_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_StartsWith_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""Hello"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("StartsWith")
                .WithLocation("/0/Test0.cs", line: 9, column: 28);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_EndsWith_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""World"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("EndsWith")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_LastIndexOf_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World Hello"";
        int index = text.LastIndexOf(""Hello"");
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("LastIndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_IndexOf_WithStartIndex_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 5);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_IndexOf_WithStartIndexAndCount_MissingStringComparison()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""World"", 0, 11);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 26);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidStringComparison_CurrentCulture()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.CurrentCulture);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("IndexOf", "CurrentCulture")
                .WithLocation("/0/Test0.cs", line: 9, column: 43);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidStringComparison_CurrentCultureIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        bool starts = text.StartsWith(""hello"", StringComparison.CurrentCultureIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("StartsWith", "CurrentCultureIgnoreCase")
                .WithLocation("/0/Test0.cs", line: 9, column: 48);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidStringComparison_InvariantCulture()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        bool ends = text.EndsWith(""World"", StringComparison.InvariantCulture);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("EndsWith", "InvariantCulture")
                .WithLocation("/0/Test0.cs", line: 9, column: 44);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task Detects_InvalidStringComparison_InvariantCultureIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.LastIndexOf(""World"", StringComparison.InvariantCultureIgnoreCase);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("LastIndexOf", "InvariantCultureIgnoreCase")
                .WithLocation("/0/Test0.cs", line: 9, column: 47);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoError_WithOrdinal()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""Hello"", StringComparison.Ordinal);
        bool starts = text.StartsWith(""Hello"", StringComparison.Ordinal);
        bool ends = text.EndsWith(""World"", StringComparison.Ordinal);
        int lastIndex = text.LastIndexOf(""World"", StringComparison.Ordinal);
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics expected
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoError_WithOrdinalIgnoreCase()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index = text.IndexOf(""hello"", StringComparison.OrdinalIgnoreCase);
        bool starts = text.StartsWith(""HELLO"", StringComparison.OrdinalIgnoreCase);
        bool ends = text.EndsWith(""WORLD"", StringComparison.OrdinalIgnoreCase);
        int lastIndex = text.LastIndexOf(""world"", StringComparison.OrdinalIgnoreCase);
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics expected
            };

            await test.RunAsync();
        }


        [Test]
        public async Task Detects_MultipleViolations_InSameMethod()
        {
            var testCode = @"
using System;

public class Sample
{
    public void M()
    {
        string text = ""Hello World"";
        int index1 = text.IndexOf(""Hello"");
        int index2 = text.IndexOf(""World"", StringComparison.CurrentCulture);
        bool starts = text.StartsWith(""Hello"");
    }
}";

            var expected1 = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("IndexOf")
                .WithLocation("/0/Test0.cs", line: 9, column: 27);

            var expected2 = new DiagnosticResult(Descriptors.LuceneDev6002_InvalidStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6002_InvalidStringComparison.MessageFormat)
                .WithArguments("IndexOf", "CurrentCulture")
                .WithLocation("/0/Test0.cs", line: 10, column: 44);

            var expected3 = new DiagnosticResult(Descriptors.LuceneDev6001_MissingStringComparison)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6001_MissingStringComparison.MessageFormat)
                .WithArguments("StartsWith")
                .WithLocation("/0/Test0.cs", line: 11, column: 28);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected1, expected2, expected3 }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task NoWarning_OnNonStringTypes()
        {
            var testCode = @"
using System;

public class CustomType
{
    public int IndexOf(string value) => 0;
    public bool StartsWith(string value) => false;
}

public class Sample
{
    public void M()
    {
        var custom = new CustomType();
        int index = custom.IndexOf(""test"");
        bool starts = custom.StartsWith(""test"");
    }
}";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6001_6002_StringComparisonAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { } // No diagnostics expected - not on System.String
            };

            await test.RunAsync();
        }
    }
}
