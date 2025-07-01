using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;
using System;
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

    public class TestLucene1000_SealIncrementTokenMethodVBCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            return new Lucene1000_SealIncrementTokenMethodVBCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            return new Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedVBAnalyzer();
        }


        //No diagnostics expected to show up
        [Test]
        public void TestEmptyFile()
        {
            var test = @"";

            VerifyBasicDiagnostic(test);
        }


        //Diagnostic and CodeFix both triggered and checked for
        [Test]
        public void TestDiagnosticAndCodeFix()
        {
            var test = @"
Imports Lucene.Net.Analysis
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Diagnostics

Namespace MyNamespace
    Class TypeName
        Inherits TokenStream

        Public Overrides Function IncrementToken() As Boolean
            Throw New NotImplementedException()
        End Function

    End Class
End Namespace";
            var expected = new DiagnosticResult
            {
                Id = Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedVBAnalyzer.DiagnosticId,
                Message = String.Format("Type name '{0}' must be marked NotInheritable or its IncrementToken() method must be marked NotOverridable.", "TypeName"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                                    new DiagnosticResultLocation("Test0.vb", 11, 5)
                        }
            };

            VerifyBasicDiagnostic(test, expected);

            var fixtest = @"
Imports Lucene.Net.Analysis
Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
Imports System.Diagnostics

Namespace MyNamespace
    Class TypeName
        Inherits TokenStream

        Public NotOverridable Overrides Function IncrementToken() As Boolean
            Throw New NotImplementedException()
        End Function

    End Class
End Namespace";
            VerifyBasicFix(test, fixtest);
        }
    }
}
