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
    public class LuceneDev1002_FloatingPointArithmeticCSCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LuceneDev1002";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.LuceneDev1002_AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.LuceneDev1002_AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.LuceneDev1002_AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Design";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            context.EnableConcurrentExecution();

            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            //context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentExpression, SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentExpression);
            //context.RegisterSyntaxNodeAction(AnalyzeEqualsMethodNode, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            //if (context.Node is Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionSyntax expression)
            //{
            //    bool hasFloatingPointType = false;

            //    foreach (var descendantNode in expression.DescendantNodes())
            //    {
            //        if (descendantNode is Microsoft.CodeAnalysis.CSharp.Syntax.CastExpressionSyntax castExpression)
            //        {
            //            var symbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, castExpression.Expression);

            //            if (Helpers.FloatingPoint.IsFloatingPointType(symbolInfo))
            //            {
            //                hasFloatingPointType = true;
            //                break; // Report
            //            }
            //        }
            //        else if (descendantNode is Microsoft.CodeAnalysis.CSharp.Syntax.PredefinedTypeSyntax predefinedType)
            //        {

            //        }

            //        //var symbolInfo = Microsoft.CodeAnalysis.CSharp.CSharpExtensions.GetSymbolInfo(context.SemanticModel, descendantNode.);
            //    }

            //    if (expression.Kind() == SyntaxKind.MultiplyExpression)
            //    {

            //    }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), context.Node.ToString()));
        }
    }
}
