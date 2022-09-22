using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
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

    public class RobustnessTest : StrategyTestCase
    {
        [Test]
        public virtual void QuadTreeRobustness()
        {
            SpatialContextFactory test_factory = new SpatialContextFactory();
            test_factory.IsGeo = false;
            test_factory.WorldBounds = new Rectangle(0.0, 700000.0, 0.0, 650000.0, null);

            SpatialContext ctx = new SpatialContext(test_factory);
            SpatialPrefixTree grid;
            SpatialStrategy strategy;

            grid = new QuadPrefixTree(ctx, 40);
            strategy = new RecursivePrefixTreeStrategy(grid, "recursive_quad");

            this.ctx = strategy.SpatialContext;
            this.strategy = strategy;

            adoc("0", ctx.MakePoint(144502.06, 639062.07));
        }

    }
}