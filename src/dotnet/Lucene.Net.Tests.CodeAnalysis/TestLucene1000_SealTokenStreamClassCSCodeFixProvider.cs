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

    public class TestLucene1000_SealTokenStreamClassCSCodeFixProvider : CodeFixVerifier
    {
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new Lucene1000_SealTokenStreamClassCSCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedCSAnalyzer();
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
                Id = Lucene1000_TokenStreamOrItsIncrementTokenMethodMustBeSealedCSAnalyzer.DiagnosticId,
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
