using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Lucene.Net.CodeAnalysis.Dev
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LuceneDev1001_FloatingPointFormattingCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1001_FloatingPointFormatting];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.SimpleMemberAccessExpression);
            //context.RegisterSyntaxNodeAction(AnalyzeEqualsMethodNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccessExpression)
            {
                if (!(memberAccessExpression.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax))
                    return;

                if (memberAccessExpression.Name.Identifier.ValueText != "ToString")
                    return;

                var leftSymbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpression.Expression);

                if (!FloatingPoint.IsFloatingPointType(leftSymbolInfo))
                    return; // Check passed

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1001_FloatingPointFormatting, context.Node.GetLocation(), memberAccessExpression.ToString()));
            }
        }
    }
}
