// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Ckb
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
    /// Tests normalization for Sorani (this is more critical than stemming...)
    /// </summary>
    public class TestSoraniNormalizationFilter : BaseTokenStreamTestCase
    {
        internal static readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new KeywordTokenizer(reader);
            return new TokenStreamComponents(tokenizer, new SoraniNormalizationFilter(tokenizer));
        });

        [Test]
        public virtual void TestY()
        {
            CheckOneTerm(a, "\u064A", "\u06CC");
            CheckOneTerm(a, "\u0649", "\u06CC");
            CheckOneTerm(a, "\u06CC", "\u06CC");
        }

        [Test]
        public virtual void TestK()
        {
            CheckOneTerm(a, "\u0643", "\u06A9");
            CheckOneTerm(a, "\u06A9", "\u06A9");
        }

        [Test]
        public virtual void TestH()
        {
            // initial
            CheckOneTerm(a, "\u0647\u200C", "\u06D5");
            // medial
            CheckOneTerm(a, "\u0647\u200C\u06A9", "\u06D5\u06A9");

            CheckOneTerm(a, "\u06BE", "\u0647");
            CheckOneTerm(a, "\u0629", "\u06D5");
        }

        [Test]
        public virtual void TestFinalH()
        {
            // always (and in final form by def), so frequently omitted
            CheckOneTerm(a, "\u0647\u0647\u0647", "\u0647\u0647\u06D5");
        }

        [Test]
        public virtual void TestRR()
        {
            CheckOneTerm(a, "\u0692", "\u0695");
        }

        [Test]
        public virtual void TestInitialRR()
        {
            // always, so frequently omitted
            CheckOneTerm(a, "\u0631\u0631\u0631", "\u0695\u0631\u0631");
        }

        [Test]
        public virtual void TestRemove()
        {
            CheckOneTerm(a, "\u0640", "");
            CheckOneTerm(a, "\u064B", "");
            CheckOneTerm(a, "\u064C", "");
            CheckOneTerm(a, "\u064D", "");
            CheckOneTerm(a, "\u064E", "");
            CheckOneTerm(a, "\u064F", "");
            CheckOneTerm(a, "\u0650", "");
            CheckOneTerm(a, "\u0651", "");
            CheckOneTerm(a, "\u0652", "");
            // we peek backwards in this case to look for h+200C, ensure this works
            CheckOneTerm(a, "\u200C", "");
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            CheckOneTerm(a, "", "");
        }
    }
}