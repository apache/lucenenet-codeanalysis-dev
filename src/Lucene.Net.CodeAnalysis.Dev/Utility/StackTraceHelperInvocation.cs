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
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    /// <summary>
    /// Reads the arguments of calls to Lucene.Net.Util.StackTraceHelper.DoesStackTraceContainMethod.
    /// </summary>
    public static class StackTraceHelperInvocation
    {
        private const string StackTraceHelperFullName = "Lucene.Net.Util.StackTraceHelper";
        private const string DoesStackTraceContainMethodName = "DoesStackTraceContainMethod";
        private const string ClassNameParameter = "className";
        private const string MethodNameParameter = "methodName";

        public static bool IsDoesStackTraceContainMethod(IInvocationOperation invocation)
        {
            var method = invocation.TargetMethod;
            return method.Name == DoesStackTraceContainMethodName
                && method.ContainingType?.ToDisplayString() == StackTraceHelperFullName;
        }

        public static IOperation? GetClassNameArgument(IInvocationOperation invocation)
            => GetArgument(invocation, ClassNameParameter);

        public static IOperation? GetMethodNameArgument(IInvocationOperation invocation)
            => GetArgument(invocation, MethodNameParameter);

        public static string? GetConstantString(IOperation operation)
            => operation.ConstantValue is { HasValue: true, Value: string value } ? value : null;

        /// <summary>
        /// Returns the types that the className argument could refer to. A nameof argument
        /// identifies one type. Otherwise, StackTraceHelper compares simple names, so any type
        /// in the current project with this name could match. Types in referenced assemblies
        /// are not searched, because a non-public type may not be visible.
        /// </summary>
        public static ImmutableArray<INamedTypeSymbol> GetClassCandidates(
            IOperation classArgument,
            string className,
            CancellationToken cancellationToken)
        {
            var semanticModel = classArgument.SemanticModel;
            if (semanticModel is null)
                return ImmutableArray<INamedTypeSymbol>.Empty;

            if (classArgument is INameOfOperation nameOf
                && semanticModel.GetSymbolInfo(nameOf.Argument.Syntax, cancellationToken).Symbol is INamedTypeSymbol type)
            {
                return ImmutableArray.Create(type);
            }

            return semanticModel.Compilation
                .GetSymbolsWithName(name => name == className, SymbolFilter.Type, cancellationToken)
                .OfType<INamedTypeSymbol>()
                .ToImmutableArray();
        }

        /// <summary>
        /// Returns the methods that a call refers to, where they can be identified at compile
        /// time. All overloads are included, since StackTraceHelper compares names.
        /// </summary>
        public static ImmutableArray<IMethodSymbol> GetReferencedMethods(
            IInvocationOperation invocation,
            CancellationToken cancellationToken)
        {
            var methodArgument = GetMethodNameArgument(invocation);
            if (methodArgument is null)
                return ImmutableArray<IMethodSymbol>.Empty;

            var classArgument = GetClassNameArgument(invocation);
            if (classArgument is null)
                return GetMethodsFromNameof(methodArgument, cancellationToken);

            var className = GetConstantString(classArgument);
            var methodName = GetConstantString(methodArgument);
            if (className is null || methodName is null)
                return ImmutableArray<IMethodSymbol>.Empty;

            return GetClassCandidates(classArgument, className, cancellationToken)
                .SelectMany(type => type.GetMembers(methodName).OfType<IMethodSymbol>())
                .ToImmutableArray();
        }

        // Without a className, only nameof identifies a method. A literal name alone could match
        // a method on any type.
        private static ImmutableArray<IMethodSymbol> GetMethodsFromNameof(
            IOperation methodArgument,
            CancellationToken cancellationToken)
        {
            if (methodArgument is not INameOfOperation nameOf || methodArgument.SemanticModel is null)
                return ImmutableArray<IMethodSymbol>.Empty;

            // A method group has candidates rather than a single symbol.
            var symbolInfo = methodArgument.SemanticModel.GetSymbolInfo(nameOf.Argument.Syntax, cancellationToken);
            return symbolInfo.CandidateSymbols
                .Add(symbolInfo.Symbol!)
                .OfType<IMethodSymbol>()
                .ToImmutableArray();
        }

        private static IOperation? GetArgument(IInvocationOperation invocation, string parameterName)
        {
            // Look up by parameter, since named arguments can be in any order.
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Name == parameterName)
                    return argument.Value;
            }
            return null;
        }
    }
}
