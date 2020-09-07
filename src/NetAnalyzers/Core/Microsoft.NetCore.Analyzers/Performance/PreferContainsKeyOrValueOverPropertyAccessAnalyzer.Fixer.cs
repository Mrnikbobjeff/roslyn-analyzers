// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
namespace Microsoft.NetCore.Analyzers.Performance
{
    public sealed class PreferContainsKeyOrValueOverPropertyAccessFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId, PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsValueRuleId);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            SyntaxNode node = root.FindNode(context.Span, getInnermostNodeForTie: true);
            if (node == null)
            {
                return;
            }

            ImmutableDictionary<string, string> properties = context.Diagnostics[0].Properties;
            if (properties == null)
            {
                return;
            }
        }
    }
}
