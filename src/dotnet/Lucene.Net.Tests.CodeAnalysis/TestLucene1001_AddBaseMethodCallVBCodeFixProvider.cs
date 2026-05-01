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

    public class TestLucene1001_AddBaseMethodCallVBCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new Lucene1001_AddBaseMethodCallVBCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new Lucene1001_TokenStreamOverrideMustCallBaseMethodVBAnalyzer();
        }

        [Test]
        public void TestEmptyFile()
        {
            VerifyBasicDiagnostic(@"");
        }

        [Test]
        public void TestResetWithoutBaseCall_DiagnosticAndFix()
        {
            var test = @"
Imports Lucene.Net.Analysis

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits TokenStream

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub Reset()
            Dim x As Integer = 1
        End Sub
    End Class
End Namespace";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodVBAnalyzer.DiagnosticId,
                Message = "Override of 'Reset()' on type 'TypeName' must call MyBase.Reset().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 12, 30) }
            };

            VerifyBasicDiagnostic(test, expected);

            var fixtest = @"
Imports Lucene.Net.Analysis

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits TokenStream

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub Reset()
            MyBase.Reset()
            Dim x As Integer = 1
        End Sub
    End Class
End Namespace";
            VerifyBasicFix(test, fixtest);
        }

        [Test]
        public void TestResetWithBaseCall_NoDiagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits TokenStream

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub Reset()
            MyBase.Reset()
        End Sub
    End Class
End Namespace";
            VerifyBasicDiagnostic(test);
        }

        [Test]
        public void TestEndWithoutBaseCall_Diagnostic()
        {
            var test = @"
Imports Lucene.Net.Analysis

Namespace MyNamespace
    NotInheritable Class TypeName
        Inherits TokenStream

        Public Overrides Function IncrementToken() As Boolean
            Return False
        End Function

        Public Overrides Sub [End]()
            ClearAttributes()
        End Sub
    End Class
End Namespace";
            var expected = new DiagnosticResult
            {
                Id = Lucene1001_TokenStreamOverrideMustCallBaseMethodVBAnalyzer.DiagnosticId,
                Message = "Override of 'End()' on type 'TypeName' must call MyBase.End().",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.vb", 12, 30) }
            };

            VerifyBasicDiagnostic(test, expected);
        }
    }
}
