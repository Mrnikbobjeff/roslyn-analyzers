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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;

namespace SecureNet.Category.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractConstArrayFixer)), Shared]
    public sealed class ExtractConstArrayFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ExtractConstArrayAnalyzerAnalyzer.DiagnosticId);

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null)
            {
                return;
            }

            if (root.FindNode(context.Span, getInnermostNodeForTie: true).Parent is not ArgumentSyntax node)
            {
                return;
            }

            var isInterface = node.FirstAncestorOrSelf<SyntaxNode>(x => x is StructDeclarationSyntax or ClassDeclarationSyntax) == node;
            if (isInterface)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: ExtractConstArrayAnalyzerAnalyzer.DiagnosticId,
                    createChangedSolution: c => ExtractConstArray(context.Document, node, c),
                    equivalenceKey: ExtractConstArrayAnalyzerAnalyzer.DiagnosticId),
                context.Diagnostics);
        }

        private static async Task<Solution> ExtractConstArray(Document document, ArgumentSyntax typeDecl, CancellationToken cancellationToken)
        {
            var originalSolution = document.Project.Solution;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (typeDecl.FirstAncestorOrSelf<SyntaxNode>(x => x is InvocationExpressionSyntax) is not InvocationExpressionSyntax paramsInvocation)
            {
                return document.Project.Solution;
            }

            if (semanticModel.GetSymbolInfo(paramsInvocation, cancellationToken: cancellationToken).Symbol is not IMethodSymbol method)
            {
                return document.Project.Solution;
            }
            var typeDisplayString = method.Parameters.Last().Type.ToMinimalDisplayString(semanticModel, method.Parameters.Last().Locations.First().SourceSpan.Start);

            var typeSyntax = SyntaxFactory.ParseTypeName(typeDisplayString);
            var equalsValueClause = SyntaxFactory.EqualsValueClause(typeDecl.Expression);
            var declarator = new SeparatedSyntaxList<VariableDeclaratorSyntax>();
            declarator = declarator.Add(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("hoisted"), null, equalsValueClause));
            var variableAssignment = SyntaxFactory.VariableDeclaration(typeSyntax, declarator).WithAdditionalAnnotations(Formatter.Annotation);
            var assignmentExpression = SyntaxFactory.FieldDeclaration(
                new SyntaxList<AttributeListSyntax>(),
                new SyntaxTokenList().Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),
                variableAssignment).WithAdditionalAnnotations(Formatter.Annotation);

            var invocationParameterReplacement = new SeparatedSyntaxList<ArgumentSyntax>();
            invocationParameterReplacement = invocationParameterReplacement.AddRange(paramsInvocation.ArgumentList.Arguments.TakeWhile(x => x != typeDecl));
            invocationParameterReplacement = invocationParameterReplacement.Add(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("hoisted")));
            invocationParameterReplacement = invocationParameterReplacement.AddRange(paramsInvocation.ArgumentList.Arguments.Skip(invocationParameterReplacement.Count));
            var newArgListSyntax = SyntaxFactory.ArgumentList(invocationParameterReplacement);
            var newDeclaration = paramsInvocation.WithArgumentList(newArgListSyntax);

            var classOrStruct = typeDecl.FirstAncestorOrSelf<SyntaxNode>(x => x is StructDeclarationSyntax or ClassDeclarationSyntax);
            var documentEditor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            documentEditor.InsertMembers(classOrStruct, 0, new[] { assignmentExpression });
            documentEditor.ReplaceNode(paramsInvocation, newDeclaration);

            var newDocument = documentEditor.GetChangedDocument();
            var finalRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            finalRoot = Formatter.Format(finalRoot, Formatter.Annotation, document.Project.Solution.Workspace, cancellationToken: cancellationToken).NormalizeWhitespace();
            return originalSolution.WithDocumentText(document.Id, finalRoot.GetText());
        }
    }
}
