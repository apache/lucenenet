using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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

    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    public class Lucene1001_TokenStreamOverrideMustCallBaseMethodVBAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Lucene1001";
        private const string Category = "Design";

        private const string Title = "TokenStream override of End()/Reset()/Close() must call the corresponding base method.";
        private const string MessageFormat = "Override of '{0}()' on type '{1}' must call MyBase.{0}().";
        private const string Description = "Subclasses of TokenStream that override End(), Reset(), or Close() must call the corresponding base method, otherwise some internal state will not be correctly reset.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        private static readonly HashSet<string> TrackedMethodNames = new HashSet<string> { "End", "Reset", "Close" };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SubBlock, SyntaxKind.FunctionBlock);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodBlock = (MethodBlockSyntax)context.Node;
            var methodStatement = (MethodStatementSyntax)methodBlock.BlockStatement;

            var methodName = methodStatement.Identifier.ValueText;
            if (!TrackedMethodNames.Contains(methodName))
            {
                return;
            }

            // Must be parameterless to match the TokenStream contract methods.
            if (methodStatement.ParameterList != null && methodStatement.ParameterList.Parameters.Count != 0)
            {
                return;
            }

            // Skip non-overrides.
            if (!methodStatement.Modifiers.Any(SyntaxKind.OverridesKeyword))
            {
                return;
            }
            if (methodStatement.Modifiers.Any(SyntaxKind.MustOverrideKeyword))
            {
                return;
            }

            var classBlock = methodBlock.Parent as ClassBlockSyntax;
            if (classBlock is null)
            {
                return;
            }

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classBlock.ClassStatement) as ITypeSymbol;
            if (!InheritsFrom(classTypeSymbol, "Lucene.Net.Analysis.TokenStream"))
            {
                return;
            }

            if (ContainsBaseCall(methodBlock, methodName))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, methodStatement.Identifier.GetLocation(), methodName, classBlock.ClassStatement.Identifier.ValueText));
        }

        private static bool ContainsBaseCall(MethodBlockSyntax methodBlock, string methodName)
        {
            foreach (var node in methodBlock.Statements.SelectMany(s => s.DescendantNodesAndSelf()))
            {
                if (node is InvocationExpressionSyntax invocation &&
                    invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) &&
                    memberAccess.Name.Identifier.ValueText == methodName &&
                    !IsInsideLambda(invocation, methodBlock))
                {
                    return true;
                }
            }
            return false;
        }

        // A base call inside a lambda does not necessarily execute when the enclosing method runs
        // (the delegate may never be invoked). Treat such occurrences as not satisfying the contract.
        private static bool IsInsideLambda(SyntaxNode node, SyntaxNode body)
        {
            for (var ancestor = node.Parent; ancestor is not null && ancestor != body; ancestor = ancestor.Parent)
            {
                if (ancestor is LambdaExpressionSyntax)
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
