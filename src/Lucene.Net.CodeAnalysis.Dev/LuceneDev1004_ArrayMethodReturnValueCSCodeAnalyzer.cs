using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1004_ArrayMethodReturnValueCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1004_ArrayMethodReturnValue];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.MethodDeclaration, SyntaxKind.ReturnKeyword);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
            {
                if (methodDeclaration.ReturnType is ArrayTypeSyntax arrayTypeSyntax)
                {
                    if (arrayTypeSyntax.ElementType is PredefinedTypeSyntax predefinedTypeSyntax)
                    {
                        if (predefinedTypeSyntax.Keyword.ValueText != "char")
                            context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1004_ArrayMethodReturnValue, arrayTypeSyntax.GetLocation(), arrayTypeSyntax.ToString()));
                    }
                }
            }
        }
    }
}
