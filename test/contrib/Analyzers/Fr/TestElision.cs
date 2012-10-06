/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Fr;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Fr
{
    /*
     * 
     */
    [TestFixture]
    public class TestElision : BaseTokenStreamTestCase
    {
        [Test]
        public void TestElision2()
        {
            String test = "Plop, juste pour voir l'embrouille avec O'brian. M'enfin.";
            Tokenizer tokenizer = new StandardTokenizer(Version.LUCENE_CURRENT, new StringReader(test));
            HashSet<String> articles = new HashSet<String>();
            articles.Add("l");
            articles.Add("M");
            TokenFilter filter = new ElisionFilter(tokenizer, articles);
            List<string> tas = Filtre(filter);
            Assert.AreEqual("embrouille", tas[4]);
            Assert.AreEqual("O'brian", tas[6]);
            Assert.AreEqual("enfin", tas[7]);
        }

        private List<string> Filtre(TokenFilter filter)
        {
            List<string> tas = new List<string>();
            ITermAttribute termAtt = filter.GetAttribute<ITermAttribute>();
            while (filter.IncrementToken())
            {
                tas.Add(termAtt.Term);
            }
            return tas;
        }
    }
}
