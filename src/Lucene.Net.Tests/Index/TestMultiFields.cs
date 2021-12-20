using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomizedTesting.Generators;

namespace Lucene.Net.Index
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

    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;

    [TestFixture]
    public class TestMultiFields : LuceneTestCase
    {
        [Test]
        public virtual void TestRandom()
        {
            int num = AtLeast(2);
            for (int iter = 0; iter < num; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter);
                }

                Directory dir = NewDirectory();

                IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NoMergePolicy.COMPOUND_FILES));
                // we can do this because we use NoMergePolicy (and dont merge to "nothing")
                w.KeepFullyDeletedSegments = true;

                IDictionary<BytesRef, IList<int>> docs = new Dictionary<BytesRef, IList<int>>();
                ISet<int?> deleted = new JCG.HashSet<int?>();
                IList<BytesRef> terms = new JCG.List<BytesRef>();

                int numDocs = TestUtil.NextInt32(Random, 1, 100 * RandomMultiplier);
                Documents.Document doc = new Documents.Document();
                Field f = NewStringField("field", "", Field.Store.NO);
                doc.Add(f);
                Field id = NewStringField("id", "", Field.Store.NO);
                doc.Add(id);

                bool onlyUniqueTerms = Random.NextBoolean();
                if (Verbose)
                {
                    Console.WriteLine("TEST: onlyUniqueTerms=" + onlyUniqueTerms + " numDocs=" + numDocs);
                }
                ISet<BytesRef> uniqueTerms = new JCG.HashSet<BytesRef>();
                for (int i = 0; i < numDocs; i++)
                {
                    if (!onlyUniqueTerms && Random.NextBoolean() && terms.Count > 0)
                    {
                        // re-use existing term
                        BytesRef term = terms[Random.Next(terms.Count)];
                        docs[term].Add(i);
                        f.SetStringValue(term.Utf8ToString());
                    }
                    else
                    {
                        string s = TestUtil.RandomUnicodeString(Random, 10);
                        BytesRef term = new BytesRef(s);
                        if (!docs.TryGetValue(term, out IList<int> docsTerm))
                        {
                            docs[term] = docsTerm = new JCG.List<int>();
                        }
                        docsTerm.Add(i);
                        terms.Add(term);
                        uniqueTerms.Add(term);
                        f.SetStringValue(s);
                    }
                    id.SetStringValue("" + i);
                    w.AddDocument(doc);
                    if (Random.Next(4) == 1)
                    {
                        w.Commit();
                    }
                    if (i > 0 && Random.Next(20) == 1)
                    {
                        int delID = Random.Next(i);
                        deleted.Add(delID);
                        w.DeleteDocuments(new Term("id", "" + delID));
                        if (Verbose)
                        {
                            Console.WriteLine("TEST: delete " + delID);
                        }
                    }
                }

                if (Verbose)
                {
                    IList<BytesRef> termsList = new JCG.List<BytesRef>(uniqueTerms);
#pragma warning disable 612, 618
                    termsList.Sort(BytesRef.UTF8SortedAsUTF16Comparer);
#pragma warning restore 612, 618
                    Console.WriteLine("TEST: terms in UTF16 order:");
                    foreach (BytesRef b in termsList)
                    {
                        Console.WriteLine("  " + UnicodeUtil.ToHexString(b.Utf8ToString()) + " " + b);
                        foreach (int docID in docs[b])
                        {
                            if (deleted.Contains(docID))
                            {
                                Console.WriteLine("    " + docID + " (deleted)");
                            }
                            else
                            {
                                Console.WriteLine("    " + docID);
                            }
                        }
                    }
                }

                IndexReader reader = w.GetReader();
                w.Dispose();
                if (Verbose)
                {
                    Console.WriteLine("TEST: reader=" + reader);
                }

                IBits liveDocs = MultiFields.GetLiveDocs(reader);
                foreach (int delDoc in deleted)
                {
                    Assert.IsFalse(liveDocs.Get(delDoc));
                }

                for (int i = 0; i < 100; i++)
                {
                    BytesRef term = terms[Random.Next(terms.Count)];
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: seek term=" + UnicodeUtil.ToHexString(term.Utf8ToString()) + " " + term);
                    }

                    DocsEnum docsEnum = TestUtil.Docs(Random, reader, "field", term, liveDocs, null, DocsFlags.NONE);
                    Assert.IsNotNull(docsEnum);

                    foreach (int docID in docs[term])
                    {
                        if (!deleted.Contains(docID))
                        {
                            Assert.AreEqual(docID, docsEnum.NextDoc());
                        }
                    }
                    Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, docsEnum.NextDoc());
                }

                reader.Dispose();
                dir.Dispose();
            }
        }

        /*
        private void verify(IndexReader r, String term, IList<Integer> expected) throws Exception {
          DocsEnum docs = TestUtil.Docs(random, r,
                                         "field",
                                         new BytesRef(term),
                                         MultiFields.GetLiveDocs(r),
                                         null,
                                         false);
          for(int docID : expected) {
            Assert.AreEqual(docID, docs.NextDoc());
          }
          Assert.AreEqual(docs.NO_MORE_DOCS, docs.NextDoc());
        }
        */

        [Test]
        public virtual void TestSeparateEnums()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Documents.Document d = new Documents.Document();
            d.Add(NewStringField("f", "j", Field.Store.NO));
            w.AddDocument(d);
            w.Commit();
            w.AddDocument(d);
            IndexReader r = w.GetReader();
            w.Dispose();
            DocsEnum d1 = TestUtil.Docs(Random, r, "f", new BytesRef("j"), null, null, DocsFlags.NONE);
            DocsEnum d2 = TestUtil.Docs(Random, r, "f", new BytesRef("j"), null, null, DocsFlags.NONE);
            Assert.AreEqual(0, d1.NextDoc());
            Assert.AreEqual(0, d2.NextDoc());
            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestTermDocsEnum()
        {
            Directory dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Documents.Document d = new Documents.Document();
            d.Add(NewStringField("f", "j", Field.Store.NO));
            w.AddDocument(d);
            w.Commit();
            w.AddDocument(d);
            IndexReader r = w.GetReader();
            w.Dispose();
            DocsEnum de = MultiFields.GetTermDocsEnum(r, null, "f", new BytesRef("j"));
            Assert.AreEqual(0, de.NextDoc());
            Assert.AreEqual(1, de.NextDoc());
            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, de.NextDoc());
            r.Dispose();
            dir.Dispose();
        }
    }
}