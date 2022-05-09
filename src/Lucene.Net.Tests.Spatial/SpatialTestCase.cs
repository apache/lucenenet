using Lucene.Net.Analysis;
using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;
using System.Collections.Generic;
using System.Text;
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

    /// <summary>
    /// A base test class for spatial lucene. It's mostly Lucene generic.
    /// </summary>
    public abstract class SpatialTestCase : LuceneTestCase
    {
        private DirectoryReader indexReader;
        protected RandomIndexWriter indexWriter;
        private Store.Directory directory;
        protected IndexSearcher indexSearcher;

        protected SpatialContext ctx;//subclass must initialize

        public override void SetUp()
        {
            base.SetUp();

            directory = NewDirectory();
            Random random = Random;
            indexWriter = new RandomIndexWriter(random, directory, newIndexWriterConfig(random));
            indexReader = indexWriter.GetReader();
            indexSearcher = NewSearcher(indexReader);
        }

        protected virtual IndexWriterConfig newIndexWriterConfig(Random random)
        {
            IndexWriterConfig indexWriterConfig = NewIndexWriterConfig(random, TEST_VERSION_CURRENT, new MockAnalyzer(random));
            //TODO can we randomly choose a doc-values supported format?
            if (NeedsDocValues())
                indexWriterConfig.SetCodec(TestUtil.AlwaysDocValuesFormat(new Lucene45DocValuesFormat())); ;
            return indexWriterConfig;
        }

        protected virtual bool NeedsDocValues()
        {
            return false;
        }

        public override void TearDown()
        {
            IOUtils.Dispose(indexWriter, indexReader, directory);
            base.TearDown();
        }

        // ================================================= Helper Methods ================================================

        protected virtual void AddDocument(Document doc)
        {
            indexWriter.AddDocument(doc);
        }

        protected virtual void addDocumentsAndCommit(IList<Document> documents)
        {
            foreach (Document document in documents)
            {
                indexWriter.AddDocument(document);
            }
            Commit();
        }

        protected virtual void DeleteAll()
        {
            indexWriter.DeleteAll();
        }

        protected virtual void Commit()
        {
            indexWriter.Commit();
            IOUtils.Dispose(indexReader);
            indexReader = indexWriter.GetReader();
            indexSearcher = NewSearcher(indexReader);
        }

        protected virtual void VerifyDocumentsIndexed(int numDocs)
        {
            assertEquals(numDocs, indexReader.NumDocs);
        }

        protected virtual SearchResults executeQuery(Query query, int numDocs)
        {
            try
            {
                TopDocs topDocs = indexSearcher.Search(query, numDocs);

                IList<SearchResult> results = new JCG.List<SearchResult>();
                foreach (ScoreDoc scoreDoc in topDocs.ScoreDocs)
                {
                    results.Add(new SearchResult(scoreDoc.Score, indexSearcher.Doc(scoreDoc.Doc)));
                }
                return new SearchResults(topDocs.TotalHits, results);
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw RuntimeException.Create("IOException thrown while executing query", ioe);
            }
        }

        protected virtual IPoint randomPoint()
        {
            IRectangle WB = ctx.WorldBounds;
            return ctx.MakePoint(
                randomIntBetween((int)WB.MinX, (int)WB.MaxX),
                randomIntBetween((int)WB.MinY, (int)WB.MaxY));
        }

        protected virtual IRectangle randomRectangle()
        {
            IRectangle WB = ctx.WorldBounds;
            int rW = (int)randomGaussianMeanMax(10, WB.Width);
            double xMin = randomIntBetween((int)WB.MinX, (int)WB.MaxX - rW);
            double xMax = xMin + rW;

            int yH = (int)randomGaussianMeanMax(Math.Min(rW, WB.Height), WB.Height);
            double yMin = randomIntBetween((int)WB.MinY, (int)WB.MaxY - yH);
            double yMax = yMin + yH;

            return ctx.MakeRectangle(xMin, xMax, yMin, yMax);
        }

        private double randomGaussianMinMeanMax(double min, double mean, double max)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(mean > min);
            return randomGaussianMeanMax(mean - min, max - min) + min;
        }

        /**
         * Within one standard deviation (68% of the time) the result is "close" to
         * mean. By "close": when greater than mean, it's the lesser of 2*mean or half
         * way to max, when lesser than mean, it's the greater of max-2*mean or half
         * way to 0. The other 32% of the time it's in the rest of the range, touching
         * either 0 or max but never exceeding.
         */
        private double randomGaussianMeanMax(double mean, double max)
        {
            // DWS: I verified the results empirically
            if (Debugging.AssertsEnabled) Debugging.Assert(mean <= max && mean >= 0);
            double g = randomGaussian();
            double mean2 = mean;
            double flip = 1;
            if (g < 0)
            {
                mean2 = max - mean;
                flip = -1;
                g *= -1;
            }
            // pivot is the distance from mean2 towards max where the boundary of
            // 1 standard deviation alters the calculation
            double pivotMax = max - mean2;
            double pivot = Math.Min(mean2, pivotMax / 2);//from 0 to max-mean2
            if (Debugging.AssertsEnabled) Debugging.Assert(pivot >= 0 && pivotMax >= pivot && g >= 0);
            double pivotResult;
            if (g <= 1)
                pivotResult = pivot * g;
            else
                pivotResult = Math.Min(pivotMax, (g - 1) * (pivotMax - pivot) + pivot);

            return mean + flip * pivotResult;
        }

        // ================================================= Inner Classes =================================================

        protected class SearchResults
        {

            public int numFound;
            public IList<SearchResult> results;

            public SearchResults(int numFound, IList<SearchResult> results)
            {
                this.numFound = numFound;
                this.results = results;
            }

            public StringBuilder toDebugString()
            {
                StringBuilder str = new StringBuilder();
                str.append("found: ").append(numFound).append('[');
                foreach (SearchResult r in results)
                {
                    string id = r.GetId();
                    str.append(id).append(", ");
                }
                str.append(']');
                return str;
            }

            public override string ToString()
            {
                return "[found:" + numFound + " " + results + "]";
            }
        }

        protected class SearchResult
        {

            public float score;
            public Document document;

            public SearchResult(float score, Document document)
            {
                this.score = score;
                this.document = document;
            }

            public string GetId()
            {
                return document.Get("id");
            }

            public override string ToString()
            {
                return "[" + score + "=" + document + "]";
            }
        }
    }
}
