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
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev
{
    public class TestLuceneDev1000_FloatingPointEqualityCSCodeAnalyzer
    {
        //No diagnostics expected to show up
        [Test]
        public async Task TestEmptyFile()
        {
            var testCode = @"";

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_LessThan()
        {
            var testCode = @"
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
            protected internal bool LessThan(float termA, float termB)
            {
                return termA < termB;
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                .WithArguments("termA < termB")
                .WithLocation("/0/Test0.cs", line: 19, column: 24);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_EqualTo()
        {
            var testCode = @"
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
            protected internal bool LessThan(float termA, float termB)
            {
                return termA == termB;
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                .WithArguments("termA == termB")
                .WithLocation("/0/Test0.cs", line: 19, column: 24);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_GreaterThan()
        {
            var testCode = @"
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
            protected internal bool LessThan(float termA, float termB)
            {
                return termA > termB;
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                .WithArguments("termA > termB")
                .WithLocation("/0/Test0.cs", line: 19, column: 24);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_LessThanOrEqualTo()
        {
            var testCode = @"
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
            protected internal bool LessThan(float termA, float termB)
            {
                return termA <= termB;
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("termA <= termB")
                 .WithLocation("/0/Test0.cs", line: 19, column: 24);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_GreaterThanOrEqualTo()
        {
            var testCode = @"
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
            protected internal bool LessThan(float termA, float termB)
            {
                return termA >= termB;
            }
       }
       ";

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("termA >= termB")
                 .WithLocation("/0/Test0.cs", line: 19, column: 24);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_EqualTo_MemberVariable()
        {
            var testCode = @"
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

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("myFloat1 == myFloat2")
                 .WithLocation("/0/Test0.cs", line: 16, column: 25);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_EqualTo_LocalVariable()
        {
            var testCode = @"
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
            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("a == b")
                 .WithLocation("/0/Test0.cs", line: 15, column: 25);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_EqualTo_PropertyOfParameter()
        {
            var testCode = @"
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

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("a.Score == b.Score")
                 .WithLocation("/0/Test0.cs", line: 13, column: 25);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }

        [Test]
        public async Task TestDiagnostic_Equals_LocalVariable()
        {
            var testCode = @"
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

            var expected = DiagnosticResult.CompilerWarning(Descriptors.LuceneDev1000_FloatingPointEquality.Id)
                 .WithMessageFormat(Descriptors.LuceneDev1000_FloatingPointEquality.MessageFormat)
                 .WithArguments("a.Equals")
                 .WithLocation("/0/Test0.cs", line: 15, column: 25);

            var test = new InjectableCSharpAnalyzerTest(() => new LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer())
            {
                TestCode = testCode,
                ExpectedDiagnostics = { expected }
            };

            await test.RunAsync();
        }
    }
}
