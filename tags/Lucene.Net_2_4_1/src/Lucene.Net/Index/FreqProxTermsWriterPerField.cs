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

namespace Lucene.Net.Index
{
    // TODO: break into separate freq and prox writers as
    // codecs; make separate container (tii/tis/skip/*) that can
    // be configured as any number of files 1..N
    internal sealed class FreqProxTermsWriterPerField : TermsHashConsumerPerField, System.IComparable
    {
        internal readonly FreqProxTermsWriterPerThread perThread;
        internal readonly TermsHashPerField termsHashPerField;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriter.DocState docState;
        internal readonly DocInverter.FieldInvertState fieldState;
        internal bool omitTf;

        public FreqProxTermsWriterPerField(TermsHashPerField termsHashPerField, FreqProxTermsWriterPerThread perThread, FieldInfo fieldInfo)
        {
            this.termsHashPerField = termsHashPerField;
            this.perThread = perThread;
            this.fieldInfo = fieldInfo;
            docState = termsHashPerField.docState;
            fieldState = termsHashPerField.fieldState;
            omitTf = fieldInfo.omitTf;
        }

        internal override int getStreamCount()
        {
            if (fieldInfo.omitTf)
                return 1;
            else
                return 2;
        }

        internal override void finish() { }

        internal bool hasPayloads;

        internal override void skippingLongTerm(Token t) { }

        public int CompareTo(object other0)
        {
            FreqProxTermsWriterPerField other = (FreqProxTermsWriterPerField)other0;
            return string.CompareOrdinal(fieldInfo.name, other.fieldInfo.name);
        }

        internal void reset()
        {
            // Record, up front, whether our in-RAM format will be
            // with or without term freqs:
            omitTf = fieldInfo.omitTf;
        }

        internal override bool start(Fieldable[] fields, int count)
        {
            for (int i = 0; i < count; i++)
                if (fields[i].IsIndexed())
                    return true;
            return false;
        }

        internal void writeProx(Token t, FreqProxTermsWriter.PostingList p, int proxCode)
        {
            Payload payload = t.GetPayload();
            if (payload != null && payload.length > 0)
            {
                termsHashPerField.writeVInt(1, (proxCode << 1) | 1);
                termsHashPerField.writeVInt(1, payload.length);
                termsHashPerField.writeBytes(1, payload.data, payload.offset, payload.length);
                hasPayloads = true;
            }
            else
                termsHashPerField.writeVInt(1, proxCode << 1);
            p.lastPosition = fieldState.position;
        }

        internal override void newTerm(Token t, RawPostingList p0)
        {
            // First time we're seeing this term since the last
            // flush
            System.Diagnostics.Debug.Assert(docState.TestPoint("FreqProxTermsWriterPerField.newTerm start"));
            FreqProxTermsWriter.PostingList p = (FreqProxTermsWriter.PostingList)p0;
            p.lastDocID = docState.docID;
            if (omitTf)
            {
                p.lastDocCode = docState.docID;
            }
            else
            {
                p.lastDocCode = docState.docID << 1;
                p.docFreq = 1;
                writeProx(t, p, fieldState.position);
            }
        }

        internal override void addTerm(Token t, RawPostingList p0)
        {

            System.Diagnostics.Debug.Assert(docState.TestPoint("FreqProxTermsWriterPerField.addTerm start"));

            FreqProxTermsWriter.PostingList p = (FreqProxTermsWriter.PostingList)p0;

            System.Diagnostics.Debug.Assert(omitTf || p.docFreq > 0);

            if (omitTf)
            {
                if (docState.docID != p.lastDocID)
                {
                    System.Diagnostics.Debug.Assert(docState.docID > p.lastDocID);
                    termsHashPerField.writeVInt(0, p.lastDocCode);
                    p.lastDocCode = docState.docID - p.lastDocID;
                    p.lastDocID = docState.docID;
                }
            }
            else
            {
                if (docState.docID != p.lastDocID)
                {
                    System.Diagnostics.Debug.Assert(docState.docID > p.lastDocID);
                    // Term not yet seen in the current doc but previously
                    // seen in other doc(s) since the last flush

                    // Now that we know doc freq for previous doc,
                    // write it & lastDocCode
                    if (1 == p.docFreq)
                        termsHashPerField.writeVInt(0, p.lastDocCode | 1);
                    else
                    {
                        termsHashPerField.writeVInt(0, p.lastDocCode);
                        termsHashPerField.writeVInt(0, p.docFreq);
                    }
                    p.docFreq = 1;
                    p.lastDocCode = (docState.docID - p.lastDocID) << 1;
                    p.lastDocID = docState.docID;
                    writeProx(t, p, fieldState.position);
                }
                else
                {
                    p.docFreq++;
                    writeProx(t, p, fieldState.position - p.lastPosition);
                }
            }
        }

        public void abort() { }
    }
}