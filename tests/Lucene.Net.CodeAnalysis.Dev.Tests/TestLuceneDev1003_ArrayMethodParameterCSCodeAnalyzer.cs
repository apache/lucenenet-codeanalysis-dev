using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

namespace Lucene.Net.CodeAnalysis.Dev.Tests
{
    public class TestLuceneDev1003_ArrayMethodParameterCSCodeAnalyzer : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer();
        }

        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestDiagnostic_ParseChar_String_Int32Array_Char()
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
            public static bool ParseChar(string id, int[] pos, char ch)
            {
                int start = pos[0];
                pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
                if (pos[0] == id.Length ||
                    id[pos[0]] != ch)
                {
                    pos[0] = start;
                    return false;
                }
                ++pos[0];
                return true;
            }
        }
       ";

            var expected = new DiagnosticResult
            {
                Id = Descriptors.LuceneDev1003_ArrayMethodParameter.Id,
                Message = string.Format("'{0}' needs to be analyzed to determine whether the array can be replaced with a ref or out parameter", "int[] pos"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 11, 53)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_ParseChar_String_CharArray_Char()
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
            public static bool ParseChar(string id, char[] pos, char ch)
            {
                int start = pos[0];
                pos[0] = PatternProps.SkipWhiteSpace(id, pos[0]);
                if (pos[0] == id.Length ||
                    id[pos[0]] != ch)
                {
                    pos[0] = start;
                    return false;
                }
                ++pos[0];
                return true;
            }
        }
       ";

            // We shouldn't trigger a warning on char[]
            VerifyCSharpDiagnostic(test);
        }
    }
}
