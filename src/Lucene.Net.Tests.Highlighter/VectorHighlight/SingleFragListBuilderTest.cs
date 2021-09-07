using Lucene.Net.Index;
using NUnit.Framework;
using System;
using System.Globalization;

namespace Lucene.Net.Search.VectorHighlight
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

    public class SingleFragListBuilderTest : AbstractTestCase
    {
        [Test]
        public void TestNullFieldFragList()
        {
            SingleFragListBuilder sflb = new SingleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "b c d"), 100);
            assertEquals(0, ffl.FragInfos.size());
        }

        [Test]
        public void TestShortFieldFragList()
        {
            SingleFragListBuilder sflb = new SingleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b c d"), 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1)))/1.0(0,2147483647)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        [Test]
        public void TestLongFieldFragList()
        {
            SingleFragListBuilder sflb = new SingleFragListBuilder();
            FieldFragList ffl = sflb.CreateFieldFragList(fpl(new TermQuery(new Term(F, "a")), "a b c d", "a b c d e f g h i", "j k l m n o p q r s t u v w x y z a b c", "d e f g"), 100);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals("subInfos=(a((0,1))a((8,9))a((60,61)))/3.0(0,2147483647)", ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
        }

        private FieldPhraseList fpl(Query query, params String[] indexValues)
        {
            make1dmfIndex(indexValues);
            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            return new FieldPhraseList(stack, fq);
        }
    }
}
