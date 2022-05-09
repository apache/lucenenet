using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Spatial
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

    public class TestTestFramework : LuceneTestCase
    {
        [Test]
        public virtual void TestQueries()
        {
            String name = StrategyTestCase.RESOURCE_PATH + StrategyTestCase.QTEST_Cities_Intersects_BBox;

            Stream @in = GetType().getResourceAsStream(name);
            SpatialContext ctx = SpatialContext.Geo;
            IEnumerator<SpatialTestQuery> iter = SpatialTestQuery.GetTestQueries(
                new SpatialArgsParser(), ctx, name, @in);//closes the InputStream
            IList<SpatialTestQuery> tests = new JCG.List<SpatialTestQuery>();
            while (iter.MoveNext())
            {
                tests.Add(iter.Current);
            }
            assertEquals(3, tests.size());

            SpatialTestQuery sf = tests[0];
            // assert
            assertEquals(1, sf.ids.size());
            assertTrue(sf.ids[0].Equals("G5391959", StringComparison.Ordinal));
            assertTrue(sf.args.Shape is IRectangle);
            assertEquals(SpatialOperation.Intersects, sf.args.Operation);
        }

        // LUCENENET specific - we don't nee to worry about the naming convention
        // because the [Test] attribute will cause the test to run regardless. 
        // So, this is a duplicate test to SpatialExample.Test() and not needed.

        //[Test]
        //public virtual void SpatialExample_Mem()
        //{
        //    //kind of a hack so that SpatialExample is tested despite
        //    // it not starting or ending with "Test".
        //    SpatialExample.Main(null);
        //}
    }
}
