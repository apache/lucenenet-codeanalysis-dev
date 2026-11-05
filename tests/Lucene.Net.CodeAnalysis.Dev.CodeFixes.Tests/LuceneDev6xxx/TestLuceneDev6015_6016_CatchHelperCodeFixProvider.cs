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
    public class TestLuceneDev6015_6016_CatchHelperCodeFixProvider
    {
        // Sorted, the options are: IsException (0), IsIOException (1). The other members
        // sort earlier or later, and would shift those indexes if they were offered.
        private const string Helpers = @"
namespace Lucene
{
    internal static class ExceptionExtensions
    {
        internal static bool IsAlwaysIgnored(this System.Exception e) => false;
        public static void Apply(this System.Exception e) { }
        public static string Describe(this System.Exception e) => e.ToString();
        public static bool Compare(this System.Exception e, System.Exception other) => e == other;
        public static bool IsException(this System.Exception e) => true;
        public static bool IsIOException(this System.Exception e) => e is System.IO.IOException;
        public static void PrintStackTrace(this System.Exception e) { }
        public static string ToTypeMessageString(this System.Exception e) => e.ToString();
    }
}";

        private const int IsExceptionIndex = 0;
        private const int IsIOExceptionIndex = 1;

        private static DiagnosticResult Expected6015(string helper, string caughtType)
            => new DiagnosticResult(Descriptors.LuceneDev6015_CatchFilteredByHelperMustCatchException)
                .WithSeverity(DiagnosticSeverity.Error)
                .WithArguments(helper, caughtType)
                .WithLocation(0);

        private static DiagnosticResult Expected6016(int location = 0)
            => new DiagnosticResult(Descriptors.LuceneDev6016_CatchShouldUseHelperFilter)
                .WithSeverity(DiagnosticSeverity.Warning)
                .WithLocation(location);

        private static async Task VerifyAsync(string testCode, string fixedCode, int codeActionIndex, params DiagnosticResult[] expected)
        {
            var test = new InjectableCodeFixTest(
                () => new LuceneDev6015_6016_CatchHelperAnalyzer(),
                () => new LuceneDev6015_6016_CatchHelperCodeFixProvider())
            {
                TestCode = testCode,
                FixedCode = fixedCode,
                CodeActionIndex = codeActionIndex
            };
            test.TestState.Sources.Add(Helpers);
            test.FixedState.Sources.Add(Helpers);
            test.ExpectedDiagnostics.AddRange(expected);
            await test.RunAsync();
        }

        private static string InMethod(string body) => @"
using System;
using System.IO;

namespace Lucene.Net.Index
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
" + body + @"
        }
    }
}";

        [Test]
        public async Task Fix6015_ReplacesCaughtTypeWithException()
        {
            await VerifyAsync(
                InMethod(@"            catch ({|#0:IOException|} e) when (e.IsIOException())
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }"),
                0,
                Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Fix6015_FullyQualifiesExceptionWithoutUsingSystem()
        {
            var testCode = @"
namespace Lucene.Net.Index
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            catch ({|#0:System.IO.IOException|} e) when (e.IsIOException())
            {
                throw;
            }
        }
    }
}";
            var fixedCode = @"
namespace Lucene.Net.Index
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            catch (System.Exception e) when (e.IsIOException())
            {
                throw;
            }
        }
    }
}";
            await VerifyAsync(testCode, fixedCode, 0, Expected6015("IsIOException", "IOException"));
        }

        [Test]
        public async Task Fix6016_SpecificType_ReplacesTypeAndAddsFilter()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException e)
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_OffersEachPublicHelper()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException e)
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsException())
            {
                throw;
            }"),
                IsExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_Exception_KeepsTypeAndAddsFilter()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (Exception ex)
            {
                throw;
            }"),
                InMethod(@"            catch (Exception ex) when (ex.IsIOException())
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_NoVariable_AddsVariable()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException)
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_BareCatch_AddsDeclarationAndFilter()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|}
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_VariableNameInUse_PicksUniqueName()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException)
            {
                int e = 1, e1 = e;
                _ = e1;
            }"),
                InMethod(@"            catch (Exception e2) when (e2.IsIOException())
            {
                int e = 1, e1 = e;
                _ = e1;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_KeepsTrailingComment()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException e) // keep me
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException()) // keep me
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016());
        }

        [Test]
        public async Task Fix6016_FixAll_FixesEachCatchClause()
        {
            await VerifyAsync(
                InMethod(@"            {|#0:catch|} (IOException e)
            {
                throw;
            }
            {|#1:catch|} (FormatException e)
            {
                throw;
            }"),
                InMethod(@"            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }"),
                IsIOExceptionIndex,
                Expected6016(0),
                Expected6016(1));
        }

        [Test]
        public async Task Fix6016_HelperNamespaceNotInScope_AddsUsing()
        {
            var testCode = @"
using System;
using System.IO;

namespace Egothor.Stemmer
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            {|#0:catch|} (IOException e)
            {
                throw;
            }
        }
    }
}";
            var fixedCode = @"
using System;
using System.IO;
using Lucene;

namespace Egothor.Stemmer
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }
        }
    }
}";
            await VerifyAsync(testCode, fixedCode, IsIOExceptionIndex, Expected6016());
        }

        [Test]
        public async Task Fix6016_HelperNamespaceAlreadyImported_DoesNotAddUsing()
        {
            var testCode = @"
using System;
using System.IO;
using Lucene;

namespace Egothor.Stemmer
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            {|#0:catch|} (IOException e)
            {
                throw;
            }
        }
    }
}";
            var fixedCode = @"
using System;
using System.IO;
using Lucene;

namespace Egothor.Stemmer
{
    public class Sample
    {
        public void M()
        {
            try
            {
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw;
            }
        }
    }
}";
            await VerifyAsync(testCode, fixedCode, IsIOExceptionIndex, Expected6016());
        }
    }
}
