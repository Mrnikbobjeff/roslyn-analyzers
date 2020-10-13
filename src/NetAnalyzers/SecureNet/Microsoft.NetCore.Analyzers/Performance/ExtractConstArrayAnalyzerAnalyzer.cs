// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    public class ExtractConstArrayAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "HA1843";

        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.ExtractConstArrayTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.ExtractConstArrayMessage), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.ExtractConstArrayDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static readonly DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
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
            context.RegisterSyntaxNodeAction(AnalyzeArrayCreationExpression, SyntaxKind.ArrayInitializerExpression);
        }

        private static bool IsConstant(ExpressionSyntax syntax, SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.GetSymbolInfo(syntax).Symbol is IFieldSymbol info && info.IsConst;
        }

        private static void AnalyzeArrayCreationExpression(SyntaxNodeAnalysisContext context)
        {
            var arrayCreationExpression = (InitializerExpressionSyntax)context.Node;
            if (arrayCreationExpression.Parent?.Parent is not ArgumentSyntax)
            {
                return;
            }

            if (!arrayCreationExpression.Expressions.All(x => x is LiteralExpressionSyntax || IsConstant(x, context)))
            {
                return;
            }

            var diagnostic = arrayCreationExpression.Parent.Parent.CreateDiagnostic(DefaultRule, arrayCreationExpression.Parent.Parent);

            context.ReportDiagnostic(diagnostic);
        }
    }
}