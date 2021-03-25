// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Standard;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Gl
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
    /// Simple tests for <seealso cref="GalicianStemFilter"/>
    /// </summary>
    public class TestGalicianStemFilter : BaseTokenStreamTestCase
    {
        private static readonly Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer source = new StandardTokenizer(TEST_VERSION_CURRENT, reader);
            TokenStream result = new LowerCaseFilter(TEST_VERSION_CURRENT, source);
            return new TokenStreamComponents(source, new GalicianStemFilter(result));
        });

        /// <summary>
        /// Test against a vocabulary from the reference impl </summary>
        [Test]
        public virtual void TestVocabulary()
        {
            VocabularyAssert.AssertVocabulary(analyzer, GetDataFile("gltestdata.zip"), "gl.txt");
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new GalicianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}