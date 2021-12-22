using NUnit.Framework;
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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestNGramPhraseQuery : LuceneTestCase
    {
        private static IndexReader reader;
        private static Directory directory;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because Similarity and TimeZone are not static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory);
            writer.Dispose();
            reader = DirectoryReader.Open(directory);
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader.Dispose();
            reader = null;
            directory.Dispose();
            directory = null;
            base.AfterClass();
        }

        [Test]
        public virtual void TestRewrite()
        {
            // bi-gram test ABC => AB/BC => AB/BC
            PhraseQuery pq1 = new NGramPhraseQuery(2);
            pq1.Add(new Term("f", "AB"));
            pq1.Add(new Term("f", "BC"));

            Query q = pq1.Rewrite(reader);
            Assert.IsTrue(q is NGramPhraseQuery);
            Assert.AreSame(pq1, q);
            pq1 = (NGramPhraseQuery)q;
            Assert.AreEqual(new Term[] { new Term("f", "AB"), new Term("f", "BC") }, pq1.GetTerms());
            Assert.AreEqual(new int[] { 0, 1 }, pq1.GetPositions());

            // bi-gram test ABCD => AB/BC/CD => AB//CD
            PhraseQuery pq2 = new NGramPhraseQuery(2);
            pq2.Add(new Term("f", "AB"));
            pq2.Add(new Term("f", "BC"));
            pq2.Add(new Term("f", "CD"));

            q = pq2.Rewrite(reader);
            Assert.IsTrue(q is PhraseQuery);
            Assert.AreNotSame(pq2, q);
            pq2 = (PhraseQuery)q;
            Assert.AreEqual(new Term[] { new Term("f", "AB"), new Term("f", "CD") }, pq2.GetTerms());
            Assert.AreEqual(new int[] { 0, 2 }, pq2.GetPositions());

            // tri-gram test ABCDEFGH => ABC/BCD/CDE/DEF/EFG/FGH => ABC///DEF//FGH
            PhraseQuery pq3 = new NGramPhraseQuery(3);
            pq3.Add(new Term("f", "ABC"));
            pq3.Add(new Term("f", "BCD"));
            pq3.Add(new Term("f", "CDE"));
            pq3.Add(new Term("f", "DEF"));
            pq3.Add(new Term("f", "EFG"));
            pq3.Add(new Term("f", "FGH"));

            q = pq3.Rewrite(reader);
            Assert.IsTrue(q is PhraseQuery);
            Assert.AreNotSame(pq3, q);
            pq3 = (PhraseQuery)q;
            Assert.AreEqual(new Term[] { new Term("f", "ABC"), new Term("f", "DEF"), new Term("f", "FGH") }, pq3.GetTerms());
            Assert.AreEqual(new int[] { 0, 3, 5 }, pq3.GetPositions());

            // LUCENE-4970: boosting test
            PhraseQuery pq4 = new NGramPhraseQuery(2);
            pq4.Add(new Term("f", "AB"));
            pq4.Add(new Term("f", "BC"));
            pq4.Add(new Term("f", "CD"));
            pq4.Boost = 100.0F;

            q = pq4.Rewrite(reader);
            Assert.AreNotSame(pq4, q);
            Assert.AreEqual(pq4.Boost, q.Boost, 0.1f);
        }
    }
}