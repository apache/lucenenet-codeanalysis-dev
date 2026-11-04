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
using Lucene.Net.CodeAnalysis.Dev.LuceneDev4xxx;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Lucene.Net.CodeAnalysis.Dev.CodeFixes.LuceneDev4xxx
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider)), Shared]
    public sealed class LuceneDev4002_StackTraceHelperNoInliningCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add [MethodImpl(MethodImplOptions.NoInlining)] to the referenced method";
        private const string CompilerServicesNamespace = "System.Runtime.CompilerServices";

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(Descriptors.LuceneDev4002_MissingNoInlining.Id);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics[0];
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
                return;

            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var invocation = node as InvocationExpressionSyntax
                ?? node.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (invocation is null)
                return;

            var targetDecl = await FindTargetDeclarationAsync(context.Document, invocation, context.CancellationToken).ConfigureAwait(false);
            if (targetDecl is null)
                return;

            var targetDocument = context.Document.Project.Solution.GetDocument(targetDecl.SyntaxTree);
            if (targetDocument is null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => AddNoInliningAsync(targetDocument, targetDecl, ct),
                    equivalenceKey: nameof(Title)),
                diagnostic);
        }

        // Finds a target's source, which may be in another project in the solution. Returns null
        // if there is none, such as for a package reference, so there is nothing to fix.
        private static async Task<MethodDeclarationSyntax?> FindTargetDeclarationAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel?.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation)
                return null;

            foreach (var method in StackTraceHelperInvocation.GetReferencedMethods(operation, cancellationToken))
            {
                var source = await SymbolFinder.FindSourceDefinitionAsync(method, document.Project.Solution, cancellationToken).ConfigureAwait(false);
                if (source is not IMethodSymbol sourceMethod
                    || !NoInliningAttributeHelper.IsMissingNoInlining(sourceMethod, cancellationToken))
                    continue;

                foreach (var reference in sourceMethod.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax(cancellationToken) is MethodDeclarationSyntax declaration)
                        return declaration;
                }
            }

            return null;
        }

        private static async Task<Solution> AddNoInliningAsync(
            Document targetDocument,
            MethodDeclarationSyntax targetDecl,
            CancellationToken cancellationToken)
        {
            var targetTree = targetDecl.SyntaxTree;
            var targetRoot = await targetTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Build [MethodImpl(MethodImplOptions.NoInlining)] with no manual trivia,
            // and let the Formatter annotation handle indentation and line endings.
            var newAttributeList = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName("MethodImpl"),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("MethodImplOptions"),
                                        SyntaxFactory.IdentifierName("NoInlining"))))))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newAttributeLists = targetDecl.AttributeLists.Insert(0, newAttributeList);
            var newMethodDecl = targetDecl.WithAttributeLists(newAttributeLists);

            var newTargetRoot = targetRoot.ReplaceNode(targetDecl, newMethodDecl);

            // Add the using if missing.
            if (newTargetRoot is CompilationUnitSyntax compilationUnit
                && !compilationUnit.Usings.Any(u => u.Name?.ToString() == CompilerServicesNamespace))
            {
                var usingDirective = SyntaxFactory.UsingDirective(
                        SyntaxFactory.ParseName(CompilerServicesNamespace))
                    .WithAdditionalAnnotations(Formatter.Annotation);
                compilationUnit = compilationUnit.AddUsings(usingDirective);
                newTargetRoot = compilationUnit;
            }

            var newTargetDocument = targetDocument.WithSyntaxRoot(newTargetRoot);

            // Honor the source file's existing line ending convention so the fix
            // doesn't introduce mixed line endings (the workspace's NewLine option
            // otherwise defaults to Environment.NewLine, which can disagree with
            // a source file that uses the opposite convention).
            var sourceText = await targetTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var newLine = DetectNewLine(sourceText);
            var options = newTargetDocument.Project.Solution.Workspace.Options
                .WithChangedOption(FormattingOptions.NewLine, LanguageNames.CSharp, newLine);

            var formatted = await Formatter.FormatAsync(
                newTargetDocument,
                Formatter.Annotation,
                options,
                cancellationToken).ConfigureAwait(false);

            return formatted.Project.Solution;
        }

        private static string DetectNewLine(SourceText text)
        {
            foreach (var line in text.Lines)
            {
                var lineBreakLength = line.EndIncludingLineBreak - line.End;
                if (lineBreakLength == 0)
                    continue;
                var firstChar = text[line.End];
                if (firstChar == '\r' && lineBreakLength == 2)
                    return "\r\n";
                if (firstChar == '\n')
                    return "\n";
                if (firstChar == '\r')
                    return "\r";
            }
            return "\n";
        }
    }
}
