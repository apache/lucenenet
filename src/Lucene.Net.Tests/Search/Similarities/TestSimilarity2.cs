using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using NUnit.Framework;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search.Similarities
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
    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SpanOrQuery = Lucene.Net.Search.Spans.SpanOrQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    /// <summary>
    /// Tests against all the similarities we have
    /// </summary>
    [TestFixture]
    public class TestSimilarity2 : LuceneTestCase
    {
        internal IList<Similarity> sims;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            sims = new JCG.List<Similarity>();
            sims.Add(new DefaultSimilarity());
            sims.Add(new BM25Similarity());
            // TODO: not great that we dup this all with TestSimilarityBase
            foreach (BasicModel basicModel in TestSimilarityBase.BASIC_MODELS)
            {
                foreach (AfterEffect afterEffect in TestSimilarityBase.AFTER_EFFECTS)
                {
                    foreach (Normalization normalization in TestSimilarityBase.NORMALIZATIONS)
                    {
                        sims.Add(new DFRSimilarity(basicModel, afterEffect, normalization));
                    }
                }
            }
            foreach (Distribution distribution in TestSimilarityBase.DISTRIBUTIONS)
            {
                foreach (Lambda lambda in TestSimilarityBase.LAMBDAS)
                {
                    foreach (Normalization normalization in TestSimilarityBase.NORMALIZATIONS)
                    {
                        sims.Add(new IBSimilarity(distribution, lambda, normalization));
                    }
                }
            }
            sims.Add(new LMDirichletSimilarity());
            sims.Add(new LMJelinekMercerSimilarity(0.1f));
            sims.Add(new LMJelinekMercerSimilarity(0.7f));
        }

        /// <summary>
        /// because of stupid things like querynorm, its possible we computeStats on a field that doesnt exist at all
        ///  test this against a totally empty index, to make sure sims handle it
        /// </summary>
        [Test]
        public virtual void TestEmptyIndex()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                Assert.AreEqual(0, @is.Search(new TermQuery(new Term("foo", "bar")), 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// similar to the above, but ORs the query with a real field </summary>
        [Test]
        public virtual void TestEmptyField()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                BooleanQuery query = new BooleanQuery(true);
                query.Add(new TermQuery(new Term("foo", "bar")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("bar", "baz")), Occur.SHOULD);
                Assert.AreEqual(1, @is.Search(query, 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// similar to the above, however the field exists, but we query with a term that doesnt exist too </summary>
        [Test]
        public virtual void TestEmptyTerm()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("foo", "bar", Field.Store.NO));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                BooleanQuery query = new BooleanQuery(true);
                query.Add(new TermQuery(new Term("foo", "bar")), Occur.SHOULD);
                query.Add(new TermQuery(new Term("foo", "baz")), Occur.SHOULD);
                Assert.AreEqual(1, @is.Search(query, 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// make sure we can retrieve when norms are disabled </summary>
        [Test]
        public virtual void TestNoNorms()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.OmitNorms = true;
            ft.Freeze();
            doc.Add(NewField("foo", "bar", ft));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                BooleanQuery query = new BooleanQuery(true);
                query.Add(new TermQuery(new Term("foo", "bar")), Occur.SHOULD);
                Assert.AreEqual(1, @is.Search(query, 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// make sure all sims work if TF is omitted </summary>
        [Test]
        public virtual void TestOmitTF()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = IndexOptions.DOCS_ONLY;
            ft.Freeze();
            Field f = NewField("foo", "bar", ft);
            doc.Add(f);
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                BooleanQuery query = new BooleanQuery(true);
                query.Add(new TermQuery(new Term("foo", "bar")), Occur.SHOULD);
                Assert.AreEqual(1, @is.Search(query, 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// make sure all sims work if TF and norms is omitted </summary>
        [Test]
        public virtual void TestOmitTFAndNorms()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            ft.IndexOptions = IndexOptions.DOCS_ONLY;
            ft.OmitNorms = true;
            ft.Freeze();
            Field f = NewField("foo", "bar", ft);
            doc.Add(f);
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                BooleanQuery query = new BooleanQuery(true);
                query.Add(new TermQuery(new Term("foo", "bar")), Occur.SHOULD);
                Assert.AreEqual(1, @is.Search(query, 10).TotalHits);
            }
            ir.Dispose();
            dir.Dispose();
        }

        /// <summary>
        /// make sure all sims work with spanOR(termX, termY) where termY does not exist </summary>
        [Test]
        public virtual void TestCrazySpans()
        {
            // The problem: "normal" lucene queries create scorers, returning null if terms dont exist
            // this means they never score a term that does not exist.
            // however with spans, there is only one scorer for the whole hierarchy:
            // inner queries are not real queries, their boosts are ignored, etc.
            Directory dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            FieldType ft = new FieldType(TextField.TYPE_NOT_STORED);
            doc.Add(NewField("foo", "bar", ft));
            iw.AddDocument(doc);
            IndexReader ir = iw.GetReader();
            iw.Dispose();
            IndexSearcher @is = NewSearcher(ir);

            foreach (Similarity sim in sims)
            {
                @is.Similarity = sim;
                SpanTermQuery s1 = new SpanTermQuery(new Term("foo", "bar"));
                SpanTermQuery s2 = new SpanTermQuery(new Term("foo", "baz"));
                Query query = new SpanOrQuery(s1, s2);
                TopDocs td = @is.Search(query, 10);
                Assert.AreEqual(1, td.TotalHits);
                float score = td.ScoreDocs[0].Score;
                Assert.IsTrue(score >= 0.0f);
                Assert.IsFalse(float.IsInfinity(score), "inf score for " + sim);
            }
            ir.Dispose();
            dir.Dispose();
        }
    }
}