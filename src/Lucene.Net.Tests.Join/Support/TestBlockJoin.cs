// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Join;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Tests.Join
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

    [Obsolete("Production tests are in Lucene.Net.Search.Join. This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class TestBlockJoin : LuceneTestCase
    {
        // One resume...
        private Document MakeResume(string name, string country)
        {
            Document resume = new Document();
            resume.Add(NewStringField("docType", "resume", Field.Store.NO));
            resume.Add(NewStringField("name", name, Field.Store.YES));
            resume.Add(NewStringField("country", country, Field.Store.NO));
            return resume;
        }

        // ... has multiple jobs
        private Document MakeJob(string skill, int year)
        {
            Document job = new Document();
            job.Add(NewStringField("skill", skill, Field.Store.YES));
            job.Add(new Int32Field("year", year, Field.Store.NO));
            job.Add(new StoredField("year", year));
            return job;
        }

        // ... has multiple qualifications
        private Document MakeQualification(string qualification, int year)
        {
            Document job = new Document();
            job.Add(NewStringField("qualification", qualification, Field.Store.YES));
            job.Add(new Int32Field("year", year, Field.Store.NO));
            return job;
        }

        [Test]
        public void TestEmptyChildFilter()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            config.SetMergePolicy(NoMergePolicy.NO_COMPOUND_FILES);
            // we don't want to merge - since we rely on certain segment setup
            IndexWriter w = new IndexWriter(dir, config);

            IList<Document> docs = new List<Document>();

            docs.Add(MakeJob("java", 2007));
            docs.Add(MakeJob("python", 2010));
            docs.Add(MakeResume("Lisa", "United Kingdom"));
            w.AddDocuments(docs);

            docs.Clear();
            docs.Add(MakeJob("ruby", 2005));
            docs.Add(MakeJob("java", 2006));
            docs.Add(MakeResume("Frank", "United States"));
            w.AddDocuments(docs);
            w.Commit();
            int num = AtLeast(10); // produce a segment that doesn't have a value in the docType field
            for (int i = 0; i < num; i++)
            {
                docs.Clear();
                docs.Add(MakeJob("java", 2007));
                w.AddDocuments(docs);
            }

            IndexReader r = DirectoryReader.Open(w, Random.NextBoolean());
            w.Dispose();
            assertTrue(r.Leaves.size() > 1);
            IndexSearcher s = new IndexSearcher(r);
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            BooleanQuery childQuery = new BooleanQuery();
            childQuery.Add(new BooleanClause(new TermQuery(new Term("skill", "java")), Occur.MUST));
            childQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 2006, 2011, true, true), Occur.MUST));

            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);

            BooleanQuery fullQuery = new BooleanQuery();
            fullQuery.Add(new BooleanClause(childJoinQuery, Occur.MUST));
            fullQuery.Add(new BooleanClause(new MatchAllDocsQuery(), Occur.MUST));
            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(Sort.RELEVANCE, 1, true, true);
            s.Search(fullQuery, c);
            ITopGroups<int> results = c.GetTopGroups(childJoinQuery, null, 0, 10, 0, true);
            assertFalse(float.IsNaN(results.MaxScore));
            assertEquals(1, results.TotalGroupedHitCount);
            assertEquals(1, results.Groups.Length);
            IGroupDocs<int> group = results.Groups[0];
            Document childDoc = s.Doc(group.ScoreDocs[0].Doc);
            assertEquals("java", childDoc.Get("skill"));
            assertNotNull(group.GroupValue);
            Document parentDoc = s.Doc(group.GroupValue);
            assertEquals("Lisa", parentDoc.Get("name"));

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestSimple()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            IList<Document> docs = new List<Document>();

            docs.Add(MakeJob("java", 2007));
            docs.Add(MakeJob("python", 2010));
            docs.Add(MakeResume("Lisa", "United Kingdom"));
            w.AddDocuments(docs);

            docs.Clear();
            docs.Add(MakeJob("ruby", 2005));
            docs.Add(MakeJob("java", 2006));
            docs.Add(MakeResume("Frank", "United States"));
            w.AddDocuments(docs);

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            // Create a filter that defines "parent" documents in the index - in this case resumes
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            // Define child document criteria (finds an example of relevant work experience)
            BooleanQuery childQuery = new BooleanQuery();
            childQuery.Add(new BooleanClause(new TermQuery(new Term("skill", "java")), Occur.MUST));
            childQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 2006, 2011, true, true), Occur.MUST));

            // Define parent document criteria (find a resident in the UK)
            Query parentQuery = new TermQuery(new Term("country", "United Kingdom"));

            // Wrap the child document query to 'join' any matches
            // up to corresponding parent:
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);

            // Combine the parent and nested child queries into a single query for a candidate
            BooleanQuery fullQuery = new BooleanQuery();
            fullQuery.Add(new BooleanClause(parentQuery, Occur.MUST));
            fullQuery.Add(new BooleanClause(childJoinQuery, Occur.MUST));

            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(Sort.RELEVANCE, 1, true, true);

            s.Search(fullQuery, c);

            ITopGroups<int> results = c.GetTopGroups(childJoinQuery, null, 0, 10, 0, true);
            assertFalse(float.IsNaN(results.MaxScore));

            //assertEquals(1, results.totalHitCount);
            assertEquals(1, results.TotalGroupedHitCount);
            assertEquals(1, results.Groups.Length);

            IGroupDocs<int> group = results.Groups[0];
            assertEquals(1, group.TotalHits);
            assertFalse(float.IsNaN(group.Score));

            Document childDoc = s.Doc(group.ScoreDocs[0].Doc);
            //System.out.println("  doc=" + group.ScoreDocs[0].Doc);
            assertEquals("java", childDoc.Get("skill"));
            assertNotNull(group.GroupValue);
            Document parentDoc = s.Doc(group.GroupValue);
            assertEquals("Lisa", parentDoc.Get("name"));


            //System.out.println("TEST: now test up");

            // Now join "up" (map parent hits to child docs) instead...:
            ToChildBlockJoinQuery parentJoinQuery = new ToChildBlockJoinQuery(parentQuery, parentsFilter, Random.NextBoolean());
            BooleanQuery fullChildQuery = new BooleanQuery();
            fullChildQuery.Add(new BooleanClause(parentJoinQuery, Occur.MUST));
            fullChildQuery.Add(new BooleanClause(childQuery, Occur.MUST));

            //System.out.println("FULL: " + fullChildQuery);
            TopDocs hits = s.Search(fullChildQuery, 10);
            assertEquals(1, hits.TotalHits);
            childDoc = s.Doc(hits.ScoreDocs[0].Doc);
            //System.out.println("CHILD = " + childDoc + " docID=" + hits.ScoreDocs[0].Doc);
            assertEquals("java", childDoc.Get("skill"));
            assertEquals(2007, childDoc.GetField("year").GetInt32ValueOrDefault());
            assertEquals("Lisa", GetParentDoc(r, parentsFilter, hits.ScoreDocs[0].Doc).Get("name"));

            // Test with filter on child docs:
            assertEquals(0, s.Search(fullChildQuery, new QueryWrapperFilter(new TermQuery(new Term("skill", "foosball"))), 1).TotalHits);

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBugCausedByRewritingTwice()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            IList<Document> docs = new List<Document>();

            for (int i = 0; i < 10; i++)
            {
                docs.Clear();
                docs.Add(MakeJob("ruby", i));
                docs.Add(MakeJob("java", 2007));
                docs.Add(MakeResume("Frank", "United States"));
                w.AddDocuments(docs);
            }

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            MultiTermQuery qc = NumericRangeQuery.NewInt32Range("year", 2007, 2007, true, true);
            // Hacky: this causes the query to need 2 rewrite
            // iterations: 
            qc.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_BOOLEAN_QUERY_REWRITE;

            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            int h1 = qc.GetHashCode();
            Query qw1 = qc.Rewrite(r);
            int h2 = qw1.GetHashCode();
            Query qw2 = qw1.Rewrite(r);
            int h3 = qw2.GetHashCode();

            assertTrue(h1 != h2);
            assertTrue(h2 != h3);
            assertTrue(h3 != h1);

            ToParentBlockJoinQuery qp = new ToParentBlockJoinQuery(qc, parentsFilter, ScoreMode.Max);
            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(Sort.RELEVANCE, 10, true, true);

            s.Search(qp, c);
            ITopGroups<int> groups = c.GetTopGroups(qp, Sort.INDEXORDER, 0, 10, 0, true);
            foreach (GroupDocs<int> group in groups.Groups)
            {
                assertEquals(1, group.TotalHits);
            }

            r.Dispose();
            dir.Dispose();
        }

        protected QueryWrapperFilter Skill(string skill)
        {
            return new QueryWrapperFilter(new TermQuery(new Term("skill", skill)));
        }

        [Test]
        public virtual void TestSimpleFilter()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            IList<Document> docs = new List<Document>();
            docs.Add(MakeJob("java", 2007));
            docs.Add(MakeJob("python", 2010));
            docs.Shuffle(Random);
            docs.Add(MakeResume("Lisa", "United Kingdom"));

            IList<Document> docs2 = new List<Document>();
            docs2.Add(MakeJob("ruby", 2005));
            docs2.Add(MakeJob("java", 2006));
            docs2.Shuffle(Random);
            docs2.Add(MakeResume("Frank", "United States"));

            AddSkillless(w);
            bool turn = Random.NextBoolean();
            w.AddDocuments(turn ? docs : docs2);

            AddSkillless(w);

            w.AddDocuments(!turn ? docs : docs2);

            AddSkillless(w);

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            // Create a filter that defines "parent" documents in the index - in this case resumes
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            // Define child document criteria (finds an example of relevant work experience)
            BooleanQuery childQuery = new BooleanQuery();
            childQuery.Add(new BooleanClause(new TermQuery(new Term("skill", "java")), Occur.MUST));
            childQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 2006, 2011, true, true), Occur.MUST));

            // Define parent document criteria (find a resident in the UK)
            Query parentQuery = new TermQuery(new Term("country", "United Kingdom"));

            // Wrap the child document query to 'join' any matches
            // up to corresponding parent:
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);

            assertEquals("no filter - both passed", 2, s.Search(childJoinQuery, 10).TotalHits);

            assertEquals("dummy filter passes everyone ", 2, s.Search(childJoinQuery, parentsFilter, 10).TotalHits);
            assertEquals("dummy filter passes everyone ", 2, s.Search(childJoinQuery, new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))), 10).TotalHits);

            // not found test
            assertEquals("noone live there", 0, s.Search(childJoinQuery, new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("country", "Oz")))), 1).TotalHits);
            assertEquals("noone live there", 0, s.Search(childJoinQuery, new QueryWrapperFilter(new TermQuery(new Term("country", "Oz"))), 1).TotalHits);

            // apply the UK filter by the searcher
            TopDocs ukOnly = s.Search(childJoinQuery, new QueryWrapperFilter(parentQuery), 1);
            assertEquals("has filter - single passed", 1, ukOnly.TotalHits);
            assertEquals("Lisa", r.Document(ukOnly.ScoreDocs[0].Doc).Get("name"));

            // looking for US candidates
            TopDocs usThen = s.Search(childJoinQuery, new QueryWrapperFilter(new TermQuery(new Term("country", "United States"))), 1);
            assertEquals("has filter - single passed", 1, usThen.TotalHits);
            assertEquals("Frank", r.Document(usThen.ScoreDocs[0].Doc).Get("name"));


            TermQuery us = new TermQuery(new Term("country", "United States"));
            assertEquals("@ US we have java and ruby", 2,
                s.Search(new ToChildBlockJoinQuery(us, parentsFilter, Random.NextBoolean()), 10).TotalHits);

            assertEquals("java skills in US", 1, s.Search(new ToChildBlockJoinQuery(us, parentsFilter, Random.NextBoolean()), Skill("java"), 10).TotalHits);

            BooleanQuery rubyPython = new BooleanQuery();
            rubyPython.Add(new TermQuery(new Term("skill", "ruby")), Occur.SHOULD);
            rubyPython.Add(new TermQuery(new Term("skill", "python")), Occur.SHOULD);
            assertEquals("ruby skills in US", 1, s.Search(new ToChildBlockJoinQuery(us, parentsFilter, Random.NextBoolean()), new QueryWrapperFilter(rubyPython), 10).TotalHits);

            r.Dispose();
            dir.Dispose();
        }

        private void AddSkillless(RandomIndexWriter w)
        {
            if (Random.NextBoolean())
            {
                w.AddDocument(MakeResume("Skillless", Random.NextBoolean() ? "United Kingdom" : "United States"));
            }
        }

        private Document GetParentDoc(IndexReader reader, Filter parents, int childDocID)
        {
            IList<AtomicReaderContext> leaves = reader.Leaves;
            int subIndex = ReaderUtil.SubIndex(childDocID, leaves);
            AtomicReaderContext leaf = leaves[subIndex];
            FixedBitSet bits = (FixedBitSet)parents.GetDocIdSet(leaf, null);
            return leaf.AtomicReader.Document(bits.NextSetBit(childDocID - leaf.DocBase));
        }

        [Test]
        public void TestBoostBug()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            ToParentBlockJoinQuery q = new ToParentBlockJoinQuery(new MatchAllDocsQuery(), new QueryWrapperFilter(new MatchAllDocsQuery()), ScoreMode.Avg);
            QueryUtils.Check(Random, q, s);
            s.Search(q, 10);
            BooleanQuery bq = new BooleanQuery();
            bq.Boost = 2f; // we boost the BQ
            bq.Add(q, Occur.MUST);
            s.Search(bq, 10);
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestNestedDocScoringWithDeletes()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));

            // Cannot assert this since we use NoMergePolicy:
            w.DoRandomForceMergeAssert = false;

            IList<Document> docs = new List<Document>();
            docs.Add(MakeJob("java", 2007));
            docs.Add(MakeJob("python", 2010));
            docs.Add(MakeResume("Lisa", "United Kingdom"));
            w.AddDocuments(docs);

            docs.Clear();
            docs.Add(MakeJob("c", 1999));
            docs.Add(MakeJob("ruby", 2005));
            docs.Add(MakeJob("java", 2006));
            docs.Add(MakeResume("Frank", "United States"));
            w.AddDocuments(docs);

            w.Commit();
            IndexSearcher s = NewSearcher(DirectoryReader.Open(dir));

            ToParentBlockJoinQuery q = new ToParentBlockJoinQuery(
                NumericRangeQuery.NewInt32Range("year", 1990, 2010, true, true),
                new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume")))),
                ScoreMode.Total
            );

            TopDocs topDocs = s.Search(q, 10);
            assertEquals(2, topDocs.TotalHits);
            assertEquals(6, topDocs.ScoreDocs[0].Doc);
            assertEquals(3.0f, topDocs.ScoreDocs[0].Score, 0.0f);
            assertEquals(2, topDocs.ScoreDocs[1].Doc);
            assertEquals(2.0f, topDocs.ScoreDocs[1].Score, 0.0f);

            s.IndexReader.Dispose();
            w.DeleteDocuments(new Term("skill", "java"));
            w.Dispose();
            s = NewSearcher(DirectoryReader.Open(dir));

            topDocs = s.Search(q, 10);
            assertEquals(2, topDocs.TotalHits);
            assertEquals(6, topDocs.ScoreDocs[0].Doc);
            assertEquals(2.0f, topDocs.ScoreDocs[0].Score, 0.0f);
            assertEquals(2, topDocs.ScoreDocs[1].Doc);
            assertEquals(1.0f, topDocs.ScoreDocs[1].Score, 0.0f);

            s.IndexReader.Dispose();
            dir.Dispose();
        }

        private string[][] GetRandomFields(int maxUniqueValues)
        {
            string[][] fields = new string[TestUtil.NextInt32(Random, 2, 4)][];
            for (int fieldID = 0; fieldID < fields.Length; fieldID++)
            {
                int valueCount;
                if (fieldID == 0)
                {
                    valueCount = 2;
                }
                else
                {
                    valueCount = TestUtil.NextInt32(Random, 1, maxUniqueValues);
                }

                string[] values = fields[fieldID] = new string[valueCount];
                for (int i = 0; i < valueCount; i++)
                {
                    values[i] = TestUtil.RandomRealisticUnicodeString(Random);
                    //values[i] = TestUtil.randomSimpleString(random);
                }
            }

            return fields;
        }

        private Term RandomParentTerm(string[] values)
        {
            return new Term("parent0", values[Random.Next(values.Length)]);
        }

        private Term RandomChildTerm(string[] values)
        {
            return new Term("child0", values[Random.Next(values.Length)]);
        }

        private Sort GetRandomSort(string prefix, int numFields)
        {
            List<SortField> sortFields = new List<SortField>();
            // TODO: sometimes sort by score; problem is scores are
            // not comparable across the two indices
            // sortFields.Add(SortField.FIELD_SCORE);
            if (Random.NextBoolean())
            {
                sortFields.Add(new SortField(prefix + Random.Next(numFields), SortFieldType.STRING, Random.NextBoolean()));
            }
            else if (Random.NextBoolean())
            {
                sortFields.Add(new SortField(prefix + Random.Next(numFields), SortFieldType.STRING, Random.NextBoolean()));
                sortFields.Add(new SortField(prefix + Random.Next(numFields), SortFieldType.STRING, Random.NextBoolean()));
            }
            // Break ties:
            sortFields.Add(new SortField(prefix + "ID", SortFieldType.INT32));
            return new Sort(sortFields.ToArray());
        }

        [Test]
        public void TestRandom()
        {
            // We build two indices at once: one normalized (which
            // ToParentBlockJoinQuery/Collector,
            // ToChildBlockJoinQuery can query) and the other w/
            // the same docs, just fully denormalized:
            Directory dir = NewDirectory();
            Directory joinDir = NewDirectory();

            int numParentDocs = TestUtil.NextInt32(Random, 100 * RandomMultiplier, 300 * RandomMultiplier);
            //int numParentDocs = 30;

            // Values for parent fields:
            string[][] parentFields = GetRandomFields(numParentDocs / 2);
            // Values for child fields:
            string[][] childFields = GetRandomFields(numParentDocs);

            bool doDeletes = Random.NextBoolean();
            IList<int> toDelete = new List<int>();

            // TODO: parallel star join, nested join cases too!
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            RandomIndexWriter joinW = new RandomIndexWriter(Random, joinDir);
            for (int parentDocID = 0; parentDocID < numParentDocs; parentDocID++)
            {
                Document parentDoc = new Document();
                Document parentJoinDoc = new Document();
                Field id = NewStringField("parentID", "" + parentDocID, Field.Store.YES);
                parentDoc.Add(id);
                parentJoinDoc.Add(id);
                parentJoinDoc.Add(NewStringField("isParent", "x", Field.Store.NO));
                for (int field = 0; field < parentFields.Length; field++)
                {
                    if (Random.NextDouble() < 0.9)
                    {
                        Field f = NewStringField("parent" + field, parentFields[field][Random.Next(parentFields[field].Length)], Field.Store.NO);
                        parentDoc.Add(f);
                        parentJoinDoc.Add(f);
                    }
                }

                if (doDeletes)
                {
                    parentDoc.Add(NewStringField("blockID", "" + parentDocID, Field.Store.NO));
                    parentJoinDoc.Add(NewStringField("blockID", "" + parentDocID, Field.Store.NO));
                }

                IList<Document> joinDocs = new List<Document>();

                if (Verbose)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("parentID=").Append(parentDoc.Get("parentID"));
                    for (int fieldID = 0; fieldID < parentFields.Length; fieldID++)
                    {
                        string parent = parentDoc.Get("parent" + fieldID);
                        if (parent != null)
                        {
                            sb.Append(" parent" + fieldID + "=" + parent);
                        }
                    }
                    Console.WriteLine("  " + sb);
                }

                int numChildDocs = TestUtil.NextInt32(Random, 1, 20);
                for (int childDocID = 0; childDocID < numChildDocs; childDocID++)
                {
                    // Denormalize: copy all parent fields into child doc:
                    Document childDoc = TestUtil.CloneDocument(parentDoc);
                    Document joinChildDoc = new Document();
                    joinDocs.Add(joinChildDoc);

                    Field childID = NewStringField("childID", "" + childDocID, Field.Store.YES);
                    childDoc.Add(childID);
                    joinChildDoc.Add(childID);

                    for (int childFieldID = 0; childFieldID < childFields.Length; childFieldID++)
                    {
                        if (Random.NextDouble() < 0.9)
                        {
                            Field f = NewStringField("child" + childFieldID, childFields[childFieldID][Random.Next(childFields[childFieldID].Length)], Field.Store.NO);
                            childDoc.Add(f);
                            joinChildDoc.Add(f);
                        }
                    }

                    if (Verbose)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append("childID=").Append(joinChildDoc.Get("childID"));
                        for (int fieldID = 0; fieldID < childFields.Length; fieldID++)
                        {
                            string child = joinChildDoc.Get("child" + fieldID);
                            if (child != null)
                            {
                                sb.Append(" child" + fieldID + "=" + child);
                            }
                        }
                        Console.WriteLine("    " + sb);
                    }

                    if (doDeletes)
                    {
                        joinChildDoc.Add(NewStringField("blockID", "" + parentDocID, Field.Store.NO));
                    }

                    w.AddDocument(childDoc);
                }

                // Parent last:
                joinDocs.Add(parentJoinDoc);
                joinW.AddDocuments(joinDocs);

                if (doDeletes && Random.Next(30) == 7)
                {
                    toDelete.Add(parentDocID);
                }
            }

            foreach (int deleteID in toDelete)
            {
                if (Verbose)
                {
                    Console.WriteLine("DELETE parentID=" + deleteID);
                }
                w.DeleteDocuments(new Term("blockID", "" + deleteID));
                joinW.DeleteDocuments(new Term("blockID", "" + deleteID));
            }

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexReader joinR = joinW.GetReader();
            joinW.Dispose();

            if (Verbose)
            {
                Console.WriteLine("TEST: reader=" + r);
                Console.WriteLine("TEST: joinReader=" + joinR);

                for (int docIDX = 0; docIDX < joinR.MaxDoc; docIDX++)
                {
                    Console.WriteLine("  docID=" + docIDX + " doc=" + joinR.Document(docIDX));
                }
            }

            IndexSearcher s = NewSearcher(r);

            IndexSearcher joinS = new IndexSearcher(joinR);

            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isParent", "x"))));

            int iters = 200 * RandomMultiplier;

            for (int iter = 0; iter < iters; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + (1 + iter) + " of " + iters);
                }

                Query childQuery;
                if (Random.Next(3) == 2)
                {
                    int childFieldID = Random.Next(childFields.Length);
                    childQuery = new TermQuery(new Term("child" + childFieldID,
                        childFields[childFieldID][Random.Next(childFields[childFieldID].Length)]));
                }
                else if (Random.Next(3) == 2)
                {
                    BooleanQuery bq = new BooleanQuery();
                    childQuery = bq;
                    int numClauses = TestUtil.NextInt32(Random, 2, 4);
                    bool didMust = false;
                    for (int clauseIDX = 0; clauseIDX < numClauses; clauseIDX++)
                    {
                        Query clause;
                        Occur occur;
                        if (!didMust && Random.NextBoolean())
                        {
                            occur = Random.NextBoolean() ? Occur.MUST : Occur.MUST_NOT;
                            clause = new TermQuery(RandomChildTerm(childFields[0]));
                            didMust = true;
                        }
                        else
                        {
                            occur = Occur.SHOULD;
                            int childFieldID = TestUtil.NextInt32(Random, 1, childFields.Length - 1);
                            clause = new TermQuery(new Term("child" + childFieldID, childFields[childFieldID][Random.Next(childFields[childFieldID].Length)]));
                        }
                        bq.Add(clause, occur);
                    }
                }
                else
                {
                    BooleanQuery bq = new BooleanQuery();
                    childQuery = bq;

                    bq.Add(new TermQuery(RandomChildTerm(childFields[0])), Occur.MUST);
                    int childFieldID = TestUtil.NextInt32(Random, 1, childFields.Length - 1);
                    bq.Add(new TermQuery(new Term("child" + childFieldID, childFields[childFieldID][Random.Next(childFields[childFieldID].Length)])),
                        Random.NextBoolean() ? Occur.MUST : Occur.MUST_NOT);
                }

                int x = Random.Next(4);
                ScoreMode agg;
                if (x == 0)
                {
                    agg = ScoreMode.None;
                }
                else if (x == 1)
                {
                    agg = ScoreMode.Max;
                }
                else if (x == 2)
                {
                    agg = ScoreMode.Total;
                }
                else
                {
                    agg = ScoreMode.Avg;
                }

                ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, agg);

                // To run against the block-join index:
                Query parentJoinQuery;

                // Same query as parentJoinQuery, but to run against
                // the fully denormalized index (so we can compare
                // results):
                Query parentQuery;

                if (Random.NextBoolean())
                {
                    parentQuery = childQuery;
                    parentJoinQuery = childJoinQuery;
                }
                else
                {
                    // AND parent field w/ child field
                    BooleanQuery bq = new BooleanQuery();
                    parentJoinQuery = bq;
                    Term parentTerm = RandomParentTerm(parentFields[0]);
                    if (Random.NextBoolean())
                    {
                        bq.Add(childJoinQuery, Occur.MUST);
                        bq.Add(new TermQuery(parentTerm), Occur.MUST);
                    }
                    else
                    {
                        bq.Add(new TermQuery(parentTerm), Occur.MUST);
                        bq.Add(childJoinQuery, Occur.MUST);
                    }

                    BooleanQuery bq2 = new BooleanQuery();
                    parentQuery = bq2;
                    if (Random.NextBoolean())
                    {
                        bq2.Add(childQuery, Occur.MUST);
                        bq2.Add(new TermQuery(parentTerm), Occur.MUST);
                    }
                    else
                    {
                        bq2.Add(new TermQuery(parentTerm), Occur.MUST);
                        bq2.Add(childQuery, Occur.MUST);
                    }
                }

                Sort parentSort = GetRandomSort("parent", parentFields.Length);
                Sort childSort = GetRandomSort("child", childFields.Length);

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: query=" + parentQuery + " joinQuery=" + parentJoinQuery + " parentSort=" + parentSort + " childSort=" + childSort);
                }

                // Merge both sorts:
                List<SortField> sortFields = new List<SortField>(parentSort.GetSort());
                sortFields.AddRange(childSort.GetSort());
                Sort parentAndChildSort = new Sort(sortFields.ToArray());

                TopDocs results = s.Search(parentQuery, null, r.NumDocs, parentAndChildSort);

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: normal index gets " + results.TotalHits + " hits");
                    ScoreDoc[] hits = results.ScoreDocs;
                    for (int hitIDX = 0; hitIDX < hits.Length; hitIDX++)
                    {
                        Document doc = s.Doc(hits[hitIDX].Doc);
                        //System.out.println("  score=" + hits[hitIDX].Score + " parentID=" + doc.Get("parentID") + " childID=" + doc.Get("childID") + " (docID=" + hits[hitIDX].Doc + ")");
                        Console.WriteLine("  parentID=" + doc.Get("parentID") + " childID=" + doc.Get("childID") + " (docID=" + hits[hitIDX].Doc + ")");
                        FieldDoc fd = (FieldDoc)hits[hitIDX];
                        if (fd.Fields != null)
                        {
                            Console.Write("    ");
                            foreach (object o in fd.Fields)
                            {
                                if (o is BytesRef)
                                {
                                    Console.Write(((BytesRef)o).Utf8ToString() + " ");
                                }
                                else
                                {
                                    Console.Write(o + " ");
                                }
                            }
                            Console.WriteLine();
                        }
                    }
                }

                bool trackScores;
                bool trackMaxScore;
                if (agg == ScoreMode.None)
                {
                    trackScores = false;
                    trackMaxScore = false;
                }
                else
                {
                    trackScores = Random.NextBoolean();
                    trackMaxScore = Random.NextBoolean();
                }
                ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(parentSort, 10, trackScores, trackMaxScore);

                joinS.Search(parentJoinQuery, c);

                int hitsPerGroup = TestUtil.NextInt32(Random, 1, 20);
                //final int hitsPerGroup = 100;
                ITopGroups<int> joinResults = c.GetTopGroups(childJoinQuery, childSort, 0, hitsPerGroup, 0, true);

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: block join index gets " + (joinResults is null ? 0 : joinResults.Groups.Length) + " groups; hitsPerGroup=" + hitsPerGroup);
                    if (joinResults != null)
                    {
                        IGroupDocs<int>[] groups = joinResults.Groups;
                        for (int groupIDX = 0; groupIDX < groups.Length; groupIDX++)
                        {
                            IGroupDocs<int> group = groups[groupIDX];
                            if (group.GroupSortValues != null)
                            {
                                Console.Write("  ");
                                foreach (object o in group.GroupSortValues)
                                {
                                    if (o is BytesRef)
                                    {
                                        Console.Write(((BytesRef)o).Utf8ToString() + " ");
                                    }
                                    else
                                    {
                                        Console.Write(o + " ");
                                    }
                                }
                                Console.WriteLine();
                            }

                            assertNotNull(group.GroupValue);
                            Document parentDoc = joinS.Doc(group.GroupValue);
                            Console.WriteLine("  group parentID=" + parentDoc.Get("parentID") + " (docID=" + group.GroupValue + ")");
                            for (int hitIDX = 0; hitIDX < group.ScoreDocs.Length; hitIDX++)
                            {
                                Document doc = joinS.Doc(group.ScoreDocs[hitIDX].Doc);
                                //System.out.println("    score=" + group.ScoreDocs[hitIDX].Score + " childID=" + doc.Get("childID") + " (docID=" + group.ScoreDocs[hitIDX].Doc + ")");
                                Console.WriteLine("    childID=" + doc.Get("childID") + " child0=" + doc.Get("child0") + " (docID=" + group.ScoreDocs[hitIDX].Doc + ")");
                            }
                        }
                    }
                }

                if (results.TotalHits == 0)
                {
                    assertNull(joinResults);
                }
                else
                {
                    CompareHits(r, joinR, results, joinResults);
                    TopDocs b = joinS.Search(childJoinQuery, 10);
                    foreach (ScoreDoc hit in b.ScoreDocs)
                    {
                        Explanation explanation = joinS.Explain(childJoinQuery, hit.Doc);
                        Document document = joinS.Doc(hit.Doc - 1);
                        int childId = Convert.ToInt32(document.Get("childID"), CultureInfo.InvariantCulture);
                        assertTrue(explanation.IsMatch);
                        assertEquals(hit.Score, explanation.Value, 0.0f);
                        assertEquals(string.Format("Score based on child doc range from {0} to {1}", hit.Doc - 1 - childId, hit.Doc - 1), explanation.Description);
                    }
                }

                // Test joining in the opposite direction (parent to
                // child):

                // Get random query against parent documents:
                Query parentQuery2;
                if (Random.Next(3) == 2)
                {
                    int fieldID = Random.Next(parentFields.Length);
                    parentQuery2 = new TermQuery(new Term("parent" + fieldID, parentFields[fieldID][Random.Next(parentFields[fieldID].Length)]));
                }
                else if (Random.Next(3) == 2)
                {
                    BooleanQuery bq = new BooleanQuery();
                    parentQuery2 = bq;
                    int numClauses = TestUtil.NextInt32(Random, 2, 4);
                    bool didMust = false;
                    for (int clauseIDX = 0; clauseIDX < numClauses; clauseIDX++)
                    {
                        Query clause;
                        Occur occur;
                        if (!didMust && Random.NextBoolean())
                        {
                            occur = Random.NextBoolean() ? Occur.MUST : Occur.MUST_NOT;
                            clause = new TermQuery(RandomParentTerm(parentFields[0]));
                            didMust = true;
                        }
                        else
                        {
                            occur = Occur.SHOULD;
                            int fieldID = TestUtil.NextInt32(Random, 1, parentFields.Length - 1);
                            clause = new TermQuery(new Term("parent" + fieldID, parentFields[fieldID][Random.Next(parentFields[fieldID].Length)]));
                        }
                        bq.Add(clause, occur);
                    }
                }
                else
                {
                    BooleanQuery bq = new BooleanQuery();
                    parentQuery2 = bq;

                    bq.Add(new TermQuery(RandomParentTerm(parentFields[0])), Occur.MUST);
                    int fieldID = TestUtil.NextInt32(Random, 1, parentFields.Length - 1);
                    bq.Add(new TermQuery(new Term("parent" + fieldID, parentFields[fieldID][Random.Next(parentFields[fieldID].Length)])), Random.NextBoolean() ? Occur.MUST : Occur.MUST_NOT);
                }

                if (Verbose)
                {
                    Console.WriteLine("\nTEST: top down: parentQuery2=" + parentQuery2);
                }

                // Maps parent query to child docs:
                ToChildBlockJoinQuery parentJoinQuery2 = new ToChildBlockJoinQuery(parentQuery2, parentsFilter, Random.NextBoolean());

                // To run against the block-join index:
                Query childJoinQuery2;

                // Same query as parentJoinQuery, but to run against
                // the fully denormalized index (so we can compare
                // results):
                Query childQuery2;

                // apply a filter to children
                Filter childFilter2, childJoinFilter2;

                if (Random.NextBoolean())
                {
                    childQuery2 = parentQuery2;
                    childJoinQuery2 = parentJoinQuery2;
                    childFilter2 = null;
                    childJoinFilter2 = null;
                }
                else
                {
                    Term childTerm = RandomChildTerm(childFields[0]);
                    if (Random.NextBoolean()) // filtered case
                    {
                        childJoinQuery2 = parentJoinQuery2;
                        Filter f = new QueryWrapperFilter(new TermQuery(childTerm));
                        childJoinFilter2 = Random.NextBoolean() ? new FixedBitSetCachingWrapperFilter(f) : f;
                    }
                    else
                    {
                        childJoinFilter2 = null;
                        // AND child field w/ parent query:
                        BooleanQuery bq = new BooleanQuery();
                        childJoinQuery2 = bq;
                        if (Random.NextBoolean())
                        {
                            bq.Add(parentJoinQuery2, Occur.MUST);
                            bq.Add(new TermQuery(childTerm), Occur.MUST);
                        }
                        else
                        {
                            bq.Add(new TermQuery(childTerm), Occur.MUST);
                            bq.Add(parentJoinQuery2, Occur.MUST);
                        }
                    }

                    if (Random.NextBoolean()) // filtered case
                    {
                        childQuery2 = parentQuery2;
                        Filter f = new QueryWrapperFilter(new TermQuery(childTerm));
                        childFilter2 = Random.NextBoolean() ? new FixedBitSetCachingWrapperFilter(f) : f;
                    }
                    else
                    {
                        childFilter2 = null;
                        BooleanQuery bq2 = new BooleanQuery();
                        childQuery2 = bq2;
                        if (Random.NextBoolean())
                        {
                            bq2.Add(parentQuery2, Occur.MUST);
                            bq2.Add(new TermQuery(childTerm), Occur.MUST);
                        }
                        else
                        {
                            bq2.Add(new TermQuery(childTerm), Occur.MUST);
                            bq2.Add(parentQuery2, Occur.MUST);
                        }
                    }
                }

                Sort childSort2 = GetRandomSort("child", childFields.Length);

                // Search denormalized index:
                if (Verbose)
                {
                    Console.WriteLine("TEST: run top down query=" + childQuery2 + " filter=" + childFilter2 + " sort=" + childSort2);
                }
                TopDocs results2 = s.Search(childQuery2, childFilter2, r.NumDocs, childSort2);
                if (Verbose)
                {
                    Console.WriteLine("  " + results2.TotalHits + " totalHits:");
                    foreach (ScoreDoc sd in results2.ScoreDocs)
                    {
                        Document doc = s.Doc(sd.Doc);
                        Console.WriteLine("  childID=" + doc.Get("childID") + " parentID=" + doc.Get("parentID") + " docID=" + sd.Doc);
                    }
                }

                // Search join index:
                if (Verbose)
                {
                    Console.WriteLine("TEST: run top down join query=" + childJoinQuery2 +
                        " filter=" + childJoinFilter2 + " sort=" + childSort2);
                }
                TopDocs joinResults2 = joinS.Search(childJoinQuery2, childJoinFilter2, joinR.NumDocs, childSort2);
                if (Verbose)
                {
                    Console.WriteLine("  " + joinResults2.TotalHits + " totalHits:");
                    foreach (ScoreDoc sd in joinResults2.ScoreDocs)
                    {
                        Document doc = joinS.Doc(sd.Doc);
                        Document parentDoc = GetParentDoc(joinR, parentsFilter, sd.Doc);
                        Console.WriteLine("  childID=" + doc.Get("childID") + " parentID=" + parentDoc.Get("parentID") + " docID=" + sd.Doc);
                    }
                }

                CompareChildHits(r, joinR, results2, joinResults2);
            }

            r.Dispose();
            joinR.Dispose();
            dir.Dispose();
            joinDir.Dispose();
        }

        private void CompareChildHits(IndexReader r, IndexReader joinR, TopDocs results, TopDocs joinResults)
        {
            assertEquals(results.TotalHits, joinResults.TotalHits);
            assertEquals(results.ScoreDocs.Length, joinResults.ScoreDocs.Length);
            for (int hitCount = 0; hitCount < results.ScoreDocs.Length; hitCount++)
            {
                ScoreDoc hit = results.ScoreDocs[hitCount];
                ScoreDoc joinHit = joinResults.ScoreDocs[hitCount];
                Document doc1 = r.Document(hit.Doc);
                Document doc2 = joinR.Document(joinHit.Doc);
                assertEquals("hit " + hitCount + " differs",
                    doc1.Get("childID"), doc2.Get("childID"));
                // don't compare scores -- they are expected to differ


                assertTrue(hit is FieldDoc);
                assertTrue(joinHit is FieldDoc);

                FieldDoc hit0 = (FieldDoc)hit;
                FieldDoc joinHit0 = (FieldDoc)joinHit;
                assertArrayEquals(hit0.Fields, joinHit0.Fields);
            }
        }

        private void CompareHits(IndexReader r, IndexReader joinR, TopDocs results, ITopGroups<int> joinResults)
        {
            // results is 'complete'; joinResults is a subset
            int resultUpto = 0;
            int joinGroupUpto = 0;

            ScoreDoc[] hits = results.ScoreDocs;
            IGroupDocs<int>[] groupDocs = joinResults.Groups;

            while (joinGroupUpto < groupDocs.Length)
            {
                IGroupDocs<int> group = groupDocs[joinGroupUpto++];
                ScoreDoc[] groupHits = group.ScoreDocs;
                assertNotNull(group.GroupValue);
                Document parentDoc = joinR.Document(group.GroupValue);
                string parentID = parentDoc.Get("parentID");
                //System.out.println("GROUP groupDoc=" + group.groupDoc + " parent=" + parentDoc);
                assertNotNull(parentID);
                assertTrue(groupHits.Length > 0);
                for (int hitIDX = 0; hitIDX < groupHits.Length; hitIDX++)
                {
                    Document nonJoinHit = r.Document(hits[resultUpto++].Doc);
                    Document joinHit = joinR.Document(groupHits[hitIDX].Doc);
                    assertEquals(parentID, nonJoinHit.Get("parentID"));
                    assertEquals(joinHit.Get("childID"), nonJoinHit.Get("childID"));
                }

                if (joinGroupUpto < groupDocs.Length)
                {
                    // Advance non-join hit to the next parentID:
                    //System.out.println("  next joingroupUpto=" + joinGroupUpto + " gd.Length=" + groupDocs.Length + " parentID=" + parentID);
                    while (true)
                    {
                        assertTrue(resultUpto < hits.Length);
                        if (!parentID.Equals(r.Document(hits[resultUpto].Doc).Get("parentID"), StringComparison.Ordinal))
                        {
                            break;
                        }
                        resultUpto++;
                    }
                }
            }
        }

        [Test]
        public void TestMultiChildTypes()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            IList<Document> docs = new List<Document>();

            docs.Add(MakeJob("java", 2007));
            docs.Add(MakeJob("python", 2010));
            docs.Add(MakeQualification("maths", 1999));
            docs.Add(MakeResume("Lisa", "United Kingdom"));
            w.AddDocuments(docs);

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            // Create a filter that defines "parent" documents in the index - in this case resumes
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            // Define child document criteria (finds an example of relevant work experience)
            BooleanQuery childJobQuery = new BooleanQuery();
            childJobQuery.Add(new BooleanClause(new TermQuery(new Term("skill", "java")), Occur.MUST));
            childJobQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 2006, 2011, true, true), Occur.MUST));

            BooleanQuery childQualificationQuery = new BooleanQuery();
            childQualificationQuery.Add(new BooleanClause(new TermQuery(new Term("qualification", "maths")), Occur.MUST));
            childQualificationQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 1980, 2000, true, true), Occur.MUST));


            // Define parent document criteria (find a resident in the UK)
            Query parentQuery = new TermQuery(new Term("country", "United Kingdom"));

            // Wrap the child document query to 'join' any matches
            // up to corresponding parent:
            ToParentBlockJoinQuery childJobJoinQuery = new ToParentBlockJoinQuery(childJobQuery, parentsFilter, ScoreMode.Avg);
            ToParentBlockJoinQuery childQualificationJoinQuery = new ToParentBlockJoinQuery(childQualificationQuery, parentsFilter, ScoreMode.Avg);

            // Combine the parent and nested child queries into a single query for a candidate
            BooleanQuery fullQuery = new BooleanQuery();
            fullQuery.Add(new BooleanClause(parentQuery, Occur.MUST));
            fullQuery.Add(new BooleanClause(childJobJoinQuery, Occur.MUST));
            fullQuery.Add(new BooleanClause(childQualificationJoinQuery, Occur.MUST));

            // Collects all job and qualification child docs for
            // each resume hit in the top N (sorted by score):
            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(Sort.RELEVANCE, 10, true, false);

            s.Search(fullQuery, c);

            // Examine "Job" children
            ITopGroups<int> jobResults = c.GetTopGroups(childJobJoinQuery, null, 0, 10, 0, true);

            //assertEquals(1, results.totalHitCount);
            assertEquals(1, jobResults.TotalGroupedHitCount);
            assertEquals(1, jobResults.Groups.Length);

            IGroupDocs<int> group = jobResults.Groups[0];
            assertEquals(1, group.TotalHits);

            Document childJobDoc = s.Doc(group.ScoreDocs[0].Doc);
            //System.out.println("  doc=" + group.ScoreDocs[0].Doc);
            assertEquals("java", childJobDoc.Get("skill"));
            assertNotNull(group.GroupValue);
            Document parentDoc = s.Doc(group.GroupValue);
            assertEquals("Lisa", parentDoc.Get("name"));

            // Now Examine qualification children
            ITopGroups<int> qualificationResults = c.GetTopGroups(childQualificationJoinQuery, null, 0, 10, 0, true);

            assertEquals(1, qualificationResults.TotalGroupedHitCount);
            assertEquals(1, qualificationResults.Groups.Length);

            IGroupDocs<int> qGroup = qualificationResults.Groups[0];
            assertEquals(1, qGroup.TotalHits);

            Document childQualificationDoc = s.Doc(qGroup.ScoreDocs[0].Doc);
            assertEquals("maths", childQualificationDoc.Get("qualification"));
            assertNotNull(qGroup.GroupValue);
            parentDoc = s.Doc(qGroup.GroupValue);
            assertEquals("Lisa", parentDoc.Get("name"));

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestAdvanceSingleParentSingleChild()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            Document childDoc = new Document();
            childDoc.Add(NewStringField("child", "1", Field.Store.NO));
            Document parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "1", Field.Store.NO));
            w.AddDocuments(new Document[] { childDoc, parentDoc });
            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);
            Query tq = new TermQuery(new Term("child", "1"));
            Filter parentFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("parent", "1"))));

            ToParentBlockJoinQuery q = new ToParentBlockJoinQuery(tq, parentFilter, ScoreMode.Avg);
            Weight weight = s.CreateNormalizedWeight(q);
            DocIdSetIterator disi = weight.GetScorer(s.IndexReader.Leaves.First(), null);
            assertEquals(1, disi.Advance(1));
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestAdvanceSingleParentNoChild()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(new LogDocMergePolicy()));
            Document parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "1", Field.Store.NO));
            parentDoc.Add(NewStringField("isparent", "yes", Field.Store.NO));
            w.AddDocuments(new Document[] { parentDoc });

            // Add another doc so scorer is not null
            parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "2", Field.Store.NO));
            parentDoc.Add(NewStringField("isparent", "yes", Field.Store.NO));
            Document childDoc = new Document();
            childDoc.Add(NewStringField("child", "2", Field.Store.NO));
            w.AddDocuments(new Document[] { childDoc, parentDoc });

            // Need single seg:
            w.ForceMerge(1);
            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);
            Query tq = new TermQuery(new Term("child", "2"));
            Filter parentFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isparent", "yes"))));

            ToParentBlockJoinQuery q = new ToParentBlockJoinQuery(tq, parentFilter, ScoreMode.Avg);
            Weight weight = s.CreateNormalizedWeight(q);
            DocIdSetIterator disi = weight.GetScorer(s.IndexReader.Leaves.First(), null);
            assertEquals(2, disi.Advance(0));
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestGetTopGroups()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            IList<Document> docs = new List<Document>();
            docs.Add(MakeJob("ruby", 2005));
            docs.Add(MakeJob("java", 2006));
            docs.Add(MakeJob("java", 2010));
            docs.Add(MakeJob("java", 2012));
            docs.Shuffle(Random);
            docs.Add(MakeResume("Frank", "United States"));

            AddSkillless(w);
            w.AddDocuments(docs);
            AddSkillless(w);

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = new IndexSearcher(r);

            // Create a filter that defines "parent" documents in the index - in this case resumes
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("docType", "resume"))));

            // Define child document criteria (finds an example of relevant work experience)
            BooleanQuery childQuery = new BooleanQuery();
            childQuery.Add(new BooleanClause(new TermQuery(new Term("skill", "java")), Occur.MUST));
            childQuery.Add(new BooleanClause(NumericRangeQuery.NewInt32Range("year", 2006, 2011, true, true), Occur.MUST));

            // Wrap the child document query to 'join' any matches
            // up to corresponding parent:
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);

            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(Sort.RELEVANCE, 2, true, true);
            s.Search(childJoinQuery, c);

            //Get all child documents within groups
            ITopGroups<int>[] getTopGroupsResults = new ITopGroups<int>[2];
            getTopGroupsResults[0] = c.GetTopGroups(childJoinQuery, null, 0, 10, 0, true);
            getTopGroupsResults[1] = c.GetTopGroupsWithAllChildDocs(childJoinQuery, null, 0, 0, true);

            foreach (ITopGroups<int> results in getTopGroupsResults)
            {
                assertFalse(float.IsNaN(results.MaxScore));
                assertEquals(2, results.TotalGroupedHitCount);
                assertEquals(1, results.Groups.Length);

                IGroupDocs<int> resultGroup = results.Groups[0];
                assertEquals(2, resultGroup.TotalHits);
                assertFalse(float.IsNaN(resultGroup.Score));
                assertNotNull(resultGroup.GroupValue);
                Document parentDocument = s.Doc(resultGroup.GroupValue);
                assertEquals("Frank", parentDocument.Get("name"));

                assertEquals(2, resultGroup.ScoreDocs.Length); //all matched child documents collected

                foreach (ScoreDoc scoreDoc in resultGroup.ScoreDocs)
                {
                    Document childDoc = s.Doc(scoreDoc.Doc);
                    assertEquals("java", childDoc.Get("skill"));
                    int year = Convert.ToInt32(childDoc.Get("year"), CultureInfo.InvariantCulture);
                    assertTrue(year >= 2006 && year <= 2011);
                }
            }

            //Get part of child documents
            ITopGroups<int> boundedResults = c.GetTopGroups(childJoinQuery, null, 0, 1, 0, true);
            assertFalse(float.IsNaN(boundedResults.MaxScore));
            assertEquals(2, boundedResults.TotalGroupedHitCount);
            assertEquals(1, boundedResults.Groups.Length);

            IGroupDocs<int> group = boundedResults.Groups[0];
            assertEquals(2, group.TotalHits);
            assertFalse(float.IsNaN(group.Score));
            assertNotNull(group.GroupValue);
            Document parentDoc = s.Doc(group.GroupValue);
            assertEquals("Frank", parentDoc.Get("name"));

            assertEquals(1, group.ScoreDocs.Length); //not all matched child documents collected

            foreach (ScoreDoc scoreDoc in group.ScoreDocs)
            {
                Document childDoc = s.Doc(scoreDoc.Doc);
                assertEquals("java", childDoc.Get("skill"));
                int year = Convert.ToInt32(childDoc.Get("year"), CultureInfo.InvariantCulture);
                assertTrue(year >= 2006 && year <= 2011);
            }

            r.Dispose();
            dir.Dispose();
        }

        // LUCENE-4968
        [Test]
        public void TestSometimesParentOnlyMatches()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document parent = new Document();
            parent.Add(new StoredField("parentID", "0"));
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));

            IList<Document> docs = new List<Document>();

            Document child = new Document();
            docs.Add(child);
            child.Add(new StoredField("childID", "0"));
            child.Add(NewTextField("childText", "text", Field.Store.NO));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            docs.Clear();

            parent = new Document();
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));
            parent.Add(new StoredField("parentID", "1"));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            IndexReader r = w.GetReader();
            w.Dispose();

            Query childQuery = new TermQuery(new Term("childText", "text"));
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isParent", "yes"))));
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);
            BooleanQuery parentQuery = new BooleanQuery();
            parentQuery.Add(childJoinQuery, Occur.SHOULD);
            parentQuery.Add(new TermQuery(new Term("parentText", "text")), Occur.SHOULD);

            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(new Sort(new SortField("parentID", SortFieldType.STRING)), 10, true, true);
            NewSearcher(r).Search(parentQuery, c);
            ITopGroups<int> groups = c.GetTopGroups(childJoinQuery, null, 0, 10, 0, false);

            // Two parents:
            assertEquals(2, (int)groups.TotalGroupCount);

            // One child docs:
            assertEquals(1, groups.TotalGroupedHitCount);

            IGroupDocs<int> group = groups.Groups[0];
            Document doc = r.Document((int)group.GroupValue);
            assertEquals("0", doc.Get("parentID"));

            group = groups.Groups[1];
            doc = r.Document((int)group.GroupValue);
            assertEquals("1", doc.Get("parentID"));

            r.Dispose();
            d.Dispose();
        }

        // LUCENE-4968
        [Test]
        public void TestChildQueryNeverMatches()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document parent = new Document();
            parent.Add(new StoredField("parentID", "0"));
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));

            IList<Document> docs = new List<Document>();

            Document child = new Document();
            docs.Add(child);
            child.Add(new StoredField("childID", "0"));
            child.Add(NewTextField("childText", "text", Field.Store.NO));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            docs.Clear();

            parent = new Document();
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));
            parent.Add(new StoredField("parentID", "1"));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            IndexReader r = w.GetReader();
            w.Dispose();

            // never matches:
            Query childQuery = new TermQuery(new Term("childText", "bogus"));
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isParent", "yes"))));
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);
            BooleanQuery parentQuery = new BooleanQuery();
            parentQuery.Add(childJoinQuery, Occur.SHOULD);
            parentQuery.Add(new TermQuery(new Term("parentText", "text")), Occur.SHOULD);

            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(new Sort(new SortField("parentID", SortFieldType.STRING)), 10, true, true);
            NewSearcher(r).Search(parentQuery, c);
            ITopGroups<int> groups = c.GetTopGroups(childJoinQuery, null, 0, 10, 0, false);

            // Two parents:
            assertEquals(2, (int)groups.TotalGroupCount);

            // One child docs:
            assertEquals(0, groups.TotalGroupedHitCount);

            IGroupDocs<int> group = groups.Groups[0];
            Document doc = r.Document((int)group.GroupValue);
            assertEquals("0", doc.Get("parentID"));

            group = groups.Groups[1];
            doc = r.Document((int)group.GroupValue);
            assertEquals("1", doc.Get("parentID"));

            r.Dispose();
            d.Dispose();
        }

        // LUCENE-4968
        [Test]
        public void TestChildQueryMatchesParent()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            Document parent = new Document();
            parent.Add(new StoredField("parentID", "0"));
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));

            IList<Document> docs = new List<Document>();

            Document child = new Document();
            docs.Add(child);
            child.Add(new StoredField("childID", "0"));
            child.Add(NewTextField("childText", "text", Field.Store.NO));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            docs.Clear();

            parent = new Document();
            parent.Add(NewTextField("parentText", "text", Field.Store.NO));
            parent.Add(NewStringField("isParent", "yes", Field.Store.NO));
            parent.Add(new StoredField("parentID", "1"));

            // parent last:
            docs.Add(parent);
            w.AddDocuments(docs);

            IndexReader r = w.GetReader();
            w.Dispose();

            // illegally matches parent:
            Query childQuery = new TermQuery(new Term("parentText", "text"));
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isParent", "yes"))));
            ToParentBlockJoinQuery childJoinQuery = new ToParentBlockJoinQuery(childQuery, parentsFilter, ScoreMode.Avg);
            BooleanQuery parentQuery = new BooleanQuery();
            parentQuery.Add(childJoinQuery, Occur.SHOULD);
            parentQuery.Add(new TermQuery(new Term("parentText", "text")), Occur.SHOULD);

            ToParentBlockJoinCollector c = new ToParentBlockJoinCollector(new Sort(new SortField("parentID", SortFieldType.STRING)), 10, true, true);

            try
            {
                NewSearcher(r).Search(parentQuery, c);
                fail("should have hit exception");
            }
            catch (Exception ise) when (ise.IsIllegalStateException())
            {
                // expected
            }

            r.Dispose();
            d.Dispose();
        }

        [Test]
        public void TestAdvanceSingleDeletedParentNoChild()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            // First doc with 1 children
            Document parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "1", Field.Store.NO));
            parentDoc.Add(NewStringField("isparent", "yes", Field.Store.NO));
            Document childDoc = new Document();
            childDoc.Add(NewStringField("child", "1", Field.Store.NO));
            w.AddDocuments(new Document[] { childDoc, parentDoc });

            parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "2", Field.Store.NO));
            parentDoc.Add(NewStringField("isparent", "yes", Field.Store.NO));
            w.AddDocuments(new Document[] { parentDoc });

            w.DeleteDocuments(new Term("parent", "2"));

            parentDoc = new Document();
            parentDoc.Add(NewStringField("parent", "2", Field.Store.NO));
            parentDoc.Add(NewStringField("isparent", "yes", Field.Store.NO));
            childDoc = new Document();
            childDoc.Add(NewStringField("child", "2", Field.Store.NO));
            w.AddDocuments(new Document[] { childDoc, parentDoc });

            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = NewSearcher(r);

            // Create a filter that defines "parent" documents in the index - in this case resumes
            Filter parentsFilter = new FixedBitSetCachingWrapperFilter(new QueryWrapperFilter(new TermQuery(new Term("isparent", "yes"))));

            Query parentQuery = new TermQuery(new Term("parent", "2"));

            ToChildBlockJoinQuery parentJoinQuery = new ToChildBlockJoinQuery(parentQuery, parentsFilter, Random.NextBoolean());
            TopDocs topdocs = s.Search(parentJoinQuery, 3);
            assertEquals(1, topdocs.TotalHits);

            r.Dispose();
            dir.Dispose();
        }
    }
}