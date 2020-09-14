// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SecureNet.Category.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UseThrowIfCancellationRequestedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA2054";

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(AnalyzerResources.ProvideCorrectArgumentToEnumHasFlagTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString s_localizableMessageNotFlags = new LocalizableResourceString(nameof(AnalyzerResources.ProvideCorrectArgumentToEnumHasFlagMessageNotFlags), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(AnalyzerResources.ProvideCorrectArgumentToEnumHasFlagDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
           DiagnosticId,
           s_localizableTitle,
           s_localizableMessageNotFlags,
           DiagnosticCategory.Usage,
           RuleLevel.BuildWarning,
           description: s_localizableDescription,
           isPortedFxCopRule: false,
           isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DefaultRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(ctx =>
            {
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingCancellationToken, out var cancellationTokenType))
                {
                    return;
                }

                var isCancellationRequestedSymbol = cancellationTokenType.GetMembers("IsCancellationRequested").First();

                ctx.RegisterSyntaxNodeAction(x => AnalzyeIfStatement(x, cancellationTokenType, isCancellationRequestedSymbol), SyntaxKind.IfStatement);
            });
        }

        private static void AnalzyeIfStatement(SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenType, ISymbol isCancellationRequested)
        {
            var ifstatement = (IfStatementSyntax)context.Node;

            if (ifstatement.Condition is MemberAccessExpressionSyntax memberAccess && isCancellationRequested.Equals(context.SemanticModel.GetSymbolInfo(memberAccess).Symbol))
            {
                if (!(context.SemanticModel.GetTypeInfo(memberAccess.Expression).Type is ITypeSymbol type) || !type.Equals(cancellationTokenType))
                    return;
                if (!(ifstatement.Statement is ThrowStatementSyntax || ifstatement.Statement is BlockSyntax block && block.Statements.Count == 1 && block.Statements.Single() is ThrowStatementSyntax))
                    return;
                var diagnostic = ifstatement.CreateDiagnostic(DefaultRule);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
