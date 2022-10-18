// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using NUnit.Framework;
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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Document = Lucene.Net.Documents.Document;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestFacetsConfig : FacetTestCase
    {

        [Test]
        public virtual void TestPathToStringAndBack()
        {
            int iters = AtLeast(1000);
            for (int i = 0; i < iters; i++)
            {
                int numParts = TestUtil.NextInt32(Random, 1, 6);
                string[] parts = new string[numParts];
                for (int j = 0; j < numParts; j++)
                {
                    string s;
                    while (true)
                    {
                        s = TestUtil.RandomUnicodeString(Random);
                        if (s.Length > 0)
                        {
                            break;
                        }
                    }
                    parts[j] = s;
                }

                string s1 = FacetsConfig.PathToString(parts);
                string[] parts2 = FacetsConfig.StringToPath(s1);
                Assert.True(Arrays.Equals(parts, parts2));
            }
        }

        [Test]
        public virtual void TestAddSameDocTwice()
        {
            // LUCENE-5367: this was a problem with the previous code, making sure it
            // works with the new code.
            Directory indexDir = NewDirectory(), taxoDir = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig facetsConfig = new FacetsConfig();
            Document doc = new Document();
            doc.Add(new FacetField("a", "b"));
            doc = facetsConfig.Build(taxoWriter, doc);
            // these two addDocument() used to fail
            indexWriter.AddDocument(doc);
            indexWriter.AddDocument(doc);
            IOUtils.Dispose(indexWriter, taxoWriter);

            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);
            FacetsCollector fc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), fc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, facetsConfig, fc);
            FacetResult res = facets.GetTopChildren(10, "a");
            Assert.AreEqual(1, res.LabelValues.Length);
            Assert.AreEqual(2, res.LabelValues[0].Value);
            IOUtils.Dispose(indexReader, taxoReader);

            IOUtils.Dispose(indexDir, taxoDir);
        }

        /// <summary>
        /// LUCENE-5479 
        /// </summary>
        [Test]
        public virtual void TestCustomDefault()
        {
            FacetsConfig config = new FacetsConfigAnonymousClass(this);

            Assert.IsTrue(config.GetDimConfig("foobar").IsHierarchical);
        }

        private sealed class FacetsConfigAnonymousClass : FacetsConfig
        {
            private readonly TestFacetsConfig outerInstance;

            public FacetsConfigAnonymousClass(TestFacetsConfig outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected override DimConfig DefaultDimConfig
            {
                get
                {
                    DimConfig config = new DimConfig();
                    config.IsHierarchical = true;
                    return config;
                }
            }
        }
    }
}