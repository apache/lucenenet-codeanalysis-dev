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
    public class TestLuceneDev6015_6016_CatchHelperAnalyzer
    {
        private const string Helpers = @"
namespace Lucene
{
    internal static class ExceptionExtensions
    {
        public static bool IsException(this System.Exception e) => true;
        public static bool IsIOException(this System.Exception e) => e is System.IO.IOException;
        public static bool IsNumberFormatException(this System.Exception e) => e is System.FormatException;
    }
}";

        private const string TestFrameworkHelpers = @"
namespace Lucene.Net
{
    internal static class ExceptionExtensions
    {
        public static bool IsIllegalArgumentException(this System.Exception e) => e is System.ArgumentException;
    }
}";

        private const string UnrelatedHelpers = @"
namespace Lucene.Net.Util
{
    public static class ExceptionExtensions
    {
        public static bool IsSuppressed(this System.Exception e) => false;
    }
}";

        private static DiagnosticResult Expected6015(string helper, string caughtType)
            => new DiagnosticResult(Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithMessageFormat(Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException.MessageFormat)
                .WithArguments(helper, caughtType)
                .WithLocation(0);

        private static DiagnosticResult Expected6016(int location = 0)
            => new DiagnosticResult(Descriptors.LuceneDev6016_CatchShouldUseHelperFilter)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithMessageFormat(Descriptors.LuceneDev6016_CatchShouldUseHelperFilter.MessageFormat)
                .WithLocation(location);

        private static async Task VerifyAsync(string testCode, params DiagnosticResult[] expected)
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6015_6016_CatchHelperAnalyzer())
            {
                TestCode = testCode
            };
            test.TestState.Sources.Add(Helpers);
            test.TestState.Sources.Add(TestFrameworkHelpers);
            test.TestState.Sources.Add(UnrelatedHelpers);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static string InMethod(string catchClauses, string usings = "using System;\nusing System.IO;\n", string ns = "Lucene.Net.Index")
            => usings + @"
namespace " + ns + @"
{
    public class Sample
    {
        public void M(Exception other)
        {
            try
            {
            }
            " + catchClauses + @"
        }
    }
}";

        // LuceneDev6015: filtered catch must catch System.Exception

        [Test]
        public async Task Detects6015_SpecificType_WithHelperFilter()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:IOException|} e) when (e.IsIOException()) { }"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Detects6015_FullyQualifiedSpecificType()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:System.IO.IOException|} e) when (e.IsIOException()) { }"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Detects6015_NegatedFilter()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:IOException|} e) when (!e.IsIOException()) { }"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Detects6015_CompoundFilter_ReportedOnce()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:IOException|} e) when (e.IsIOException() || e.IsNumberFormatException()) { }"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Detects6015_StaticInvocationForm()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:IOException|} e) when (Lucene.ExceptionExtensions.IsIOException(e)) { }"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Detects6015_TestFrameworkHelper()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:ArgumentException|} e) when (e.IsIllegalArgumentException()) { }"),
                Expected6015("IsIllegalArgumentException", "ArgumentException"));
        }

        [Test]
        public async Task Detects6015_InSupportNamespace()
        {
            await VerifyAsync(
                InMethod("catch ({|#0:IOException|} e) when (e.IsIOException()) { }", ns: "Lucene.Net.Support.IO"),
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task NoDiagnostic_ExceptionWithHelperFilter()
        {
            await VerifyAsync(InMethod("catch (Exception e) when (e.IsIOException()) { }"));
        }

        [Test]
        public async Task NoDiagnostic_FullyQualifiedExceptionWithHelperFilter()
        {
            await VerifyAsync(InMethod("catch (System.Exception e) when (e.IsIOException()) { }"));
        }

        [Test]
        public async Task NoDiagnostic_SpecificTypeWithNonHelperFilter()
        {
            await VerifyAsync(InMethod("catch (IOException e) when (e.HResult != 0) { }"));
        }

        [Test]
        public async Task NoDiagnostic_SpecificType_HelperCalledOnAnotherVariable()
        {
            await VerifyAsync(InMethod("catch (IOException e) when (other.IsIOException()) { }"));
        }

        [Test]
        public async Task NoDiagnostic_SpecificType_HelperCalledOnInnerException()
        {
            await VerifyAsync(InMethod("catch (IOException e) when (e.InnerException.IsIOException()) { }"));
        }

        [Test]
        public async Task NoDiagnostic_SpecificType_MethodFromUnrelatedExtensionsClass()
        {
            await VerifyAsync(InMethod(
                "catch (IOException e) when (e.IsSuppressed()) { }",
                usings: "using System;\nusing System.IO;\nusing Lucene.Net.Util;\n"));
        }

        // LuceneDev6016: catch should use a when clause with a helper

        [Test]
        public async Task Detects6016_SpecificType_NoFilter()
        {
            await VerifyAsync(InMethod("{|#0:catch|} (IOException e) { }"), Expected6016());
        }

        [Test]
        public async Task Detects6016_SpecificType_NoVariable()
        {
            await VerifyAsync(InMethod("{|#0:catch|} (IOException) { }"), Expected6016());
        }

        [Test]
        public async Task Detects6016_Exception_NoFilter()
        {
            await VerifyAsync(InMethod("{|#0:catch|} (Exception e) { }"), Expected6016());
        }

        [Test]
        public async Task Detects6016_BareCatch()
        {
            await VerifyAsync(InMethod("{|#0:catch|} { }"), Expected6016());
        }

        [Test]
        public async Task Detects6016_MultipleCatchClauses()
        {
            await VerifyAsync(
                InMethod("{|#0:catch|} (IOException e) { } {|#1:catch|} (FormatException e) { }"),
                Expected6016(0),
                Expected6016(1));
        }

        [Test]
        public async Task Detects6016_InLambda()
        {
            var testCode = @"
using System;
using System.IO;

namespace Lucene.Net.Index
{
    public class Sample
    {
        public Action A = () =>
        {
            try
            {
            }
            {|#0:catch|} (IOException e)
            {
            }
        };
    }
}";
            await VerifyAsync(testCode, Expected6016());
        }

        [Test]
        public async Task Detects6016_NamespaceOnlyResemblingSupport()
        {
            await VerifyAsync(
                InMethod("{|#0:catch|} (IOException e) { }", ns: "Lucene.Net.Analysis.Support"),
                Expected6016());
        }

        [Test]
        public async Task NoDiagnostic6016_SupportNamespace()
        {
            await VerifyAsync(InMethod("catch (IOException e) { }", ns: "Lucene.Net.Support"));
        }

        [Test]
        public async Task NoDiagnostic6016_SupportChildNamespace()
        {
            await VerifyAsync(InMethod("catch { }", ns: "Lucene.Net.Support.IO"));
        }

        [Test]
        public async Task NoDiagnostic_ExistingNonHelperFilter()
        {
            await VerifyAsync(InMethod("catch (Exception e) when (e is not OutOfMemoryException) { }"));
        }

        [Test]
        public async Task NoDiagnostic_BareCatchWithFilter()
        {
            await VerifyAsync(InMethod("catch when (other != null) { }"));
        }

        [Test]
        public async Task NoDiagnostic_HelpersNotInCompilation()
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6015_6016_CatchHelperAnalyzer())
            {
                TestCode = InMethod("catch (IOException e) { }")
            };
            await test.RunAsync();
        }

        [Test]
        public async Task NoDiagnostic_HelpersInaccessible()
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6015_6016_CatchHelperAnalyzer())
            {
                TestCode = InMethod("catch (IOException e) { }")
            };
            test.TestState.AdditionalProjects["Helpers"].Sources.Add(Helpers);
            test.TestState.AdditionalProjectReferences.Add("Helpers");
            await test.RunAsync();
        }

        [Test]
        public async Task Detects6016_HelpersAccessibleThroughInternalsVisibleTo()
        {
            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev6015_6016_CatchHelperAnalyzer())
            {
                TestCode = InMethod("{|#0:catch|} (IOException e) { }")
            };
            test.TestState.AdditionalProjects["Helpers"].Sources.Add(
                "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"TestProject\")]\n" + Helpers);
            test.TestState.AdditionalProjectReferences.Add("Helpers");
            test.ExpectedDiagnostics.Add(Expected6016());
            await test.RunAsync();
        }
    }
}
