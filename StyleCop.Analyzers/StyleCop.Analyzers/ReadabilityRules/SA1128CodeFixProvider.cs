﻿// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

namespace StyleCop.Analyzers.ReadabilityRules
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using StyleCop.Analyzers.Helpers;

    /// <summary>
    /// Implements a code fix for <see cref="SA1107CodeMustNotContainMultipleStatementsOnOneLine"/>.
    /// </summary>
    /// <remarks>
    /// <para>To fix a violation of this rule, add or remove a space after the keyword, according to the description
    /// above.</para>
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SA1128CodeFixProvider))]
    [Shared]
    internal class SA1128CodeFixProvider : CodeFixProvider
    {
        ////private static readonly SA1107FixAllProvider FixAllProvider = new SA1107FixAllProvider();

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(SA1128ConstructorInitializerMustBeOnOwnLine.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider()
        {
            return FixAll.Instance;
        }

        /// <inheritdoc/>
        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        ReadabilityResources.SA1128CodeFix,
                        cancellationToken => GetTransformedDocumentAsync(context.Document, diagnostic, cancellationToken),
                        equivalenceKey: nameof(SA1128CodeFixProvider)),
                    diagnostic);
            }

            return SpecializedTasks.CompletedTask;
        }

        private static async Task<Document> GetTransformedDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var indentationOptions = IndentationOptions.FromDocument(document);
            var newLine = TriviaHelper.GetNewLineTrivia(document);

            var constructorInitializer = (ConstructorInitializerSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var constructorDeclaration = (ConstructorDeclarationSyntax)constructorInitializer.Parent;

            var newConstructorDeclaration = ReformatConstructorDeclaration(constructorDeclaration, indentationOptions, newLine);

            var newSyntaxRoot = syntaxRoot.ReplaceNode(constructorDeclaration, newConstructorDeclaration);
            return document.WithSyntaxRoot(newSyntaxRoot);
        }

        private static ConstructorDeclarationSyntax ReformatConstructorDeclaration(ConstructorDeclarationSyntax constructorDeclaration, IndentationOptions indentationOptions, SyntaxTrivia newLine)
        {
            var constructorInitializer = constructorDeclaration.Initializer;

            var newParameterList = constructorDeclaration.ParameterList
                .WithTrailingTrivia(constructorDeclaration.ParameterList.GetTrailingTrivia().WithoutTrailingWhitespace().Add(newLine));

            var indentationSteps = IndentationHelper.GetIndentationSteps(indentationOptions, constructorDeclaration);
            var indentation = IndentationHelper.GenerateWhitespaceTrivia(indentationOptions, indentationSteps + 1);

            var newColonTrailingTrivia = constructorInitializer.ColonToken.TrailingTrivia.WithoutTrailingWhitespace();

            var newColonToken = constructorInitializer.ColonToken
                .WithLeadingTrivia(indentation)
                .WithTrailingTrivia(newColonTrailingTrivia);

            var newInitializer = constructorInitializer
                .WithColonToken(newColonToken)
                .WithThisOrBaseKeyword(constructorInitializer.ThisOrBaseKeyword.WithLeadingTrivia(SyntaxFactory.Space));

            return constructorDeclaration
                .WithParameterList(newParameterList)
                .WithInitializer(newInitializer);
        }

        private class FixAll : DocumentBasedFixAllProvider
        {
            public static FixAllProvider Instance { get; } =
                new FixAll();

            protected override string CodeActionTitle { get; } =
                ReadabilityResources.SA1128CodeFix;

            protected override async Task<SyntaxNode> FixAllInDocumentAsync(FixAllContext fixAllContext, Document document)
            {
                var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                if (diagnostics.IsEmpty)
                {
                    return null;
                }

                var syntaxRoot = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                var indentationOptions = IndentationOptions.FromDocument(document);
                var newLine = TriviaHelper.GetNewLineTrivia(document);

                SortedDictionary<SyntaxNode, SyntaxNode> replaceMap = new SortedDictionary<SyntaxNode, SyntaxNode>(new SpanComparer());

                foreach (var diagnostic in diagnostics)
                {
                    var constructorInitializer = (ConstructorInitializerSyntax)syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                    var constructorDeclaration = (ConstructorDeclarationSyntax)constructorInitializer.Parent;

                    var newConstructorDeclaration = ReformatConstructorDeclaration(constructorDeclaration, indentationOptions, newLine);

                    replaceMap.Add(constructorDeclaration, newConstructorDeclaration);
                }

                return syntaxRoot.ReplaceNodes(replaceMap.Keys, (original, maybeRewritten) => replaceMap[original]);
            }
        }

        private class SpanComparer : IComparer<SyntaxNode>
        {
            public int Compare(SyntaxNode x, SyntaxNode y)
            {
                return x.Span.Start - y.Span.Start;
            }
        }
    }
}