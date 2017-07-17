using Lucene.Net.Facet;
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
    public class TestExpressionAggregationFacetsExample : LuceneTestCase
    {
        [Test]
        public void TestSimple()
        {
            FacetResult result = new ExpressionAggregationFacetsExample().RunSearch();
            //assertEquals("dim=A path=[] value=3.9681187 childCount=2\n  B (2.236068)\n  C (1.7320508)\n", result.toString());
            // LUCENENET TODO: string output is not quite the same as in Java, but it is close enough not to be considered a bug
            assertEquals("dim=A path=[] value=3.968119 childCount=2\n  B (2.236068)\n  C (1.732051)\n", result.toString());
        }
    }
}
