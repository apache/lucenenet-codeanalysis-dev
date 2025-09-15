using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1003_ArrayMethodParameterCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1003_ArrayMethodParameter];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.MethodDeclaration, SyntaxKind.ParameterList, SyntaxKind.Parameter, SyntaxKind.ArrayType, SyntaxKind.PredefinedType);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax methodDeclaration)
            {
                foreach (var parameter in methodDeclaration.ParameterList.Parameters)
                {
                    if (parameter.Type is ArrayTypeSyntax arrayTypeSyntax)
                    {
                        if (arrayTypeSyntax.ElementType is PredefinedTypeSyntax predefinedTypeSyntax)
                        {
                            if (predefinedTypeSyntax.Keyword.ValueText != "char")
                                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1003_ArrayMethodParameter, parameter.GetLocation(), parameter.ToString()));
                        }
                    }
                }
            }
        }
    }
}
