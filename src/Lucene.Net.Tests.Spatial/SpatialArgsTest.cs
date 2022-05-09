using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;

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

    public class SpatialArgsTest : LuceneTestCase
    {
        [Test]
        public void CalcDistanceFromErrPct()
        {
            SpatialContext ctx = SpatialContext.Geo;
            double DEP = 0.5;//distErrPct

            //the result is the diagonal distance from the center to the closest corner,
            // times distErrPct

            IShape superwide = ctx.MakeRectangle(-180, 180, 0, 0);

            // LUCENENET specific: Added delta to the first 3 asserts because it is not a 
            // valid expectation that they are exactly on the nose when dealing with floating point
            // types. And in .NET Core 2.0, the implementation has changed which now makes this test
            // fail without delta.

            //0 distErrPct means 0 distance always
            assertEquals(0, SpatialArgs.CalcDistanceFromErrPct(superwide, 0, ctx), 0.0001);
            assertEquals(180 * DEP, SpatialArgs.CalcDistanceFromErrPct(superwide, DEP, ctx), 0.0001);

            IShape supertall = ctx.MakeRectangle(0, 0, -90, 90);
            assertEquals(90 * DEP, SpatialArgs.CalcDistanceFromErrPct(supertall, DEP, ctx), 0.0001);

            IShape upperhalf = ctx.MakeRectangle(-180, 180, 0, 90);
            assertEquals(45 * DEP, SpatialArgs.CalcDistanceFromErrPct(upperhalf, DEP, ctx), 0.0001);

            IShape midCircle = ctx.MakeCircle(0, 0, 45);
            assertEquals(60 * DEP, SpatialArgs.CalcDistanceFromErrPct(midCircle, DEP, ctx), 0.0001);
        }
    }
}
