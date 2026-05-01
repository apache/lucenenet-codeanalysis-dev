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

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx
{
    public static class NoInliningAttributeHelper
    {
        private const int NoInlining = 0x0008;

        public static AttributeSyntax? FindNoInliningAttribute(
            MethodDeclarationSyntax methodDecl,
            SemanticModel semantic,
            INamedTypeSymbol methodImplAttrSymbol)
        {
            foreach (var attrList in methodDecl.AttributeLists)
            {
                foreach (var attr in attrList.Attributes)
                {
                    var attrType = semantic.GetTypeInfo(attr).Type as INamedTypeSymbol
                        ?? semantic.GetSymbolInfo(attr).Symbol?.ContainingType;
                    if (!SymbolEqualityComparer.Default.Equals(attrType, methodImplAttrSymbol))
                        continue;

                    if (AttributeSpecifiesNoInlining(attr, semantic))
                        return attr;
                }
            }
            return null;
        }

        // Symbol-based variant: works across syntax trees and avoids
        // Compilation.GetSemanticModel (which would trip RS1030 in an analyzer).
        public static bool HasNoInliningAttribute(
            IMethodSymbol method,
            INamedTypeSymbol methodImplAttrSymbol)
        {
            foreach (var attr in method.GetAttributes())
            {
                if (!SymbolEqualityComparer.Default.Equals(attr.AttributeClass, methodImplAttrSymbol))
                    continue;

                if (attr.ConstructorArguments.Length == 0)
                    continue;

                var first = attr.ConstructorArguments[0];
                if (first.Value is int intValue && (intValue & NoInlining) == NoInlining)
                    return true;
            }
            return false;
        }

        private static bool AttributeSpecifiesNoInlining(AttributeSyntax attr, SemanticModel semantic)
        {
            if (attr.ArgumentList is null || attr.ArgumentList.Arguments.Count == 0)
                return false;

            // Only the first positional argument controls MethodImplOptions; the second
            // optional argument is MethodCodeType. Skip named arguments.
            var firstPositional = attr.ArgumentList.Arguments
                .FirstOrDefault(a => a.NameEquals is null && a.NameColon is null);
            if (firstPositional is null)
                return false;

            var constant = semantic.GetConstantValue(firstPositional.Expression);
            if (constant.HasValue && constant.Value is int intValue)
            {
                return (intValue & NoInlining) == NoInlining;
            }

            return false;
        }

        public static bool IsInterfaceOrAbstractMethod(MethodDeclarationSyntax methodDecl)
        {
            if (methodDecl.Parent is InterfaceDeclarationSyntax)
                return true;
            if (methodDecl.Modifiers.Any(SyntaxKind.AbstractKeyword))
                return true;
            return false;
        }

        public static bool HasEmptyBody(MethodDeclarationSyntax methodDecl)
        {
            if (methodDecl.Body is null && methodDecl.ExpressionBody is null)
                return false;
            if (methodDecl.ExpressionBody is not null)
                return false;
            return methodDecl.Body!.Statements.Count == 0;
        }
    }
}
