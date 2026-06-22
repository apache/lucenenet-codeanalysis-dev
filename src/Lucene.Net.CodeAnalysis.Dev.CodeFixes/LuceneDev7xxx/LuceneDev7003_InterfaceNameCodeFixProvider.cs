/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lucene.Net.CodeAnalysis.Dev.CodeFixes;
using Lucene.Net.CodeAnalysis.Dev.CodeFixes.Utility;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Rename;

namespace Lucene.Net.CodeAnalysis.Dev;

/// <summary>
/// Code fix for <see cref="Descriptors.LuceneDev7003_InterfaceName"/>: renames an interface to
/// begin with a capital 'I'.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev7003_InterfaceNameCodeFixProvider)), Shared]
public class LuceneDev7003_InterfaceNameCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [Descriptors.LuceneDev7003_InterfaceName.Id];

    // Renames touch the whole solution; the batch fixer is not appropriate.
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var node = root?.FindNode(diagnosticSpan);
        if (node is null)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
            return;

        var symbol = semanticModel.GetDeclaredSymbol(node, context.CancellationToken);
        if (symbol is not INamedTypeSymbol typeSymbol || typeSymbol.TypeKind != TypeKind.Interface)
            return;

        var newName = FieldRenameHelper.ToInterfaceName(typeSymbol.Name);
        if (newName is null || newName == typeSymbol.Name)
            return;

        context.RegisterCodeFix(
            CodeActionHelper.CreateFromResource(
                CodeFixResources.RenameToX,
                createChangedSolution: c => RenameAsync(context.Document, typeSymbol, newName, c),
                $"RenameInterface_{newName}",
                newName),
            diagnostic);
    }

    private static async Task<Solution> RenameAsync(Document document, ISymbol symbol, string newName, CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;
        var options = new SymbolRenameOptions();
        return await Renamer.RenameSymbolAsync(solution, symbol, options, newName, cancellationToken).ConfigureAwait(false);
    }
}
