using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.CodeAnalysis
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    [ExportCodeFixProvider(LanguageNames.VisualBasic, Name = nameof(Lucene1002_AddEndOverrideVBCodeFixProvider)), Shared]
    public class Lucene1002_AddEndOverrideVBCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add End() override that calls MyBase.End()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Lucene1002_TokenizerMustOverrideEndVBAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var classBlock = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassBlockSyntax>().FirstOrDefault();
            if (classBlock is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddEndOverrideAsync(context.Document, classBlock, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddEndOverrideAsync(Document document, ClassBlockSyntax classBlock, CancellationToken cancellationToken)
        {
            // Public Overrides Sub End()
            //     MyBase.End()
            //     ' TODO: set the final offset and finish up other end-of-stream attributes
            // End Sub
            var subStatement = SyntaxFactory.SubStatement(
                    attributeLists: default,
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverridesKeyword)),
                    subOrFunctionKeyword: SyntaxFactory.Token(SyntaxKind.SubKeyword),
                    identifier: SyntaxFactory.Identifier("End"),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(),
                    asClause: null,
                    handlesClause: null,
                    implementsClause: null);

            var baseInvocation = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.SimpleMemberAccessExpression(
                        SyntaxFactory.MyBaseExpression(),
                        SyntaxFactory.Token(SyntaxKind.DotToken),
                        SyntaxFactory.IdentifierName("End")),
                    SyntaxFactory.ArgumentList()));

            var endSubStatement = SyntaxFactory.EndSubStatement()
                .WithLeadingTrivia(SyntaxFactory.CommentTrivia("' TODO: set the final offset and finish up other end-of-stream attributes"), SyntaxFactory.ElasticCarriageReturnLineFeed);

            var methodBlock = SyntaxFactory.SubBlock(
                subStatement,
                SyntaxFactory.SingletonList<StatementSyntax>(baseInvocation),
                endSubStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newClassBlock = classBlock.AddMembers(methodBlock);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(classBlock, newClassBlock);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
