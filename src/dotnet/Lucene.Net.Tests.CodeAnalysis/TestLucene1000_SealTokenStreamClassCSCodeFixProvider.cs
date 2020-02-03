using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System;
using TestHelper;

namespace Lucene.Net.CodeAnalysis
{
    public class TestLucene1000_SealTokenStreamClassCSCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new Lucene1000_SealTokenStreamClassCSCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedAnalyzer();
        }


        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }


        //Diagnostic and CodeFix both triggered and checked for
        [Test]
        public void TestDiagnosticAndCodeFix()
        {
            var test = @"
using Lucene.Net.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyNamespace
{
    class TypeName : TokenStream
    {
        public override bool IncrementToken()
        {
            throw new NotImplementedException();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedAnalyzer.DiagnosticId,
                Message = String.Format("Type name '{0}' or its IncrementToken() method must be marked sealed.", "TypeName"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.cs", 12, 5)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using Lucene.Net.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken()
        {
            throw new NotImplementedException();
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }
    }
}
