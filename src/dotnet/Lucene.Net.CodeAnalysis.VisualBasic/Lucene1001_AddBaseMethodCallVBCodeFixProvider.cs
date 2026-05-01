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

    [ExportCodeFixProvider(LanguageNames.VisualBasic, Name = nameof(Lucene1001_AddBaseMethodCallVBCodeFixProvider)), Shared]
    public class Lucene1001_AddBaseMethodCallVBCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(Lucene1001_TokenStreamOverrideMustCallBaseMethodVBAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodBlock = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodBlockSyntax>().FirstOrDefault();
            if (methodBlock is null)
            {
                return;
            }

            var methodName = ((MethodStatementSyntax)methodBlock.BlockStatement).Identifier.ValueText;
            var title = $"Add MyBase.{methodName}() call";

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => AddBaseCallAsync(context.Document, methodBlock, c),
                    equivalenceKey: title),
                diagnostic);
        }

        private static async Task<Document> AddBaseCallAsync(Document document, MethodBlockSyntax methodBlock, CancellationToken cancellationToken)
        {
            var methodName = ((MethodStatementSyntax)methodBlock.BlockStatement).Identifier.ValueText;

            var baseInvocation = SyntaxFactory.InvocationExpression(
                SyntaxFactory.SimpleMemberAccessExpression(
                    SyntaxFactory.MyBaseExpression(),
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(methodName)),
                SyntaxFactory.ArgumentList());

            var baseStatement = SyntaxFactory.ExpressionStatement(baseInvocation)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newStatements = methodBlock.Statements.Insert(0, baseStatement);
            var newMethodBlock = methodBlock.WithStatements(newStatements);

            var oldRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(methodBlock, newMethodBlock);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
