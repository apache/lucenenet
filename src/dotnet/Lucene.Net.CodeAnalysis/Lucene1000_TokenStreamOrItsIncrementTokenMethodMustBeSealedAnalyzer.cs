using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;


namespace Lucene.Net.CodeAnalysis
{
    // LUCENENET: In Lucene, the TokenStream class had an AssertFinal() method with Reflection code to determine
    // whether subclasses or their IncrementToken() method were marked sealed. This code was not intended to be
    // used at runtime. In .NET, debug code is compiled out, and running Reflection code conditionally is not
    // practical. Instead, this analyzer is installed into the IDE and used at design/build time.
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "Lucene1000";
        private const string Category = "Design";

        private const string TitleCS = "TokenStream derived type or its IncrementToken() method must be marked sealed.";
        private const string MessageFormatCS = "Type name '{0}' or its IncrementToken() method must be marked sealed.";
        private const string DescriptionCS = "TokenStream derived types or their IncrementToken() method must be marked sealed.";

        private const string TitleVB = "TokenStream derived type must be marked NotInheritable or its IncrementToken() method must be marked NotOverridable.";
        private const string MessageFormatVB = "Type name '{0}' must be marked NotInheritable or its IncrementToken() method must be marked NotOverridable.";
        private const string DescriptionVB = "TokenStream derived types must be marked NotInheritable or their IncrementToken() method must be marked NotOverridable.";

        private static readonly DiagnosticDescriptor RuleCS = new DiagnosticDescriptor(DiagnosticId, TitleCS, MessageFormatCS, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionCS);

        private static readonly DiagnosticDescriptor RuleVB = new DiagnosticDescriptor(DiagnosticId, TitleVB, MessageFormatVB, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: DescriptionVB);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleCS, RuleVB);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNodeCS, Microsoft.CodeAnalysis.CSharp.SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeNodeVB, Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.ClassBlock);
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

            context.ReportDiagnostic(Diagnostic.Create(RuleCS, context.Node.GetLocation(), classDeclaration.Identifier));
        }

        private static void AnalyzeNodeVB(SyntaxNodeAnalysisContext context)
        {
            var classBlock = (Microsoft.CodeAnalysis.VisualBasic.Syntax.ClassBlockSyntax)context.Node;

            var classDeclaration = classBlock.ClassStatement;

            var classTypeSymbol = context.SemanticModel.GetDeclaredSymbol(classDeclaration) as ITypeSymbol;

            if (!InheritsFrom(classTypeSymbol, "Lucene.Net.Analysis.TokenStream"))
            {
                return;
            }
            if (classDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotInheritableKeyword) || classDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.MustInheritKeyword))
            {
                return;
            }
            foreach (var member in classBlock.Members.Where(m => m.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.FunctionBlock)))
            {
                var functionBlock = (Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodBlockSyntax)member;

                var methodDeclaration = (Microsoft.CodeAnalysis.VisualBasic.Syntax.MethodStatementSyntax)functionBlock.BlockStatement;

                if (methodDeclaration.Identifier.ValueText == "IncrementToken")
                {
                    if (methodDeclaration.Modifiers.Any(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.NotOverridableKeyword))
                        return; // The method is marked sealed, check passed
                    else
                        break; // The method is not marked sealed, exit the loop and report
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(RuleVB, context.Node.GetLocation(), classDeclaration.Identifier));
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
