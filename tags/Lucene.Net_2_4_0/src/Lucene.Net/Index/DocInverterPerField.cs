/**
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

using Fieldable = Lucene.Net.Documents.Fieldable;
using Token = Lucene.Net.Analysis.Token;
using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Index
{
    /// <summary>
    /// Holds state for inverting all occurrences of a single
    /// field in the document.  This class doesn't do anything
    /// itself; instead, it forwards the tokens produced by
    /// analysis to its own consumer
    /// (InvertedDocConsumerPerField).  It also interacts with an
    /// endConsumer (InvertedDocEndConsumerPerField).
    /// </summary>
    internal sealed class DocInverterPerField : DocFieldConsumerPerField
    {

        private readonly DocInverterPerThread perThread;
        private readonly FieldInfo fieldInfo;
        internal readonly InvertedDocConsumerPerField consumer;
        internal readonly InvertedDocEndConsumerPerField endConsumer;
        internal readonly DocumentsWriter.DocState docState;
        internal readonly DocInverter.FieldInvertState fieldState;

        public DocInverterPerField(DocInverterPerThread perThread, FieldInfo fieldInfo)
        {
            this.perThread = perThread;
            this.fieldInfo = fieldInfo;
            docState = perThread.docState;
            fieldState = perThread.fieldState;
            this.consumer = perThread.consumer.addField(this, fieldInfo);
            this.endConsumer = perThread.endConsumer.addField(this, fieldInfo);
        }

        internal override void abort()
        {
            consumer.abort();
            endConsumer.abort();
        }

        internal override void processFields(Fieldable[] fields,
                                  int count)
        {

            fieldState.reset(docState.doc.GetBoost());

            int maxFieldLength = docState.maxFieldLength;

            bool doInvert = consumer.start(fields, count);

            for (int i = 0; i < count; i++)
            {

                Fieldable field = fields[i];

                // TODO FI: this should be "genericized" to querying
                // consumer if it wants to see this particular field
                // tokenized.
                if (field.IsIndexed() && doInvert)
                {

                    if (fieldState.length > 0)
                        fieldState.position += docState.analyzer.GetPositionIncrementGap(fieldInfo.name);

                    if (!field.IsTokenized())
                    {		  // un-tokenized field
                        string stringValue = field.StringValue();
                        int valueLength = stringValue.Length;
                        Token token = perThread.localToken.Reinit(stringValue, fieldState.offset, fieldState.offset + valueLength);
                        bool success = false;
                        try
                        {
                            consumer.add(token);
                            success = true;
                        }
                        finally
                        {
                            if (!success)
                                docState.docWriter.SetAborting();
                        }
                        fieldState.offset += valueLength;
                        fieldState.length++;
                        fieldState.position++;
                    }
                    else
                    {                                  // tokenized field
                        TokenStream stream;
                        TokenStream streamValue = field.TokenStreamValue();

                        if (streamValue != null)
                            stream = streamValue;
                        else
                        {
                            // the field does not have a TokenStream,
                            // so we have to obtain one from the analyzer
                            System.IO.TextReader reader;			  // find or make Reader
                            System.IO.TextReader readerValue = field.ReaderValue();

                            if (readerValue != null)
                                reader = readerValue;
                            else
                            {
                                string stringValue = field.StringValue();
                                if (stringValue == null)
                                    throw new System.ArgumentException("field must have either TokenStream, string or Reader value");
                                perThread.stringReader.Init(stringValue);
                                reader = perThread.stringReader;
                            }

                            // Tokenize field and add to postingTable
                            stream = docState.analyzer.ReusableTokenStream(fieldInfo.name, reader);
                        }

                        // reset the TokenStream to the first token
                        stream.Reset();

                        try
                        {
                            int offsetEnd = fieldState.offset - 1;
                            Token localToken = perThread.localToken;
                            for (; ; )
                            {

                                // If we hit an exception in stream.next below
                                // (which is fairly common, eg if analyzer
                                // chokes on a given document), then it's
                                // non-aborting and (above) this one document
                                // will be marked as deleted, but still
                                // consume a docID
                                Token token = stream.Next(localToken);

                                if (token == null) break;
                                fieldState.position += (token.GetPositionIncrement() - 1);
                                bool success = false;
                                try
                                {
                                    // If we hit an exception in here, we abort
                                    // all buffered documents since the last
                                    // flush, on the likelihood that the
                                    // internal state of the consumer is now
                                    // corrupt and should not be flushed to a
                                    // new segment:
                                    consumer.add(token);
                                    success = true;
                                }
                                finally
                                {
                                    if (!success)
                                        docState.docWriter.SetAborting();
                                }
                                fieldState.position++;
                                offsetEnd = fieldState.offset + token.EndOffset();

                                if (++fieldState.length >= maxFieldLength)
                                {
                                    if (docState.infoStream != null)
                                        docState.infoStream.WriteLine("maxFieldLength " + maxFieldLength + " reached for field " + fieldInfo.name + ", ignoring following tokens");
                                    break;
                                }
                            }
                            fieldState.offset = offsetEnd + 1;
                        }
                        finally
                        {
                            stream.Close();
                        }
                    }

                    fieldState.boost *= field.GetBoost();
                }
            }

            consumer.finish();
            endConsumer.finish();
        }
    }
}
