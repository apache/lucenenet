// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Ar;
using Lucene.Net.Analysis.Core;
using NUnit.Framework;
using System.IO;

namespace Lucene.Net.Analysis.Fa
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
    /// Test the Persian Normalization Filter
    /// 
    /// </summary>
    public class TestPersianNormalizationFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestFarsiYeh()
        {
            Check("های", "هاي");
        }

        [Test]
        public virtual void TestYehBarree()
        {
            Check("هاے", "هاي");
        }

        [Test]
        public virtual void TestKeheh()
        {
            Check("کشاندن", "كشاندن");
        }

        [Test]
        public virtual void TestHehYeh()
        {
            Check("كتابۀ", "كتابه");
        }

        [Test]
        public virtual void TestHehHamzaAbove()
        {
            Check("كتابهٔ", "كتابه");
        }

        [Test]
        public virtual void TestHehGoal()
        {
            Check("زادہ", "زاده");
        }

        private void Check(string input, string expected)
        {
#pragma warning disable 612, 618
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(TEST_VERSION_CURRENT, new StringReader(input));
#pragma warning restore 612, 618
            PersianNormalizationFilter filter = new PersianNormalizationFilter(tokenStream);
            AssertTokenStreamContents(filter, new string[] { expected });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new PersianNormalizationFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}