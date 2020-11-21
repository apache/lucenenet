using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System.Diagnostics.CodeAnalysis;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;

    [TestFixture]
    public class FuzzyTermOnShortTermsTest : LuceneTestCase
    {
        private const string FIELD = "field";

        [Test]
        public virtual void Test()
        {
            // proves rule that edit distance between the two terms
            // must be > smaller term for there to be a match
            Analyzer a = Analyzer;
            //these work
            CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "ab"), 1), 1);
            CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "abc"), 1), 1);

            CountHits(a, new string[] { "abcde" }, new FuzzyQuery(new Term(FIELD, "abc"), 2), 1);
            CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "abcde"), 2), 1);

            //these don't
            CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "a"), 1), 0);
            CountHits(a, new string[] { "a" }, new FuzzyQuery(new Term(FIELD, "ab"), 1), 0);

            CountHits(a, new string[] { "abc" }, new FuzzyQuery(new Term(FIELD, "a"), 2), 0);
            CountHits(a, new string[] { "a" }, new FuzzyQuery(new Term(FIELD, "abc"), 2), 0);

            CountHits(a, new string[] { "abcd" }, new FuzzyQuery(new Term(FIELD, "ab"), 2), 0);
            CountHits(a, new string[] { "ab" }, new FuzzyQuery(new Term(FIELD, "abcd"), 2), 0);
        }

        private void CountHits(Analyzer analyzer, string[] docs, Query q, int expected)
        {
            Directory d = GetDirectory(analyzer, docs);
            IndexReader r = DirectoryReader.Open(d);
            IndexSearcher s = new IndexSearcher(r);
            TotalHitCountCollector c = new TotalHitCountCollector();
            s.Search(q, c);
            Assert.AreEqual(expected, c.TotalHits, q.ToString());
            r.Dispose();
            d.Dispose();
        }

        [SuppressMessage("Style", "IDE0025:Use expression body for properties", Justification = "Multiple lines")]
        public static Analyzer Analyzer
        {
            get
            {
                return Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                    return new TokenStreamComponents(tokenizer, tokenizer);
                });
            }
        }

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        public Directory GetDirectory(Analyzer analyzer, string[] vals)
        {
            Directory directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 100, 1000)).SetMergePolicy(NewLogMergePolicy()));

            foreach (string s in vals)
            {
                Document d = new Document();
                d.Add(NewTextField(FIELD, s, Field.Store.YES));
                writer.AddDocument(d);
            }
            writer.Dispose();
            return directory;
        }
    }
}