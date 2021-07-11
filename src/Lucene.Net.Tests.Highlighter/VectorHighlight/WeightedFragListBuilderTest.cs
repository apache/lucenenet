using NUnit.Framework;
using System;
using System.Globalization;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
using WeightedFragInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo;

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

    public class WeightedFragListBuilderTest : AbstractTestCase
    {
        [Test]
        public void Test2WeightedFragList()
        {
            TestCase(pqF("the", "both"), 100,
                "subInfos=(theboth((195,203)))/0.8679108(149,249)",
                0.8679108);
        }

        [Test]
        public void Test2SubInfos()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("the", "both"), Occur.MUST);
            query.Add(tq("examples"), Occur.MUST);

            TestCase(query, 1000,
                "subInfos=(examples((19,27))examples((66,74))theboth((195,203)))/1.8411169(0,1000)",
                1.8411169);
        }

        private void TestCase(Query query, int fragCharSize, String expectedFragInfo,
            double expectedTotalSubInfoBoost)
        {
            makeIndexLongMV();

            FieldQuery fq = new FieldQuery(query, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            WeightedFragListBuilder wflb = new WeightedFragListBuilder();
            FieldFragList ffl = wflb.CreateFieldFragList(fpl, fragCharSize);
            assertEquals(1, ffl.FragInfos.size());
            assertEquals(expectedFragInfo, ffl.FragInfos[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware

            float totalSubInfoBoost = 0;
            foreach (WeightedFragInfo info in ffl.FragInfos)
            {
                foreach (SubInfo subInfo in info.SubInfos)
                {
                    totalSubInfoBoost += subInfo.Boost;
                }
            }
            assertEquals(expectedTotalSubInfoBoost, totalSubInfoBoost, .0000001);
        }
    }
}
