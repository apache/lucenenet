using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using Lucene.Net.Randomized.Generators;
    using NUnit.Framework;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

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

    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestMixedCodecs : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            int NUM_DOCS = AtLeast(1000);

            Directory dir = NewDirectory();
            RandomIndexWriter w = null;

            int docsLeftInthisSegment = 0;

            int docUpto = 0;
            while (docUpto < NUM_DOCS)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: " + docUpto + " of " + NUM_DOCS);
                }
                if (docsLeftInthisSegment == 0)
                {
                    IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
                    if (Random().NextBoolean())
                    {
                        // Make sure we aggressively mix in SimpleText
                        // since it has different impls for all codec
                        // formats...
                        iwc.SetCodec(Codec.ForName("Lucene46"));
                    }
                    if (w != null)
                    {
                        w.Dispose();
                    }
                    w = new RandomIndexWriter(Random(), dir, iwc);
                    docsLeftInthisSegment = TestUtil.NextInt(Random(), 10, 100);
                }
                Document doc = new Document();
                doc.Add(NewStringField("id", Convert.ToString(docUpto), Field.Store.YES));
                w.AddDocument(doc);
                docUpto++;
                docsLeftInthisSegment--;
            }

            if (VERBOSE)
            {
                Console.WriteLine("\nTEST: now delete...");
            }

            // Random delete half the docs:
            HashSet<int?> deleted = new HashSet<int?>();
            while (deleted.Count < NUM_DOCS / 2)
            {
                int? toDelete = Random().Next(NUM_DOCS);
                if (!deleted.Contains(toDelete))
                {
                    deleted.Add(toDelete);
                    w.DeleteDocuments(new Term("id", Convert.ToString(toDelete)));
                    if (Random().Next(17) == 6)
                    {
                        IndexReader r = w.Reader;
                        Assert.AreEqual(NUM_DOCS - deleted.Count, r.NumDocs);
                        r.Dispose();
                    }
                }
            }

            w.Dispose();
            dir.Dispose();
        }
    }
}