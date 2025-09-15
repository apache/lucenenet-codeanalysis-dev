using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

namespace Lucene.Net.CodeAnalysis.Dev.Tests
{
    public class TestLuceneDev1001_FloatingPointFormattingCSCodeAnalyzer : DiagnosticVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer();
        }

        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestDiagnostic_Float_ToString()
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
            private readonly float float1 = 1f;

            public void MyMethod()
            {
                string result = float1.ToString(CultureInfo.InvariantCulture);
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = Descriptors.LuceneDev1001_FloatingPointFormatting.Id,
                Message = string.Format("'{0}' may fail due to floating point precision issues on .NET Framework and .NET Core prior to version 3.0. Floating point types should be formatted with J2N.Numerics.Single.ToString() or J2N.Numerics.Double.ToString().", "float1.ToString"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 15, 33)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}
