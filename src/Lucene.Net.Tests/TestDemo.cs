using Lucene.Net.Search;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Field = Lucene.Net.Documents.Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A very simple demo used in the API documentation (src/java/overview.html).
    /// 
    /// Please try to keep src/java/overview.html up-to-date when making changes
    /// to this class.
    /// </summary>
    public class TestDemo_ : LuceneTestCase
    {

        [Test]
        public virtual void TestDemo()
        {
            Analyzer analyzer = new MockAnalyzer(Random);

            // Store the index in memory:
            using Directory directory = NewDirectory();
            string longTerm = "longtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongtermlongterm";
            string text = "this is the text to be indexed. " + longTerm;

            // To store an index on disk, use this instead:
            // Directory directory = FSDirectory.open(new File("/tmp/testindex"));
            using (RandomIndexWriter iwriter = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(Random, TEST_VERSION_CURRENT, analyzer)))
            {
                Documents.Document doc = new Documents.Document();
                doc.Add(NewTextField("fieldname", text, Field.Store.YES));
                iwriter.AddDocument(doc);
            }

            // Now search the index:
            using IndexReader ireader = DirectoryReader.Open(directory);
            IndexSearcher isearcher = NewSearcher(ireader);

            Assert.AreEqual(1, isearcher.Search(new TermQuery(new Term("fieldname", longTerm)), 1).TotalHits);
            Query query = new TermQuery(new Term("fieldname", "text"));
            TopDocs hits = isearcher.Search(query, null, 1);
            Assert.AreEqual(1, hits.TotalHits);
            // Iterate through the results:
            for (int i = 0; i < hits.ScoreDocs.Length; i++)
            {
                Documents.Document hitDoc = isearcher.Doc(hits.ScoreDocs[i].Doc);
                Assert.AreEqual(text, hitDoc.Get("fieldname"));
            }

            // Test simple phrase query
            PhraseQuery phraseQuery = new PhraseQuery();
            phraseQuery.Add(new Term("fieldname", "to"));
            phraseQuery.Add(new Term("fieldname", "be"));
            Assert.AreEqual(1, isearcher.Search(phraseQuery, null, 1).TotalHits);
        }
    }
}