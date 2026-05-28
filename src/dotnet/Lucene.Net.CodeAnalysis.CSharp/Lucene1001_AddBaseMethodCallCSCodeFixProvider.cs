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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(Lucene1001_AddBaseMethodCallCSCodeFixProvider)), Shared]
    public class Lucene1001_AddBaseMethodCallCSCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration is null)
            {
                return;
            }

            var methodName = methodDeclaration.Identifier.ValueText;
            var title = $"Add base.{methodName}() call";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddBaseCallAsync(context.Document, methodDeclaration, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private static async Task<Document> AddBaseCallAsync(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
        {
            var methodName = methodDeclaration.Identifier.ValueText;

            var baseInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.BaseExpression(),
                    SyntaxFactory.IdentifierName(methodName)));

            var baseStatement = SyntaxFactory.ExpressionStatement(baseInvocation)
                .WithAdditionalAnnotations(Formatter.Annotation);

            MethodDeclarationSyntax newMethodDeclaration;

            if (methodDeclaration.Body is not null)
            {
                var newBody = methodDeclaration.Body.WithStatements(
                    methodDeclaration.Body.Statements.Insert(0, baseStatement));
                newMethodDeclaration = methodDeclaration.WithBody(newBody);
            }
            else if (methodDeclaration.ExpressionBody is not null)
            {
                // Convert expression body => to a block containing base.X(); followed by the original expression as a statement.
                var existingExpression = methodDeclaration.ExpressionBody.Expression;
                StatementSyntax existingStatement = existingExpression is ThrowExpressionSyntax throwExpression
                    ? SyntaxFactory.ThrowStatement(throwExpression.Expression)
                    : SyntaxFactory.ExpressionStatement(existingExpression);
                existingStatement = existingStatement.WithAdditionalAnnotations(Formatter.Annotation);
                var newBody = SyntaxFactory.Block(baseStatement, existingStatement)
                    .WithAdditionalAnnotations(Formatter.Annotation);
                newMethodDeclaration = methodDeclaration
                    .WithExpressionBody(null)
                    .WithSemicolonToken(default)
                    .WithBody(newBody);
            }
            else
            {
                return document;
            }

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
