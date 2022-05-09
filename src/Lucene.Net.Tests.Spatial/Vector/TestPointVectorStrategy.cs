﻿using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;

namespace Lucene.Net.Spatial.Vector
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

    public class TestPointVectorStrategy : StrategyTestCase
    {
        public override void SetUp()
        {
            base.SetUp();
            this.ctx = SpatialContext.Geo;
            this.strategy = new PointVectorStrategy(ctx, GetType().Name);
        }

        [Test]
        public virtual void TestCircleShapeSupport()
        {
            ICircle circle = ctx.MakeCircle(ctx.MakePoint(0, 0), 10);
            SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects, circle);
            Query query = this.strategy.MakeQuery(args);

            assertNotNull(query);
        }

        [Test]
        public virtual void TestInvalidQueryShape()
        {
            IPoint point = ctx.MakePoint(0, 0);
            SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects, point);
            Assert.Throws<NotSupportedException>(() => this.strategy.MakeQuery(args));
        }

        [Test]
        public virtual void TestCitiesIntersectsBBox()
        {
            getAddAndVerifyIndexedDocuments(DATA_WORLD_CITIES_POINTS);
            executeQueries(SpatialMatchConcern.FILTER, QTEST_Cities_Intersects_BBox);
        }
    }
}
