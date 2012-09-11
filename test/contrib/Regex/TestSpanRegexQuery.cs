/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;
using NUnit.Framework;

namespace Contrib.Regex.Test
{
    [TestFixture]
    public class TestSpanRegexQuery : TestCase
    {
        Directory indexStoreA = new RAMDirectory();

        Directory indexStoreB = new RAMDirectory();

        [Test]
        public void TestSpanRegex()
        {
            RAMDirectory directory = new RAMDirectory();
            IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
            Document doc = new Document();
            // doc.Add(new Field("field", "the quick brown fox jumps over the lazy dog",
            // Field.Store.NO, Field.Index.ANALYZED));
            // writer.AddDocument(doc);
            // doc = new Document();
            doc.Add(new Field("field", "auto update", Field.Store.NO,
                Field.Index.ANALYZED));
            writer.AddDocument(doc);
            doc = new Document();
            doc.Add(new Field("field", "first auto update", Field.Store.NO,
                Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Optimize();
            writer.Close();

            IndexSearcher searcher = new IndexSearcher(directory, true);
            SpanRegexQuery srq = new SpanRegexQuery(new Term("field", "aut.*"));
            SpanFirstQuery sfq = new SpanFirstQuery(srq, 1);
            // SpanNearQuery query = new SpanNearQuery(new SpanQuery[] {srq, stq}, 6,
            // true);
            int numHits = searcher.Search(sfq, null, 1000).TotalHits;
            Assert.AreEqual(1, numHits);
        }

        [Test]
        public void TestSpanRegexBug()
        {
            CreateRamDirectories();

            SpanRegexQuery srq = new SpanRegexQuery(new Term("field", "a.*"));
            SpanRegexQuery stq = new SpanRegexQuery(new Term("field", "b.*"));
            SpanNearQuery query = new SpanNearQuery(new SpanQuery[] { srq, stq }, 6,
                true);

            // 1. Search the same store which works
            IndexSearcher[] arrSearcher = new IndexSearcher[2];
            arrSearcher[0] = new IndexSearcher(indexStoreA, true);
            arrSearcher[1] = new IndexSearcher(indexStoreB, true);
            MultiSearcher searcher = new MultiSearcher(arrSearcher);
            int numHits = searcher.Search(query, null, 1000).TotalHits;
            arrSearcher[0].Close();
            arrSearcher[1].Close();

            // Will fail here
            // We expect 2 but only one matched
            // The rewriter function only write it once on the first IndexSearcher
            // So it's using term: a1 b1 to search on the second IndexSearcher
            // As a result, it won't match the document in the second IndexSearcher
            Assert.AreEqual(2, numHits);
            indexStoreA.Close();
            indexStoreB.Close();
        }

        private void CreateRamDirectories()
        {
            // creating a document to store
            Document lDoc = new Document();
            lDoc.Add(new Field("field", "a1 b1", Field.Store.NO,
                Field.Index.ANALYZED_NO_NORMS));

            // creating a document to store
            Document lDoc2 = new Document();
            lDoc2.Add(new Field("field", "a2 b2", Field.Store.NO,
                Field.Index.ANALYZED_NO_NORMS));

            // creating first index writer
            IndexWriter writerA = new IndexWriter(indexStoreA, new StandardAnalyzer(Version.LUCENE_CURRENT),
                true, IndexWriter.MaxFieldLength.LIMITED);
            writerA.AddDocument(lDoc);
            writerA.Optimize();
            writerA.Close();

            // creating second index writer
            IndexWriter writerB = new IndexWriter(indexStoreB, new StandardAnalyzer(Version.LUCENE_CURRENT),
                true, IndexWriter.MaxFieldLength.LIMITED);
            writerB.AddDocument(lDoc2);
            writerB.Optimize();
            writerB.Close();
        }
    }
}
