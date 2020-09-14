// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SecureNet.Category.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferSpanArgumentOverSubstringFixer)), Shared]
    public class PreferSpanArgumentOverSubstringFixer : CodeFixProvider
    {
        private const string title = "Use Span.Slice";

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(PreferSpanArgumentOverSubstringAnalyzer.DiagnosticId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in context.Diagnostics)
            {
                var diagnosticSpan = diagnostic.Location.SourceSpan;

                var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentSyntax>().First();

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedSolution: c => AddAsSpan(context.Document, declaration, c),
                        equivalenceKey: title),
                    diagnostic);
            }
        }

        private static async Task<Solution> AddAsSpan(Document document, ArgumentSyntax argumentDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var substringInvocation = argumentDeclaration.Expression as InvocationExpressionSyntax;

            if (substringInvocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return document.Project.Solution; //unreachable?
            }

            var asSpan = InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        InvocationExpression(
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                               memberAccess.Expression,
                                                IdentifierName("AsSpan"))),
                                        IdentifierName("Slice")))
                                .WithArgumentList(substringInvocation.ArgumentList);
            var newRoot = root.ReplaceNode(argumentDeclaration, argumentDeclaration.WithExpression(asSpan)).NormalizeWhitespace();
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
        }
    }
}
