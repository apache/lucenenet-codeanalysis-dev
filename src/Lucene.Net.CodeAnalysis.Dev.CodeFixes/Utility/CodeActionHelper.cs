using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility
{
    internal static class CodeActionHelper
    {
        /// <summary>
        /// Create a CodeAction using a resource string and formatting arguments.
        /// </summary>
        public static CodeAction CreateFromResource(
            string resourceValue,
            Func<CancellationToken, Task<Document>> createChangedDocument,
            string equivalenceKey,
            params object[] args)
        {
            var title = string.Format(resourceValue, args);
            return CodeAction.Create(title, createChangedDocument, equivalenceKey);
        }
    }
}
