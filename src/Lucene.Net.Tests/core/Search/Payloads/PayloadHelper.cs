using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using NUnit.Framework;
using System;

namespace Lucene.Net.Search.Payloads
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

    using Lucene.Net.Analysis;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using English = Lucene.Net.Util.English;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using TextField = TextField;

    ///
    ///
    ///
    public class PayloadHelper
    {
        private byte[] PayloadField = new byte[] { 1 };
        private byte[] PayloadMultiField1 = new byte[] { 2 };
        private byte[] PayloadMultiField2 = new byte[] { 4 };
        public const string NO_PAYLOAD_FIELD = "noPayloadField";
        public const string MULTI_FIELD = "multiField";
        public const string FIELD = "field";

        public IndexReader Reader;

        public sealed class PayloadAnalyzer : Analyzer
        {
            private readonly PayloadHelper OuterInstance;

            public PayloadAnalyzer(PayloadHelper outerInstance)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(OuterInstance, result, fieldName));
            }
        }

        public sealed class PayloadFilter : TokenFilter
        {
            private readonly PayloadHelper OuterInstance;

            internal readonly string FieldName;
            internal int NumSeen = 0;
            internal readonly IPayloadAttribute PayloadAtt;

            public PayloadFilter(PayloadHelper outerInstance, TokenStream input, string fieldName)
                : base(input)
            {
                this.OuterInstance = outerInstance;
                this.FieldName = fieldName;
                PayloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (FieldName.Equals(FIELD))
                    {
                        PayloadAtt.Payload = new BytesRef(OuterInstance.PayloadField);
                    }
                    else if (FieldName.Equals(MULTI_FIELD))
                    {
                        if (NumSeen % 2 == 0)
                        {
                            PayloadAtt.Payload = new BytesRef(OuterInstance.PayloadMultiField1);
                        }
                        else
                        {
                            PayloadAtt.Payload = new BytesRef(OuterInstance.PayloadMultiField2);
                        }
                        NumSeen++;
                    }
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                base.Reset();
                this.NumSeen = 0;
            }
        }

        /// <summary>
        /// Sets up a RAMDirectory, and adds documents (using English.IntToEnglish()) with two fields: field and multiField
        /// and analyzes them using the PayloadAnalyzer </summary>
        /// <param name="similarity"> The Similarity class to use in the Searcher </param>
        /// <param name="numDocs"> The num docs to add </param>
        /// <returns> An IndexSearcher </returns>
        // TODO: randomize
        public virtual IndexSearcher SetUp(Random random, Similarity similarity, int numDocs)
        {
            Directory directory = new MockDirectoryWrapper(random, new RAMDirectory());
            PayloadAnalyzer analyzer = new PayloadAnalyzer(this);

            // TODO randomize this
            IndexWriter writer = new IndexWriter(directory, (new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer)).SetSimilarity(similarity));
            // writer.infoStream = System.out;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new TextField(FIELD, English.IntToEnglish(i), Field.Store.YES));
                doc.Add(new TextField(MULTI_FIELD, English.IntToEnglish(i) + "  " + English.IntToEnglish(i), Field.Store.YES));
                doc.Add(new TextField(NO_PAYLOAD_FIELD, English.IntToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = DirectoryReader.Open(writer, true);
            writer.Dispose();

            IndexSearcher searcher = LuceneTestCase.NewSearcher(Reader, similarity);
            searcher.Similarity = similarity;
            return searcher;
        }

        [TearDown]
        public virtual void TearDown()
        {
            Reader.Dispose();
        }
    }
}