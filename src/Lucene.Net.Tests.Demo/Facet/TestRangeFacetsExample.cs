using Lucene.Net.Facet;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Demo.Facet
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

    [SuppressCodecs("Lucene3x")]
    public class TestRangeFacetsExample : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            using RangeFacetsExample example = new RangeFacetsExample();
            example.Index();
            FacetResult result = example.Search();
            assertEquals("dim=timestamp path=[] value=87 childCount=3\n  Past hour (4)\n  Past six hours (22)\n  Past day (87)\n", result.toString());
        }

        [Test]
        public void TestDrillDown()
        {
            using RangeFacetsExample example = new RangeFacetsExample();
            example.Index();
            TopDocs hits = example.DrillDown(example.PAST_SIX_HOURS);
            assertEquals(22, hits.TotalHits);
        }
    }
}
