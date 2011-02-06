/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Analyzer = Lucene.Net.Analysis.Analyzer;
using LowerCaseTokenizer = Lucene.Net.Analysis.LowerCaseTokenizer;
using Token = Lucene.Net.Analysis.Token;
using TokenFilter = Lucene.Net.Analysis.TokenFilter;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using _Payload = Lucene.Net.Index.Payload;
using IndexSearcher = Lucene.Net.Search.IndexSearcher;
using Similarity = Lucene.Net.Search.Similarity;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using English = Lucene.Net.Util.English;

namespace Lucene.Net.Search.Payloads
{
    public class PayloadHelper
    {
        private static readonly byte[] payloadField = new byte[] { 1 };
        private static readonly byte[] payloadMultiField1 = new byte[] { 2 };
        private static readonly byte[] payloadMultiField2 = new byte[] { 4 };
        public static readonly string NO_PAYLOAD_FIELD = "noPayloadField";
        public static readonly string MULTI_FIELD = "multiField";
        public static readonly string FIELD = "field";

        public class PayloadAnalyzer : Analyzer
        {
            public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
            {
                TokenStream result = new LowerCaseTokenizer(reader);
                result = new PayloadFilter(result, fieldName);
                return result;
            }
        }

        public class PayloadFilter : TokenFilter
        {
            string fieldName;
            int numSeen = 0;

            public PayloadFilter(TokenStream input, string fieldName)
                : base(input)
            {
                this.fieldName = fieldName;
            }

            public override Token Next()
            {
                Token result = input.Next();
                if (result != null)
                {
                    if (fieldName.Equals(FIELD))
                    {
                        result.SetPayload(new _Payload(PayloadHelper.payloadField));
                    }
                    else if (fieldName.Equals(MULTI_FIELD))
                    {
                        if (numSeen % 2 == 0)
                        {
                            result.SetPayload(new _Payload(PayloadHelper.payloadMultiField1));
                        }
                        else
                        {
                            result.SetPayload(new _Payload(PayloadHelper.payloadMultiField2));
                        }
                        numSeen++;
                    }

                }
                return result;
            }
        }

        /**
         * Sets up a RAMDirectory, and adds documents (using English.intToEnglish()) with two fields: field and multiField
         * and analyzes them using the PayloadAnalyzer
         * @param similarity The Similarity class to use in the Searcher
         * @param numDocs The num docs to add
         * @return An IndexSearcher
         * @throws IOException
         */
        public IndexSearcher SetUp(Similarity similarity, int numDocs)
        {
            RAMDirectory directory = new RAMDirectory();
            PayloadAnalyzer analyzer = new PayloadAnalyzer();
            IndexWriter writer
                    = new IndexWriter(directory, analyzer, true);
            writer.SetSimilarity(similarity);
            //writer.infoStream = System.out;
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(new Field(FIELD, English.IntToEnglish(i), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field(MULTI_FIELD, English.IntToEnglish(i) + "  " + English.IntToEnglish(i), Field.Store.YES, Field.Index.ANALYZED));
                doc.Add(new Field(NO_PAYLOAD_FIELD, English.IntToEnglish(i), Field.Store.YES, Field.Index.ANALYZED));
                writer.AddDocument(doc);
            }
            //writer.optimize();
            writer.Close();

            IndexSearcher searcher = new IndexSearcher(directory);
            searcher.SetSimilarity(similarity);
            return searcher;
        }
    }
}
