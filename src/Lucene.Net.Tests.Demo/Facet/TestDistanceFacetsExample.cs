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
    public class TestDistanceFacetsExample : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            using DistanceFacetsExample example = new DistanceFacetsExample();
            example.Index();
            FacetResult result = example.Search();
            assertEquals("dim=field path=[] value=3 childCount=4\n  < 1 km (1)\n  < 2 km (2)\n  < 5 km (2)\n  < 10 km (3)\n", result.toString());
        }

        [Test]
        public void TestDrillDown()
        {
            using DistanceFacetsExample example = new DistanceFacetsExample();
            example.Index();
            TopDocs hits = example.DrillDown(DistanceFacetsExample.FIVE_KM);
            assertEquals(2, hits.TotalHits);
        }
    }
}
