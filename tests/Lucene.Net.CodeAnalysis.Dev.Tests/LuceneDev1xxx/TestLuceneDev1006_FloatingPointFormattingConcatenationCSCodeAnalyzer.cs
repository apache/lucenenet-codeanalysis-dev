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
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.Utility;

namespace Lucene.Net.CodeAnalysis.Dev
{
    public class TestLuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer
    {
        [Test]
        public async Task TestDiagnostic_FloatValuesInConcatenation()
        {
            var testCode = @"
public class C
{
    private float levelBottom = 1f;
    private float maxLevel = 2f;

    private string Message(string value) => value;

    public void M(int upto, int start)
    {
        Message(""  level "" + levelBottom + "" to "" + maxLevel + "": "" + (1 + upto - start) + "" segments"");
    }
}
";

            var expectedLevelBottom = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1006_FloatingPointFormatting.Id)
                .WithMessageFormat(Descriptors.LuceneDev1006_FloatingPointFormatting.MessageFormat)
                .WithArguments("levelBottom")
                .WithLocation("/0/Test0.cs", line: 11, column: 30);

            var expectedMaxLevel = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1006_FloatingPointFormatting.Id)
                .WithMessageFormat(Descriptors.LuceneDev1006_FloatingPointFormatting.MessageFormat)
                .WithArguments("maxLevel")
                .WithLocation("/0/Test0.cs", line: 11, column: 53);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expectedLevelBottom, expectedMaxLevel }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestNoDiagnostic_WhenValuesAlreadyFormatted()
        {
            var testCode = @"
namespace J2N.Numerics
{
    public static class Single
    {
        public static string ToString(float value) => value.ToString();
    }

    public static class Double
    {
        public static string ToString(double value) => value.ToString();
    }
}

public class C
{
    private float levelBottom = 1f;
    private float maxLevel = 2f;

    private string Message(string value) => value;

    public void M()
    {
        Message(""  level "" + J2N.Numerics.Single.ToString(levelBottom) + "" to "" + J2N.Numerics.Single.ToString(maxLevel));
    }
}
";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1006_FloatingPointFormattingConcatenationCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }
    }
}
