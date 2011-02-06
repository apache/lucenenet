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

namespace Lucene.Net.Index
{
    /// <summary>
    /// Used by DocumentsWriter to merge the postings from
    /// multiple ThreadStates when creating a segment
    /// </summary>
    internal sealed class FreqProxFieldMergeState
    {
        internal readonly FreqProxTermsWriterPerField field;
        internal readonly int numPostings;
        internal readonly CharBlockPool charPool;
        internal readonly RawPostingList[] postings;

        private FreqProxTermsWriter.PostingList p;
        internal char[] text;
        internal int textOffset;

        private int postingUpto = -1;

        internal readonly ByteSliceReader freq = new ByteSliceReader();
        internal readonly ByteSliceReader prox = new ByteSliceReader();

        internal int docID;
        internal int termFreq;

        public FreqProxFieldMergeState(FreqProxTermsWriterPerField field)
        {
            this.field = field;
            this.charPool = field.perThread.termsHashPerThread.charPool;
            this.numPostings = field.termsHashPerField.numPostings;
            this.postings = field.termsHashPerField.sortPostings();
        }

        internal bool nextTerm()
        {
            postingUpto++;
            if (postingUpto == numPostings)
                return false;

            p = (FreqProxTermsWriter.PostingList)postings[postingUpto];
            docID = 0;

            text = charPool.buffers[p.textStart >> DocumentsWriter.CHAR_BLOCK_SHIFT];
            textOffset = p.textStart & DocumentsWriter.CHAR_BLOCK_MASK;

            field.termsHashPerField.initReader(freq, p, 0);
            if (!field.fieldInfo.omitTf)
                field.termsHashPerField.initReader(prox, p, 1);

            // Should always be true
            bool result = nextDoc();
            System.Diagnostics.Debug.Assert(result);

            return true;
        }

        public bool nextDoc()
        {
            if (freq.Eof())
            {
                if (p.lastDocCode != -1)
                {
                    // Return last doc
                    docID = p.lastDocID;
                    if (!field.omitTf)
                        termFreq = p.docFreq;
                    p.lastDocCode = -1;
                    return true;
                }
                else
                    // EOF
                    return false;
            }

            int code = freq.ReadVInt();
            if (field.omitTf)
                docID += code;
            else
            {
                docID += (int)((uint)code >> 1);
                if ((code & 1) != 0)
                    termFreq = 1;
                else
                    termFreq = freq.ReadVInt();
            }

            System.Diagnostics.Debug.Assert(docID != p.lastDocID);

            return true;
        }
    }
}
