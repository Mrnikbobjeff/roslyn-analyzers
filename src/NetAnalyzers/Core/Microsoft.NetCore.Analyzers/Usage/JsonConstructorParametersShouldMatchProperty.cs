// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class JsonConstructorParametersShouldMatchPropertyAnalyzer : DiagnosticAnalyzer
    {
        private sealed class StringByOrdinalIgnoreCaseComparer : IEqualityComparer<string>
        {
            public bool Equals(string x, string y)
            {
                return x.Equals(y, System.StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(string obj)
            {
                return obj.ToUpperInvariant().GetHashCode();
            }
        }
        internal const string RuleId = "CA1072";
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.JsonConstructorParametersShouldMatchPropertyTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageDifferentType = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.JsonConstructorParametersShouldMatchPropertyMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.JsonConstructorParametersShouldMatchPropertyDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
            RuleId,
            s_localizableTitle,
            s_localizableMessageDifferentType,
            DiagnosticCategory.Design,
            RuleLevel.IdeSuggestion,
            description: s_localizableDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DefaultRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSymbolAction(AnalyzerConstructor, SymbolKind.Method);
        }

        public static string GetActualParameterName(ISymbol member, INamedTypeSymbol attributeType)
        {
            if (member.HasAttribute(attributeType))
                return member.GetAttributes().First(a => a.AttributeClass.Equals(attributeType)).ConstructorArguments.First().Value.ToString();
            else
                return member.Name;
        }

        private static void AnalyzerConstructor(SymbolAnalysisContext context)
        {
            var symbol = context.Symbol as IMethodSymbol;
            if (!symbol.IsConstructor())
                return;
            Compilation compilation = context.Compilation;
            WellKnownTypeProvider wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            var jsonConstructorAttribute = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.NewtonsoftJsonJsonConstructorAttribute);

            var jsonParameterAttribute = wellKnownTypeProvider.GetOrCreateTypeByMetadataName(WellKnownTypeNames.NewtonsoftJsonJsonPropertyAttribute);

            if (jsonConstructorAttribute == null || jsonParameterAttribute == null)
                return;
            if (!symbol.HasAttribute(jsonConstructorAttribute))
                return;
            if (symbol.Parameters.Length == 0)
                return;
            var definedProperties = symbol.ContainingType.GetMembers().Select(x => GetActualParameterName(x, jsonParameterAttribute)).ToImmutableHashSet(new StringByOrdinalIgnoreCaseComparer());
            var actualParameterNames = symbol.Parameters.Select(p => p.Name).ToArray()!;
            for (var i = 0; i < actualParameterNames.Length; i++)
            {
                var parameter = actualParameterNames[i];
                if (!definedProperties.Contains(parameter!))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DefaultRule, symbol.Parameters.Skip(i).First().Locations[0], parameter));
                }
            }
        }
    }
}
