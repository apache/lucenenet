using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

    // LUCENENET: TokenStream documents that subclasses overriding End(), Reset(), or Close()
    // must call the corresponding base method, otherwise some internal state will not be
    // correctly reset and Tokenizer will throw at runtime. This analyzer surfaces the same
    // contract at compile time. See: src/Lucene.Net/Analysis/TokenStream.cs and the
    // "More Requirements for Analysis Component Classes" section of Analysis package.md.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Lucene1001";
        private const string Category = "Design";

        private const string Title = "TokenStream override of End()/Reset()/Close() must call the corresponding base method.";
        private const string MessageFormat = "Override of '{0}()' on type '{1}' must call base.{0}().";
        private const string Description = "Subclasses of TokenStream that override End(), Reset(), or Close() must call the corresponding base method, otherwise some internal state will not be correctly reset.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly HashSet<string> TrackedMethodNames = new HashSet<string> { "End", "Reset", "Close" };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            var methodName = methodDeclaration.Identifier.ValueText;
            if (!TrackedMethodNames.Contains(methodName))
            {
                return;
            }

            // Must be parameterless to match the TokenStream contract methods.
            if (methodDeclaration.ParameterList.Parameters.Count != 0)
            {
                return;
            }

            // Skip non-overrides; abstract overrides have no body to inspect.
            if (!methodDeclaration.Modifiers.Any(SyntaxKind.OverrideKeyword))
            {
                return;
            }
            if (methodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword) ||
                methodDeclaration.Modifiers.Any(SyntaxKind.ExternKeyword))
            {
                return;
            }

            var classDeclaration = methodDeclaration.Parent as ClassDeclarationSyntax;
            if (classDeclaration is null)
            {
                return;
            }

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;
            if (!InheritsFrom(classTypeSymbol, "Lucene.Net.Analysis.TokenStream"))
            {
                return;
            }

            if (ContainsBaseCall(methodDeclaration, methodName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclaration.Identifier.GetLocation(), methodName, classDeclaration.Identifier.ValueText));
        }

        private static bool ContainsBaseCall(MethodDeclarationSyntax methodDeclaration, string methodName)
        {
            // Inspect either a block body or an expression-bodied member.
            SyntaxNode body;
            IEnumerable<SyntaxNode> nodes;
            if (methodDeclaration.Body is not null)
            {
                body = methodDeclaration.Body;
                nodes = methodDeclaration.Body.DescendantNodes();
            }
            else if (methodDeclaration.ExpressionBody is not null)
            {
                body = methodDeclaration.ExpressionBody;
                nodes = methodDeclaration.ExpressionBody.DescendantNodesAndSelf();
            }
            else
            {
                // No body (e.g. partial without implementation) — nothing to check.
                return true;
            }

            foreach (var node in nodes)
            {
                if (node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression.IsKind(SyntaxKind.BaseExpression) &&
                    memberAccess.Name.Identifier.ValueText == methodName &&
                    !IsInsideNestedFunction(invocation, body))
                {
                    return true;
                }
            }
            return false;
        }

        // A base call inside a lambda, anonymous method, or local function does not necessarily
        // execute when the enclosing method runs (the delegate may never be invoked). Treat such
        // occurrences as not satisfying the contract.
        private static bool IsInsideNestedFunction(SyntaxNode node, SyntaxNode body)
        {
            for (var ancestor = node.Parent; ancestor is not null && ancestor != body; ancestor = ancestor.Parent)
            {
                if (ancestor is LambdaExpressionSyntax ||
                    ancestor is AnonymousMethodExpressionSyntax ||
                    ancestor is LocalFunctionStatementSyntax)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool InheritsFrom(ITypeSymbol symbol, string expectedParentTypeName)
        {
            while (symbol != null)
            {
                if (symbol.ToString().Equals(expectedParentTypeName))
                {
                    return true;
                }
                symbol = symbol.BaseType;
            }
            return false;
        }
    }
}
