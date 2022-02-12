using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Serialized;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
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

    public class DistanceStrategyTest : StrategyTestCase
    {
        //@ParametersFactory
        public static IList<Object[]> Parameters()
        {
            IList<Object[]> ctorArgs = new JCG.List<object[]>();

            SpatialContext ctx = SpatialContext.Geo;
            SpatialPrefixTree grid;
            SpatialStrategy strategy;

            grid = new QuadPrefixTree(ctx, 25);
            strategy = new RecursivePrefixTreeStrategy(grid, "recursive_quad");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            grid = new GeohashPrefixTree(ctx, 12);
            strategy = new TermQueryPrefixTreeStrategy(grid, "termquery_geohash");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            strategy = new PointVectorStrategy(ctx, "pointvector");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            strategy = new SerializedDVStrategy(ctx, "serialized");
            ctorArgs.Add(new Object[] { new Param(strategy) });

            return ctorArgs;
        }

        // this is a hack for clover!
        public class Param
        {
            internal SpatialStrategy strategy;

            internal Param(SpatialStrategy strategy)
            {
                this.strategy = strategy;
            }


            public override String ToString()
            {
                return strategy.FieldName;
            }
        }

        //  private String fieldName;

        //public DistanceStrategyTest(Param param)
        //{
        //    SpatialStrategy strategy = param.strategy;
        //    this.ctx = strategy.SpatialContext;
        //    this.strategy = strategy;
        //}

        public override void SetUp()
        {
            SpatialStrategy strategy = ((Param)(RandomPicks.RandomFrom(Random, Parameters()))[0]).strategy;
            this.ctx = strategy.SpatialContext;
            this.strategy = strategy;
            base.SetUp();
        }


        protected override bool NeedsDocValues()
        {
            return (strategy is SerializedDVStrategy);
        }

        [Test]
        public virtual void TestDistanceOrder()
        {
            adoc("100", ctx.MakePoint(2, 1));
            adoc("101", ctx.MakePoint(-1, 4));
            adoc("103", (IShape)null);//test score for nothing
            adoc("999", ctx.MakePoint(2, 1));//test deleted
            Commit();
            DeleteDoc("999");
            Commit();
            //FYI distances are in docid order
            checkDistValueSource(ctx.MakePoint(4, 3), 2.8274937f, 5.0898066f, 180f);
            checkDistValueSource(ctx.MakePoint(0, 4), 3.6043684f, 0.9975641f, 180f);
        }

        [Test]
        public virtual void TestRecipScore()
        {
            IPoint p100 = ctx.MakePoint(2, 1);
            adoc("100", p100);
            IPoint p101 = ctx.MakePoint(-1, 4);
            adoc("101", p101);
            adoc("103", (IShape)null);//test score for nothing
            adoc("999", ctx.MakePoint(2, 1));//test deleted
            Commit();
            DeleteDoc("999");
            Commit();

            double dist = ctx.DistanceCalculator.Distance(p100, p101);
            IShape queryShape = ctx.MakeCircle(2.01, 0.99, dist);
            CheckValueSource(strategy.MakeRecipDistanceValueSource(queryShape),
            new float[] { 1.00f, 0.10f, 0f }, 0.09f);
        }

        // @Override
        // protected Document newDoc(String id, Shape shape) {
        //   //called by adoc().  Make compatible with BBoxStrategy.
        //   if (shape != null && strategy instanceof BBoxStrategy)
        //     shape = ctx.makeRectangle(shape.getCenter(), shape.getCenter());
        //   return super.newDoc(id, shape);
        // }

        internal void checkDistValueSource(IPoint pt, params float[] distances)
        {
            float multiplier = (float)Random.NextDouble() * 100f;
            float[]
            dists2 = Arrays.CopyOf(distances, distances.Length);
            for (int i = 0; i < dists2.Length; i++)
            {
                dists2[i] *= multiplier;
            }
            CheckValueSource(strategy.MakeDistanceValueSource(pt, multiplier), dists2, 1.0e-3f);
        }

    }
}
