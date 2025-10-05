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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.Utility;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes
{
    public class TestLuceneDev1006_FloatingPointFormattingInterpolationCSCodeFixProvider
    {
        [Test]
        public async Task TestFix_FloatInInterpolation()
        {
            var testCode = @"
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {levelBottom}"";
    }
}
";

            var fixedCode = @"
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {J2N.Numerics.Single.ToString(levelBottom)}"";
    }
}
";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1006_FloatingPointFormatting)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1006_FloatingPointFormatting.MessageFormat)
                .WithArguments("levelBottom")
                .WithLocation("/0/Test0.cs", line: 16, column: 27);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer(),
                () => new LuceneDev1001_FloatingPointFormattingCSCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }


        [Test]
        public async Task TestFix_WithAlignmentAndFormatClause()
        {
            var testCode = @"namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
        public static string ToString(float value, string format) => value.ToString(format);
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {levelBottom,5:F2}"";
    }
}
";

            var fixedCode = @"namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
        public static string ToString(float value, string format) => value.ToString(format);
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {J2N.Numerics.Single.ToString(levelBottom, ""F2""),5}"";
    }
}
";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1006_FloatingPointFormatting)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1006_FloatingPointFormatting.MessageFormat)
                .WithArguments("levelBottom")
                .WithLocation("/0/Test0.cs", line: 16, column: 27);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer(),
                () => new LuceneDev1001_FloatingPointFormattingCSCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestFix_WithFormatClause()
        {
            var testCode = @"
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
        public static string ToString(float value, string format) => value.ToString(format);
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {levelBottom:F2}"";
    }
}
";

            var fixedCode = @"
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
        public static string ToString(float value, string format) => value.ToString(format);
    }
}

public class C
{
    private float levelBottom = 1f;

    public string Message()
    {
        return $""  level {J2N.Numerics.Single.ToString(levelBottom, ""F2"")}"";
    }
}
";

            var expected = new DiagnosticResult(Descriptors.LuceneDev1006_FloatingPointFormatting)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev1006_FloatingPointFormatting.MessageFormat)
                .WithArguments("levelBottom")
                .WithLocation("/0/Test0.cs", line: 17, column: 27);

            var test = new InjectableCodeFixTest(
                () => new LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer(),
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
