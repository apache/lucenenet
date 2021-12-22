using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using System.IO;

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
        private readonly byte[] payloadField = new byte[] { 1 };
        private readonly byte[] payloadMultiField1 = new byte[] { 2 };
        private readonly byte[] payloadMultiField2 = new byte[] { 4 };
        public const string NO_PAYLOAD_FIELD = "noPayloadField";
        public const string MULTI_FIELD = "multiField";
        public const string FIELD = "field";

        public IndexReader Reader;

        public sealed class PayloadAnalyzer : Analyzer
        {
            private readonly PayloadHelper outerInstance;

            public PayloadAnalyzer(PayloadHelper outerInstance)
                : base(PER_FIELD_REUSE_STRATEGY)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                return new TokenStreamComponents(result, new PayloadFilter(outerInstance, result, fieldName));
            }
        }

        public sealed class PayloadFilter : TokenFilter
        {
            private readonly PayloadHelper outerInstance;

            internal readonly string fieldName;
            internal int numSeen = 0;
            internal readonly IPayloadAttribute payloadAtt;

            public PayloadFilter(PayloadHelper outerInstance, TokenStream input, string fieldName)
                : base(input)
            {
                this.outerInstance = outerInstance;
                this.fieldName = fieldName;
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public override bool IncrementToken()
            {
                if (m_input.IncrementToken())
                {
                    if (fieldName.Equals(FIELD, StringComparison.Ordinal))
                    {
                        payloadAtt.Payload = new BytesRef(outerInstance.payloadField);
                    }
                    else if (fieldName.Equals(MULTI_FIELD, StringComparison.Ordinal))
                    {
                        if (numSeen % 2 == 0)
                        {
                            payloadAtt.Payload = new BytesRef(outerInstance.payloadMultiField1);
                        }
                        else
                        {
                            payloadAtt.Payload = new BytesRef(outerInstance.payloadMultiField2);
                        }
                        numSeen++;
                    }
                    return true;
                }
                return false;
            }

            public override void Reset()
            {
                base.Reset();
                this.numSeen = 0;
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
                doc.Add(new TextField(FIELD, English.Int32ToEnglish(i), Field.Store.YES));
                doc.Add(new TextField(MULTI_FIELD, English.Int32ToEnglish(i) + "  " + English.Int32ToEnglish(i), Field.Store.YES));
                doc.Add(new TextField(NO_PAYLOAD_FIELD, English.Int32ToEnglish(i), Field.Store.YES));
                writer.AddDocument(doc);
            }
            Reader = DirectoryReader.Open(writer, true);
            writer.Dispose();

            IndexSearcher searcher = LuceneTestCase.NewSearcher(Reader);
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