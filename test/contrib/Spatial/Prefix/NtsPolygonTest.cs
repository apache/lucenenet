using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Support;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Context.Nts;
using Spatial4n.Core.Io.Samples;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test.Prefix
{
	public class NtsPolygonTest : StrategyTestCase
	{
		private static readonly double LUCENE_4464_distErrPct = SpatialArgs.DEFAULT_DISTERRPCT;//DEFAULT 2.5%

		public NtsPolygonTest()
		{
			//var args = new HashMap<String, String> {{"spatialContextFactory", "com.spatial4j.core.context.jts.JtsSpatialContextFactory"}};
			//SpatialContextFactory.MakeSpatialContext(args, getClass().getClassLoader());
			ctx = new NtsSpatialContext(true);

			var grid = new GeohashPrefixTree(ctx, 11);//< 1 meter == 11 maxLevels
			this.strategy = new RecursivePrefixTreeStrategy(grid, GetType().Name);
			((RecursivePrefixTreeStrategy)this.strategy).DistErrPct = LUCENE_4464_distErrPct;//1% radius (small!)
		}

		[Test]
		public void testLineStrings()
		{
			var sdIterator = new List<SampleData>
                                              {
                                                  new SampleData("LineB\tLineB\tLINESTRING (0 1, 1 1, 2 1)"),
                                                  new SampleData("LineC\tLineC\tLINESTRING (0 1, 0.8 0.8, 2 1)"),
                                                  new SampleData("LineD\tLineD\tLINESTRING (0 1, 0.5 1, 2 1)")
                                              };

			ctx = NtsSpatialContext.GEO;
			List<Document> testDocuments = getDocuments(sdIterator.GetEnumerator());
			addDocumentsAndCommit(testDocuments);
			verifyDocumentsIndexed(testDocuments.Count);

			var lineA = ctx.ReadShape("LINESTRING (0 0, 1 1, 1 2)");
			var args = new SpatialArgs(SpatialOperation.Intersects, lineA);
			var got = executeQuery(strategy.MakeQuery(args), 100);
			assertEquals(3, got.numFound); //found all 3
		}
	}
}
