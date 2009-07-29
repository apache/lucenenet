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
using IndexOutput = Lucene.Net.Store.IndexOutput;
using Token = Lucene.Net.Analysis.Token;
using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

namespace Lucene.Net.Index
{
    internal sealed class TermVectorsTermsWriterPerField : TermsHashConsumerPerField
    {

        internal readonly TermVectorsTermsWriterPerThread perThread;
        internal readonly TermsHashPerField termsHashPerField;
        internal readonly TermVectorsTermsWriter termsWriter;
        internal readonly FieldInfo fieldInfo;
        internal readonly DocumentsWriter.DocState docState;
        internal readonly DocInverter.FieldInvertState fieldState;

        internal bool doVectors;
        internal bool doVectorPositions;
        internal bool doVectorOffsets;

        internal int maxNumPostings;

        public TermVectorsTermsWriterPerField(TermsHashPerField termsHashPerField, TermVectorsTermsWriterPerThread perThread, FieldInfo fieldInfo)
        {
            this.termsHashPerField = termsHashPerField;
            this.perThread = perThread;
            this.termsWriter = perThread.termsWriter;
            this.fieldInfo = fieldInfo;
            docState = termsHashPerField.docState;
            fieldState = termsHashPerField.fieldState;
        }

        internal override int getStreamCount()
        {
            return 2;
        }

        internal override bool start(Fieldable[] fields, int count)
        {
            doVectors = false;
            doVectorPositions = false;
            doVectorOffsets = false;

            for (int i = 0; i < count; i++)
            {
                Fieldable field = fields[i];
                if (field.IsIndexed() && field.IsTermVectorStored())
                {
                    doVectors = true;
                    doVectorPositions |= field.IsStorePositionWithTermVector();
                    doVectorOffsets |= field.IsStoreOffsetWithTermVector();
                }
            }

            if (doVectors)
            {
                if (perThread.doc == null)
                {
                    perThread.doc = termsWriter.getPerDoc();
                    perThread.doc.docID = docState.docID;
                    System.Diagnostics.Debug.Assert(perThread.doc.numVectorFields == 0);
                    System.Diagnostics.Debug.Assert(0 == perThread.doc.tvf.Length());
                    System.Diagnostics.Debug.Assert(0 == perThread.doc.tvf.GetFilePointer());
                }
                else
                {
                    System.Diagnostics.Debug.Assert(perThread.doc.docID == docState.docID);

                    if (termsHashPerField.numPostings != 0)
                        // Only necessary if previous doc hit a
                        // non-aborting exception while writing vectors in
                        // this field:
                        termsHashPerField.reset();
                }
            }

            // TODO: only if needed for performance
            //perThread.postingsCount = 0;

            return doVectors;
        }

        public void abort() { }

        /** Called once per field per document if term vectors
         *  are enabled, to write the vectors to
         *  RAMOutputStream, which is then quickly flushed to
         *  * the real term vectors files in the Directory. */
        internal override void finish()
        {

            System.Diagnostics.Debug.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.finish start"));

            int numPostings = termsHashPerField.numPostings;

            System.Diagnostics.Debug.Assert(numPostings >= 0);

            if (!doVectors || numPostings == 0)
                return;

            if (numPostings > maxNumPostings)
                maxNumPostings = numPostings;

            IndexOutput tvf = perThread.doc.tvf;

            // This is called once, after inverting all occurences
            // of a given field in the doc.  At this point we flush
            // our hash into the DocWriter.

            System.Diagnostics.Debug.Assert(fieldInfo.storeTermVector);
            System.Diagnostics.Debug.Assert(perThread.vectorFieldsInOrder(fieldInfo));

            perThread.doc.addField(termsHashPerField.fieldInfo.number);

            RawPostingList[] postings = termsHashPerField.sortPostings();

            tvf.WriteVInt(numPostings);
            byte bits = 0x0;
            if (doVectorPositions)
                bits |= TermVectorsReader.STORE_POSITIONS_WITH_TERMVECTOR;
            if (doVectorOffsets)
                bits |= TermVectorsReader.STORE_OFFSET_WITH_TERMVECTOR;
            tvf.WriteByte(bits);

            int encoderUpto = 0;
            int lastTermBytesCount = 0;

            ByteSliceReader reader = perThread.vectorSliceReader;
            char[][] charBuffers = perThread.termsHashPerThread.charPool.buffers;
            for (int j = 0; j < numPostings; j++)
            {
                TermVectorsTermsWriter.PostingList posting = (TermVectorsTermsWriter.PostingList)postings[j];
                int freq = posting.freq;

                char[] text2 = charBuffers[posting.textStart >> DocumentsWriter.CHAR_BLOCK_SHIFT];
                int start2 = posting.textStart & DocumentsWriter.CHAR_BLOCK_MASK;

                // We swap between two encoders to save copying
                // last Term's byte array
                UnicodeUtil.UTF8Result utf8Result = perThread.utf8Results[encoderUpto];

                // TODO: we could do this incrementally
                UnicodeUtil.UTF16toUTF8(text2, start2, utf8Result);
                int termBytesCount = utf8Result.length;

                // TODO: UTF16toUTF8 could tell us this prefix
                // Compute common prefix between last term and
                // this term
                int prefix = 0;
                if (j > 0)
                {
                    byte[] lastTermBytes = perThread.utf8Results[1 - encoderUpto].result;
                    byte[] termBytes = perThread.utf8Results[encoderUpto].result;
                    while (prefix < lastTermBytesCount && prefix < termBytesCount)
                    {
                        if (lastTermBytes[prefix] != termBytes[prefix])
                            break;
                        prefix++;
                    }
                }
                encoderUpto = 1 - encoderUpto;
                lastTermBytesCount = termBytesCount;

                int suffix = termBytesCount - prefix;
                tvf.WriteVInt(prefix);
                tvf.WriteVInt(suffix);
                tvf.WriteBytes(utf8Result.result, prefix, suffix);
                tvf.WriteVInt(freq);

                if (doVectorPositions)
                {
                    termsHashPerField.initReader(reader, posting, 0);
                    reader.WriteTo(tvf);
                }

                if (doVectorOffsets)
                {
                    termsHashPerField.initReader(reader, posting, 1);
                    reader.WriteTo(tvf);
                }
            }

            termsHashPerField.reset();
            perThread.termsHashPerThread.reset(false);
        }

        internal void shrinkHash()
        {
            termsHashPerField.shrinkHash(maxNumPostings);
            maxNumPostings = 0;
        }

        internal override void newTerm(Token t, RawPostingList p0)
        {

            System.Diagnostics.Debug.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.newTerm start"));

            TermVectorsTermsWriter.PostingList p = (TermVectorsTermsWriter.PostingList)p0;

            p.freq = 1;

            if (doVectorOffsets)
            {
                int startOffset = fieldState.offset + t.StartOffset();
                int endOffset = fieldState.offset + t.EndOffset();
                termsHashPerField.writeVInt(1, startOffset);
                termsHashPerField.writeVInt(1, endOffset - startOffset);
                p.lastOffset = endOffset;
            }

            if (doVectorPositions)
            {
                termsHashPerField.writeVInt(0, fieldState.position);
                p.lastPosition = fieldState.position;
            }
        }

        internal override void addTerm(Token t, RawPostingList p0)
        {

            System.Diagnostics.Debug.Assert(docState.TestPoint("TermVectorsTermsWriterPerField.addTerm start"));

            TermVectorsTermsWriter.PostingList p = (TermVectorsTermsWriter.PostingList)p0;
            p.freq++;

            if (doVectorOffsets)
            {
                int startOffset = fieldState.offset + t.StartOffset();
                int endOffset = fieldState.offset + t.EndOffset();
                termsHashPerField.writeVInt(1, startOffset - p.lastOffset);
                termsHashPerField.writeVInt(1, endOffset - startOffset);
                p.lastOffset = endOffset;
            }

            if (doVectorPositions)
            {
                termsHashPerField.writeVInt(0, fieldState.position - p.lastPosition);
                p.lastPosition = fieldState.position;
            }
        }

        internal override void skippingLongTerm(Token t) { }
    }
}
