// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Fr;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Util
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


    public class TestElision : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestElision_()
        {
            string test = "Plop, juste pour voir l'embrouille avec O'brian. M'enfin.";
            Tokenizer tokenizer = new StandardTokenizer(TEST_VERSION_CURRENT, new StringReader(test));
            CharArraySet articles = new CharArraySet(TEST_VERSION_CURRENT, AsSet("l", "M"), false);
            TokenFilter filter = new ElisionFilter(tokenizer, articles);
            IList<string> tas = Filter(filter);
            assertEquals("embrouille", tas[4]);
            assertEquals("O'brian", tas[6]);
            assertEquals("enfin", tas[7]);
        }

        private IList<string> Filter(TokenFilter filter)
        {
            IList<string> tas = new JCG.List<string>();
            ICharTermAttribute termAtt = filter.GetAttribute<ICharTermAttribute>();
            filter.Reset();
            while (filter.IncrementToken())
            {
                tas.Add(termAtt.ToString());
            }
            filter.End();
            filter.Dispose();
            return tas;
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new ElisionFilter(tokenizer, FrenchAnalyzer.DEFAULT_ARTICLES));
            });
            CheckOneTerm(a, "", "");
        }
    }
}