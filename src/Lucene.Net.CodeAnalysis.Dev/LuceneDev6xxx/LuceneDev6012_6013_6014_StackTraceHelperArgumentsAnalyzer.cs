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

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    /// <summary>
    /// Checks the arguments of StackTraceHelper.DoesStackTraceContainMethod:
    /// <list type="bullet">
    ///     <item><description>LuceneDev6012: a constant className should use nameof.</description></item>
    ///     <item><description>LuceneDev6013: a constant methodName should use nameof.</description></item>
    ///     <item><description>LuceneDev6014: the named type must declare the named method.</description></item>
    /// </list>
    /// Arguments that are not constant are skipped because their value is not known at
    /// compile time. LuceneDev4002 covers whether the referenced method is NoInlining.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6012_6013_6014_StackTraceHelperArgumentsAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(
                Descriptors.LuceneDev6012_StackTraceHelperClassNameShouldUseNameof,
                Descriptors.LuceneDev6013_StackTraceHelperMethodNameShouldUseNameof,
                Descriptors.LuceneDev6014_StackTraceHelperMethodNotDeclared);

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

            var classArgument = StackTraceHelperInvocation.GetClassNameArgument(invocation);
            var methodArgument = StackTraceHelperInvocation.GetMethodNameArgument(invocation);

            ReportIfNotNameof(ctx, classArgument, Descriptors.LuceneDev6012_StackTraceHelperClassNameShouldUseNameof);
            ReportIfNotNameof(ctx, methodArgument, Descriptors.LuceneDev6013_StackTraceHelperMethodNameShouldUseNameof);

            if (classArgument is not null && methodArgument is not null)
                ReportIfMethodNotDeclared(ctx, classArgument, methodArgument);
        }

        private static void ReportIfNotNameof(
            OperationAnalysisContext ctx,
            IOperation? argument,
            DiagnosticDescriptor descriptor)
        {
            if (argument is null or INameOfOperation || StackTraceHelperInvocation.GetConstantString(argument) is null)
                return;

            ctx.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                argument.Syntax.GetLocation(),
                argument.Syntax.ToString()));
        }

        private static void ReportIfMethodNotDeclared(
            OperationAnalysisContext ctx,
            IOperation classArgument,
            IOperation methodArgument)
        {
            var className = StackTraceHelperInvocation.GetConstantString(classArgument);
            var methodName = StackTraceHelperInvocation.GetConstantString(methodArgument);
            if (className is null || methodName is null)
                return;

            var candidates = StackTraceHelperInvocation.GetClassCandidates(classArgument, className, ctx.CancellationToken);
            if (candidates.Length == 0 || candidates.Any(type => DeclaresMethod(type, methodName)))
                return;

            // Private members of referenced assemblies are not visible to this compilation, so a
            // literal name may still be a real method. A nameof argument is known to be visible.
            if (methodArgument is not INameOfOperation
                && candidates.Any(type => type.DeclaringSyntaxReferences.IsEmpty))
                return;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Descriptors.LuceneDev6014_StackTraceHelperMethodNotDeclared,
                methodArgument.Syntax.GetLocation(),
                className,
                methodName));
        }

        // GetMembers excludes inherited members, matching StackTraceHelper, which compares the
        // type that declares the frame's method. It also finds accessors by their runtime names
        // (get_Count), while a property's own name is correctly not treated as a method.
        private static bool DeclaresMethod(INamedTypeSymbol type, string methodName)
            => type.GetMembers(methodName).OfType<IMethodSymbol>().Any();
    }
}
