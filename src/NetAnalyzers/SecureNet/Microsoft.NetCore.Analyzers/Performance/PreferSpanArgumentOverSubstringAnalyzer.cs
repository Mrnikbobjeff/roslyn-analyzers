// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SecureNet.Category.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferSpanArgumentOverSubstringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "HA1841";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.PreferSpanArgumentOverSubstringTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.PreferSpanArgumentOverSubstringMessage), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.PreferSpanArgumentOverSubstringDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static readonly DiagnosticDescriptor Rule = DiagnosticDescriptorHelper.Create(
            DiagnosticId,
            Title,
            MessageFormat,
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: Description,
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(ctx =>
            {
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemString, out var stringType))
                {
                    return;
                }

                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemSpan1, out var spanType))
                {
                    return;
                }

                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemReadOnlySpan1, out var readOnlySpanType))
                {
                    return;
                }

                var methodSymbols = stringType.GetMembers("Substring");
                ctx.RegisterSyntaxNodeAction(x => AnalyzeArgumentValue(x, methodSymbols, new[] { spanType, readOnlySpanType }), SyntaxKind.Argument);
            });
        }

        private static int NonDefaultCount<T>(T t) where T : IEnumerable<IParameterSymbol> => t.Sum(x => x.HasExplicitDefaultValue ? 0 : 1);

        private static void AnalyzeArgumentValue(SyntaxNodeAnalysisContext context, ImmutableArray<ISymbol> substringSymbols, INamedTypeSymbol[] namedTypeSymbols)
        {
            var argument = (ArgumentSyntax)context.Node;

            if (!(argument.Expression is InvocationExpressionSyntax invocation
                && invocation.Expression is MemberAccessExpressionSyntax memberAccess
                && context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is IMethodSymbol method
                && substringSymbols.Any(s => s.Equals(method))))
            {
                return;
            }

            var isInInvocation = argument.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            if (isInInvocation is null)
            {
                return;
            }

            var isInNonAsyncMethod = argument.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (isInNonAsyncMethod is null)
            {
                return;
            }

            if (isInNonAsyncMethod.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(isInInvocation).Symbol is not IMethodSymbol currentInvocationMethod)
            {
                return;
            }

            var indexOfArg = isInInvocation.ArgumentList.Arguments.IndexOf(argument);
            if (currentInvocationMethod.Parameters[indexOfArg].Type.SpecialType != SpecialType.System_String)
            {
                return;
            }

            var methods = context.SemanticModel.GetMemberGroup(isInInvocation.Expression);

            if (methods.OfType<IMethodSymbol>().Where(x => NonDefaultCount(x.Parameters) == NonDefaultCount(currentInvocationMethod.Parameters) && currentInvocationMethod.Arity == x.Arity)
                .Where(x => namedTypeSymbols.Contains(x.Parameters[indexOfArg].Type.OriginalDefinition)).Any())
            {
                var diagnostic = invocation.CreateDiagnostic(Rule, argument);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
