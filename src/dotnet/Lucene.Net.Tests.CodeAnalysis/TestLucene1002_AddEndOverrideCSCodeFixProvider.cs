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

    public class TestLucene1002_AddEndOverrideCSCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new Lucene1002_AddEndOverrideCSCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Lucene1002_TokenizerMustOverrideEndCSAnalyzer();
        }

        [Test]
        public void TestEmptyFile()
        {
            VerifyCSharpDiagnostic(@"");
        }

        [Test]
        public void TestTokenizerMissingEndOverride_Diagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    sealed class TypeName : Tokenizer
    {
        public TypeName(TextReader input) : base(input) { }

        public override bool IncrementToken() => false;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1002_TokenizerMustOverrideEndCSAnalyzer.DiagnosticId,
                Message = "Tokenizer subclass 'TypeName' must override End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 18) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Test]
        public void TestTokenizerWithEndOverride_NoDiagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    sealed class TypeName : Tokenizer
    {
        public TypeName(TextReader input) : base(input) { }

        public override bool IncrementToken() => false;

        public override void End()
        {
            base.End();
        }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestAbstractTokenizer_NoDiagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    abstract class FooTokenizer : Tokenizer
    {
        protected FooTokenizer(TextReader input) : base(input) { }
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestTokenFilter_NoDiagnostic()
        {
            // TokenFilter is out of scope — only Tokenizer is required to override End().
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenFilter
    {
        public TypeName(TokenStream input) : base(input) { }

        public override bool IncrementToken() => false;
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestPlainTokenStream_NoDiagnostic()
        {
            // Plain TokenStream subclasses are out of scope.
            var test = @"
using Lucene.Net.Analysis;

namespace MyNamespace
{
    sealed class TypeName : TokenStream
    {
        public override bool IncrementToken() => false;
    }
}";
            VerifyCSharpDiagnostic(test);
        }

        [Test]
        public void TestTokenizerMissingEndOverride_CodeFix()
        {
            var test = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    sealed class TypeName : Tokenizer
    {
        public TypeName(TextReader input) : base(input) { }

        public override bool IncrementToken() => false;
    }
}";
            var fixtest = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    sealed class TypeName : Tokenizer
    {
        public TypeName(TextReader input) : base(input) { }

        public override bool IncrementToken() => false;

        public override void End()
        {
            base.End();
            // TODO: set the final offset and finish up other end-of-stream attributes
        }
    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        [Test]
        public void TestIndirectTokenizerInheritance_Diagnostic()
        {
            var test = @"
using Lucene.Net.Analysis;
using System.IO;

namespace MyNamespace
{
    abstract class FooTokenizer : Tokenizer
    {
        protected FooTokenizer(TextReader input) : base(input) { }
    }

    sealed class TypeName : FooTokenizer
    {
        public TypeName(TextReader input) : base(input) { }

        public override bool IncrementToken() => false;
    }
}";
            var expected = new DiagnosticResult
            {
                Id = Lucene1002_TokenizerMustOverrideEndCSAnalyzer.DiagnosticId,
                Message = "Tokenizer subclass 'TypeName' must override End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 18) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }
    }
}
