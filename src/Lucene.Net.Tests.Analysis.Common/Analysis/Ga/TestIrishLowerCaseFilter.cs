// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Ga
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

    /// <summary>
    /// Test the Irish lowercase filter.
    /// </summary>
    public class TestIrishLowerCaseFilter : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Test lowercase
        /// </summary>
        [Test]
        public virtual void TestIrishLowerCaseFilter_()
        {
            TokenStream stream = new MockTokenizer(new StringReader("nAthair tUISCE hARD"), MockTokenizer.WHITESPACE, false);
            IrishLowerCaseFilter filter = new IrishLowerCaseFilter(stream);
            AssertTokenStreamContents(filter, new string[] { "n-athair", "t-uisce", "hard" });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new IrishLowerCaseFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}