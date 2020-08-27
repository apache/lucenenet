using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Search.Spell
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
    /// Test case for LuceneDictionary.
    /// It first creates a simple index and then a couple of instances of LuceneDictionary
    /// on different fields and checks if all the right text comes back.
    /// </summary>
    public class TestLuceneDictionary : LuceneTestCase
    {
        private Directory store;

        private IndexReader indexReader = null;
        private LuceneDictionary ld;
#pragma warning disable CS0618 // Type or member is obsolete
        private IBytesRefIterator it;
#pragma warning restore CS0618 // Type or member is obsolete
        private BytesRef spare = new BytesRef();


        public override void SetUp()
        {
            base.SetUp();
            store = NewDirectory();
            IndexWriter writer = new IndexWriter(store, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));

            Document doc;

            doc = new Document();
            doc.Add(NewTextField("aaa", "foo", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("aaa", "foo", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("contents", "Tom", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("contents", "Jerry", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(NewTextField("zzz", "bar", Field.Store.YES));
            writer.AddDocument(doc);

            writer.ForceMerge(1);
            writer.Dispose();
        }

        public override void TearDown()
        {
            if (indexReader != null)
                indexReader.Dispose();
            store.Dispose();
            base.TearDown();
        }

        [Test]
        public void TestFieldNonExistent()
        {
            IBytesRefEnumerator it;
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "nonexistent_field");
                it = ld.GetEntryEnumerator();

                assertFalse("More elements than expected", it.MoveNext());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        [Obsolete("This will be removed in 4.8.0 release candidate.")]
        public void TestFieldNonExistentIterator()
        {
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "nonexistent_field");
                it = ld.GetEntryIterator();

                assertNull("More elements than expected", spare = it.Next());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        public void TestFieldAaa()
        {
            IBytesRefEnumerator it;
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "aaa");
                it = ld.GetEntryEnumerator();
                assertTrue("First element doesn't exist.", it.MoveNext());
                assertTrue("First element isn't correct", it.Current.Utf8ToString().Equals("foo", StringComparison.Ordinal));
                assertFalse("More elements than expected", it.MoveNext());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        [Obsolete("This will be removed in 4.8.0 release candidate.")]
        public void TestFieldAaaIterator()
        {
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "aaa");
                it = ld.GetEntryIterator();
                assertNotNull("First element doesn't exist.", spare = it.Next());
                assertTrue("First element isn't correct", spare.Utf8ToString().Equals("foo", StringComparison.Ordinal));
                assertNull("More elements than expected", it.Next());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        public void TestFieldContents_1()
        {
            IBytesRefEnumerator it;
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryEnumerator();

                assertTrue("First element doesn't exist.", it.MoveNext());
                assertTrue("First element isn't correct", it.Current.Utf8ToString().Equals("Jerry", StringComparison.Ordinal));
                assertTrue("Second element doesn't exist.", it.MoveNext());
                assertTrue("Second element isn't correct", it.Current.Utf8ToString().Equals("Tom", StringComparison.Ordinal));
                assertFalse("More elements than expected", it.MoveNext());

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryEnumerator();

                int counter = 2;
                while (it.MoveNext())
                {
                    counter--;
                }

                assertTrue("Number of words incorrect", counter == 0);
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        [Obsolete("This will be removed in 4.8.0 release candidate.")]
        public void TestFieldContents_1_Iterator()
        {
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryIterator();

                assertNotNull("First element doesn't exist.", spare = it.Next());
                assertTrue("First element isn't correct", spare.Utf8ToString().Equals("Jerry", StringComparison.Ordinal));
                assertNotNull("Second element doesn't exist.", spare = it.Next());
                assertTrue("Second element isn't correct", spare.Utf8ToString().Equals("Tom", StringComparison.Ordinal));
                assertNull("More elements than expected", it.Next());

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryIterator();

                int counter = 2;
                while (it.Next() != null)
                {
                    counter--;
                }

                assertTrue("Number of words incorrect", counter == 0);
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        public void TestFieldContents_2()
        {
            IBytesRefEnumerator it;
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryEnumerator();

                // just iterate through words
                assertTrue(it.MoveNext());
                assertEquals("First element isn't correct", "Jerry", it.Current.Utf8ToString());
                assertTrue(it.MoveNext());
                assertEquals("Second element isn't correct", "Tom", it.Current.Utf8ToString());
                assertFalse("Nonexistent element is really null", it.MoveNext());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        [Obsolete("This will be removed in 4.8.0 release candidate.")]
        public void TestFieldContents_2_Iterator()
        {
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetEntryIterator();

                // just iterate through words
                assertEquals("First element isn't correct", "Jerry", it.Next().Utf8ToString());
                assertEquals("Second element isn't correct", "Tom", it.Next().Utf8ToString());
                assertNull("Nonexistent element is really null", it.Next());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        public void TestFieldZzz()
        {
            IBytesRefEnumerator it;
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "zzz");
                it = ld.GetEntryEnumerator();

                assertTrue("First element doesn't exist.", it.MoveNext());
                assertEquals("First element isn't correct", "bar", it.Current.Utf8ToString());
                assertFalse("More elements than expected", it.MoveNext());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        [Obsolete("This will be removed in 4.8.0 release candidate.")]
        public void TestFieldZzzIterator()
        {
            try
            {
                indexReader = DirectoryReader.Open(store);

                ld = new LuceneDictionary(indexReader, "zzz");
                it = ld.GetEntryIterator();

                assertNotNull("First element doesn't exist.", spare = it.Next());
                assertEquals("First element isn't correct", "bar", spare.Utf8ToString());
                assertNull("More elements than expected", it.Next());
            }
            finally
            {
                if (indexReader != null) { indexReader.Dispose(); }
            }
        }

        [Test]
        public void TestSpellchecker()
        {
            Directory dir = NewDirectory();
            SpellChecker sc = new SpellChecker(dir);
            indexReader = DirectoryReader.Open(store);
            sc.IndexDictionary(new LuceneDictionary(indexReader, "contents"), NewIndexWriterConfig(TEST_VERSION_CURRENT, null), false);
            string[] suggestions = sc.SuggestSimilar("Tam", 1);
            assertEquals(1, suggestions.Length);
            assertEquals("Tom", suggestions[0]);
            suggestions = sc.SuggestSimilar("Jarry", 1);
            assertEquals(1, suggestions.Length);
            assertEquals("Jerry", suggestions[0]);
            indexReader.Dispose();
            sc.Dispose();
            dir.Dispose();
        }
    }
}
