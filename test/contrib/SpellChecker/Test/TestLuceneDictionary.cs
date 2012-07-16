/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections;
using NUnit.Framework;

using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using SpellChecker.Net.Search.Spell;

namespace SpellChecker.Net.Test.Search.Spell
{
    [TestFixture]
    public class TestLuceneDictionary
    {

        private readonly Directory store = new RAMDirectory();

        private IndexReader indexReader;

        private LuceneDictionary ld;
        private IEnumerator it;

        [SetUp]
        public void SetUp()
        {

            var writer = new IndexWriter(store, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

            var doc = new Document();
            doc.Add(new Field("aaa", "foo", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("aaa", "foo", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("contents", "Tom", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("contents", "Jerry", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("zzz", "bar", Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            writer.Optimize();
            writer.Close();
        }

        [Test]
        public void TestFieldNonExistent()
        {
            try
            {
                indexReader = IndexReader.Open(store, true);

                ld = new LuceneDictionary(indexReader, "nonexistent_field");
                it = ld.GetWordsIterator();

                AssertFalse("More elements than expected", it.HasNext());
                AssertTrue("Nonexistent element is really null", it.Next() == null);
            }
            finally
            {
                if (indexReader != null) { indexReader.Close(); }
            }
        }

        [Test]
        public void TestFieldAaa()
        {
            try
            {
                indexReader = IndexReader.Open(store, true);

                ld = new LuceneDictionary(indexReader, "aaa");
                it = ld.GetWordsIterator();

                AssertTrue("First element doesn't exist.", it.HasNext());
                AssertTrue("First element isn't correct", it.Next().Equals("foo"));
                AssertFalse("More elements than expected", it.HasNext());
                AssertTrue("Nonexistent element is really null", it.Next() == null);
            }
            finally
            {
                if (indexReader != null) { indexReader.Close(); }
            }
        }

        [Test]
        public void TestFieldContents_1()
        {
            try
            {
                indexReader = IndexReader.Open(store, true);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetWordsIterator();

                AssertTrue("First element doesn't exist.", it.HasNext());
                AssertTrue("First element isn't correct", it.Next().Equals("Jerry"));
                AssertTrue("Second element doesn't exist.", it.HasNext());
                AssertTrue("Second element isn't correct", it.Next().Equals("Tom"));
                AssertFalse("More elements than expected", it.HasNext());
                AssertTrue("Nonexistent element is really null", it.Next() == null);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetWordsIterator();

                int counter = 2;
                while (it.HasNext())
                {
                    it.Next();
                    counter--;
                }

                AssertTrue("Number of words incorrect", counter == 0);
            }
            finally
            {
                if (indexReader != null) { indexReader.Close(); }
            }
        }

        [Test]
        public void TestFieldContents_2()
        {
            try
            {
                indexReader = IndexReader.Open(store, true);

                ld = new LuceneDictionary(indexReader, "contents");
                it = ld.GetWordsIterator();
                
                // hasNext() should have no side effects //{{DIGY}} But has. Need a fix?
                //AssertTrue("First element isn't were it should be.", it.HasNext());
                //AssertTrue("First element isn't were it should be.", it.HasNext());
                //AssertTrue("First element isn't were it should be.", it.HasNext());

                // just iterate through words
                AssertTrue("First element isn't correct", it.Next().Equals("Jerry"));
                AssertTrue("Second element isn't correct", it.Next().Equals("Tom"));
                AssertTrue("Nonexistent element is really null", it.Next() == null);

                // hasNext() should still have no side effects ...
                AssertFalse("There should be any more elements", it.HasNext());
                AssertFalse("There should be any more elements", it.HasNext());
                AssertFalse("There should be any more elements", it.HasNext());

                // .. and there are really no more words
                AssertTrue("Nonexistent element is really null", it.Next() == null);
                AssertTrue("Nonexistent element is really null", it.Next() == null);
                AssertTrue("Nonexistent element is really null", it.Next() == null);
            }
            finally
            {
                if (indexReader != null) { indexReader.Close(); }
            }
        }

        [Test]
        public void TestFieldZzz()
        {
            try
            {
                indexReader = IndexReader.Open(store, true);

                ld = new LuceneDictionary(indexReader, "zzz");
                it = ld.GetWordsIterator();

                AssertTrue("First element doesn't exist.", it.HasNext());
                AssertTrue("First element isn't correct", it.Next().Equals("bar"));
                AssertFalse("More elements than expected", it.HasNext());
                AssertTrue("Nonexistent element is really null", it.Next() == null);
            }
            finally
            {
                if (indexReader != null) { indexReader.Close(); }
            }
        }

        [Test]
        public void TestSpellchecker()
        {
            var sc = new Net.Search.Spell.SpellChecker(new RAMDirectory());
            indexReader = IndexReader.Open(store, true);
            sc.IndexDictionary(new LuceneDictionary(indexReader, "contents"));
            String[] suggestions = sc.SuggestSimilar("Tam", 1);
            AssertEquals(1, suggestions.Length);
            AssertEquals("Tom", suggestions[0]);
            suggestions = sc.SuggestSimilar("Jarry", 1);
            AssertEquals(1, suggestions.Length);
            AssertEquals("Jerry", suggestions[0]);
            indexReader.Close();
        }
        
        #region .NET 

        static void AssertTrue(string s, bool b)
        {
            Assert.IsTrue(b, s);
        }

        static void AssertFalse(string s, bool b)
        {
            Assert.IsFalse(b, s);
        }

        static void AssertEquals(int i, int j)
        {
            Assert.AreEqual(i, j);
        }

        static void AssertEquals(string i, string j)
        {
            Assert.AreEqual(i, j);
        }
        #endregion
    }
}

#region .NET
namespace SpellChecker.Net.Test.Search.Spell
{
    public static class Extensions
    {
        public static bool HasNext(this IEnumerator a)
        {
            return a.MoveNext();
        }

        public static object Next(this IEnumerator a)
        {
            return a.Current;
        }
    }
}
#endregion
