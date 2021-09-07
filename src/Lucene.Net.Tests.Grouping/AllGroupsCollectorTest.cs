using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search.Grouping.Function;
using Lucene.Net.Search.Grouping.Terms;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Mutable;
using NUnit.Framework;
using System;
using System.Collections;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Grouping
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

    public class AllGroupsCollectorTest : LuceneTestCase
    {
        [Test]
        public void TestTotalGroupCount()
        {

            string groupField = "author";
            FieldType customType = new FieldType();
            customType.IsStored = true;

            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(
                Random,
                dir,
                NewIndexWriterConfig(TEST_VERSION_CURRENT,
                    new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            bool canUseIDV = !"Lucene3x".Equals(w.IndexWriter.Config.Codec.Name, StringComparison.Ordinal);

            // 0
            Document doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "random text", Field.Store.YES));
            doc.Add(new Field("id", "1", customType));
            w.AddDocument(doc);

            // 1
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random text blob", Field.Store.YES));
            doc.Add(new Field("id", "2", customType));
            w.AddDocument(doc);

            // 2
            doc = new Document();
            AddGroupField(doc, groupField, "author1", canUseIDV);
            doc.Add(new TextField("content", "some more random textual data", Field.Store.YES));
            doc.Add(new Field("id", "3", customType));
            w.AddDocument(doc);
            w.Commit(); // To ensure a second segment

            // 3
            doc = new Document();
            AddGroupField(doc, groupField, "author2", canUseIDV);
            doc.Add(new TextField("content", "some random text", Field.Store.YES));
            doc.Add(new Field("id", "4", customType));
            w.AddDocument(doc);

            // 4
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "some more random text", Field.Store.YES));
            doc.Add(new Field("id", "5", customType));
            w.AddDocument(doc);

            // 5
            doc = new Document();
            AddGroupField(doc, groupField, "author3", canUseIDV);
            doc.Add(new TextField("content", "random blob", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            // 6 -- no author field
            doc = new Document();
            doc.Add(new TextField("content", "random word stuck in alot of other text", Field.Store.YES));
            doc.Add(new Field("id", "6", customType));
            w.AddDocument(doc);

            IndexSearcher indexSearcher = NewSearcher(w.GetReader());
            w.Dispose();

            IAbstractAllGroupsCollector<object> allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "random")), allGroupsCollector);
            assertEquals(4, allGroupsCollector.GroupCount);

            allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "some")), allGroupsCollector);
            assertEquals(3, allGroupsCollector.GroupCount);

            allGroupsCollector = CreateRandomCollector(groupField, canUseIDV);
            indexSearcher.Search(new TermQuery(new Term("content", "blob")), allGroupsCollector);
            assertEquals(2, allGroupsCollector.GroupCount);

            indexSearcher.IndexReader.Dispose();
            dir.Dispose();
        }

        private void AddGroupField(Document doc, string groupField, string value, bool canUseIDV)
        {
            doc.Add(new TextField(groupField, value, Field.Store.YES));
            if (canUseIDV)
            {
                doc.Add(new SortedDocValuesField(groupField, new BytesRef(value)));
            }
        }

        private IAbstractAllGroupsCollector<object> CreateRandomCollector(string groupField, bool canUseIDV)
        {
            IAbstractAllGroupsCollector<object> selected;
            if (Random.nextBoolean())
            {
                selected = new TermAllGroupsCollector(groupField);
            }
            else
            {
                ValueSource vs = new BytesRefFieldSource(groupField);
                selected = new FunctionAllGroupsCollector<MutableValue>(vs, new Hashtable());   // LUCENENET Specific type for generic must be specified.
            }

            if (Verbose)
            {
                Console.WriteLine("Selected implementation: " + selected.GetType().Name);
            }

            return selected;
        }
    }
}
