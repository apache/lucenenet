using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Support;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Context.Nts;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
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

    public class NtsPolygonTest : StrategyTestCase
    {
        private static readonly double LUCENE_4464_distErrPct = SpatialArgs.DEFAULT_DISTERRPCT;//DEFAULT 2.5%

        public NtsPolygonTest()
        {
            try
            {
                IDictionary<string, string> args = new Dictionary<string, string>
                {
                    ["SpatialContextFactory"] = typeof(NtsSpatialContextFactory).FullName
                };
                ctx = SpatialContextFactory.MakeSpatialContext(args, GetType().Assembly);
            }
            catch (Exception e) when (e.IsNoClassDefFoundError())
            {
                AssumeTrue("This test requires Spatial4n: " + e, false);
            }

            GeohashPrefixTree grid = new GeohashPrefixTree(ctx, 11);//< 1 meter == 11 maxLevels
            this.strategy = new RecursivePrefixTreeStrategy(grid, GetType().Name);
            ((RecursivePrefixTreeStrategy)this.strategy).DistErrPct = (LUCENE_4464_distErrPct);//1% radius (small!)
        }

        [Test]
        /** LUCENE-4464 */
        public virtual void TestCloseButNoMatch()
        {
            getAddAndVerifyIndexedDocuments("LUCENE-4464.txt");
            SpatialArgs args = q(
                "POLYGON((-93.18100824442227 45.25676372469945," +
                    "-93.23182001200654 45.21421290799412," +
                    "-93.16315546122038 45.23742639412364," +
                    "-93.18100824442227 45.25676372469945))",
                LUCENE_4464_distErrPct);
            SearchResults got = executeQuery(strategy.MakeQuery(args), 100);
            assertEquals(1, got.numFound);
            assertEquals("poly2", got.results[0].document.Get("id"));
            //did not find poly 1 !
        }

        private SpatialArgs q(String shapeStr, double distErrPct)
        {
            IShape shape = ctx.ReadShapeFromWkt(shapeStr);
            SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects, shape);
            args.DistErrPct = (distErrPct);
            return args;
        }

        /**
         * A PrefixTree pruning optimization gone bad.
         * See <a href="https://issues.apache.org/jira/browse/LUCENE-4770>LUCENE-4770</a>.
         */
        [Test]
        public virtual void TestBadPrefixTreePrune()
        {

            IShape area = ctx.ReadShapeFromWkt("POLYGON((-122.83 48.57, -122.77 48.56, -122.79 48.53, -122.83 48.57))");

            SpatialPrefixTree trie = new QuadPrefixTree(ctx, 12);
            TermQueryPrefixTreeStrategy strategy = new TermQueryPrefixTreeStrategy(trie, "geo");
            Document doc = new Document();
            doc.Add(new TextField("id", "1", Field.Store.YES));

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
