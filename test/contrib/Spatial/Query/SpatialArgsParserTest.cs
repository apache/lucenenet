using System;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Query;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test.Query
{
	public class SpatialArgsParserTest
	{
		private readonly SpatialContext ctx = SpatialContext.GEO_KM;

		//The args parser is only dependent on the ctx for IO so I don't care to test
		// with other implementations.

		[Test]
		public void TestArgParser()
		{
			SpatialArgsParser parser = new SpatialArgsParser();

			String arg = SpatialOperation.IsWithin + "(-10 -20 10 20)";
			SpatialArgs outValue = parser.Parse(arg, ctx);
			Assert.AreEqual(SpatialOperation.IsWithin, outValue.Operation);
			Rectangle bounds = (Rectangle)outValue.GetShape();
			Assert.AreEqual(-10.0, bounds.GetMinX(), 0D);
			Assert.AreEqual(10.0, bounds.GetMaxX(), 0D);

			// Disjoint should not be scored
			arg = SpatialOperation.IsDisjointTo + " (-10 10 -20 20)";
			outValue = parser.Parse(arg, ctx);
			Assert.AreEqual(SpatialOperation.IsDisjointTo, outValue.Operation);

			try
			{
				parser.Parse(SpatialOperation.IsDisjointTo + "[ ]", ctx);
				Assert.True(false, "spatial operations need args");
			}
			catch (Exception)
			{
				//expected
			}

			try
			{
				parser.Parse("XXXX(-10 10 -20 20)", ctx);
				Assert.True(false, "unknown operation!");
			}
			catch (Exception)
			{
				//expected
			}
		}
	}
}
