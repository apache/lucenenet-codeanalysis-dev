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
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 */

using System.Collections.Immutable;
using System.Linq;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx
{
    /// <summary>
    /// LuceneDev4002: Reports methods referenced by StackTraceHelper.DoesStackTraceContainMethod
    /// that lack [MethodImpl(MethodImplOptions.NoInlining)], including methods in referenced
    /// assemblies. Without it the JIT may inline the method out of the stack trace, silently
    /// breaking the check. The diagnostic is reported on the invocation so the IDE surfaces it
    /// as a local diagnostic and a code fix can apply the attribute to the target method.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev4002_StackTraceHelperNoInliningAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptors.LuceneDev4002_MissingNoInlining);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext ctx)
        {
            var invocation = (IInvocationOperation)ctx.Operation;

            if (!StackTraceHelperInvocation.IsDoesStackTraceContainMethod(invocation))
                return;

            var method = StackTraceHelperInvocation.GetReferencedMethods(invocation, ctx.CancellationToken)
                .FirstOrDefault(m => NoInliningAttributeHelper.IsMissingNoInlining(m, ctx.CancellationToken));
            if (method is null)
                return;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev4002_MissingNoInlining,
                invocation.Syntax.GetLocation(),
                $"{method.ContainingType.Name}.{method.Name}"));
        }
    }
}
