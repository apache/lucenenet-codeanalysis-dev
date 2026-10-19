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

using System.Collections.Generic;
using System.Collections.Immutable;
using Lucene.Net.CodeAnalysis.Dev.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Lucene.Net.CodeAnalysis.Dev.LuceneDev6xxx
{
    /// <summary>
    /// Detects references to collection types that have a preferred J2N equivalent:
    /// <list type="bullet">
    ///     <item><description>LuceneDev6006: SCG.List&lt;T&gt;, SCG.HashSet&lt;T&gt;, SCG.Dictionary&lt;TKey, TValue&gt;
    ///     and SCG.OrderedDictionary&lt;TKey, TValue&gt; should be replaced with their JCG counterparts.</description></item>
    ///     <item><description>LuceneDev6007: JCG.LinkedHashSet&lt;T&gt; and JCG.LinkedDictionary&lt;TKey, TValue&gt;
    ///     should be replaced with JCG.OrderedHashSet&lt;T&gt; and JCG.OrderedDictionary&lt;TKey, TValue&gt;.</description></item>
    /// </list>
    /// A rule is only reported when both the original and the replacement type are available
    /// in the compilation. Unbound generic names (e.g. <c>typeof(List&lt;&gt;)</c>) and XML doc
    /// comment references are ignored.
    /// Severity: Warning.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LuceneDev6006_6007_PreferJ2NCollectionAnalyzer : DiagnosticAnalyzer
    {
        /// <summary>
        /// The diagnostic property key containing the metadata name (e.g. <c>List`1</c>)
        /// of the replacement type in the <c>J2N.Collections.Generic</c> namespace.
        /// </summary>
        public const string ReplacementMetadataNameKey = "ReplacementMetadataName";

        public const string J2NCollectionsGenericNamespace = "J2N.Collections.Generic";

        private static readonly (string Original, string Replacement, DiagnosticDescriptor Descriptor)[] Mappings =
        {
            ("System.Collections.Generic.List`1", "List`1", Descriptors.LuceneDev6006_PreferJ2NCollection),
            ("System.Collections.Generic.HashSet`1", "HashSet`1", Descriptors.LuceneDev6006_PreferJ2NCollection),
            ("System.Collections.Generic.Dictionary`2", "Dictionary`2", Descriptors.LuceneDev6006_PreferJ2NCollection),
            ("System.Collections.Generic.OrderedDictionary`2", "OrderedDictionary`2", Descriptors.LuceneDev6006_PreferJ2NCollection),
            ("J2N.Collections.Generic.LinkedHashSet`1", "OrderedHashSet`1", Descriptors.LuceneDev6007_PreferJ2NOrderedCollection),
            ("J2N.Collections.Generic.LinkedDictionary`2", "OrderedDictionary`2", Descriptors.LuceneDev6007_PreferJ2NOrderedCollection),
        };

        private static readonly SymbolDisplayFormat TypeDisplayFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(
                Descriptors.LuceneDev6006_PreferJ2NCollection,
                Descriptors.LuceneDev6007_PreferJ2NOrderedCollection);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var replacements = new Dictionary<INamedTypeSymbol, Replacement>(SymbolEqualityComparer.Default);

                foreach (var (original, replacement, descriptor) in Mappings)
                {
                    var originalType = compilationContext.Compilation.GetTypeByMetadataName(original);
                    var replacementType = compilationContext.Compilation.GetTypeByMetadataName($"{J2NCollectionsGenericNamespace}.{replacement}");
                    if (originalType is null || replacementType is null)
                        continue;

                    replacements[originalType] = new Replacement(
                        descriptor,
                        replacement,
                        originalType.ToDisplayString(TypeDisplayFormat),
                        replacementType.ToDisplayString(TypeDisplayFormat));
                }

                if (replacements.Count == 0)
                    return;

                compilationContext.RegisterSyntaxNodeAction(
                    ctx => AnalyzeGenericName(ctx, replacements),
                    SyntaxKind.GenericName);
            });
        }

        private static void AnalyzeGenericName(SyntaxNodeAnalysisContext ctx, Dictionary<INamedTypeSymbol, Replacement> replacements)
        {
            var genericName = (GenericNameSyntax)ctx.Node;

            if (genericName.IsUnboundGenericName || genericName.IsPartOfStructuredTrivia())
                return;

            if (ctx.SemanticModel.GetSymbolInfo(genericName, ctx.CancellationToken).Symbol is not INamedTypeSymbol type)
                return;

            if (!replacements.TryGetValue(type.OriginalDefinition, out var replacement))
                return;

            var properties = ImmutableDictionary<string, string?>.Empty
                .Add(ReplacementMetadataNameKey, replacement.MetadataName);

            ctx.ReportDiagnostic(Diagnostic.Create(
                replacement.Descriptor,
                GetFullTypeName(genericName).GetLocation(),
                properties,
                replacement.ReplacementDisplayName,
                replacement.OriginalDisplayName));
        }

        /// <summary>
        /// Returns the outermost node of a (possibly namespace- or alias-qualified) type name,
        /// so that e.g. <c>SCG.List&lt;int&gt;</c> is reported as a whole rather than only <c>List&lt;int&gt;</c>.
        /// </summary>
        public static SyntaxNode GetFullTypeName(GenericNameSyntax genericName)
        {
            SyntaxNode current = genericName;
            while (true)
            {
                switch (current.Parent)
                {
                    case QualifiedNameSyntax qualified when qualified.Right == current:
                        current = qualified;
                        continue;
                    case AliasQualifiedNameSyntax aliasQualified when aliasQualified.Name == current:
                        current = aliasQualified;
                        continue;
                    case MemberAccessExpressionSyntax memberAccess when memberAccess.Name == current:
                        current = memberAccess;
                        continue;
                    default:
                        return current;
                }
            }
        }

        private sealed class Replacement
        {
            public Replacement(DiagnosticDescriptor descriptor, string metadataName, string originalDisplayName, string replacementDisplayName)
            {
                Descriptor = descriptor;
                MetadataName = metadataName;
                OriginalDisplayName = originalDisplayName;
                ReplacementDisplayName = replacementDisplayName;
            }

            public DiagnosticDescriptor Descriptor { get; }
            public string MetadataName { get; }
            public string OriginalDisplayName { get; }
            public string ReplacementDisplayName { get; }
        }
    }
}
