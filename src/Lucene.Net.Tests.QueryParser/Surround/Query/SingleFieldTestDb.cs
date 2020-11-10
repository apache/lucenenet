using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.QueryParsers.Surround.Query
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

    public class SingleFieldTestDb
    {
        private Directory db;
        private string[] docs;
        private string fieldName;

        public SingleFieldTestDb(Random random, string[] documents, string fName)
        {
            db = new MockDirectoryWrapper(random, new RAMDirectory());
            docs = documents;
            fieldName = fName;
            using IndexWriter writer = new IndexWriter(db, new IndexWriterConfig(
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                new MockAnalyzer(random)));
            for (int j = 0; j < docs.Length; j++)
            {
                Document d = new Document();
                d.Add(new TextField(fieldName, docs[j], Field.Store.NO));
                writer.AddDocument(d);
            }
        }

        public Directory Db => db;
        [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Lucene's design requires some array properties")]
        public string[] Docs => docs;
        public string Fieldname => fieldName;
    }
}
