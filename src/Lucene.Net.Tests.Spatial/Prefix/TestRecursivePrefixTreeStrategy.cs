using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Distance;
using Spatial4n.Shapes;
using System.Collections.Generic;
using System.Globalization;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Spatial.Prefix
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

    public class TestRecursivePrefixTreeStrategy : StrategyTestCase
    {
        private int maxLength;

        //Tests should call this first.
        private void init(int maxLength)
        {
            this.maxLength = maxLength;
            this.ctx = SpatialContext.Geo;
            GeohashPrefixTree grid = new GeohashPrefixTree(ctx, maxLength);
            this.strategy = new RecursivePrefixTreeStrategy(grid, GetType().Name);
        }

        [Test]
        public virtual void TestFilterWithVariableScanLevel()
        {
            init(GeohashPrefixTree.MaxLevelsPossible);
            getAddAndVerifyIndexedDocuments(DATA_WORLD_CITIES_POINTS);

            //execute queries for each prefix grid scan level
            for (int i = 0; i <= maxLength; i++)
            {
                ((RecursivePrefixTreeStrategy)strategy).PrefixGridScanLevel = (i);
                executeQueries(SpatialMatchConcern.FILTER, QTEST_Cities_Intersects_BBox);
            }
        }

        [Test]
        public virtual void TestOneMeterPrecision()
        {
            init(GeohashPrefixTree.MaxLevelsPossible);
            GeohashPrefixTree grid = (GeohashPrefixTree)((RecursivePrefixTreeStrategy)strategy).Grid;
            //DWS: I know this to be true.  11 is needed for one meter
            double degrees = DistanceUtils.Dist2Degrees(0.001, DistanceUtils.EarthMeanRadiusKilometers);
            assertEquals(11, grid.GetLevelForDistance(degrees));
        }

        [Test]
        public virtual void TestPrecision()
        {
            init(GeohashPrefixTree.MaxLevelsPossible);

            IPoint iPt = ctx.MakePoint(2.8028712999999925, 48.3708044);//lon, lat
            AddDocument(newDoc("iPt", iPt));
            Commit();

            IPoint qPt = ctx.MakePoint(2.4632387000000335, 48.6003516);

            double KM2DEG = DistanceUtils.Dist2Degrees(1, DistanceUtils.EarthMeanRadiusKilometers);
            double DEG2KM = 1 / KM2DEG;

            double DIST = 35.75;//35.7499...
            assertEquals(DIST, ctx.DistanceCalculator.Distance(iPt, qPt) * DEG2KM, 0.001);

            //distErrPct will affect the query shape precision. The indexed precision
            // was set to nearly zilch via init(GeohashPrefixTree.getMaxLevelsPossible());
            double distErrPct = 0.025; //the suggested default, by the way
            double distMult = 1 + distErrPct;

            assertTrue(35.74 * distMult >= DIST);
            checkHits(q(qPt, 35.74 * KM2DEG, distErrPct), 1, null);

            assertTrue(30 * distMult < DIST);
            checkHits(q(qPt, 30 * KM2DEG, distErrPct), 0, null);

            assertTrue(33 * distMult < DIST);
            checkHits(q(qPt, 33 * KM2DEG, distErrPct), 0, null);

            assertTrue(34 * distMult < DIST);
            checkHits(q(qPt, 34 * KM2DEG, distErrPct), 0, null);
        }

        private SpatialArgs q(IPoint pt, double distDEG, double distErrPct)
        {
            IShape shape = ctx.MakeCircle(pt, distDEG);
            SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects, shape);
            args.DistErrPct = (distErrPct);
            return args;
        }

        private void checkHits(SpatialArgs args, int assertNumFound, int[] assertIds)
        {
            SearchResults got = executeQuery(strategy.MakeQuery(args), 100);
            assertEquals("" + args, assertNumFound, got.numFound);
            if (assertIds != null)
            {
                ISet<int> gotIds = new JCG.HashSet<int>();
                foreach (SearchResult result in got.results)
                {
                    gotIds.Add(int.Parse(result.document.Get("id"), CultureInfo.InvariantCulture));
                }
                foreach (int assertId in assertIds)
                {
                    assertTrue("has " + assertId, gotIds.Contains(assertId));
                }
            }
        }
    }
}
