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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Lucene.Net.CodeAnalysis.Dev.Utility;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1005_LuceneNetSupportPublicTypesCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntax,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.EnumDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.DelegateDeclaration);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            // any public types in the Lucene.Net.Support (or child) namespace should raise the diagnostic
            if (context.Node.Parent is not BaseNamespaceDeclarationSyntax namespaceDeclarationSyntax
                || !namespaceDeclarationSyntax.Name.ToString().StartsWith("Lucene.Net.Support"))
            {
                return;
            }

            if (context.Node is DelegateDeclarationSyntax delegateDeclarationSyntax
                && delegateDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                const string typeKind = "Delegate";
                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes, delegateDeclarationSyntax.GetLocation(), typeKind, delegateDeclarationSyntax.Identifier.ToString()));
            }
            else if (context.Node is BaseTypeDeclarationSyntax baseTypeDeclarationSyntax
                     && baseTypeDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                var typeKind = context.Node switch
                {
                    ClassDeclarationSyntax => "Class",
                    EnumDeclarationSyntax => "Enum",
                    InterfaceDeclarationSyntax => "Interface",
                    RecordDeclarationSyntax record when record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) => "Record struct",
                    RecordDeclarationSyntax => "Record",
                    StructDeclarationSyntax => "Struct",
                    _ => "Type", // should not happen
                };

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1005_LuceneNetSupportPublicTypes, baseTypeDeclarationSyntax.GetLocation(), typeKind, baseTypeDeclarationSyntax.Identifier.ToString()));
            }
        }
    }
}
