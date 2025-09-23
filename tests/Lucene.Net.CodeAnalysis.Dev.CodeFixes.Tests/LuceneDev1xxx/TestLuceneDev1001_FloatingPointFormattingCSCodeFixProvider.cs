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

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes
{
    public class TestLuceneDev1001_FloatingPointFormattingCSCodeFixProvider
    {
        [Test]
        public async Task TestFix_Float_ToString()
        {
            var testCode = @"
using System;
using System.Globalization;

// Mock for J2N.Numerics so we don't need to reference it
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float f, IFormatProvider provider) => string.Empty;
    }

    public static class Double
    {
        public static string ToString(double d, IFormatProvider provider) => string.Empty;
    }
}

public class MyClass
{
    private readonly float float1 = 1f;

    public void MyMethod()
    {
        string result = float1.ToString(CultureInfo.InvariantCulture);
    }
}";

            var fixedCode = @"
using System;
using System.Globalization;

// Mock for J2N.Numerics so we don't need to reference it
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float f, IFormatProvider provider) => string.Empty;
    }

    public static class Double
    {
        public static string ToString(double d, IFormatProvider provider) => string.Empty;
    }
}

public class MyClass
{
    private readonly float float1 = 1f;

    public void MyMethod()
    {
        string result = J2N.Numerics.Single.ToString(float1, CultureInfo.InvariantCulture);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1001_FloatingPointFormatting)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1001_FloatingPointFormatting.MessageFormat)
                .WithArguments("float1.ToString")
                .WithLocation("/0/Test0.cs", line: 25, column: 25);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer(),
                () => new LuceneDev1001_FloatingPointFormattingCSCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_Double_ToString()
        {
            var testCode = @"
using System;
using System.Globalization;

// Mock for J2N.Numerics so we don't need to reference it
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float f, IFormatProvider provider) => string.Empty;
    }

    public static class Double
    {
        public static string ToString(double d, IFormatProvider provider) => string.Empty;
    }
}

public class MyClass
{
    private readonly double double1 = 1.0;

    public void MyMethod()
    {
        string result = double1.ToString(CultureInfo.InvariantCulture);
    }
}";

            var fixedCode = @"
using System;
using System.Globalization;

// Mock for J2N.Numerics so we don't need to reference it
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float f, IFormatProvider provider) => string.Empty;
    }

    public static class Double
    {
        public static string ToString(double d, IFormatProvider provider) => string.Empty;
    }
}

public class MyClass
{
    private readonly double double1 = 1.0;

    public void MyMethod()
    {
        string result = J2N.Numerics.Double.ToString(double1, CultureInfo.InvariantCulture);
    }
}";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1001_FloatingPointFormatting)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1001_FloatingPointFormatting.MessageFormat)
                .WithArguments("double1.ToString")
                .WithLocation("/0/Test0.cs", line: 25, column: 25);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer(),
                () => new LuceneDev1001_FloatingPointFormattingCSCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
