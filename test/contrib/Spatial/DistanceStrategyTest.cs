using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.BBox;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Vector;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test
{
    public class DistanceStrategyTest : StrategyTestCase
    {
        public class TestValuesProvider
        {
            public IEnumerable<Param> ParamsProvider()
            {
                var ctorArgs = new List<Param>();

                SpatialContext ctx = SpatialContext.GEO;
                SpatialPrefixTree grid;
                SpatialStrategy strategy;

                grid = new QuadPrefixTree(ctx, 25);
                strategy = new RecursivePrefixTreeStrategy(grid, "recursive_quad");
                ctorArgs.Add(new Param(strategy));

                grid = new GeohashPrefixTree(ctx, 12);
                strategy = new TermQueryPrefixTreeStrategy(grid, "termquery_geohash");
                ctorArgs.Add(new Param(strategy));

                strategy = new TwoDoublesStrategy(ctx, "twodoubles");
                ctorArgs.Add(new Param(strategy));

                strategy = new BBoxStrategy(ctx, "bbox");
                ctorArgs.Add(new Param(strategy));

                return ctorArgs;
            }
        }

        public class Param
        {
            public readonly SpatialStrategy strategy;

            public Param(SpatialStrategy strategy) { this.strategy = strategy; }

            public override String ToString()
            {
                return strategy.GetFieldName();
            }
        }

        //  private String fieldName;

        public void Init(Param param)
        {
            SpatialStrategy strategy = param.strategy;
            this.ctx = strategy.GetSpatialContext();
            this.strategy = strategy;
        }

        [Test]
        public void testDistanceOrder([ValueSource(typeof(TestValuesProvider), "ParamsProvider")] Param p)
        {
            Init(p);

            adoc("100", ctx.MakePoint(2, 1));
            adoc("101", ctx.MakePoint(-1, 4));
            adoc("103", (Shape)null);//test score for nothing
            commit();
            //FYI distances are in docid order
            checkDistValueSource("3,4", 2.8274937f, 5.0898066f, 180f);
            checkDistValueSource("4,0", 3.6043684f, 0.9975641f, 180f);
        }

        [Test]
        public void testRecipScore([ValueSource(typeof(TestValuesProvider), "ParamsProvider")] Param p)
        {
            Init(p);

            Point p100 = ctx.MakePoint(2, 1);
            adoc("100", p100);
            Point p101 = ctx.MakePoint(-1, 4);
            adoc("101", p101);
            adoc("103", (Shape)null); //test score for nothing
            commit();

            double dist = ctx.GetDistCalc().Distance(p100, p101);
            Shape queryShape = ctx.MakeCircle(2.01, 0.99, dist);
            checkValueSource(strategy.MakeRecipDistanceValueSource(queryShape),
                             new float[] { 1.00f, 0.10f, 0f }, 0.09f);
        }

        protected override Document newDoc(String id, Shape shape)
        {
            //called by adoc().  Make compatible with BBoxStrategy.
            if (shape != null && strategy is BBoxStrategy)
                shape = ctx.MakeRectangle(shape.GetCenter(), shape.GetCenter());
            return base.newDoc(id, shape);
        }

        void checkDistValueSource(String ptStr, params float[] distances)
        {
            Point pt = (Point)ctx.ReadShape(ptStr);
            checkValueSource(strategy.MakeDistanceValueSource(pt), distances, 1.0e-4f);
        }
    }
}
