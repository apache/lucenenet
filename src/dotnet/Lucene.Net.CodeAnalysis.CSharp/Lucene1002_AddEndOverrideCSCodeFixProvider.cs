using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Lucene1002_AddEndOverrideCSCodeFixProvider)), Shared]
    public class Lucene1002_AddEndOverrideCSCodeFixProvider : CodeFixProvider
    {
        private const string Title = "Add End() override that calls base.End()";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Lucene1002_TokenizerMustOverrideEndCSAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var classDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration is null)
            {
                return;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Title,
                    createChangedDocument: c => AddEndOverrideAsync(context.Document, classDeclaration, c),
                    equivalenceKey: Title),
                diagnostic);
        }

        private static async Task<Document> AddEndOverrideAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken)
        {
            // public override void End()
            // {
            //     base.End();
            //     // TODO: set the final offset and finish up other end-of-stream attributes
            // }
            var baseInvocation = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.BaseExpression(),
                        SyntaxFactory.IdentifierName("End"))));

            var todoComment = SyntaxFactory.Comment("// TODO: set the final offset and finish up other end-of-stream attributes");
            var closeBrace = SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                .WithLeadingTrivia(SyntaxFactory.TriviaList(todoComment, SyntaxFactory.ElasticCarriageReturnLineFeed));

            var body = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                SyntaxFactory.SingletonList<StatementSyntax>(baseInvocation),
                closeBrace);

            var endMethod = SyntaxFactory.MethodDeclaration(
                    attributeLists: default,
                    modifiers: SyntaxFactory.TokenList(
                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                        SyntaxFactory.Token(SyntaxKind.OverrideKeyword)),
                    returnType: SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                    explicitInterfaceSpecifier: null,
                    identifier: SyntaxFactory.Identifier("End"),
                    typeParameterList: null,
                    parameterList: SyntaxFactory.ParameterList(),
                    constraintClauses: default,
                    body: body,
                    expressionBody: null,
                    semicolonToken: default)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newClassDeclaration = classDeclaration.AddMembers(endMethod);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(classDeclaration, newClassDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
