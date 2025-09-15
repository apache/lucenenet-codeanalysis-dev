using Microsoft.CodeAnalysis;
using static Microsoft.CodeAnalysis.DiagnosticSeverity;
using static Lucene.Net.CodeAnalysis.Dev.Utility.Category;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    public static partial class Descriptors
    {
        public static DiagnosticDescriptor LuceneDev1000_FloatingPointEquality { get; } =
            Diagnostic(
                "LuceneDev1000",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1001_FloatingPointFormatting { get; } =
            Diagnostic(
                "LuceneDev1001",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1002_FloatingPointArithmetic { get; } =
            Diagnostic(
                "LuceneDev1002",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1003_ArrayMethodParameter { get; } =
            Diagnostic(
                "LuceneDev1003",
                Design,
                Warning
            );

        public static DiagnosticDescriptor LuceneDev1004_ArrayMethodReturnValue { get; } =
            Diagnostic(
                "LuceneDev1004",
                Design,
                Warning
            );
    }
}
