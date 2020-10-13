// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SecureNet.Category.Tasks
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskWaitAllCodeFixProvider)), Shared]
    public class TaskWaitAllCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TaskWaitAllAnalyzer.RuleId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is not InvocationExpressionSyntax invocationExpression)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: TaskWaitAllAnalyzer.RuleId,
                    createChangedSolution: c => ReplaceWithSingleWait(context.Document, invocationExpression, c),
                    equivalenceKey: TaskWaitAllAnalyzer.RuleId), context.Diagnostics);
        }

        private static async Task<Solution> ReplaceWithSingleWait(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var waitExpression = InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                                invocationExpression.ArgumentList.Arguments[0].Expression,
                                            IdentifierName("Wait")));
            var newRoot = root.ReplaceNode(invocationExpression, waitExpression.WithTriviaFrom(invocationExpression));
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot.NormalizeWhitespace());
        }
    }
}