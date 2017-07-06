using Lucene.Net.Facet;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;

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

    // We require sorted set DVs:
    [SuppressCodecs("Lucene40", "Lucene41", "Appending", "Lucene3x")]
    public class TestSimpleSortedSetFacetsExample : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            IList<FacetResult> results = new SimpleSortedSetFacetsExample().RunSearch();
            assertEquals(2, results.size());
            assertEquals("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Frank (1)\n  Susan (1)\n", results[0].toString());
            assertEquals("dim=Publish Year path=[] value=5 childCount=3\n  2010 (2)\n  2012 (2)\n  1999 (1)\n", results[1].toString());
        }

        [Test]
        public void TestDrillDown()
        {
            FacetResult result = new SimpleSortedSetFacetsExample().RunDrillDown();
            assertEquals("dim=Author path=[] value=2 childCount=2\n  Bob (1)\n  Lisa (1)\n", result.toString());
        }
    }
}
