using Lucene.Net.Analysis;
using Lucene.Net.Attributes;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    [SuppressCodecs("Lucene3x")]
    public class DocumentValueSourceDictionaryTest : LuceneTestCase
    {
        const string FIELD_NAME = "f1";
        const string WEIGHT_FIELD_NAME_1 = "w1";
        const string WEIGHT_FIELD_NAME_2 = "w2";
        const string WEIGHT_FIELD_NAME_3 = "w3";
        const string PAYLOAD_FIELD_NAME = "p1";
        const string CONTEXTS_FIELD_NAME = "c1";

        private IDictionary<string, Document> GenerateIndexDocuments(int ndocs)
        {
            IDictionary<string, Document> docs = new JCG.Dictionary<string, Document>();
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
                docs[field.GetStringValue()] = doc;
            }
            return docs;
        }

        [Test]
        public void TestEmptyReader()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            // Make sure the index is created?
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            writer.Commit();
            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new DoubleConstValueSource(10), PAYLOAD_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();

            assertFalse(inputIterator.MoveNext());
            assertEquals(inputIterator.Weight, 0);
            assertNull(inputIterator.Payload);

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestBasic()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new Int64FieldSource(WEIGHT_FIELD_NAME_1), new Int64FieldSource(WEIGHT_FIELD_NAME_2), new Int64FieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumSingleFunction(toAdd), PAYLOAD_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                //Document doc = docs.remove(f.utf8ToString());
                long w1 = doc.GetField(WEIGHT_FIELD_NAME_1).GetInt64ValueOrDefault();
                long w2 = doc.GetField(WEIGHT_FIELD_NAME_2).GetInt64ValueOrDefault();
                long w3 = doc.GetField(WEIGHT_FIELD_NAME_3).GetInt64ValueOrDefault();
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(docs.Count == 0);
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithContext()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new Int64FieldSource(WEIGHT_FIELD_NAME_1), new Int64FieldSource(WEIGHT_FIELD_NAME_2), new Int64FieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumSingleFunction(toAdd), PAYLOAD_FIELD_NAME, CONTEXTS_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                long w1 = doc.GetField(WEIGHT_FIELD_NAME_1).GetInt64ValueOrDefault();
                long w2 = doc.GetField(WEIGHT_FIELD_NAME_2).GetInt64ValueOrDefault();
                long w3 = doc.GetField(WEIGHT_FIELD_NAME_3).GetInt64ValueOrDefault();
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));

                // LUCENENET NOTE: This test was once failing because we used SCG.HashSet<T> whose
                // Equals() implementation does not check for set equality. As a result SortedInputEnumerator
                // had been modified to reverse the results to get the test to pass. However, using JCG.HashSet<T>
                // ensures that set equality (that is equality that doesn't care about order of items) is respected.
                // SortedInputEnumerator has also had the specific sorting removed.
                ISet<BytesRef> originalCtxs = new JCG.HashSet<BytesRef>();
                foreach (IIndexableField ctxf in doc.GetFields(CONTEXTS_FIELD_NAME))
                {
                    originalCtxs.add(ctxf.GetBinaryValue());
                }
                assertEquals(originalCtxs, inputIterator.Contexts);
            }
            assertTrue(docs.Count == 0);
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithoutPayload()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            ValueSource[] toAdd = new ValueSource[] { new Int64FieldSource(WEIGHT_FIELD_NAME_1), new Int64FieldSource(WEIGHT_FIELD_NAME_2), new Int64FieldSource(WEIGHT_FIELD_NAME_3) };
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumSingleFunction(toAdd));
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                long w1 = doc.GetField(WEIGHT_FIELD_NAME_1).GetInt64ValueOrDefault();
                long w2 = doc.GetField(WEIGHT_FIELD_NAME_2).GetInt64ValueOrDefault();
                long w3 = doc.GetField(WEIGHT_FIELD_NAME_3).GetInt64ValueOrDefault();
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, (w1 + w2 + w3));
                assertEquals(inputIterator.Payload, null);
            }
            assertTrue(docs.Count == 0);
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithDeletions()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            Random rand = Random;
            IList<string> termsToDel = new JCG.List<string>();
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
            ValueSource[] toAdd = new ValueSource[] { new Int64FieldSource(WEIGHT_FIELD_NAME_1), new Int64FieldSource(WEIGHT_FIELD_NAME_2) };

            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new SumSingleFunction(toAdd), PAYLOAD_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                long w1 = doc.GetField(WEIGHT_FIELD_NAME_1).GetInt64ValueOrDefault();
                long w2 = doc.GetField(WEIGHT_FIELD_NAME_2).GetInt64ValueOrDefault();
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, w2 + w1);
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(docs.Count == 0);
            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithValueSource()
        {

            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            IDictionary<string, Document> docs = GenerateIndexDocuments(AtLeast(100));
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();

            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentValueSourceDictionary(ir, FIELD_NAME, new DoubleConstValueSource(10), PAYLOAD_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                assertEquals(inputIterator.Weight, 10);
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }
            assertTrue(docs.Count == 0);
            ir.Dispose();
            dir.Dispose();
        }
    }
}
