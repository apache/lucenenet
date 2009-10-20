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

using System.Collections.Generic;

using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;
using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

namespace Lucene.Net.Index
{
    internal sealed class FreqProxTermsWriter : TermsHashConsumer
    {

        internal override TermsHashConsumerPerThread addThread(TermsHashPerThread perThread)
        {
            return new FreqProxTermsWriterPerThread(perThread);
        }

        internal override void createPostings(RawPostingList[] postings, int start, int count)
        {
            int end = start + count;
            for (int i = start; i < end; i++)
                postings[i] = new PostingList();
        }

        private static int compareText(char[] text1, int pos1, char[] text2, int pos2)
        {
            while (true)
            {
                char c1 = text1[pos1++];
                char c2 = text2[pos2++];
                if (c1 != c2)
                {
                    if (0xffff == c2)
                        return 1;
                    else if (0xffff == c1)
                        return -1;
                    else
                        return c1 - c2;
                }
                else if (0xffff == c1)
                    return 0;
            }
        }

        internal override void closeDocStore(DocumentsWriter.FlushState state) { }
        internal override void Abort() { }


        // TODO: would be nice to factor out morme of this, eg the
        // FreqProxFieldMergeState, and code to visit all Fields
        // under the same FieldInfo together, up into TermsHash*.
        // Other writers would presumably share alot of this...

        internal override void flush(IDictionary<object, object> threadsAndFields, DocumentsWriter.FlushState state)
        {

            // Gather all FieldData's that have postings, across all
            // ThreadStates
            List<object> allFields = new List<object>();

            IEnumerator<KeyValuePair<object, object>> it = threadsAndFields.GetEnumerator();
            while (it.MoveNext())
            {

                KeyValuePair<object, object> entry = (KeyValuePair<object, object>)it.Current;

                ICollection<object> fields = (ICollection<object>)entry.Value;

                IEnumerator<object> fieldsIt = fields.GetEnumerator();

                while (fieldsIt.MoveNext())
                {
                    FreqProxTermsWriterPerField perField = (FreqProxTermsWriterPerField)fieldsIt.Current;
                    if (perField.termsHashPerField.numPostings > 0)
                        allFields.Add(perField);
                }
            }

            // Sort by field name
            allFields.Sort();
            int numAllFields = allFields.Count;

            TermInfosWriter termsOut = new TermInfosWriter(state.directory,
                                                                 state.segmentName,
                                                                 fieldInfos,
                                                                 state.docWriter.writer.GetTermIndexInterval());

            IndexOutput freqOut = state.directory.CreateOutput(state.SegmentFileName(IndexFileNames.FREQ_EXTENSION));
            IndexOutput proxOut;

            if (fieldInfos.HasProx())
                proxOut = state.directory.CreateOutput(state.SegmentFileName(IndexFileNames.PROX_EXTENSION));
            else
                proxOut = null;

            DefaultSkipListWriter skipListWriter = new DefaultSkipListWriter(termsOut.skipInterval,
                                                                                   termsOut.maxSkipLevels,
                                                                                   state.numDocsInRAM, freqOut, proxOut);

            int start = 0;
            while (start < numAllFields)
            {
                FieldInfo fieldInfo = ((FreqProxTermsWriterPerField)allFields[start]).fieldInfo;
                string fieldName = fieldInfo.name;

                int end = start + 1;
                while (end < numAllFields && ((FreqProxTermsWriterPerField)allFields[end]).fieldInfo.name.Equals(fieldName))
                    end++;

                FreqProxTermsWriterPerField[] fields = new FreqProxTermsWriterPerField[end - start];
                for (int i = start; i < end; i++)
                {
                    fields[i - start] = (FreqProxTermsWriterPerField)allFields[i];

                    // Aggregate the storePayload as seen by the same
                    // field across multiple threads
                    fieldInfo.storePayloads |= fields[i - start].hasPayloads;
                }

                // If this field has postings then add them to the
                // segment
                AppendPostings(state, fields, termsOut, freqOut, proxOut, skipListWriter);

                for (int i = 0; i < fields.Length; i++)
                {
                    TermsHashPerField perField = fields[i].termsHashPerField;
                    int numPostings = perField.numPostings;
                    perField.reset();
                    perField.shrinkHash(numPostings);
                    fields[i].reset();
                }

                start = end;
            }

            it = threadsAndFields.GetEnumerator();
            while (it.MoveNext())
            {
                KeyValuePair<object, object> entry = (KeyValuePair<object, object>)it.Current;
                FreqProxTermsWriterPerThread perThread = (FreqProxTermsWriterPerThread)entry.Key;
                perThread.termsHashPerThread.reset(true);
            }

            freqOut.Close();
            if (proxOut != null)
            {
                state.flushedFiles[state.SegmentFileName(IndexFileNames.PROX_EXTENSION)] = state.SegmentFileName(IndexFileNames.PROX_EXTENSION);
                proxOut.Close();
            }
            termsOut.Close();

            // Record all files we have flushed
            state.flushedFiles[state.SegmentFileName(IndexFileNames.FIELD_INFOS_EXTENSION)] = state.SegmentFileName(IndexFileNames.FIELD_INFOS_EXTENSION);
            state.flushedFiles[state.SegmentFileName(IndexFileNames.FREQ_EXTENSION)] = state.SegmentFileName(IndexFileNames.FREQ_EXTENSION);
            state.flushedFiles[state.SegmentFileName(IndexFileNames.TERMS_EXTENSION)] = state.SegmentFileName(IndexFileNames.TERMS_EXTENSION);
            state.flushedFiles[state.SegmentFileName(IndexFileNames.TERMS_INDEX_EXTENSION)] = state.SegmentFileName(IndexFileNames.TERMS_INDEX_EXTENSION);
        }

        byte[] copyByteBuffer = new byte[4096];

        /** Copy numBytes from srcIn to destIn */
        void copyBytes(IndexInput srcIn, IndexOutput destIn, long numBytes)
        {
            // TODO: we could do this more efficiently (save a copy)
            // because it's always from a ByteSliceReader ->
            // IndexOutput
            while (numBytes > 0)
            {
                int chunk;
                if (numBytes > 4096)
                    chunk = 4096;
                else
                    chunk = (int)numBytes;
                srcIn.ReadBytes(copyByteBuffer, 0, chunk);
                destIn.WriteBytes(copyByteBuffer, 0, chunk);
                numBytes -= chunk;
            }
        }

        /* Walk through all unique text tokens (Posting
         * instances) found in this field and serialize them
         * into a single RAM segment. */
        void AppendPostings(DocumentsWriter.FlushState flushState,
                            FreqProxTermsWriterPerField[] fields,
                            TermInfosWriter termsOut,
                            IndexOutput freqOut,
                            IndexOutput proxOut,
                            DefaultSkipListWriter skipListWriter)
        {

            int fieldNumber = fields[0].fieldInfo.number;
            int numFields = fields.Length;

            FreqProxFieldMergeState[] mergeStates = new FreqProxFieldMergeState[numFields];

            for (int i = 0; i < numFields; i++)
            {
                FreqProxFieldMergeState fms = mergeStates[i] = new FreqProxFieldMergeState(fields[i]);

                System.Diagnostics.Debug.Assert(fms.field.fieldInfo == fields[0].fieldInfo);

                // Should always be true
                bool result = fms.nextTerm();
                System.Diagnostics.Debug.Assert(result);
            }

            int skipInterval = termsOut.skipInterval;
            bool currentFieldOmitTf = fields[0].fieldInfo.omitTf;

            // If current field omits tf then it cannot store
            // payloads.  We silently drop the payloads in this case:
            bool currentFieldStorePayloads = currentFieldOmitTf ? false : fields[0].fieldInfo.storePayloads;

            FreqProxFieldMergeState[] termStates = new FreqProxFieldMergeState[numFields];

            while (numFields > 0)
            {

                // Get the next term to merge
                termStates[0] = mergeStates[0];
                int numToMerge = 1;

                for (int i = 1; i < numFields; i++)
                {
                    char[] text = mergeStates[i].text;
                    int textOffset = mergeStates[i].textOffset;
                    int cmp = compareText(text, textOffset, termStates[0].text, termStates[0].textOffset);

                    if (cmp < 0)
                    {
                        termStates[0] = mergeStates[i];
                        numToMerge = 1;
                    }
                    else if (cmp == 0)
                        termStates[numToMerge++] = mergeStates[i];
                }

                int df = 0;
                int lastPayloadLength = -1;

                int lastDoc = 0;

                char[] text_Renamed = termStates[0].text;
                int start = termStates[0].textOffset;

                long freqPointer = freqOut.GetFilePointer();
                long proxPointer;
                if (proxOut != null)
                    proxPointer = proxOut.GetFilePointer();
                else
                    proxPointer = 0;

                skipListWriter.ResetSkip();

                // Now termStates has numToMerge FieldMergeStates
                // which all share the same term.  Now we must
                // interleave the docID streams.
                while (numToMerge > 0)
                {

                    if ((++df % skipInterval) == 0)
                    {
                        skipListWriter.SetSkipData(lastDoc, currentFieldStorePayloads, lastPayloadLength);
                        skipListWriter.BufferSkip(df);
                    }

                    FreqProxFieldMergeState minState = termStates[0];
                    for (int i = 1; i < numToMerge; i++)
                        if (termStates[i].docID < minState.docID)
                            minState = termStates[i];

                    int doc = minState.docID;
                    int termDocFreq = minState.termFreq;

                    System.Diagnostics.Debug.Assert(doc < flushState.numDocsInRAM);
                    System.Diagnostics.Debug.Assert(doc > lastDoc || df == 1);

                    ByteSliceReader prox = minState.prox;

                    // Carefully copy over the prox + payload info,
                    // changing the format to match Lucene's segment
                    // format.
                    if (!currentFieldOmitTf)
                    {
                        // omitTf == false so we do write positions & payload          
                        System.Diagnostics.Debug.Assert(proxOut != null);
                        for (int j = 0; j < termDocFreq; j++)
                        {
                            int code = prox.ReadVInt();
                            if (currentFieldStorePayloads)
                            {
                                int payloadLength;
                                if ((code & 1) != 0)
                                {
                                    // This position has a payload
                                    payloadLength = prox.ReadVInt();
                                }
                                else
                                    payloadLength = 0;
                                if (payloadLength != lastPayloadLength)
                                {
                                    proxOut.WriteVInt(code | 1);
                                    proxOut.WriteVInt(payloadLength);
                                    lastPayloadLength = payloadLength;
                                }
                                else
                                    proxOut.WriteVInt(code & (~1));
                                if (payloadLength > 0)
                                    copyBytes(prox, proxOut, payloadLength);
                            }
                            else
                            {
                                System.Diagnostics.Debug.Assert(0 == (code & 1));
                                proxOut.WriteVInt(code >> 1);
                            }
                        } //End for

                        int newDocCode = (doc - lastDoc) << 1;

                        if (1 == termDocFreq)
                        {
                            freqOut.WriteVInt(newDocCode | 1);
                        }
                        else
                        {
                            freqOut.WriteVInt(newDocCode);
                            freqOut.WriteVInt(termDocFreq);
                        }
                    }
                    else
                    {
                        // omitTf==true: we store only the docs, without
                        // term freq, positions, payloads
                        freqOut.WriteVInt(doc - lastDoc);
                    }

                    lastDoc = doc;

                    if (!minState.nextDoc())
                    {

                        // Remove from termStates
                        int upto = 0;
                        for (int i = 0; i < numToMerge; i++)
                            if (termStates[i] != minState)
                                termStates[upto++] = termStates[i];
                        numToMerge--;
                        System.Diagnostics.Debug.Assert(upto == numToMerge);

                        // Advance this state to the next term

                        if (!minState.nextTerm())
                        {
                            // OK, no more terms, so remove from mergeStates
                            // as well
                            upto = 0;
                            for (int i = 0; i < numFields; i++)
                                if (mergeStates[i] != minState)
                                    mergeStates[upto++] = mergeStates[i];
                            numFields--;
                            System.Diagnostics.Debug.Assert(upto == numFields);
                        }
                    }
                }

                System.Diagnostics.Debug.Assert(df > 0);

                // Done merging this term

                long skipPointer = skipListWriter.WriteSkip(freqOut);

                // Write term
                termInfo.Set(df, freqPointer, proxPointer, (int)(skipPointer - freqPointer));

                // TODO: we could do this incrementally
                UnicodeUtil.UTF16toUTF8(text_Renamed, start, termsUTF8);

                // TODO: we could save O(n) re-scan of the term by
                // computing the shared prefix with the last term
                // while during the UTF8 encoding
                termsOut.Add(fieldNumber,
                             termsUTF8.result,
                             termsUTF8.length,
                             termInfo);
            }
        }

        private readonly TermInfo termInfo = new TermInfo(); // minimize consing

        internal readonly UnicodeUtil.UTF8Result termsUTF8 = new UnicodeUtil.UTF8Result();

        void files(ICollection<object> files) { }

        internal sealed class PostingList : RawPostingList
        {
            internal int docFreq;                                    // # times this term occurs in the current doc
            internal int lastDocID;                                  // Last docID where this term occurred
            internal int lastDocCode;                                // Code for prior doc
            internal int lastPosition;                               // Last position where this term occurred
        }

        internal override int bytesPerPosting()
        {
            return RawPostingList.BYTES_SIZE + 4 * DocumentsWriter.INT_NUM_BYTE;
        }
    }
}
