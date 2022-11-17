using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
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
    public class DocumentDictionaryTest : LuceneTestCase
    {
        internal const string FIELD_NAME = "f1";
        internal const string WEIGHT_FIELD_NAME = "w1";
        internal const string PAYLOAD_FIELD_NAME = "p1";
        internal const string CONTEXT_FIELD_NAME = "c1";

        /** Returns Pair(list of invalid document terms, Map of document term -> document) */
        private KeyValuePair<IList<string>, IDictionary<string, Document>> GenerateIndexDocuments(int ndocs, bool requiresPayload, bool requiresContexts)
        {
            IDictionary<string, Document> docs = new JCG.Dictionary<string, Document>();
            IList<string> invalidDocTerms = new JCG.List<string>();
            for (int i = 0; i < ndocs; i++)
            {
                Document doc = new Document();
                bool invalidDoc = false;
                Field field = null;
                // usually have valid term field in document
                if (Usually())
                {
                    field = new TextField(FIELD_NAME, "field_" + i, Field.Store.YES);
                    doc.Add(field);
                }
                else
                {
                    invalidDoc = true;
                }

                // even if payload is not required usually have it
                if (requiresPayload || Usually())
                {
                    // usually have valid payload field in document
                    if (Usually())
                    {
                        Field payload = new StoredField(PAYLOAD_FIELD_NAME, new BytesRef("payload_" + i));
                        doc.Add(payload);
                    }
                    else if (requiresPayload)
                    {
                        invalidDoc = true;
                    }
                }

                if (requiresContexts || Usually())
                {
                    if (Usually())
                    {
                        for (int j = 0; j < AtLeast(2); j++)
                        {
                            doc.Add(new StoredField(CONTEXT_FIELD_NAME, new BytesRef("context_" + i + "_" + j)));
                        }
                    }
                    // we should allow entries without context
                }

                // usually have valid weight field in document
                if (Usually())
                {
                    Field weight = (Rarely()) ?
                        (Field)new StoredField(WEIGHT_FIELD_NAME, 100d + i) :
                        (Field)new NumericDocValuesField(WEIGHT_FIELD_NAME, 100 + i);
                    doc.Add(weight);
                }

                string term = null;
                if (invalidDoc)
                {
                    term = (field != null) ? field.GetStringValue() : "invalid_" + i;
                    invalidDocTerms.Add(term);
                }
                else
                {
                    term = field.GetStringValue();
                }

                docs[term] = doc;
            }
            return new KeyValuePair<IList<string>, IDictionary<string, Document>>(invalidDocTerms, docs);
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
            IDictionary dictionary = new DocumentDictionary(ir, FIELD_NAME, WEIGHT_FIELD_NAME, PAYLOAD_FIELD_NAME);
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
            KeyValuePair<IList<string>, IDictionary<string, Document>> res = GenerateIndexDocuments(AtLeast(1000), true, false);
            IDictionary<string, Document> docs = res.Value;
            IList<String> invalidDocTerms = res.Key;
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentDictionary(ir, FIELD_NAME, WEIGHT_FIELD_NAME, PAYLOAD_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                //Document doc = docs.Remove(inputIterator.Current.Utf8ToString());
                assertTrue(inputIterator.Current.Equals(new BytesRef(doc.Get(FIELD_NAME))));
                IIndexableField weightField = doc.GetField(WEIGHT_FIELD_NAME);
                assertEquals(inputIterator.Weight, (weightField != null) ? weightField.GetInt64ValueOrDefault() : 0);
                assertTrue(inputIterator.Payload.Equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
            }

            foreach (string invalidTerm in invalidDocTerms)
            {
                var invalid = docs[invalidTerm];
                docs.Remove(invalidTerm);
                assertNotNull(invalid);
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
            KeyValuePair<IList<string>, IDictionary<string, Document>> res = GenerateIndexDocuments(AtLeast(1000), false, false);
            IDictionary<string, Document> docs = res.Value;
            IList<string> invalidDocTerms = res.Key;
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentDictionary(ir, FIELD_NAME, WEIGHT_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                var field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                IIndexableField weightField = doc.GetField(WEIGHT_FIELD_NAME);
                assertEquals(inputIterator.Weight, (weightField != null) ? weightField.GetInt64ValueOrDefault() : 0);
                assertEquals(inputIterator.Payload, null);
            }

            foreach (string invalidTerm in invalidDocTerms)
            {
                var invalid = docs[invalidTerm];
                docs.Remove(invalidTerm);
                assertNotNull(invalid);
            }


            assertTrue(docs.Count == 0);

            ir.Dispose();
            dir.Dispose();
        }

        [Test]
        public void TestWithContexts()
        {
            Directory dir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetMergePolicy(NewLogMergePolicy());
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            KeyValuePair<IList<string>, IDictionary<string, Document>> res = GenerateIndexDocuments(AtLeast(1000), true, true);
            IDictionary<string, Document> docs = res.Value;
            IList<string> invalidDocTerms = res.Key;
            foreach (Document doc in docs.Values)
            {
                writer.AddDocument(doc);
            }
            writer.Commit();
            writer.Dispose();
            IndexReader ir = DirectoryReader.Open(dir);
            IDictionary dictionary = new DocumentDictionary(ir, FIELD_NAME, WEIGHT_FIELD_NAME, PAYLOAD_FIELD_NAME, CONTEXT_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                string field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                //Document doc = docs.remove(f.utf8ToString());
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                IIndexableField weightField = doc.GetField(WEIGHT_FIELD_NAME);
                assertEquals(inputIterator.Weight, (weightField != null) ? weightField.GetInt64ValueOrDefault() : 0);
                assertTrue(inputIterator.Payload.equals(doc.GetField(PAYLOAD_FIELD_NAME).GetBinaryValue()));
                ISet<BytesRef> oriCtxs = new JCG.HashSet<BytesRef>();
                ICollection<BytesRef> contextSet = inputIterator.Contexts;
                foreach (IIndexableField ctxf in doc.GetFields(CONTEXT_FIELD_NAME))
                {
                    oriCtxs.add(ctxf.GetBinaryValue());
                }
                assertEquals(oriCtxs.size(), contextSet.Count);
            }

            foreach (string invalidTerm in invalidDocTerms)
            {
                var invalid = docs[invalidTerm];
                docs.Remove(invalidTerm);
                assertNotNull(invalid);
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
            KeyValuePair<IList<string>, IDictionary<string, Document>> res = GenerateIndexDocuments(AtLeast(1000), false, false);
            IDictionary<string, Document> docs = res.Value;
            IList<String> invalidDocTerms = res.Key;
            Random rand = Random;
            IList<string> termsToDel = new JCG.List<string>();
            foreach (Document doc in docs.Values)
            {
                IIndexableField f2 = doc.GetField(FIELD_NAME);
                if (rand.nextBoolean() && f2 != null && !invalidDocTerms.Contains(f2.GetStringValue()))
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
                assertTrue(toDel != null);
                docs.Remove(termToDel);
            }

            IndexReader ir = DirectoryReader.Open(dir);
            assertEquals(ir.NumDocs, docs.size());
            IDictionary dictionary = new DocumentDictionary(ir, FIELD_NAME, WEIGHT_FIELD_NAME);
            IInputEnumerator inputIterator = dictionary.GetEntryEnumerator();
            while (inputIterator.MoveNext())
            {
                var field = inputIterator.Current.Utf8ToString();
                Document doc = docs[field];
                docs.Remove(field);
                assertTrue(inputIterator.Current.equals(new BytesRef(doc.Get(FIELD_NAME))));
                IIndexableField weightField = doc.GetField(WEIGHT_FIELD_NAME);
                assertEquals(inputIterator.Weight, (weightField != null) ? weightField.GetInt64ValueOrDefault() : 0);
                assertEquals(inputIterator.Payload, null);
            }

            foreach (string invalidTerm in invalidDocTerms)
            {
                var invalid = docs[invalidTerm];
                docs.Remove(invalidTerm);
                assertNotNull(invalid);
            }
            assertTrue(docs.Count == 0);

            ir.Dispose();
            dir.Dispose();
        }
    }
}
