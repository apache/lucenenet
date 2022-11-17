using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Support;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

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

    public class SpatialOpRecursivePrefixTreeTest : StrategyTestCase
    {
        const int ITERATIONS = 1;//Test Iterations

        private SpatialPrefixTree grid;

        private SpatialContext ctx2D;

        public override void SetUp()
        {
            base.SetUp();
            DeleteAll();
        }

        public virtual void SetupGrid(int maxLevels)
        {
            if (Random.nextBoolean())
                SetupQuadGrid(maxLevels);
            else
                SetupGeohashGrid(maxLevels);
            SetupCtx2D(ctx);
            //((PrefixTreeStrategy) strategy).setDistErrPct(0);//fully precise to grid

            Console.WriteLine("Strategy: " + strategy.toString());
        }

        private void SetupCtx2D(SpatialContext ctx)
        {
            if (!ctx.IsGeo)
                ctx2D = ctx;
            //A non-geo version of ctx.
            SpatialContextFactory ctxFactory = new SpatialContextFactory
            {
                IsGeo = false,
                WorldBounds = ctx.WorldBounds
            };
            ctx2D = ctxFactory.CreateSpatialContext();
        }

        private void SetupQuadGrid(int maxLevels)
        {
            //non-geospatial makes this test a little easier (in gridSnap), and using boundary values 2^X raises
            // the prospect of edge conditions we want to test, plus makes for simpler numbers (no decimals).
            SpatialContextFactory factory = new SpatialContextFactory
            {
                IsGeo = false,
                WorldBounds = new Rectangle(0, 256, -128, 128, null)
            };
            this.ctx = factory.CreateSpatialContext();
            //A fairly shallow grid, and default 2.5% distErrPct
            if (maxLevels == -1)
                maxLevels = randomIntBetween(1, 8);//max 64k cells (4^8), also 256*256
            this.grid = new QuadPrefixTree(ctx, maxLevels);
            this.strategy = new RecursivePrefixTreeStrategy(grid, GetType().Name);
        }

        public virtual void SetupGeohashGrid(int maxLevels)
        {
            this.ctx = SpatialContext.Geo;
            //A fairly shallow grid, and default 2.5% distErrPct
            if (maxLevels == -1)
                maxLevels = randomIntBetween(1, 3);//max 16k cells (32^3)
            this.grid = new GeohashPrefixTree(ctx, maxLevels);
            this.strategy = new RecursivePrefixTreeStrategy(grid, GetType().Name);
        }

        [Test, Repeat(ITERATIONS)]
        public virtual void TestIntersects()
        {
            SetupGrid(-1);
            doTest(SpatialOperation.Intersects);
        }

        [Test, Repeat(ITERATIONS)]
        public virtual void TestWithin()
        {
            SetupGrid(-1);
            doTest(SpatialOperation.IsWithin);
        }

        [Test , Repeat(ITERATIONS)]
        public virtual void TestContains()
        {
            SetupGrid(-1);
            doTest(SpatialOperation.Contains);
        }

        [Test, Repeat(ITERATIONS)]
        public virtual void TestDisjoint()
        {
            SetupGrid(-1);
            doTest(SpatialOperation.IsDisjointTo);
        }

        /** See LUCENE-5062, <see cref="ContainsPrefixTreeFilter.m_multiOverlappingIndexedShapes"/>. */
        [Test, Repeat(ITERATIONS)]
        public virtual void TestContainsPairOverlap()
        {
            SetupQuadGrid(3);
            adoc("0", new ShapePair(ctx.MakeRectangle(0, 33, -128, 128), ctx.MakeRectangle(33, 128, -128, 128), true, ctx, ctx2D));
            Commit();
            Query query = strategy.MakeQuery(new SpatialArgs(SpatialOperation.Contains,
                ctx.MakeRectangle(0, 128, -16, 128)));
            SearchResults searchResults = executeQuery(query, 1);
            assertEquals(1, searchResults.numFound);
        }

        [Test]
        public virtual void TestWithinDisjointParts()
        {
            SetupQuadGrid(7);
            //one shape comprised of two parts, quite separated apart
            adoc("0", new ShapePair(ctx.MakeRectangle(0, 10, -120, -100), ctx.MakeRectangle(220, 240, 110, 125), false, ctx, ctx2D));
            Commit();
            //query surrounds only the second part of the indexed shape
            Query query = strategy.MakeQuery(new SpatialArgs(SpatialOperation.IsWithin,
                ctx.MakeRectangle(210, 245, 105, 128)));
            SearchResults searchResults = executeQuery(query, 1);
            //we shouldn't find it because it's not completely within
            assertTrue(searchResults.numFound == 0);
        }

        [Test] /** LUCENE-4916 */
        public virtual void TestWithinLeafApproxRule()
        {
            SetupQuadGrid(2);//4x4 grid
                             //indexed shape will simplify to entire right half (2 top cells)
            adoc("0", ctx.MakeRectangle(192, 204, -128, 128));
            Commit();

            ((RecursivePrefixTreeStrategy)strategy).PrefixGridScanLevel = (Random.nextInt(2 + 1));

            //query does NOT contain it; both indexed cells are leaves to the query, and
            // when expanded to the full grid cells, the top one's top row is disjoint
            // from the query and thus not a match.
            assertTrue(executeQuery(strategy.MakeQuery(
                new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeRectangle(38, 192, -72, 56))
            ), 1).numFound == 0);//no-match

            //this time the rect is a little bigger and is considered a match. It's a
            // an acceptable false-positive because of the grid approximation.
            assertTrue(executeQuery(strategy.MakeQuery(
                new SpatialArgs(SpatialOperation.IsWithin, ctx.MakeRectangle(38, 192, -72, 80))
            ), 1).numFound == 1);//match
        }

        [Test]
        public void TestShapePair()
        {
            ctx = SpatialContext.Geo;
            SetupCtx2D(ctx);

            IShape leftShape = new ShapePair(ctx.MakeRectangle(-74, -56, -8, 1), ctx.MakeRectangle(-180, 134, -90, 90), true, ctx, ctx2D);
            IShape queryShape = ctx.MakeRectangle(-180, 180, -90, 90);
            assertEquals(SpatialRelation.Within, leftShape.Relate(queryShape));
        }

        //Override so we can index parts of a pair separately, resulting in the detailLevel
        // being independent for each shape vs the whole thing
        protected override Document newDoc(String id, IShape shape)
        {
            Document doc = new Document();
            doc.Add(new StringField("id", id, Field.Store.YES));
            if (shape != null)
            {
                IList<IShape> shapes;
                if (shape is ShapePair)
                {
                    shapes = new JCG.List<IShape>(2);
                    shapes.Add(((ShapePair)shape).shape1);
                    shapes.Add(((ShapePair)shape).shape2);
                }
                else
                {
                    shapes = new JCG.List<IShape>(new IShape[] { shape });//Collections.Singleton(shape);
                }
                foreach (IShape shapei in shapes)
                {
                    foreach (Field f in strategy.CreateIndexableFields(shapei))
                    {
                        doc.Add(f);
                    }
                }
                if (storeShape)//just for diagnostics
                    doc.Add(new StoredField(strategy.FieldName, shape.toString()));
            }
            return doc;
        }

        private void doTest(SpatialOperation operation)
        {
            //first show that when there's no data, a query will result in no results
            {
                Query query = strategy.MakeQuery(new SpatialArgs(operation, randomRectangle()));
                SearchResults searchResults = executeQuery(query, 1);
                assertEquals(0, searchResults.numFound);
            }

            bool biasContains = (operation == SpatialOperation.Contains);

            //Main index loop:
            IDictionary<String, IShape> indexedShapes = new JCG.LinkedDictionary<String, IShape>();
            IDictionary<String, IShape> indexedShapesGS = new JCG.LinkedDictionary<String, IShape>();//grid snapped
            int numIndexedShapes = randomIntBetween(1, 6);
#pragma warning disable 219
            bool indexedAtLeastOneShapePair = false;
#pragma warning restore 219
            for (int i = 0; i < numIndexedShapes; i++)
            {
                String id = "" + i;
                IShape indexedShape;
                int R = Random.nextInt(12);
                if (R == 0)
                {//1 in 12
                    indexedShape = null;
                }
                else if (R == 1)
                {//1 in 12
                    indexedShape = randomPoint();//just one point
                }
                else if (R <= 4)
                {//3 in 12
                 //comprised of more than one shape
                    indexedShape = randomShapePairRect(biasContains);
                    indexedAtLeastOneShapePair = true;
                }
                else
                {
                    indexedShape = randomRectangle();//just one rect
                }

                indexedShapes[id] = indexedShape;
                indexedShapesGS[id] = gridSnap(indexedShape);

                adoc(id, indexedShape);

                if (Random.nextInt(10) == 0)
                    Commit();//intermediate commit, produces extra segments

            }
            //delete some documents randomly
            IEnumerator<String> idIter = indexedShapes.Keys.ToList().GetEnumerator();
            while (idIter.MoveNext())
            {
                String id = idIter.Current;
                if (Random.nextInt(10) == 0)
                {
                    DeleteDoc(id);
                    //idIter.Remove();
                    indexedShapes.Remove(id);
                    indexedShapesGS.Remove(id);
                }
            }

            Commit();

            //Main query loop:
            int numQueryShapes = AtLeast(20);
            for (int i = 0; i < numQueryShapes; i++)
            {
                int scanLevel = randomInt(grid.MaxLevels);
                ((RecursivePrefixTreeStrategy)strategy).PrefixGridScanLevel = (scanLevel);

                IShape queryShape;
                switch (randomInt(10))
                {
                    case 0: queryShape = randomPoint(); break;
                    // LUCENE-5549
                    //TODO debug: -Dtests.method=testWithin -Dtests.multiplier=3 -Dtests.seed=5F5294CE2E075A3E:AAD2F0F79288CA64
                    //        case 1:case 2:case 3:
                    //          if (!indexedAtLeastOneShapePair) { // avoids ShapePair.relate(ShapePair), which isn't reliable
                    //            queryShape = randomShapePairRect(!biasContains);//invert biasContains for query side
                    //            break;
                    //          }
                    default: queryShape = randomRectangle(); break;
                }
                IShape queryShapeGS = gridSnap(queryShape);

                bool opIsDisjoint = operation == SpatialOperation.IsDisjointTo;

                //Generate truth via brute force:
                // We ensure true-positive matches (if the predicate on the raw shapes match
                //  then the search should find those same matches).
                // approximations, false-positive matches
                ISet<string> expectedIds = new JCG.LinkedHashSet<string>();//true-positives
                ISet<string> secondaryIds = new JCG.LinkedHashSet<string>();//false-positives (unless disjoint)
                foreach (var entry in indexedShapes)
                {
                    string id = entry.Key;
                    IShape indexedShapeCompare = entry.Value;
                    if (indexedShapeCompare is null)
                        continue;
                    IShape queryShapeCompare = queryShape;

                    if (operation.Evaluate(indexedShapeCompare, queryShapeCompare))
                    {
                        expectedIds.Add(id);
                        if (opIsDisjoint)
                        {
                            //if no longer intersect after buffering them, for disjoint, remember this
                            indexedShapeCompare = indexedShapesGS[id];
                            queryShapeCompare = queryShapeGS;
                            if (!operation.Evaluate(indexedShapeCompare, queryShapeCompare))
                                secondaryIds.Add(id);
                        }
                    }
                    else if (!opIsDisjoint)
                    {
                        //buffer either the indexed or query shape (via gridSnap) and try again
                        if (operation == SpatialOperation.Intersects)
                        {
                            indexedShapeCompare = indexedShapesGS[id];
                            queryShapeCompare = queryShapeGS;
                            //TODO Unfortunately, grid-snapping both can result in intersections that otherwise
                            // wouldn't happen when the grids are adjacent. Not a big deal but our test is just a
                            // bit more lenient.
                        }
                        else if (operation == SpatialOperation.Contains)
                        {
                            indexedShapeCompare = indexedShapesGS[id];
                        }
                        else if (operation == SpatialOperation.IsWithin)
                        {
                            queryShapeCompare = queryShapeGS;
                        }
                        if (operation.Evaluate(indexedShapeCompare, queryShapeCompare))
                            secondaryIds.Add(id);
                    }
                }

                //Search and verify results
                SpatialArgs args = new SpatialArgs(operation, queryShape);
                if (queryShape is ShapePair)
                    args.DistErrPct = (0.0);//a hack; we want to be more detailed than gridSnap(queryShape)
                Query query = strategy.MakeQuery(args);
                SearchResults got = executeQuery(query, 100);
                ISet<String> remainingExpectedIds = new JCG.LinkedHashSet<string>(expectedIds);
                foreach (SearchResult result in got.results)
                {
                    String id = result.GetId();
                    bool removed = remainingExpectedIds.Remove(id);
                    if (!removed && (!opIsDisjoint && !secondaryIds.Contains(id)))
                    {
                        fail("Shouldn't match", id, indexedShapes, indexedShapesGS, queryShape);
                    }
                }
                if (opIsDisjoint)
                    remainingExpectedIds.ExceptWith(secondaryIds);
                if (remainingExpectedIds.Count > 0)
                {
                    var iter = remainingExpectedIds.GetEnumerator();
                    iter.MoveNext();
                    String id = iter.Current;
                    fail("Should have matched", id, indexedShapes, indexedShapesGS, queryShape);
                }
            }
        }

        private IShape randomShapePairRect(bool biasContains)
        {
            IRectangle shape1 = randomRectangle();
            IRectangle shape2 = randomRectangle();
            return new ShapePair(shape1, shape2, biasContains, ctx, ctx2D);
        }

        private void fail(String label, String id, IDictionary<String, IShape> indexedShapes, IDictionary<String, IShape> indexedShapesGS, IShape queryShape)
        {
            Console.WriteLine("Ig:" + indexedShapesGS[id] + " Qg:" + gridSnap(queryShape));
            fail(label + " I#" + id + ":" + indexedShapes[id] + " Q:" + queryShape);
        }

        //  private Rectangle inset(Rectangle r) {
        //    //typically inset by 1 (whole numbers are easy to read)
        //    double d = Math.min(1.0, grid.getDistanceForLevel(grid.getMaxLevels()) / 4);
        //    return ctx.makeRectangle(r.getMinX() + d, r.getMaxX() - d, r.getMinY() + d, r.getMaxY() - d);
        //  }

        protected IShape gridSnap(IShape snapMe)
        {
            if (snapMe is null)
                return null;
            if (snapMe is ShapePair)
            {
                ShapePair me = (ShapePair)snapMe;
                return new ShapePair(gridSnap(me.shape1), gridSnap(me.shape2), me.biasContainsThenWithin, ctx, ctx2D);
            }
            if (snapMe is IPoint)
            {
                snapMe = snapMe.BoundingBox;
            }
            //The next 4 lines mimic PrefixTreeStrategy.createIndexableFields()
            double distErrPct = ((PrefixTreeStrategy)strategy).DistErrPct;
            double distErr = SpatialArgs.CalcDistanceFromErrPct(snapMe, distErrPct, ctx);
            int detailLevel = grid.GetLevelForDistance(distErr);
            IList<Cell> cells = grid.GetCells(snapMe, detailLevel, false, true);

            //calc bounding box of cells.
            IList<IShape> cellShapes = new JCG.List<IShape>(cells.size());
            foreach (Cell cell in cells)
            {
                cellShapes.Add(cell.Shape);
            }
            return new ShapeCollection(cellShapes, ctx).BoundingBox;
        }

        /**
         * An aggregate of 2 shapes. Unfortunately we can't simply use a ShapeCollection because:
         * (a) ambiguity between CONTAINS & WITHIN for equal shapes, and
         * (b) adjacent pairs could as a whole contain the input shape.
         * The tests here are sensitive to these matters, although in practice ShapeCollection
         * is fine.
         */
        private class ShapePair : ShapeCollection /*<Shape>*/
        {

            private readonly SpatialContext ctx;
            internal readonly IShape shape1, shape2;
            internal readonly IShape shape1_2D, shape2_2D;//not geo (bit of a hack)
            internal bool biasContainsThenWithin;//a hack
            private readonly SpatialContext ctx2D;

            public ShapePair(IShape shape1, IShape shape2, bool containsThenWithin, SpatialContext ctx, SpatialContext ctx2D)
                        : base(new JCG.List<IShape> { shape1, shape2 }, ctx)
            {
                this.ctx = ctx;
                this.ctx2D = ctx2D;

                this.shape1 = shape1;
                this.shape2 = shape2;
                this.shape1_2D = ToNonGeo(shape1);
                this.shape2_2D = ToNonGeo(shape2);
                biasContainsThenWithin = containsThenWithin;
            }

            private IShape ToNonGeo(IShape shape)
            {
                if (!ctx.IsGeo)
                    return shape;//already non-geo
                if (shape is IRectangle) {
                    Rectangle rect = (Rectangle)shape;
                    if (rect.CrossesDateLine)
                    {
                        return new ShapePair(
                            ctx2D.MakeRectangle(rect.MinX, 180, rect.MinY, rect.MaxY),
                            ctx2D.MakeRectangle(-180, rect.MaxX, rect.MinY, rect.MaxY),
                            biasContainsThenWithin, ctx, ctx2D);
                    }
                    else
                    {
                        return ctx2D.MakeRectangle(rect.MinX, rect.MaxX, rect.MinY, rect.MaxY);
                    }
                }
                //no need to do others; this addresses the -180/+180 ambiguity corner test problem
                return shape;
            }

            public override SpatialRelation Relate(IShape other)
            {
                SpatialRelation r = RelateApprox(other);
                if (r == SpatialRelation.Disjoint)
                    return r;
                if (r == SpatialRelation.Contains)
                    return r;
                if (r == SpatialRelation.Within && !biasContainsThenWithin)
                    return r;

                //See if the correct answer is actually Contains, when the indexed shapes are adjacent,
                // creating a larger shape that contains the input shape.
                bool pairTouches = shape1.Relate(shape2).Intersects();
                if (!pairTouches)
                    return r;
                // LUCENENET: From commit: https://github.com/apache/lucene/commit/e9906a334b8e123e93b917c3feb6e55fed0a8c57
                //test all 4 corners
                // Note: awkwardly, we use a non-geo context for this because in geo, -180 & +180 are the same place, which means
                //  that "other" might wrap the world horizontally and yet all its corners could be in shape1 (or shape2) even
                //  though shape1 is only adjacent to the dateline. I couldn't think of a better way to handle this.
                IRectangle oRect = (IRectangle)other;
                if (CornerContainsNonGeo(oRect.MinX, oRect.MinY)
                    && CornerContainsNonGeo(oRect.MinX, oRect.MaxY)
                    && CornerContainsNonGeo(oRect.MaxX, oRect.MinY)
                    && CornerContainsNonGeo(oRect.MaxX, oRect.MaxY))
                    return SpatialRelation.Contains;
                return r;
            }

            private bool CornerContainsNonGeo(double x, double y)
            {
                IShape pt = ctx2D.MakePoint(x, y);
                return shape1_2D.Relate(pt).Intersects() || shape2_2D.Relate(pt).Intersects();
            }

            private SpatialRelation RelateApprox(IShape other)
            {
                if (biasContainsThenWithin)
                {
                    if (shape1.Relate(other) == SpatialRelation.Contains || shape1.equals(other)
                        || shape2.Relate(other) == SpatialRelation.Contains || shape2.equals(other)) return SpatialRelation.Contains;

                    if (shape1.Relate(other) == SpatialRelation.Within && shape2.Relate(other) == SpatialRelation.Within) return SpatialRelation.Within;

                }
                else
                {
                    if ((shape1.Relate(other) == SpatialRelation.Within || shape1.equals(other))
                        && (shape2.Relate(other) == SpatialRelation.Within || shape2.equals(other))) return SpatialRelation.Within;

                    if (shape1.Relate(other) == SpatialRelation.Contains || shape2.Relate(other) == SpatialRelation.Contains) return SpatialRelation.Contains;
                }

                if (shape1.Relate(other).Intersects() || shape2.Relate(other).Intersects())
                    return SpatialRelation.Intersects;//might actually be 'CONTAINS' if the pair are adjacent but we handle that later
                return SpatialRelation.Disjoint;
            }

            public override String ToString()
            {
                return "ShapePair(" + shape1 + " , " + shape2 + ")";
            }
        }
    }
}
