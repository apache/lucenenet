using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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

    // LUCENENET: In Lucene, the TokenStream class had an AssertFinal() method with Reflection code to determine
    // whether subclasses or their IncrementToken() method were marked sealed. This code was not intended to be
    // used at runtime. In .NET, debug code is compiled out, and running Reflection code conditionally is not
    // practical. Instead, this analyzer is installed into the IDE and used at design/build time.
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedCSAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Lucene1000";
        private const string Category = "Design";

        private const string Title = "TokenStream derived type or its IncrementToken() method must be marked sealed.";
        private const string MessageFormat = "Type name '{0}' or its IncrementToken() method must be marked sealed.";
        private const string Description = "TokenStream derived types or their IncrementToken() method must be marked sealed.";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
        }

        private static void AnalyzeNodeCS(SyntaxNodeAnalysisContext context)
        {
            var classDeclaration = (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)context.Node;

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

            if (!InheritsFrom(classTypeSymbol, "Lucene.Net.Analysis.TokenStream"))
            {
                return;
            }
            if (classDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SealedKeyword) || classDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AbstractKeyword))
            {
                return;
            }
            foreach (var member in classDeclaration.Members.Where(m => m.Kind() == Microsoft.CodeAnalysis.CSharp.SyntaxKind.MethodDeclaration))
            {
                var methodDeclaration = (Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax)member;

                if (methodDeclaration.Identifier.ValueText == "IncrementToken")
                {
                    if (methodDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SealedKeyword))
                        return; // The method is marked sealed, check passed
                    else
                        break; // The method is not marked sealed, exit the loop and report
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation(), classDeclaration.Identifier));
        }

        private static bool InheritsFrom(ITypeSymbol symbol, string expectedParentTypeName)
        {
            while (true)
            {
                if (symbol.ToString().Equals(expectedParentTypeName))
                {
                    return true;
                }

                if (symbol.BaseType != null)
                {
                    symbol = symbol.BaseType;
                    continue;
                }
                break;
            }

            return false;
        }
    }
}
