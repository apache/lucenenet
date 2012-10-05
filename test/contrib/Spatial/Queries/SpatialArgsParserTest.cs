/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test.Queries
{
	public class SpatialArgsParserTest
	{
		private readonly SpatialContext ctx = SpatialContext.GEO;

		//The args parser is only dependent on the ctx for IO so I don't care to test
		// with other implementations.

		[Test]
		public void TestArgParser()
		{
			SpatialArgsParser parser = new SpatialArgsParser();

			String arg = SpatialOperation.IsWithin + "(-10 -20 10 20)";
			SpatialArgs outValue = parser.Parse(arg, ctx);
			Assert.AreEqual(SpatialOperation.IsWithin, outValue.Operation);
			Rectangle bounds = (Rectangle)outValue.Shape;
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
