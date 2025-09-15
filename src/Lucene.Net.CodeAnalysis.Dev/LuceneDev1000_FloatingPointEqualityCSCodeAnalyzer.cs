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
    public class LuceneDev1000_FloatingPointEqualityCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Descriptors.LuceneDev1000_FloatingPointEquality];

        public override void Initialize(AnalysisContext context)
        {
            // LUCENENET TODO: Enable this once we get it stable - for now we will skip so we can check other issues

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            //var x = Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax;
            //var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();
            //Microsoft.CodeAnalysis.CSharp.Sy

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.EqualsExpression, SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanExpression, SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanOrEqualExpression);
            //context.RegisterSyntaxNodeAction(AnalyzeEqualsMethodNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.BinaryExpressionSyntax binaryExpression)
            {
                var leftSymbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, binaryExpression.Left);
                var rightSymbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, binaryExpression.Right);

                // Attempt to cast to a field
                //var leftField = leftSymbolInfo.Symbol as Microsoft.CodeAnalysis.ITypeParameterSymbol;

                if (!FloatingPoint.IsFloatingPointType(leftSymbolInfo) && !FloatingPoint.IsFloatingPointType(rightSymbolInfo))
                    return; // Check passed

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1000_FloatingPointEquality, context.Node.GetLocation(), binaryExpression.ToString()));
            }
            else if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccessExpression)
            {
                if (!(memberAccessExpression.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax))
                    return;

                if (memberAccessExpression.Name.Identifier.ValueText != "Equals")
                    return;

                var leftSymbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, memberAccessExpression.Expression);

                if (!FloatingPoint.IsFloatingPointType(leftSymbolInfo))
                    return; // Check passed

                context.ReportDiagnostic(Diagnostic.Create(Descriptors.LuceneDev1000_FloatingPointEquality, context.Node.GetLocation(), memberAccessExpression.ToString()));
            }
        }
    }
}
