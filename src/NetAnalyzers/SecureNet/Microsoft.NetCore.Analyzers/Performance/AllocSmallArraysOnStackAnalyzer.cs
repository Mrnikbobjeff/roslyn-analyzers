// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    public sealed class AllocSmallArraysOnStackAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "UA2053";
        private const int SizeThreshold = 1024;
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackTitle), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackMessage), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(AnalyzerResources.AllocSmallArraysOnStackDescription), AnalyzerResources.ResourceManager, typeof(AnalyzerResources));

        internal static DiagnosticDescriptor DefaultRule = DiagnosticDescriptorHelper.Create(
           DiagnosticId,
           Title,
           MessageFormat,
           DiagnosticCategory.Usage,
           RuleLevel.IdeSuggestion,
           description: Description,
           isPortedFxCopRule: false,
           isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DefaultRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.ArrayCreationExpression);
        }

        private static int GetSize(string name, int size)
        {
            return name switch
            {
                "Byte" => sizeof(bool) * size,
                "UInt16" => sizeof(short) * size,
                "UInt32" => sizeof(int) * size,
                "UInt64" => sizeof(long) * size,
                _ => int.MaxValue,//Prevent triggering by making array to large for unknown types
            };
        }
        private static bool IsConstant(ExpressionSyntax syntax, SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.GetSymbolInfo(syntax).Symbol is IFieldSymbol info && info.IsConst;
        }

        private static int GetConstantValue(SemanticModel model, ExpressionSyntax syntax)
        {
            if (syntax is LiteralExpressionSyntax literalSyntax)
                return (int)literalSyntax.Token.Value;
            return (int)((model.GetSymbolInfo(syntax).Symbol as IFieldSymbol)?.ConstantValue ?? int.MaxValue);
        }

        private static void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            if (context.Compilation.Options is CSharpCompilationOptions opt && !opt.AllowUnsafe)
            {
                return;
            }

            var creationExpression = (ArrayCreationExpressionSyntax)context.Node;
            var method = (MethodDeclarationSyntax)creationExpression.FirstAncestorOrSelf<SyntaxNode>(x => x is MethodDeclarationSyntax);
            if (method is null)
            {
                return;
            }

            if (method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)))
            {
                return;
            }

            if (context.SemanticModel.GetTypeInfo(creationExpression.Type).Type is not IArrayTypeSymbol type)
            {
                return;
            }

            if (creationExpression.FirstAncestorOrSelf<ForEachStatementSyntax>() != null
                || creationExpression.FirstAncestorOrSelf<ForStatementSyntax>() != null
                || creationExpression.FirstAncestorOrSelf<DoStatementSyntax>() != null
                || creationExpression.FirstAncestorOrSelf<WhileStatementSyntax>() != null)
            {
                return;
            }

            if (creationExpression.Type.RankSpecifiers.Count <= 1)
            {
                int arraySize;
                if (creationExpression.Type.RankSpecifiers.Single().Sizes.Single() is OmittedArraySizeExpressionSyntax)
                {
                    arraySize = creationExpression.Initializer.Expressions.Count;
                }
                else
                {
                    var exp = creationExpression.Type.RankSpecifiers.Single().Sizes.Single();
                    if (!(exp is LiteralExpressionSyntax || IsConstant(exp, context)))
                    {
                        return;
                    }

                    arraySize = GetConstantValue(context.SemanticModel, exp);

                }
                if (GetSize(type.ElementType.Name, arraySize) > SizeThreshold)
                    return;
                var isVariableDeclaration = creationExpression.FirstAncestorOrSelf<VariableDeclarationSyntax>();
                if (isVariableDeclaration == null)
                {
                    return; // May escape, this way we only capture variables assigned to a local
                }

                if (method.Body.Statements.OfType<ReturnStatementSyntax>()
                    .Where(ret => ret.Expression is IdentifierNameSyntax id
                        && id.Identifier.ValueText.Equals(isVariableDeclaration.Variables.First().Identifier.ValueText, StringComparison.Ordinal)).Any())
                {
                    return;
                }

                var variableName = isVariableDeclaration.Variables.First().Identifier.ValueText;
                var isPassedAsParameter = method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>()
                     .Where(invocation => invocation.ArgumentList.Arguments.Any(arg =>
                                                                                     arg.Expression is IdentifierNameSyntax id
                         && id.Identifier.ValueText.Equals(variableName, StringComparison.Ordinal))).Any();
                if (isPassedAsParameter)
                    return;

                var isPassedToConstructor = method.Body.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                    .Where(invocation => invocation.ArgumentList.Arguments.Any(arg =>
                                                                                    arg.Expression is IdentifierNameSyntax id
                        && id.Identifier.ValueText.Equals(isVariableDeclaration.Variables.First().Identifier.ValueText, StringComparison.Ordinal))).Any();
                if (isPassedToConstructor)
                {
                    return;
                }

                var isWrittenOutside = method.Body.DescendantNodes().OfType<AssignmentExpressionSyntax>()
                   .Where(invocation => invocation.Right is IdentifierNameSyntax id
                       && id.Identifier.ValueText.Equals(isVariableDeclaration.Variables.First().Identifier.ValueText, StringComparison.Ordinal)).Any();
                if (isWrittenOutside)
                {
                    return;
                }
            }

            var diagnostic = creationExpression.Parent.CreateDiagnostic(DefaultRule, creationExpression);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
