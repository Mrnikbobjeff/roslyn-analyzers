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
using Microsoft.CodeAnalysis.Formatting;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SecureNet.Category.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseThrowIfCancellationRequestedCodeFixProvider)), Shared]
    public class UseThrowIfCancellationRequestedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(UseThrowIfCancellationRequestedAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root.FindNode(context.Span) is not IfStatementSyntax ifStatement)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: UseThrowIfCancellationRequestedAnalyzer.DiagnosticId,
                    createChangedSolution: c => ReplaceIfStatementAsync(context.Document, ifStatement, c),
                    equivalenceKey: UseThrowIfCancellationRequestedAnalyzer.DiagnosticId), context.Diagnostics);
        }

        private static async Task<Solution> ReplaceIfStatementAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            if (ifStatement.Condition is not MemberAccessExpressionSyntax memberAccess)
            {
                return document.Project.Solution;
            }

            var throwIfCt = ExpressionStatement(
                                InvocationExpression(
                                   MemberAccessExpression(
                                       SyntaxKind.SimpleMemberAccessExpression,
                                       memberAccess.Expression,
                                       IdentifierName("ThrowIfCancellationRequested")))).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, throwIfCt.WithTriviaFrom(ifStatement.Statement));
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot.NormalizeWhitespace());
        }
    }
}
