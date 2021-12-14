using Lucene.Net.Facet;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;

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
    public class TestAssociationsFacetsExample : LuceneTestCase
    {
        [Test]
        public void TestExamples()
        {
            IList<FacetResult> res = new AssociationsFacetsExample().RunSumAssociations();
            assertEquals("Wrong number of results", 2, res.Count);
            assertEquals("dim=tags path=[] value=-1 childCount=2\n  lucene (4)\n  solr (2)\n", res[0].ToString(CultureInfo.InvariantCulture));
            assertEquals("dim=genre path=[] value=-1.0 childCount=2\n  computing (1.62)\n  software (0.34)\n", res[1].ToString(CultureInfo.InvariantCulture));
        }

        [Test]
        public void TestDrillDown()
        {
            FacetResult result = new AssociationsFacetsExample().RunDrillDown();
            assertEquals("dim=genre path=[] value=-1.0 childCount=2\n  computing (0.75)\n  software (0.34)\n", result.ToString(CultureInfo.InvariantCulture));
        }
    }
}
