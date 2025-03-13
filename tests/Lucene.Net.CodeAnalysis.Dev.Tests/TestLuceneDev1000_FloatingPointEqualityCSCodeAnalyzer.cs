using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

namespace Lucene.Net.CodeAnalysis.Dev.Tests
{
    public class TestLuceneDev1000_FloatingPointEqualityCSCodeAnalyzer : DiagnosticVerifier
    {

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer();
        }

        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestDiagnostic_LessThan()
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
            public void MyMethod()
            {
            }
            public void MyMethod(int n)
            {
            }
            protected internal override bool LessThan(float termA, float termB)
            {
                return termA < termB;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "termA < termB"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 19, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_EqualTo()
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
            public void MyMethod()
            {
            }
            public void MyMethod(int n)
            {
            }
            protected internal override bool LessThan(float termA, float termB)
            {
                return termA == termB;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "termA == termB"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 19, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_GreaterThan()
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
            public void MyMethod()
            {
            }
            public void MyMethod(int n)
            {
            }
            protected internal override bool LessThan(float termA, float termB)
            {
                return termA > termB;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "termA > termB"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 19, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_LessThanOrEqualTo()
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
            public void MyMethod()
            {
            }
            public void MyMethod(int n)
            {
            }
            protected internal override bool LessThan(float termA, float termB)
            {
                return termA <= termB;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "termA <= termB"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 19, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_GreaterThanOrEqualTo()
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
            public void MyMethod()
            {
            }
            public void MyMethod(int n)
            {
            }
            protected internal override bool LessThan(float termA, float termB)
            {
                return termA >= termB;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "termA >= termB"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 19, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_EqualTo_MemberVariable()
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
            private readonly float myFloat1 = 1f;
            private readonly float myFloat2 = 1f;

            public void MyMethod()
            {
                var x = myFloat1 == myFloat2;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "myFloat1 == myFloat2"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 16, 25)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_EqualTo_LocalVariable()
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
            public void MyMethod()
            {
                float a = 1f;
                float b = 1f;
                var x = a == b;
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "a == b"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 15, 25)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_EqualTo_PropertyOfParameter()
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
            public void MyMethod(Term a, Term b)
            {
                var x = a.Score == b.Score;
            }
       }
       public class Term
       {
           public float Score { get; set; }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "a.Score == b.Score"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 13, 25)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestDiagnostic_Equals_LocalVariable()
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
            public void MyMethod()
            {
                float a = 1f;
                float b = 1f;
                var x = a.Equals(b);
            }
       }
       ";

            var expected = new DiagnosticResult
            {
                Id = LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer.DiagnosticId,
                Message = string.Format("'{0}' may fail due to JIT optimizations. Floating point types should not be compared for exact equality.", "a.Equals"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                        new DiagnosticResultLocation("Test0.cs", 15, 25)
                    }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}
