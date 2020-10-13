// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferReadOnlySpanOverArrayOnPrivateFieldsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "HA1846";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackMessage), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
           DiagnosticId,
           Title,
           MessageFormat,
           DiagnosticCategory.Performance,
           RuleLevel.IdeSuggestion,
           description: Description,
           isPortedFxCopRule: false,
           isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DefaultRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(ctx =>
            {
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemReadOnlySpan1, out var _))
                {
                    return;
                }
                ctx.RegisterSymbolAction(x => AnalyzerPropertySymbol(x), SymbolKind.Property);
            });
        }

        private static void AnalyzerPropertySymbol(SymbolAnalysisContext context)
        {
            var propertySymbol = (IPropertySymbol)context.Symbol;
            if (propertySymbol.Type.TypeKind != TypeKind.Array
                || !(propertySymbol.Type.SpecialType == SpecialType.System_Boolean
                || propertySymbol.Type.SpecialType == SpecialType.System_Byte
                || propertySymbol.Type.SpecialType == SpecialType.System_SByte)
                || !propertySymbol.IsConst()
                || !(propertySymbol.DeclaredAccessibility == Accessibility.Private || propertySymbol.DeclaredAccessibility == Accessibility.Internal)
                || !propertySymbol.IsStatic)
            {
                return;
            }
            propertySymbol.CreateDiagnostic(DefaultRule, propertySymbol);
        }
    }
}
