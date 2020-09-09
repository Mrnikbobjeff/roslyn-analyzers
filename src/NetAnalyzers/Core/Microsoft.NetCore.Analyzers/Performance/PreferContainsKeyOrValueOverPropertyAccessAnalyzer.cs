// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class PreferContainsKeyOrValueOverPropertyAccessAnalyzer : DiagnosticAnalyzer
    {
        internal const string ContainsKeyRuleId = "CA1839";
        internal const string ContainsValueRuleId = "CA1840";

        private static readonly string[] PropertyNames = new string[] { "Keys", "Values" };
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferContainsKeyOrValueOverPropertyAccessTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableKeyMessage = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferContainsKeyOverPropertyAccessMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableValueMessage = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferContainsValueOverPropertyAccessMessage), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableKeyDescription = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferContainsKeyOverPropertyDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableValueDescription = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.PreferContainsValueOverPropertyDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));

        internal static readonly DiagnosticDescriptor ContainsKeyRule = DiagnosticDescriptorHelper.Create(
            ContainsKeyRuleId,
            s_localizableTitle,
            s_localizableKeyMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: s_localizableKeyDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static readonly DiagnosticDescriptor ContainsValueRule = DiagnosticDescriptorHelper.Create(
            ContainsValueRuleId,
            s_localizableTitle,
            s_localizableValueMessage,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: s_localizableValueDescription,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ContainsKeyRule, ContainsValueRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(ctx =>
            {
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIReadOnlyDictionary2, out var iReadOnlyDictionary)
                    || !ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericIDictionary2, out var iDictionary)
                    || !ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemCollectionsGenericDictionary2, out var dictionary))
                {
                    return;
                }
                ctx.RegisterSyntaxNodeAction(x => AnalyzeInvocation(x, dictionary, iDictionary, iReadOnlyDictionary), SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol dictionary, INamedTypeSymbol iDictionary, INamedTypeSymbol iReadOnlyDictionary)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.ArgumentList.Arguments.Count != 1)
            {
                return; //As stated in issue we are only interested in single argument calls
            }

            var nestedMemberAccessExpression = (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;
            if (nestedMemberAccessExpression is not MemberAccessExpressionSyntax possibleValueOrKeysMemberAccess)
            {
                return;
            }

            if (!PropertyNames.Contains(possibleValueOrKeysMemberAccess.Name.Identifier.ValueText))
            {
                return; //Not Contains on .Keys or .Values
            }

            var typeMemberAccess = context.SemanticModel.GetTypeInfo(possibleValueOrKeysMemberAccess.Expression).Type;
            if (typeMemberAccess is null)
            {
                return;
            }

            // Perform actual method checking and not just compare strings.
            if (typeMemberAccess.AllInterfaces.Any(@interface => (@interface.ConstructedFrom.Equals(iDictionary) || @interface.ConstructedFrom.Equals(iReadOnlyDictionary)) && @interface.Arity == 2)
                || typeMemberAccess is INamedTypeSymbol namedType && namedType.Arity == 2 && (namedType.ConstructedFrom.Equals(iReadOnlyDictionary) || namedType.ConstructedFrom.Equals(iDictionary)))
            {
                Diagnostic? diagnostic = null;
                if (possibleValueOrKeysMemberAccess.Name.Identifier.ValueText.Equals("Values", System.StringComparison.Ordinal))
                {
                    if (!typeMemberAccess.OriginalDefinition.Equals(dictionary))
                    {
                        return; //ContainsValue is only available on Dictionary<'> type
                    }

                    diagnostic = Diagnostic.Create(ContainsValueRule, invocation.GetLocation());
                }
                else
                {
                    diagnostic = Diagnostic.Create(ContainsKeyRule, invocation.GetLocation());
                }

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
