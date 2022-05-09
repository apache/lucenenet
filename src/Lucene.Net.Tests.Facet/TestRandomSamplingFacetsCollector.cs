// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet
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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using Document = Lucene.Net.Documents.Document;
    using FastTaxonomyFacetCounts = Lucene.Net.Facet.Taxonomy.FastTaxonomyFacetCounts;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchingDocs = Lucene.Net.Facet.FacetsCollector.MatchingDocs;
    using MultiCollector = Lucene.Net.Search.MultiCollector;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Store = Lucene.Net.Documents.Field.Store;
    using StringField = Lucene.Net.Documents.StringField;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;

    public class TestRandomSamplingFacetsCollector : FacetTestCase
    {
        [Test]
        public virtual void TestRandomSampling()
        {
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            FacetsConfig config = new FacetsConfig();

            int numDocs = AtLeast(10000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("EvenOdd", (i % 2 == 0) ? "even" : "odd", Store.NO));
                doc.Add(new FacetField("iMod10", Convert.ToString(i % 10, CultureInfo.InvariantCulture)));
                writer.AddDocument(config.Build(taxoWriter, doc));
            }
            Random random = Random;

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            IOUtils.Dispose(writer, taxoWriter);

            // Test empty results
            RandomSamplingFacetsCollector collectRandomZeroResults = new RandomSamplingFacetsCollector(numDocs / 10, random.NextInt64());

            // There should be no divisions by zero
            searcher.Search(new TermQuery(new Term("EvenOdd", "NeverMatches")), collectRandomZeroResults);

            // There should be no divisions by zero and no null result
            Assert.IsNotNull(collectRandomZeroResults.GetMatchingDocs());

            // There should be no results at all
            foreach (MatchingDocs doc in collectRandomZeroResults.GetMatchingDocs())
            {
                Assert.AreEqual(0, doc.TotalHits);
            }

            // Now start searching and retrieve results.

            // Use a query to select half of the documents.
            TermQuery query = new TermQuery(new Term("EvenOdd", "even"));

            // there will be 5 facet values (0, 2, 4, 6 and 8), as only the even (i %
            // 10) are hits.
            // there is a REAL small chance that one of the 5 values will be missed when
            // sampling.
            // but is that 0.8 (chance not to take a value) ^ 2000 * 5 (any can be
            // missing) ~ 10^-193
            // so that is probably not going to happen.
            int maxNumChildren = 5;

            RandomSamplingFacetsCollector random100Percent = new RandomSamplingFacetsCollector(numDocs, random.NextInt64()); // no sampling
            RandomSamplingFacetsCollector random10Percent = new RandomSamplingFacetsCollector(numDocs / 10, random.NextInt64()); // 10 % of total docs, 20% of the hits

            FacetsCollector fc = new FacetsCollector();

            searcher.Search(query, MultiCollector.Wrap(fc, random100Percent, random10Percent));

            FastTaxonomyFacetCounts random10FacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, random10Percent);
            FastTaxonomyFacetCounts random100FacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, random100Percent);
            FastTaxonomyFacetCounts exactFacetCounts = new FastTaxonomyFacetCounts(taxoReader, config, fc);

            FacetResult random10Result = random10Percent.AmortizeFacetCounts(random10FacetCounts.GetTopChildren(10, "iMod10"), config, searcher);
            FacetResult random100Result = random100FacetCounts.GetTopChildren(10, "iMod10");
            FacetResult exactResult = exactFacetCounts.GetTopChildren(10, "iMod10");

            Assert.AreEqual(random100Result, exactResult);

            // we should have five children, but there is a small chance we have less.
            // (see above).
            Assert.IsTrue(random10Result.ChildCount <= maxNumChildren);
            // there should be one child at least.
            Assert.IsTrue(random10Result.ChildCount >= 1);

            // now calculate some statistics to determine if the sampled result is 'ok'.
            // because random sampling is used, the results will vary each time.
            int sum = 0;
            foreach (LabelAndValue lav in random10Result.LabelValues)
            {
                sum += (int)lav.Value;
            }
            float mu = (float)sum / (float)maxNumChildren;

            float variance = 0;
            foreach (LabelAndValue lav in random10Result.LabelValues)
            {
                variance += (float)Math.Pow((mu - (int)lav.Value), 2);
            }
            variance = variance / maxNumChildren;
            float sigma = (float)Math.Sqrt(variance);

            // we query only half the documents and have 5 categories. The average
            // number of docs in a category will thus be the total divided by 5*2
            float targetMu = numDocs / (5.0f * 2.0f);

            // the average should be in the range and the standard deviation should not
            // be too great
            Assert.IsTrue(sigma < 200);
            Assert.IsTrue(targetMu - 3 * sigma < mu && mu < targetMu + 3 * sigma);

            IOUtils.Dispose(searcher.IndexReader, taxoReader, dir, taxoDir);
        }
    }
}