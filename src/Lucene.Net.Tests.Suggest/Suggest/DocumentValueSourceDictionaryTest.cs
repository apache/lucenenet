using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Search.Suggest
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

    public class DocumentValueSourceDictionaryTest : LuceneTestCase
    {
        static readonly string FIELD_NAME = "f1";
        static readonly string WEIGHT_FIELD_NAME_1 = "w1";
        static readonly string WEIGHT_FIELD_NAME_2 = "w2";
        static readonly string WEIGHT_FIELD_NAME_3 = "w3";
        static readonly string PAYLOAD_FIELD_NAME = "p1";
        static readonly string CONTEXTS_FIELD_NAME = "c1";

        private IDictionary<string, Document> GenerateIndexDocuments(int ndocs)
        {
            IDictionary<string, Document> docs = new HashMap<string, Document>();
            for (int i = 0; i < ndocs; i++)
            {
                Field field = new TextField(FIELD_NAME, "field_" + i, Field.Store.YES);
                Field payload = new StoredField(PAYLOAD_FIELD_NAME, new BytesRef("payload_" + i));
                Field weight1 = new NumericDocValuesField(WEIGHT_FIELD_NAME_1, 10 + i);
                Field weight2 = new NumericDocValuesField(WEIGHT_FIELD_NAME_2, 20 + i);
                Field weight3 = new NumericDocValuesField(WEIGHT_FIELD_NAME_3, 30 + i);
                Field contexts = new StoredField(CONTEXTS_FIELD_NAME, new BytesRef("ctx_" + i + "_0"));
                Document doc = new Document();
                doc.Add(field);
                doc.Add(payload);
                doc.Add(weight1);
                doc.Add(weight2);
                doc.Add(weight3);
                doc.Add(contexts);
                for (int j = 1; j < AtLeast(3); j++)
                {
                    contexts.SetBytesValue(new BytesRef("ctx_" + i + "_" + j));
                    doc.Add(contexts);
                }
                docs.Put(field.GetStringValue(), doc);
            }
            return docs;
        }

        [Test]
        public void TestEmptyReader()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            // Make sure the index is created?
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            writer.Commit();
            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new DoubleConstValueSource(10), PAYLOAD_FIELD_NAME);
            IInputIterator inputIterator = dictionary.EntryIterator;

            assertNull(inputIterator.Next());
            assertEquals(inputIterator.Weight, 0);
            assertNull(inputIterator.Payload);

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBasic()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new LongFieldSource(WEIGHT_FIELD_NAME_1), new LongFieldSource(WEIGHT_FIELD_NAME_2), new LongFieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumFloatFunction(toAdd), PAYLOAD_FIELD_NAME);
            IInputIterator inputIterator = dictionary.EntryIterator;
            BytesRef f;
            while ((f = inputIterator.Next()) != null)
            {
                string field = f.Utf8ToString();
                Document doc = docs.ContainsKey(field) ? docs[field] : null;
                docs.Remove(field);
                //Document doc = docs.remove(f.utf8ToString());
                long w1 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_1).GetNumericValue());
                long w2 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_2).GetNumericValue());
                long w3 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_3).GetNumericValue());
                assertTrue(f.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(!docs.Any());
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithContext()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new LongFieldSource(WEIGHT_FIELD_NAME_1), new LongFieldSource(WEIGHT_FIELD_NAME_2), new LongFieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumFloatFunction(toAdd), PAYLOAD_FIELD_NAME, CONTEXTS_FIELD_NAME);
            IInputIterator inputIterator = dictionary.EntryIterator;
            BytesRef f;
            while ((f = inputIterator.Next()) != null)
            {
                string field = f.Utf8ToString();
                Document doc = docs.ContainsKey(field) ? docs[field] : null;
                docs.Remove(field);
                long w1 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_1).GetNumericValue());
                long w2 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_2).GetNumericValue());
                long w3 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_3).GetNumericValue());
                assertTrue(f.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
                ISet<BytesRef> originalCtxs = new HashSet<BytesRef>();
                foreach (IndexableField ctxf in doc.GetFields(CONTEXTS_FIELD_NAME))
                {
                    originalCtxs.add(ctxf.GetBinaryValue());
                }
                assertEquals(originalCtxs, inputIterator.Contexts);
            }
            assertTrue(!docs.Any());
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithoutPayload()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new LongFieldSource(WEIGHT_FIELD_NAME_1), new LongFieldSource(WEIGHT_FIELD_NAME_2), new LongFieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumFloatFunction(toAdd));
            IInputIterator inputIterator = dictionary.EntryIterator;
            BytesRef f;
            while ((f = inputIterator.Next()) != null)
            {
                string field = f.Utf8ToString();
                Document doc = docs.ContainsKey(field) ? docs[field] : null;
                docs.Remove(field);
                long w1 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_1).GetNumericValue());
                long w2 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_2).GetNumericValue());
                long w3 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_3).GetNumericValue());
                assertTrue(f.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertEquals(inputIterator.Payload, null);
            }
            assertTrue(!docs.Any());
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithDeletions()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            Random rand = Random();
            List<string> termsToDel = new List<string>();
            foreach (Document doc in docs.Values)
            {
                if (rand.nextBoolean() && termsToDel.size() < docs.size() - 1)
                {
                    termsToDel.Add(doc.Get(FIELD_NAME));
                }
                writer.AddDocument(doc);
            }
            writer.Commit();

            Term[] delTerms = new Term[termsToDel.size()];
            for (int i = 0; i < termsToDel.size(); i++)
            {
                delTerms[i] = new Term(FIELD_NAME, termsToDel[i]);
            }

            foreach (Term delTerm in delTerms)
            {
                writer.DeleteDocuments(delTerm);
            }
            writer.Commit();
            writer.Dispose();

            foreach (string termToDel in termsToDel)
            {
                var toDel = docs[termToDel];
                docs.Remove(termToDel);
                assertTrue(null != toDel);
            }

            IndexReader ir = DirectoryReader.Open(dir);
            assertTrue("NumDocs should be > 0 but was " + ir.NumDocs, ir.NumDocs > 0);
            assertEquals(ir.NumDocs, docs.size());
            ValueSource[] toAdd = new ValueSource[] { new LongFieldSource(WEIGHT_FIELD_NAME_1), new LongFieldSource(WEIGHT_FIELD_NAME_2) };

            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumFloatFunction(toAdd), PAYLOAD_FIELD_NAME);
            IInputIterator inputIterator = dictionary.EntryIterator;
            BytesRef f;
            while ((f = inputIterator.Next()) != null)
            {
                string field = f.Utf8ToString();
                Document doc = docs.ContainsKey(field) ? docs[field] : null;
                docs.Remove(field);
                long w1 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_1).GetNumericValue());
                long w2 = Convert.ToInt64(doc.GetField(WEIGHT_FIELD_NAME_2).GetNumericValue());
                assertTrue(f.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, w2 + w1);
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(!docs.Any());
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithValueSource()
        {

            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new DoubleConstValueSource(10), PAYLOAD_FIELD_NAME);
            IInputIterator inputIterator = dictionary.EntryIterator;
            BytesRef f;
            while ((f = inputIterator.Next()) != null)
            {
                string field = f.Utf8ToString();
                Document doc = docs.ContainsKey(field) ? docs[field] : null;
                docs.Remove(field);
                assertTrue(f.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, 10);
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(!docs.Any());
            ir.Dispose();
            dir.Dispose();
        }
    }
}
