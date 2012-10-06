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
using Lucene.Net.Analysis.AR;
using Lucene.Net.Analysis.Fa;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Fa
{
    /*
     * Test the Arabic Normalization Filter
     * 
     */
    [TestFixture]
    public class TestPersianNormalizationFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestFarsiYeh()
        {
            Check("های", "هاي");
        }

        [Test]
        public void TestYehBarree()
        {
            Check("هاے", "هاي");
        }

        [Test]
        public void TestKeheh()
        {
            Check("کشاندن", "كشاندن");
        }

        [Test]
        public void TestHehYeh()
        {
            Check("كتابۀ", "كتابه");
        }

        [Test]
        public void TestHehHamzaAbove()
        {
            Check("كتابهٔ", "كتابه");
        }

        [Test]
        public void TestHehGoal()
        {
            Check("زادہ", "زاده");
        }

        private void Check(String input, String expected)
        {
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(
                new StringReader(input));
            PersianNormalizationFilter filter = new PersianNormalizationFilter(
                tokenStream);
            AssertTokenStreamContents(filter, new String[] { expected });
        }
    }
}
