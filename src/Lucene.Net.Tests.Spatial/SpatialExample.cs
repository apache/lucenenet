using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Distance;
using Spatial4n.Shapes;
using System;
using System.Globalization;

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

    /// <summary>
    /// This class serves as example code to show how to use the Lucene spatial
    /// module.
    /// </summary>
    public class SpatialExample : LuceneTestCase
    {
        // LUCENENET specific: removed this because the test will run
        // with only an attribute, it doesn't need to worry about a convention
        ////Note: Test invoked via TestTestFramework.spatialExample()
        //public static void Main(String[] args)
        //{
        //    new SpatialExample().Test();
        //}

        [Test]
        public virtual void Test()
        {
            Init();
            IndexPoints();
            Search();
        }

        /**
         * The Spatial4j <see cref="SpatialContext"/> is a sort of global-ish singleton
         * needed by Lucene spatial.  It's a facade to the rest of Spatial4j, acting
         * as a factory for <see cref="IShape"/>s and provides access to reading and writing
         * them from Strings.
         */
        private SpatialContext ctx;//"ctx" is the conventional variable name

        /**
         * The Lucene spatial <see cref="SpatialStrategy"/> encapsulates an approach to
         * indexing and searching shapes, and providing distance values for them.
         * It's a simple API to unify different approaches. You might use more than
         * one strategy for a shape as each strategy has its strengths and weaknesses.
         * <p />
         * Note that these are initialized with a field name.
         */
        private SpatialStrategy strategy;

        private Directory directory;

        protected void Init()
        {
            //Typical geospatial context
            //  These can also be constructed from SpatialContextFactory
            this.ctx = SpatialContext.Geo;

            int maxLevels = 11;//results in sub-meter precision for geohash
                               //TODO demo lookup by detail distance
                               //  This can also be constructed from SpatialPrefixTreeFactory
            SpatialPrefixTree grid = new GeohashPrefixTree(ctx, maxLevels);

            this.strategy = new RecursivePrefixTreeStrategy(grid, "myGeoField");

            this.directory = new RAMDirectory();
        }

        private void IndexPoints()
        {
            IndexWriterConfig iwConfig = new IndexWriterConfig(TEST_VERSION_CURRENT, null);
            IndexWriter indexWriter = new IndexWriter(directory, iwConfig);

            //Spatial4j is x-y order for arguments
            indexWriter.AddDocument(NewSampleDocument(
                2, ctx.MakePoint(-80.93, 33.77)));

            //Spatial4j has a WKT parser which is also "x y" order
            indexWriter.AddDocument(NewSampleDocument(
                4, ctx.ReadShapeFromWkt("POINT(60.9289094 -50.7693246)")));

            indexWriter.AddDocument(NewSampleDocument(
                20, ctx.MakePoint(0.1, 0.1), ctx.MakePoint(0, 0)));

            indexWriter.Dispose();
        }

        private Document NewSampleDocument(int id, params IShape[] shapes)
        {
            Document doc = new Document();
            doc.Add(new Int32Field("id", id, Field.Store.YES));
            //Potentially more than one shape in this field is supported by some
            // strategies; see the javadocs of the SpatialStrategy impl to see.
            foreach (IShape shape in shapes)
            {
                foreach (IIndexableField f in strategy.CreateIndexableFields(shape))
                {
                    doc.Add(f);
                }
                //store it too; the format is up to you
                //  (assume point in this example)
                IPoint pt = (IPoint)shape;
                doc.Add(new StoredField(strategy.FieldName, J2N.Numerics.Double.ToString(pt.X, CultureInfo.InvariantCulture) + " " + J2N.Numerics.Double.ToString(pt.Y, CultureInfo.InvariantCulture)));
            }

            return doc;
        }

        private void Search()
        {
            IndexReader indexReader = DirectoryReader.Open(directory);
            IndexSearcher indexSearcher = new IndexSearcher(indexReader);
            Sort idSort = new Sort(new SortField("id", SortFieldType.INT32));

            //--Filter by circle (<= distance from a point)
            {
                //Search with circle
                //note: SpatialArgs can be parsed from a string
                SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects,
                    ctx.MakeCircle(-80.0, 33.0, DistanceUtils.Dist2Degrees(200, DistanceUtils.EarthMeanRadiusKilometers)));
                Filter filter = strategy.MakeFilter(args);
                TopDocs docs = indexSearcher.Search(new MatchAllDocsQuery(), filter, 10, idSort);
                AssertDocMatchedIds(indexSearcher, docs, 2);
                //Now, lets get the distance for the 1st doc via computing from stored point value:
                // (this computation is usually not redundant)
                Document doc1 = indexSearcher.Doc(docs.ScoreDocs[0].Doc);
                String doc1Str = doc1.GetField(strategy.FieldName).GetStringValue();
                //assume doc1Str is "x y" as written in newSampleDocument()
                int spaceIdx = doc1Str.IndexOf(' ');
                double x = double.Parse(doc1Str.Substring(0, spaceIdx - 0), CultureInfo.InvariantCulture);
                double y = double.Parse(doc1Str.Substring(spaceIdx + 1), CultureInfo.InvariantCulture);
                double doc1DistDEG = ctx.CalcDistance(args.Shape.Center, x, y);
                assertEquals(121.6d, DistanceUtils.Degrees2Dist(doc1DistDEG, DistanceUtils.EarthMeanRadiusKilometers), 0.1);
                //or more simply:
                assertEquals(121.6d, doc1DistDEG * DistanceUtils.DegreesToKilometers, 0.1);
            }
            //--Match all, order by distance ascending
            {
                IPoint pt = ctx.MakePoint(60, -50);
                ValueSource valueSource = strategy.MakeDistanceValueSource(pt, DistanceUtils.DegreesToKilometers);//the distance (in km)
                Sort distSort = new Sort(valueSource.GetSortField(false)).Rewrite(indexSearcher);//false=asc dist
                TopDocs docs = indexSearcher.Search(new MatchAllDocsQuery(), 10, distSort);
                AssertDocMatchedIds(indexSearcher, docs, 4, 20, 2);
                //To get the distance, we could compute from stored values like earlier.
                // However in this example we sorted on it, and the distance will get
                // computed redundantly.  If the distance is only needed for the top-X
                // search results then that's not a big deal. Alternatively, try wrapping
                // the ValueSource with CachingDoubleValueSource then retrieve the value
                // from the ValueSource now. See LUCENE-4541 for an example.
            }
            //demo arg parsing
            {
                SpatialArgs args = new SpatialArgs(SpatialOperation.Intersects,
                    ctx.MakeCircle(-80.0, 33.0, 1));
                SpatialArgs args2 = new SpatialArgsParser().Parse("Intersects(BUFFER(POINT(-80 33),1))", ctx);
                assertEquals(args.toString(), args2.toString());
            }

            indexReader.Dispose();
        }

        private void AssertDocMatchedIds(IndexSearcher indexSearcher, TopDocs docs, params int[] ids)
        {
            int[]
            gotIds = new int[docs.TotalHits];
            for (int i = 0; i < gotIds.Length; i++)
            {
                gotIds[i] = indexSearcher.Doc(docs.ScoreDocs[i].Doc).GetField("id").GetInt32Value().Value;
            }
            assertArrayEquals(ids, gotIds);
        }
    }
}
