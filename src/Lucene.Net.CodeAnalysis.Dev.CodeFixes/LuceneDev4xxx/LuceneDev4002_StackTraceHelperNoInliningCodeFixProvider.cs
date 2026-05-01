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
using Microsoft.CodeAnalysis.Formatting;
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

            context.RegisterCodeFix(
                CodeAction.Create(
                    Title,
                    ct => AddNoInliningToTargetAsync(context.Document, invocation, ct),
                    equivalenceKey: nameof(Title)),
                diagnostic);
        }

        private static async Task<Solution> AddNoInliningToTargetAsync(
            Document document,
            InvocationExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (semanticModel is null || invocation.ArgumentList.Arguments.Count != 2)
                return solution;

            var classArg = invocation.ArgumentList.Arguments[0].Expression;
            var methodArg = invocation.ArgumentList.Arguments[1].Expression;

            var (classNameValue, classTypeFromNameof) = ResolveClassReference(classArg, semanticModel);
            if (classNameValue is null)
                return solution;

            var methodNameValue = ResolveMethodNameValue(methodArg, semanticModel);
            if (methodNameValue is null)
                return solution;

            var compilation = semanticModel.Compilation;
            var targetType = classTypeFromNameof
                ?? FindSourceTypeByName(compilation, classNameValue);
            if (targetType is null)
                return solution;

            var methodImplAttrSymbol = compilation.GetTypeByMetadataName(
                "System.Runtime.CompilerServices.MethodImplAttribute");
            if (methodImplAttrSymbol is null)
                return solution;

            MethodDeclarationSyntax? targetDecl = null;
            foreach (var member in targetType.GetMembers(methodNameValue).OfType<IMethodSymbol>())
            {
                if (member.MethodKind != MethodKind.Ordinary)
                    continue;

                if (NoInliningAttributeHelper.HasNoInliningAttribute(member, methodImplAttrSymbol))
                    continue;

                foreach (var declRef in member.DeclaringSyntaxReferences)
                {
                    if (declRef.GetSyntax(cancellationToken) is not MethodDeclarationSyntax methodDecl)
                        continue;

                    if (NoInliningAttributeHelper.HasEmptyBody(methodDecl))
                        continue;
                    if (NoInliningAttributeHelper.IsInterfaceOrAbstractMethod(methodDecl))
                        continue;

                    targetDecl = methodDecl;
                    break;
                }

                if (targetDecl is not null)
                    break;
            }

            if (targetDecl is null)
                return solution;

            var targetTree = targetDecl.SyntaxTree;
            var targetDocument = solution.GetDocument(targetTree);
            if (targetDocument is null)
                return solution;

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

        // ---- Argument resolution (mirrors the analyzer) ----

        private static (string? Name, INamedTypeSymbol? TypeFromNameof) ResolveClassReference(
            ExpressionSyntax expr,
            SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                var typeSymbol = semantic.GetTypeInfo(inner).Type as INamedTypeSymbol
                    ?? semantic.GetSymbolInfo(inner).Symbol as INamedTypeSymbol;
                if (typeSymbol is not null)
                    return (typeSymbol.Name, typeSymbol);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return (literal.Token.ValueText, null);

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return (s, null);

            return (null, null);
        }

        private static string? ResolveMethodNameValue(ExpressionSyntax expr, SemanticModel semantic)
        {
            if (expr is InvocationExpressionSyntax inv
                && inv.Expression is IdentifierNameSyntax id
                && id.Identifier.ValueText == "nameof"
                && inv.ArgumentList.Arguments.Count == 1)
            {
                var inner = inv.ArgumentList.Arguments[0].Expression;
                return ExtractRightmostIdentifier(inner);
            }

            if (expr is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
                return literal.Token.ValueText;

            var constant = semantic.GetConstantValue(expr);
            if (constant.HasValue && constant.Value is string s)
                return s;

            return null;
        }

        private static string? ExtractRightmostIdentifier(ExpressionSyntax expr)
        {
            return expr switch
            {
                IdentifierNameSyntax id => id.Identifier.ValueText,
                MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
                _ => null,
            };
        }

        private static INamedTypeSymbol? FindSourceTypeByName(Compilation compilation, string typeName)
        {
            // Use Roslyn's symbol-name index instead of walking every namespace.
            // Restrict to the source assembly so we don't match metadata types.
            foreach (var symbol in compilation.GetSymbolsWithName(n => n == typeName, SymbolFilter.Type))
            {
                if (symbol is INamedTypeSymbol type
                    && SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, compilation.Assembly))
                {
                    return type;
                }
            }
            return null;
        }
    }
}
