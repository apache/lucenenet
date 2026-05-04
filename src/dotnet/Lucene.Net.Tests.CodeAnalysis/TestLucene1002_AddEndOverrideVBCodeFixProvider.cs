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

    public class TestLucene1002_AddEndOverrideVBCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new Lucene1002_AddEndOverrideVBCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new Lucene1002_TokenizerMustOverrideEndVBAnalyzer();
        }

        [Test]
        public void TestEmptyFile()
        {
            VerifyBasicDiagnostic(@"");
        }

        [Test]
        public void TestTokenizerMissingEndOverride_Diagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits Tokenizer

        Public Sub New(input As TextReader)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function
    End Class
End Namespace";
            var expected = new DiagnosticResult
            {
                Id = Lucene1002_TokenizerMustOverrideEndVBAnalyzer.DiagnosticId,
                Message = "Tokenizer subclass 'TypeName' must override End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 6, 26) }
            };

            VerifyBasicDiagnostic(test, expected);
        }

        [Test]
        public void TestTokenizerWithEndOverride_NoDiagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits Tokenizer

        Public Sub New(input As TextReader)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub [End]()
            MyBase.End()
        End Sub
    End Class
End Namespace";
            VerifyBasicDiagnostic(test);
        }

        [Test]
        public void TestAbstractTokenizer_NoDiagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    MustInherit Class FooTokenizer
        Inherits Tokenizer

        Protected Sub New(input As TextReader)
            MyBase.New(input)
        End Sub
    End Class
End Namespace";
            VerifyBasicDiagnostic(test);
        }

        [Test]
        public void TestTokenizerMissingEndOverride_CodeFix()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits Tokenizer

        Public Sub New(input As TextReader)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function
    End Class
End Namespace";
            var fixtest = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits Tokenizer

        Public Sub New(input As TextReader)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub End()
            MyBase.End()
            ' TODO: set the final offset and finish up other end-of-stream attributes
        End Sub
    End Class
End Namespace";
            VerifyBasicFix(test, fixtest);
        }

        [Test]
        public void TestIndirectTokenizerInheritance_Diagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System.IO

Namespace MyNamespace
    MustInherit Class FooTokenizer
        Inherits Tokenizer

        Protected Sub New(input As TextReader)
            MyBase.New(input)
        End Sub
    End Class

    NotInheritable Class TypeName
        Inherits FooTokenizer

        Public Sub New(input As TextReader)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function
    End Class
End Namespace";
            var expected = new DiagnosticResult
            {
                Id = Lucene1002_TokenizerMustOverrideEndVBAnalyzer.DiagnosticId,
                Message = "Tokenizer subclass 'TypeName' must override End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 14, 26) }
            };

            VerifyBasicDiagnostic(test, expected);
        }

        [Test]
        public void TestTokenFilter_NoDiagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits TokenFilter

        Public Sub New(input As TokenStream)
            MyBase.New(input)
        End Sub

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function
    End Class
End Namespace";
            VerifyBasicDiagnostic(test);
        }
    }
}
