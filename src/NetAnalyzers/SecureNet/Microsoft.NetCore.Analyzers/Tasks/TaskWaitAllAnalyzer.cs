// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SecureNet.Category.Tasks
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskWaitAllAnalyzer : DiagnosticAnalyzer
    {
        public const string RuleId = "UA2053";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.TaskDelayUseCancellationTokenInWhenAnyClauseTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.TaskDelayUseCancellationTokenInWhenAnyClauseMessage), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.TaskDelayUseCancellationTokenInWhenAnyClauseDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
           RuleId,
           Title,
           MessageFormat,
           DiagnosticCategory.Usage,
           RuleLevel.BuildWarning,
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
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemThreadingTasksTask, out var taskType))
                {
                    return;
                }

                var whenAnyInvocation = taskType.GetMembers("WaitAll").OfType<IMethodSymbol>();
                ctx.RegisterSyntaxNodeAction(x => AnalyzeTaskDelayInvocation(x, whenAnyInvocation), SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeTaskDelayInvocation(SyntaxNodeAnalysisContext context, IEnumerable<IMethodSymbol> waitAllMethods)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.ArgumentList.Arguments.Count != 1)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (waitAllMethods.Any(method => method.Equals(methodSymbol))
                && context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression).Type?.TypeKind != TypeKind.Array)
            {
                var diagnostic = invocation.CreateDiagnostic(DefaultRule, invocation);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}