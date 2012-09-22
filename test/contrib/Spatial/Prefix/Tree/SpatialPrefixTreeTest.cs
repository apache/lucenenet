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

using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Contrib.Spatial.Test.Prefix.Tree
{
	public class SpatialPrefixTreeTest : LuceneTestCase
	{
		//TODO plug in others and test them
		private SpatialContext ctx;
		private SpatialPrefixTree trie;

		[SetUp]
		  public override void SetUp()
		{
			base.SetUp();
			ctx = SpatialContext.GEO;
			trie = new GeohashPrefixTree(ctx, 4);
		}

		[Test]
		public void testNodeTraverse()
		{
			Node prevN = null;
			Node n = trie.GetWorldNode();
			Assert.AreEqual(0, n.GetLevel());
			Assert.AreEqual(ctx.GetWorldBounds(), n.GetShape());
			while (n.GetLevel() < trie.GetMaxLevels())
			{
				prevN = n;
				var it = n.GetSubCells().GetEnumerator();
				it.MoveNext();
				n = it.Current; //TODO random which one?

				Assert.AreEqual(prevN.GetLevel() + 1, n.GetLevel());
				Rectangle prevNShape = (Rectangle) prevN.GetShape();
				Shape s = n.GetShape();
				Rectangle sbox = s.GetBoundingBox();
				Assert.IsTrue(prevNShape.GetWidth() > sbox.GetWidth());
				Assert.IsTrue(prevNShape.GetHeight() > sbox.GetHeight());
			}
		}
	}
}
