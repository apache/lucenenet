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

using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Documents
{
    [TestFixture]
    public class TestLazyDocument : LuceneTestCase
    {
        public readonly int NUM_DOCS = AtLeast(10);
        public readonly string[] FIELDS = new string[]
            { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k" };
        public readonly int NUM_VALUES = AtLeast(100);

        public Directory dir = NewDirectory();

        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific - changed from RemoveIndex() to ensure calling order vs base class
        {
            if (null != dir)
            {
                try
                {
                    dir.Dispose();
                    dir = null;
                }
                catch (Exception e) when (e.IsException()) { /* NOOP */ }
            }

            base.AfterClass();
        }

        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - changed from CreateIndex() to ensure calling order vs base class
        {
            base.BeforeClass();

            Analyzer analyzer = new MockAnalyzer(Random);
            IndexWriter writer = new IndexWriter
              (dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            try
            {
                for (int docid = 0; docid < NUM_DOCS; docid++)
                {
                    Document d = new Document();
                    d.Add(NewStringField("docid", "" + docid, Field.Store.YES));
                    d.Add(NewStringField("never_load", "fail", Field.Store.YES));
                    foreach (string f in FIELDS)
                    {
                        for (int val = 0; val < NUM_VALUES; val++)
                        {
                            d.Add(NewStringField(f, docid + "_" + f + "_" + val, Field.Store.YES));
                        }
                    }
                    d.Add(NewStringField("load_later", "yes", Field.Store.YES));
                    writer.AddDocument(d);
                }
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Test]
        public void TestLazy()
        {
            int id = Random.nextInt(NUM_DOCS);
            IndexReader reader = DirectoryReader.Open(dir);
            try
            {
                Query q = new TermQuery(new Term("docid", "" + id));
                IndexSearcher searcher = NewSearcher(reader);
                ScoreDoc[] hits = searcher.Search(q, 100).ScoreDocs;
                assertEquals("Too many docs", 1, hits.Length);
                LazyTestingStoredFieldVisitor visitor
                    = new LazyTestingStoredFieldVisitor(new LazyDocument(reader, hits[0].Doc),
                                                      FIELDS);
                reader.Document(hits[0].Doc, visitor);
                Document d = visitor.doc;

                int numFieldValues = 0;
                IDictionary<string, int> fieldValueCounts = new JCG.Dictionary<string, int>();

                // at this point, all FIELDS should be Lazy and unrealized
                foreach (IIndexableField f in d)
                {
                    numFieldValues++;
                    if (f.Name.Equals("never_load", StringComparison.Ordinal))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.Equals("load_later", StringComparison.Ordinal))
                    {
                        fail("load_later was loaded on first pass");
                    }
                    if (f.Name.Equals("docid", StringComparison.Ordinal))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        if (!fieldValueCounts.TryGetValue(f.Name, out int count))
                            count = 0;
                        count++;
                        fieldValueCounts[f.Name] = count;
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertFalse(f.Name + " is loaded", lf.HasBeenLoaded);
                    }
                }
                Console.WriteLine("numFieldValues == " + numFieldValues);
                assertEquals("numFieldValues", 1 + (NUM_VALUES * FIELDS.Length),
                             numFieldValues);

                foreach (string field in fieldValueCounts.Keys)
                {
                    assertEquals("fieldName count: " + field,
                                 NUM_VALUES, fieldValueCounts[field]);
                }

                // pick a single field name to load a single value
                string fieldName = FIELDS[Random.nextInt(FIELDS.Length)];
                IIndexableField[] fieldValues = d.GetFields(fieldName);
                assertEquals("#vals in field: " + fieldName,
                             NUM_VALUES, fieldValues.Length);
                int valNum = Random.nextInt(fieldValues.Length);
                assertEquals(id + "_" + fieldName + "_" + valNum,
                             fieldValues[valNum].GetStringValue());

                // now every value of fieldName should be loaded
                foreach (IIndexableField f in d)
                {
                    if (f.Name.Equals("never_load", StringComparison.Ordinal))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.Equals("load_later", StringComparison.Ordinal))
                    {
                        fail("load_later was loaded too soon");
                    }
                    if (f.Name.Equals("docid", StringComparison.Ordinal))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertEquals(f.Name + " is loaded?",
                                     lf.Name.Equals(fieldName, StringComparison.Ordinal), lf.HasBeenLoaded);
                    }
                }

                // use the same LazyDoc to ask for one more lazy field
                visitor = new LazyTestingStoredFieldVisitor(new LazyDocument(reader, hits[0].Doc),
                                                            "load_later");
                reader.Document(hits[0].Doc, visitor);
                d = visitor.doc;

                // ensure we have all the values we expect now, and that
                // adding one more lazy field didn't "unload" the existing LazyField's
                // we already loaded.
                foreach (IIndexableField f in d)
                {
                    if (f.Name.Equals("never_load", StringComparison.Ordinal))
                    {
                        fail("never_load was loaded");
                    }
                    if (f.Name.Equals("docid", StringComparison.Ordinal))
                    {
                        assertFalse(f.Name, f is LazyDocument.LazyField);
                    }
                    else
                    {
                        assertTrue(f.Name + " is " + f.GetType(),
                                   f is LazyDocument.LazyField);
                        LazyDocument.LazyField lf = (LazyDocument.LazyField)f;
                        assertEquals(f.Name + " is loaded?",
                                     lf.Name.Equals(fieldName, StringComparison.Ordinal), lf.HasBeenLoaded);
                    }
                }

                // even the underlying doc shouldn't have never_load
                assertNull("never_load was loaded in wrapped doc",
                           visitor.lazyDoc.GetDocument().GetField("never_load"));

            }
            finally
            {
                reader.Dispose();
            }
        }

        internal class LazyTestingStoredFieldVisitor : StoredFieldVisitor
        {
            public readonly Document doc = new Document();
            public readonly LazyDocument lazyDoc;
            public readonly ISet<string> lazyFieldNames;

            internal LazyTestingStoredFieldVisitor(LazyDocument l, params string[] fields)
            {
                lazyDoc = l;
                lazyFieldNames = new JCG.HashSet<string>(fields);
            }


            public override Status NeedsField(FieldInfo fieldInfo)
            {
                if (fieldInfo.Name.Equals("docid", StringComparison.Ordinal))
                {
                    return Status.YES;
                }
                else if (fieldInfo.Name.Equals("never_load", StringComparison.Ordinal))
                {
                    return Status.NO;
                }
                else
                {
                    if (lazyFieldNames.contains(fieldInfo.Name))
                    {
                        doc.Add(lazyDoc.GetField(fieldInfo));
                    }
                }
                return Status.NO;
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = fieldInfo.HasVectors;
                ft.IsIndexed = fieldInfo.IsIndexed;
                ft.OmitNorms = fieldInfo.OmitsNorms;
                ft.IndexOptions = fieldInfo.IndexOptions;
                doc.Add(new Field(fieldInfo.Name, value, ft));
            }
        }
    }
}
