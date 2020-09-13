using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.NetCore.Analyzers.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class PassCorrectArgumentToBufferClockCopyAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA2250";

        //TODO: add actual strings
        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.ProvideCorrectArgumentToEnumHasFlagTitle), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageNotFlags = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.ProvideCorrectArgumentToEnumHasFlagMessageNotFlags), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftNetCoreAnalyzersResources.ProvideCorrectArgumentToEnumHasFlagDescription), MicrosoftNetCoreAnalyzersResources.ResourceManager, typeof(MicrosoftNetCoreAnalyzersResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
           RuleId,
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
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemBuffer, out var bufferType))
                {
                    return;
                }
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemByte, out var byteType))
                {
                    return;
                }
                var blockCopyInvocation = bufferType.GetMembers("BlockCopy").Single();
                ctx.RegisterSyntaxNodeAction(x => AnalyzeBufferBlockCopy(x, blockCopyInvocation, byteType), SyntaxKind.InvocationExpression);
            });
        }

        private void AnalyzeBufferBlockCopy(SyntaxNodeAnalysisContext context, ISymbol blockCopyType, INamedTypeSymbol byteType)
        {
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            if (context.SemanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (!methodSymbol.Equals(blockCopyType))
            {
                return;
            }

            var firstArgument = invocationExpression.ArgumentList.Arguments.FirstOrDefault();
            if (firstArgument is null)
            {
                return;
            }

            if (context.SemanticModel.GetTypeInfo(firstArgument.Expression).Type is not IArrayTypeSymbol arrayType)
            {
                return;
            }

            if (arrayType.ElementType.Equals(byteType))
            {
                return; //Byte type length matches array length
            }

            var lastArgument = invocationExpression.ArgumentList.Arguments.Last();
            if (lastArgument.Expression is MemberAccessExpressionSyntax memberAccess
                && memberAccess.Name.Identifier.ValueText.Equals("Length", StringComparison.Ordinal)
                && memberAccess.Expression is IdentifierNameSyntax id
                && firstArgument.Expression is IdentifierNameSyntax arrayId
                && id.Identifier.ValueText.Equals(arrayId.Identifier.ValueText, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(invocationExpression.CreateDiagnostic(DefaultRule));
            }
        }
    }
}
