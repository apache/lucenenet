using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
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
    public class Lucene1002_TokenizerMustOverrideEndVBAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Lucene1002";
        private const string Category = "Design";

        private const string Title = "Tokenizer subclasses must override End().";
        private const string MessageFormat = "Tokenizer subclass '{0}' must override End().";
        private const string Description = "Tokenizer subclasses must override End() to set the correct final offset and finish other end-of-stream attributes.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ClassBlock);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var classBlock = (ClassBlockSyntax)context.Node;
            var classStatement = classBlock.ClassStatement;

            // Skip MustInherit (abstract) classes.
            if (classStatement.Modifiers.Any(SyntaxKind.MustInheritKeyword))
            {
                return;
            }

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classStatement) as INamedTypeSymbol;
            if (!InheritsFromTokenizer(classTypeSymbol))
            {
                return;
            }

            if (HasEndOverrideInHierarchy(classTypeSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, classStatement.Identifier.GetLocation(), classStatement.Identifier.ValueText));
        }

        private static bool InheritsFromTokenizer(INamedTypeSymbol symbol)
        {
            if (symbol == null)
            {
                return false;
            }
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (baseType.ToString().Equals("Lucene.Net.Analysis.Tokenizer"))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private static bool HasEndOverrideInHierarchy(INamedTypeSymbol symbol)
        {
            var current = symbol;
            while (current != null && current.ToString() != "Lucene.Net.Analysis.Tokenizer")
            {
                foreach (var member in current.GetMembers("End"))
                {
                    if (member is IMethodSymbol method &&
                        method.Parameters.Length == 0 &&
                        method.IsOverride)
                    {
                        return true;
                    }
                }
                current = current.BaseType;
            }
            return false;
        }
    }
}
