using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CorruptIndexException = Lucene.Net.Index.CorruptIndexException;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    internal class PreFlexRWFieldsWriter : FieldsConsumer
    {
        private readonly TermInfosWriter TermsOut;
        private readonly IndexOutput FreqOut;
        private readonly IndexOutput ProxOut;
        private readonly PreFlexRWSkipListWriter SkipListWriter;
        private readonly int TotalNumDocs;

        public PreFlexRWFieldsWriter(SegmentWriteState state)
        {
            TermsOut = new TermInfosWriter(state.Directory, state.SegmentInfo.Name, state.FieldInfos, state.TermIndexInterval);

            bool success = false;
            try
            {
                string freqFile = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "", Lucene3xPostingsFormat.FREQ_EXTENSION);
                FreqOut = state.Directory.CreateOutput(freqFile, state.Context);
                TotalNumDocs = state.SegmentInfo.DocCount;
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(TermsOut);
                }
            }

            success = false;
            try
            {
                if (state.FieldInfos.HasProx)
                {
                    string proxFile = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, "", Lucene3xPostingsFormat.PROX_EXTENSION);
                    ProxOut = state.Directory.CreateOutput(proxFile, state.Context);
                }
                else
                {
                    ProxOut = null;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(TermsOut, FreqOut);
                }
            }

            SkipListWriter = new PreFlexRWSkipListWriter(TermsOut.SkipInterval, TermsOut.MaxSkipLevels, TotalNumDocs, FreqOut, ProxOut);
            //System.out.println("\nw start seg=" + segment);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            Debug.Assert(field.Number != -1);
            if (field.IndexOptions >= IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS)
            {
                throw new System.NotSupportedException("this codec cannot index offsets");
            }
            //System.out.println("w field=" + field.Name + " storePayload=" + field.storePayloads + " number=" + field.number);
            return new PreFlexTermsWriter(this, field);
        }

        public override void Dispose()
        {
            IOUtils.Close(TermsOut, FreqOut, ProxOut);
        }

        private class PreFlexTermsWriter : TermsConsumer
        {
            internal virtual void InitializeInstanceFields()
            {
                postingsWriter = new PostingsWriter(this);
            }

            private readonly PreFlexRWFieldsWriter OuterInstance;

            internal readonly FieldInfo FieldInfo;
            internal readonly bool OmitTF;
            internal readonly bool StorePayloads;

            internal readonly TermInfo TermInfo = new TermInfo();
            internal PostingsWriter postingsWriter;

            public PreFlexTermsWriter(PreFlexRWFieldsWriter outerInstance, FieldInfo fieldInfo)
            {
                this.OuterInstance = outerInstance;

                InitializeInstanceFields();
                this.FieldInfo = fieldInfo;
                OmitTF = fieldInfo.IndexOptions == IndexOptions.DOCS_ONLY;
                StorePayloads = fieldInfo.HasPayloads;
            }

            internal class PostingsWriter : PostingsConsumer
            {
                private readonly PreFlexRWFieldsWriter.PreFlexTermsWriter OuterInstance;

                public PostingsWriter(PreFlexRWFieldsWriter.PreFlexTermsWriter outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                internal int LastDocID;
                internal int LastPayloadLength = -1;
                internal int LastPosition;
                internal int Df;

                public PostingsWriter Reset()
                {
                    Df = 0;
                    LastDocID = 0;
                    LastPayloadLength = -1;
                    return this;
                }

                public override void StartDoc(int docID, int termDocFreq)
                {
                    //System.out.println("    w doc=" + docID);

                    int delta = docID - LastDocID;
                    if (docID < 0 || (Df > 0 && delta <= 0))
                    {
                        throw new CorruptIndexException("docs out of order (" + docID + " <= " + LastDocID + " )");
                    }

                    if ((++Df % OuterInstance.OuterInstance.TermsOut.SkipInterval) == 0)
                    {
                        OuterInstance.OuterInstance.SkipListWriter.SetSkipData(LastDocID, OuterInstance.StorePayloads, LastPayloadLength);
                        OuterInstance.OuterInstance.SkipListWriter.BufferSkip(Df);
                    }

                    LastDocID = docID;

                    Debug.Assert(docID < OuterInstance.OuterInstance.TotalNumDocs, "docID=" + docID + " totalNumDocs=" + OuterInstance.OuterInstance.TotalNumDocs);

                    if (OuterInstance.OmitTF)
                    {
                        OuterInstance.OuterInstance.FreqOut.WriteVInt(delta);
                    }
                    else
                    {
                        int code = delta << 1;
                        if (termDocFreq == 1)
                        {
                            OuterInstance.OuterInstance.FreqOut.WriteVInt(code | 1);
                        }
                        else
                        {
                            OuterInstance.OuterInstance.FreqOut.WriteVInt(code);
                            OuterInstance.OuterInstance.FreqOut.WriteVInt(termDocFreq);
                        }
                    }
                    LastPosition = 0;
                }

                public override void AddPosition(int position, BytesRef payload, int startOffset, int endOffset)
                {
                    Debug.Assert(OuterInstance.OuterInstance.ProxOut != null);
                    Debug.Assert(startOffset == -1);
                    Debug.Assert(endOffset == -1);
                    //System.out.println("      w pos=" + position + " payl=" + payload);
                    int delta = position - LastPosition;
                    LastPosition = position;

                    if (OuterInstance.StorePayloads)
                    {
                        int payloadLength = payload == null ? 0 : payload.Length;
                        if (payloadLength != LastPayloadLength)
                        {
                            //System.out.println("        write payload len=" + payloadLength);
                            LastPayloadLength = payloadLength;
                            OuterInstance.OuterInstance.ProxOut.WriteVInt((delta << 1) | 1);
                            OuterInstance.OuterInstance.ProxOut.WriteVInt(payloadLength);
                        }
                        else
                        {
                            OuterInstance.OuterInstance.ProxOut.WriteVInt(delta << 1);
                        }
                        if (payloadLength > 0)
                        {
                            OuterInstance.OuterInstance.ProxOut.WriteBytes(payload.Bytes, payload.Offset, payload.Length);
                        }
                    }
                    else
                    {
                        OuterInstance.OuterInstance.ProxOut.WriteVInt(delta);
                    }
                }

                public override void FinishDoc()
                {
                }
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //System.out.println("  w term=" + text.utf8ToString());
                OuterInstance.SkipListWriter.ResetSkip();
                TermInfo.FreqPointer = OuterInstance.FreqOut.FilePointer;
                if (OuterInstance.ProxOut != null)
                {
                    TermInfo.ProxPointer = OuterInstance.ProxOut.FilePointer;
                }
                return postingsWriter.Reset();
            }

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                if (stats.DocFreq > 0)
                {
                    long skipPointer = OuterInstance.SkipListWriter.WriteSkip(OuterInstance.FreqOut);
                    TermInfo.DocFreq = stats.DocFreq;
                    TermInfo.SkipOffset = (int)(skipPointer - TermInfo.FreqPointer);
                    //System.out.println("  w finish term=" + text.utf8ToString() + " fnum=" + fieldInfo.number);
                    OuterInstance.TermsOut.Add(FieldInfo.Number, text, TermInfo);
                }
            }

            public override void Finish(long sumTotalTermCount, long sumDocFreq, int docCount)
            {
            }

            public override IComparer<BytesRef> Comparator
            {
                get
                {
                    return BytesRef.UTF8SortedAsUTF16Comparer;
                }
            }
        }
    }
}