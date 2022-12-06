using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Serialized;
using Lucene.Net.Spatial.Vector;
using Lucene.Net.Util;
using NUnit.Framework;
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

    public class QueryEqualsHashCodeTest : LuceneTestCase
    {
        private readonly SpatialContext ctx = SpatialContext.Geo;

        [Test]
        public virtual void TestEqualsHashCode()
        {

            SpatialPrefixTree gridQuad = new QuadPrefixTree(ctx, 10);
            SpatialPrefixTree gridGeohash = new GeohashPrefixTree(ctx, 10);

            IList<SpatialStrategy> strategies = new JCG.List<SpatialStrategy>();
            strategies.Add(new RecursivePrefixTreeStrategy(gridGeohash, "recursive_geohash"));
            strategies.Add(new TermQueryPrefixTreeStrategy(gridQuad, "termquery_quad"));
            strategies.Add(new PointVectorStrategy(ctx, "pointvector"));
            //strategies.Add(new BBoxStrategy(ctx, "bbox"));
            strategies.Add(new SerializedDVStrategy(ctx, "serialized"));
            foreach (SpatialStrategy strategy in strategies)
            {
                TestEqualsHashcode(strategy);
            }
        }

        private sealed class ObjGeneratorQueryAnonymousClass : ObjGenerator
        {
            private readonly SpatialStrategy strategy;

            public ObjGeneratorQueryAnonymousClass(SpatialStrategy strategy)
            {
                this.strategy = strategy;
            }

            public object gen(SpatialArgs args)
            {
                return strategy.MakeQuery(args);
            }
        }

        private sealed class ObjGeneratorFilterAnonymousClass : ObjGenerator
        {
            private readonly SpatialStrategy strategy;

            public ObjGeneratorFilterAnonymousClass(SpatialStrategy strategy)
            {
                this.strategy = strategy;
            }

            public object gen(SpatialArgs args)
            {
                return strategy.MakeFilter(args);
            }
        }

        private sealed class ObjGeneratorDistanceValueSourceAnonymousClass : ObjGenerator
        {
            private readonly SpatialStrategy strategy;

            public ObjGeneratorDistanceValueSourceAnonymousClass(SpatialStrategy strategy)
            {
                this.strategy = strategy;
            }

            public object gen(SpatialArgs args)
            {
                return strategy.MakeDistanceValueSource(args.Shape.Center);
            }
        }

        private void TestEqualsHashcode(SpatialStrategy strategy)
        {
            SpatialArgs args1 = MakeArgs1();
            SpatialArgs args2 = MakeArgs2();
            TestEqualsHashcode(args1, args2, new ObjGeneratorQueryAnonymousClass(strategy));
            TestEqualsHashcode(args1, args2, new ObjGeneratorFilterAnonymousClass(strategy));
            TestEqualsHashcode(args1, args2, new ObjGeneratorDistanceValueSourceAnonymousClass(strategy));
        }

        private void TestEqualsHashcode(SpatialArgs args1, SpatialArgs args2, ObjGenerator generator)
        {
            Object first;
            try
            {
                first = generator.gen(args1);
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                return;
            }
            if (first is null)
                return;//unsupported op?
            Object second = generator.gen(args1);//should be the same
            assertEquals(first, second);
            assertEquals(first.GetHashCode(), second.GetHashCode());
            second = generator.gen(args2);//now should be different
            assertNotSame(args1, args2);
        }

        private SpatialArgs MakeArgs1()
        {
            IShape shape1 = ctx.MakeRectangle(0, 0, 10, 10);
            return new SpatialArgs(SpatialOperation.Intersects, shape1);
        }

        private SpatialArgs MakeArgs2()
        {
            IShape shape2 = ctx.MakeRectangle(0, 0, 20, 20);
            return new SpatialArgs(SpatialOperation.Intersects, shape2);
        }

        interface ObjGenerator
        {
            Object gen(SpatialArgs args);
        }
    }
}
