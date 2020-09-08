// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Analyzer.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.NetCore.Analyzers.Performance
{
    public sealed class PreferContainsKeyOrValueOverPropertyAccessFixer : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId, PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsValueRuleId);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root.FindNode(context.Span, getInnermostNodeForTie: true) is not InvocationExpressionSyntax node)
            {
                return;
            }

            var nestedMemberAccessExpression = (node.Expression as MemberAccessExpressionSyntax)?.Expression;
            if (nestedMemberAccessExpression is not MemberAccessExpressionSyntax possibleValueOrKeysMemberAccess)
            {
                return;
            }

            if (possibleValueOrKeysMemberAccess.Name.Identifier.ValueText.Equals("Keys", System.StringComparison.Ordinal))
            {
                var identifier = SyntaxFactory.IdentifierName(@"ContainsKey");
                context.RegisterCodeFix(
                    new MyCodeAction(
                        title: PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId,
                        createChangedDocument: c => ReplaceWithSpecializedContainsKey(context.Document, node, possibleValueOrKeysMemberAccess, identifier, c),
                        equivalenceKey: PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId), context.Diagnostics);
            }
            else
            {
                var identifier = SyntaxFactory.IdentifierName(@"ContainsValue");
                context.RegisterCodeFix(
                    new MyCodeAction(
                        title: PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId,
                        createChangedDocument: c => ReplaceWithSpecializedContainsKey(context.Document, node, possibleValueOrKeysMemberAccess, identifier, c),
                        equivalenceKey: PreferContainsKeyOrValueOverPropertyAccessAnalyzer.ContainsKeyRuleId), context.Diagnostics);
            }
        }

        private static async Task<Document> ReplaceWithSpecializedContainsKey(Document document, InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax keyAccessExpression,IdentifierNameSyntax id, CancellationToken cancellationToken)
        {
            var correctMemberAccess = keyAccessExpression.Expression; // This is the IDictionary<'2>

            if (correctMemberAccess is null)
            {
                return document;
            }

            var callAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, correctMemberAccess, id);
            var containsKeyOrValueCall = SyntaxFactory.InvocationExpression(callAccess, invocation.ArgumentList);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(invocation, containsKeyOrValueCall);
            return document.WithSyntaxRoot(newRoot);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(title, createChangedDocument, equivalenceKey)
            {
            }
        }
    }
}
