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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace Lucene.Net.CodeAnalysis.Dev.TestUtilities
{
    public class InjectableCSharpAnalyzerTest : AnalyzerTest<Verifier>
    {
        private readonly Func<DiagnosticAnalyzer> analyzerFactory;

        public InjectableCSharpAnalyzerTest(Func<DiagnosticAnalyzer> analyzerFactory)
        {
            this.analyzerFactory = analyzerFactory ?? throw new ArgumentNullException(nameof(analyzerFactory));
        }

        public override string Language => LanguageNames.CSharp;

        protected override string DefaultFileExt => "cs";

        protected override CompilationOptions CreateCompilationOptions()
        {
            return new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        }

        protected override ParseOptions CreateParseOptions()
        {
            return new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose);
        }

        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            yield return analyzerFactory();
        }
    }
}
