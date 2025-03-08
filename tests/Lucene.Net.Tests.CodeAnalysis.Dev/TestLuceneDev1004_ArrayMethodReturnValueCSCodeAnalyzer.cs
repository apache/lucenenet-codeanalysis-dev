using Lucene.Net.CodeAnalysis.Dev;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

namespace Lucene.Net.Tests.CodeAnalysis.Dev
{
    public class TestLuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer();
        }

        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestDiagnostic_GetVersionByteArrayFromCompactInt32_ByteArrayReturnType()
        {
            var test = @"
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.Threading.Tasks;
        using System.Diagnostics;

        public class MyClass
        {
            public static byte[] GetVersionByteArrayFromCompactInt32(int version) // ICU4N specific - Renamed from GetVersionByteArrayFromCompactInt
            {
                return new byte[] {
                    (byte)(version >> 24),
                    (byte)(version >> 16),
                    (byte)(version >> 8),
                    (byte)(version)
                };
            }
        }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' return type needs to be analyzed to determine whether the array return value can be replaced with one or more out parameters or a return ValueTuple instead of an array to avoid the heap allocation", "byte[]"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 11, 27)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_GetVersionCharArrayFromCompactInt32_CharArrayReturnType()
        {
            var test = @"
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text;
        using System.Threading.Tasks;
        using System.Diagnostics;

        public class MyClass
        {
            public static char[] GetVersionCharArrayFromCompactInt32(int version)
            {
                return new char[] {
                    (char)(version >> 24),
                    (char)(version >> 16),
                    (char)(version >> 8),
                    (char)(version)
                };
            }
        }
       ";

            // We shouldn't trigger a warning on char[]
            VerifyCSharpDiagnostic(test);
        }
    }
}
