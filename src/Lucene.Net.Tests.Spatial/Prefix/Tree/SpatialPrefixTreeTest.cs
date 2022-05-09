using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Spatial.Prefix.Tree
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

    public class SpatialPrefixTreeTest : SpatialTestCase
    {
        //TODO plug in others and test them
        new private SpatialContext ctx;
        private SpatialPrefixTree trie;

        public override void SetUp()
        {
            base.SetUp();
            ctx = SpatialContext.Geo;
        }

        [Test]
        public virtual void TestCellTraverse()
        {
            trie = new GeohashPrefixTree(ctx, 4);

            Cell prevC = null;
            Cell c = trie.WorldCell;
            assertEquals(0, c.Level);
            assertEquals(ctx.WorldBounds, c.Shape);
            while (c.Level < trie.MaxLevels)
            {
                prevC = c;

                var iter = c.GetSubCells().GetEnumerator();
                iter.MoveNext();
                c = iter.Current;
                //c = c.GetSubCells().GetEnumerator().next();//TODO random which one?

                assertEquals(prevC.Level + 1, c.Level);
                IRectangle prevNShape = (IRectangle)prevC.Shape;
                IShape s = c.Shape;
                IRectangle sbox = s.BoundingBox;
                assertTrue(prevNShape.Width > sbox.Width);
                assertTrue(prevNShape.Height > sbox.Height);
            }
        }
        /**
         * A PrefixTree pruning optimization gone bad.
         * See <a href="https://issues.apache.org/jira/browse/LUCENE-4770>LUCENE-4770</a>.
         */
        [Test]
        public virtual void TestBadPrefixTreePrune()
        {

            trie = new QuadPrefixTree(ctx, 12);
            TermQueryPrefixTreeStrategy strategy = new TermQueryPrefixTreeStrategy(trie, "geo");
            Document doc = new Document();
            doc.Add(new TextField("id", "1", Field.Store.YES));

            IShape area = ctx.MakeRectangle(-122.82, -122.78, 48.54, 48.56);

            Field[] fields = strategy.CreateIndexableFields(area, 0.025);
            foreach (Field field in fields)
            {
                doc.Add(field);
            }
            AddDocument(doc);

            IPoint upperleft = ctx.MakePoint(-122.88, 48.54);
            IPoint lowerright = ctx.MakePoint(-122.82, 48.62);

            Query query = strategy.MakeQuery(new SpatialArgs(SpatialOperation.Intersects, ctx.MakeRectangle(upperleft, lowerright)));

            Commit();

            TopDocs search = indexSearcher.Search(query, 10);
            ScoreDoc[] scoreDocs = search.ScoreDocs;
            foreach (ScoreDoc scoreDoc in scoreDocs)
            {
                Console.WriteLine(indexSearcher.Doc(scoreDoc.Doc));
            }

            assertEquals(1, search.TotalHits);
        }
    }
}
