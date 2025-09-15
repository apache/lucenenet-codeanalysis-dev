using Microsoft.CodeAnalysis;
using System.Collections.Concurrent;

namespace Lucene.Net.CodeAnalysis.Dev.Utility
{
    public static partial class Descriptors
    {
        static readonly ConcurrentDictionary<Category, string> categoryMapping = new();

        static DiagnosticDescriptor Diagnostic(
            string id,
            Category category,
            DiagnosticSeverity defaultSeverity)
            => Diagnostic(id, category, defaultSeverity, isEnabledByDefault: true);

        static DiagnosticDescriptor Diagnostic(
            string id,
            Category category,
            DiagnosticSeverity defaultSeverity,
            bool isEnabledByDefault)
        {
            //string? helpLink = null;
            var categoryString = categoryMapping.GetOrAdd(category, c => c.ToString());

            var title = new LocalizableResourceString($"{id}_AnalyzerTitle", Resources.ResourceManager, typeof(Resources));
            var messageFormat = new LocalizableResourceString($"{id}_AnalyzerMessageFormat", Resources.ResourceManager, typeof(Resources));
            var description = new LocalizableResourceString($"{id}_AnalyzerDescription", Resources.ResourceManager, typeof(Resources));

            //return new DiagnosticDescriptor(id, title, messageFormat, categoryString, defaultSeverity, isEnabledByDefault: true, helpLinkUri: helpLink);
            return new DiagnosticDescriptor(id, title, messageFormat, categoryString, defaultSeverity, isEnabledByDefault);
        }
    }
}
