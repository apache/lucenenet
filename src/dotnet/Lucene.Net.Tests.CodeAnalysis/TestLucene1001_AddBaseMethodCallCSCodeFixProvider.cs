using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using TestHelper;

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

    public class TestLucene1001_AddBaseMethodCallCSCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new Lucene1001_AddBaseMethodCallCSCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer();
        }

        [Test]
        public void TestEmptyFile()
        {
            VerifyCSharpDiagnostic(@"");
        }

        [Test]
        public void TestNonTokenStreamSubclass_NoDiagnostic()
        {
            // Override of Reset() on a class that doesn't inherit from TokenStream — out of scope.
            var test = @"
namespace MyNamespace
{
    abstract class Other
    {
        public virtual void Reset() { }
    }

    sealed class TypeName : Other
    {
        public override void Reset()
        {
            // No base call, but we don't care about non-TokenStream classes.
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestResetWithoutBaseCall_DiagnosticAndFix()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            ClearAttributes();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            base.Reset();
            ClearAttributes();
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        [Test]
        public void TestResetWithBaseCall_NoDiagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            base.Reset();
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestEndWithoutBaseCall_Diagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void End()
        {
            ClearAttributes();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'End()' on type 'TypeName' must call base.End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestCloseWithoutBaseCall_Diagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Close()
        {
            // missing base.Close()
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Close()' on type 'TypeName' must call base.Close().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestExpressionBodiedWithoutBaseCall_Diagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset() => System.Diagnostics.Debug.WriteLine(""hi"");
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            base.Reset();
            System.Diagnostics.Debug.WriteLine(""hi"");
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        [Test]
        public void TestExpressionBodiedThrow_Diagnostic_CodeFix()
        {
            // Regression: an expression-bodied void method whose body is `=> throw ...;`
            // must be converted to a block whose trailing statement is a ThrowStatement,
            // not an ExpressionStatement (which would be invalid C#).
            var test = @"
using Lucene.Net.Analysis;
using System;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset() => throw new NotSupportedException();
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using Lucene.Net.Analysis;
using System;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            base.Reset();
            throw new NotSupportedException();
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        [Test]
        public void TestExpressionBodiedWithBaseCall_NoDiagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset() => base.Reset();
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestNewMethodNotOverride_NoDiagnostic()
        {
            // A new method named Reset that's not an override is not the contract method.
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public void Reset(int x)
        {
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestTokenFilterMissingBaseCall_Diagnostic()
        {
            // Indirect inheritance via TokenFilter must still trigger.
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenFilter
    {
        public TypeName(TokenStream input) : base(input) { }

        public override bool IncrementToken() => false;

        public override void Reset()
        {
            // missing base.Reset()
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestBaseCallInsideLambda_Diagnostic()
        {
            // A base call inside an uninvoked lambda does not satisfy the contract.
            var test = @"
using Lucene.Net.Analysis;
using System;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            Action a = () => base.Reset();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestBaseCallInsideLocalFunction_Diagnostic()
        {
            // A base call inside an uninvoked local function does not satisfy the contract.
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;

        public override void Reset()
        {
            void Local() => base.Reset();
        }
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodCSAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call base.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}
