using Lucene.Net.Search;
using NUnit.Framework;
using Spatial4n.Context;

namespace Lucene.Net.Spatial.Serialized
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

    public class SerializedStrategyTest : StrategyTestCase
    {
        public override void SetUp()
        {
            base.SetUp();
            this.ctx = SpatialContext.Geo;
            this.strategy = new SerializedDVStrategy(ctx, "serialized");
        }


        protected override bool NeedsDocValues()
        {
            return true;
        }

        //called by StrategyTestCase; we can't let it call our makeQuery which will UOE ex.
        protected override Query makeQuery(SpatialTestQuery q)
        {
            return new FilteredQuery(new MatchAllDocsQuery(), strategy.MakeFilter(q.args),
                FilteredQuery.QUERY_FIRST_FILTER_STRATEGY);
        }

        [Test]
        public virtual void TestBasicOperaions()
        {
            getAddAndVerifyIndexedDocuments(DATA_SIMPLE_BBOX);

            executeQueries(SpatialMatchConcern.EXACT, QTEST_Simple_Queries_BBox);
        }

        [Test]
        public virtual void TestStatesBBox()
        {
            getAddAndVerifyIndexedDocuments(DATA_STATES_BBOX);

            executeQueries(SpatialMatchConcern.FILTER, QTEST_States_IsWithin_BBox);
            executeQueries(SpatialMatchConcern.FILTER, QTEST_States_Intersects_BBox);
        }

        [Test]
        public virtual void TestCitiesIntersectsBBox()
        {
            getAddAndVerifyIndexedDocuments(DATA_WORLD_CITIES_POINTS);

            executeQueries(SpatialMatchConcern.FILTER, QTEST_Cities_Intersects_BBox);
        }

        //sorting is tested in DistanceStrategyTest
    }
}
