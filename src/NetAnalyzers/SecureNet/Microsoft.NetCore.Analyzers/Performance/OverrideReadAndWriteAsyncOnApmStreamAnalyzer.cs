// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Analyzer.Utilities;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SecureNet.Category.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class OverrideReadAndWriteAsyncOnApmStreamAnalyzer : DiagnosticAnalyzer
    {
        public const string ReadRuleId = "HA1000";
        public const string WriteRuleId = "HA1001";
        private static readonly string[] ApmRead = new string[] { "BeginRead", "EndRead" };
        private static readonly string[] ApmWrite = new string[] { "BeginWrite", "EndWrite" };
        private const string ReadAsync = nameof(ReadAsync);
        private const string WriteAsync = nameof(WriteAsync);

        internal static readonly DiagnosticDescriptor ReadRule = DiagnosticDescriptorHelper.Create(
            ReadRuleId,
            "",
            "",
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: "",
            isPortedFxCopRule: false,
            isDataflowRule: false);

        internal static readonly DiagnosticDescriptor WriteRule = DiagnosticDescriptorHelper.Create(
            WriteRuleId,
            "",
            "",
            DiagnosticCategory.Performance,
            RuleLevel.IdeSuggestion,
            description: "",
            isPortedFxCopRule: false,
            isDataflowRule: false);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ReadRule, WriteRule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(ctx =>
            {
                if (!ctx.Compilation.TryGetOrCreateTypeByMetadataName(WellKnownTypeNames.SystemIOStream, out var streamType))
                {
                    return;
                }
                ctx.RegisterSymbolAction(x => AnalyzeSymbol(x, streamType), SymbolKind.NamedType);
            });
        }

        private static bool IsActualDescendantOf(INamedTypeSymbol typeSymbol, INamedTypeSymbol baseType)
        {
            var container = typeSymbol;
            do
            {
                if (container.Equals(baseType))
                    return true;
                container = container.BaseType;
            } while (container != null);
            return false;
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, INamedTypeSymbol streamType)
        {
            var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;
            if (!IsActualDescendantOf(namedTypeSymbol, streamType))
            {
                return;
            }

            var declaredMethodsNotInStream = namedTypeSymbol
                .GetMembers()
                .OfType<IMethodSymbol>()
                .Where(t => !t.ContainingType.Equals(streamType));
            var readByteCallPresent = declaredMethodsNotInStream
                .Where(t => t.Name.Equals(WriteAsync, StringComparison.Ordinal)).Any();
            var writeByteCallPresent = declaredMethodsNotInStream
                .Where(t => t.Name.Equals(ReadAsync, StringComparison.Ordinal)).Any();

            if (writeByteCallPresent && readByteCallPresent)
            {
                return; //All are present
            }

            if (declaredMethodsNotInStream.Count(x => ApmRead.Contains(x.Name)) == 2 && !readByteCallPresent)
            {
                namedTypeSymbol.CreateDiagnostic(ReadRule);
            }

            if (declaredMethodsNotInStream.Count(x => ApmWrite.Contains(x.Name)) == 2 && !writeByteCallPresent)
            {
                namedTypeSymbol.CreateDiagnostic(WriteRule);
            }
        }
    }
}
