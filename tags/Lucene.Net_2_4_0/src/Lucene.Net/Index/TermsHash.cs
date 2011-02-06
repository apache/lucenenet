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

using ArrayUtil = Lucene.Net.Util.ArrayUtil;

namespace Lucene.Net.Index
{
    /** This class implements {@link InvertedDocConsumer}, which
     *  is passed each token produced by the analyzer on each
     *  field.  It stores these tokens in a hash table, and
     *  allocates separate byte streams per token.  Consumers of
     *  this class, eg {@link FreqProxTermsWriter} and {@link
     *  TermVectorsTermsWriter}, write their own byte streams
     *  under each term.
     */
    sealed internal class TermsHash : InvertedDocConsumer
    {

        internal readonly TermsHashConsumer consumer;
        internal readonly TermsHash nextTermsHash;
        internal readonly int bytesPerPosting;
        internal readonly int postingsFreeChunk;
        internal readonly DocumentsWriter docWriter;

        //private TermsHash primaryTermsHash;

        private RawPostingList[] postingsFreeList = new RawPostingList[1];
        private int postingsFreeCount;
        private int postingsAllocCount;
        internal bool trackAllocations;

        public TermsHash(DocumentsWriter docWriter, bool trackAllocations, TermsHashConsumer consumer, TermsHash nextTermsHash)
        {
            this.docWriter = docWriter;
            this.consumer = consumer;
            this.nextTermsHash = nextTermsHash;
            this.trackAllocations = trackAllocations;

            // Why + 4*POINTER_NUM_BYTE below?
            //   +1: Posting is referenced by postingsFreeList array
            //   +3: Posting is referenced by hash, which
            //       targets 25-50% fill factor; approximate this
            //       as 3X # pointers
            bytesPerPosting = consumer.bytesPerPosting() + 4 * DocumentsWriter.POINTER_NUM_BYTE;
            postingsFreeChunk = (int)(DocumentsWriter.BYTE_BLOCK_SIZE / bytesPerPosting);
        }

        internal override InvertedDocConsumerPerThread addThread(DocInverterPerThread docInverterPerThread)
        {
            return new TermsHashPerThread(docInverterPerThread, this, nextTermsHash, null);
        }

        internal TermsHashPerThread addThread(DocInverterPerThread docInverterPerThread, TermsHashPerThread primaryPerThread)
        {
            return new TermsHashPerThread(docInverterPerThread, this, nextTermsHash, primaryPerThread);
        }

        internal override void setFieldInfos(FieldInfos fieldInfos)
        {
            this.fieldInfos = fieldInfos;
            consumer.setFieldInfos(fieldInfos);
        }

        internal override void abort()
        {
            lock (this)
            {
                consumer.Abort();
                if (nextTermsHash != null)
                    nextTermsHash.abort();
            }
        }

        internal void shrinkFreePostings(IDictionary<object, ICollection<object>> threadsAndFields, DocumentsWriter.FlushState state)
        {

            System.Diagnostics.Debug.Assert(postingsFreeCount == postingsAllocCount, System.Threading.Thread.CurrentThread.Name + ": postingsFreeCount=" + postingsFreeCount + " postingsAllocCount=" + postingsAllocCount + " consumer=" + consumer);

            int newSize = ArrayUtil.GetShrinkSize(postingsFreeList.Length, postingsAllocCount);
            if (newSize != postingsFreeList.Length)
            {
                RawPostingList[] newArray = new RawPostingList[newSize];
                System.Array.Copy(postingsFreeList, 0, newArray, 0, postingsFreeCount);
                postingsFreeList = newArray;
            }
        }

        internal override void closeDocStore(DocumentsWriter.FlushState state)
        {
            lock (this)
            {
                consumer.closeDocStore(state);
                if (nextTermsHash != null)
                    nextTermsHash.closeDocStore(state);
            }
        }

        internal override void flush(IDictionary<object, ICollection<object>> threadsAndFields, DocumentsWriter.FlushState state)
        {
            lock (this)
            {
                IDictionary<object, object> childThreadsAndFields = new Dictionary<object, object>();
                IDictionary<object, ICollection<object>> nextThreadsAndFields;

                if (nextTermsHash != null)
                    nextThreadsAndFields = new Dictionary<object, ICollection<object>>();
                else
                    nextThreadsAndFields = null;

                IEnumerator<KeyValuePair<object, ICollection<object>>> it = threadsAndFields.GetEnumerator();
                while (it.MoveNext())
                {

                    KeyValuePair<object, ICollection<object>> entry = it.Current;

                    TermsHashPerThread perThread = (TermsHashPerThread)entry.Key;

                    ICollection<object> fields = entry.Value;

                    IEnumerator<object> fieldsIt = fields.GetEnumerator();
                    IDictionary<object, object> childFields = new Dictionary<object, object>();
                    IDictionary<object, object> nextChildFields;

                    if (nextTermsHash != null)
                        nextChildFields = new Dictionary<object, object>();
                    else
                        nextChildFields = null;

                    while (fieldsIt.MoveNext())
                    {
                        TermsHashPerField perField = (TermsHashPerField)fieldsIt.Current;
                        childFields[perField.consumer] = perField.consumer;
                        if (nextTermsHash != null)
                            nextChildFields[perField.nextPerField] = perField.nextPerField;
                    }

                    childThreadsAndFields[perThread.consumer] = childFields.Keys;
                    if (nextTermsHash != null)
                        nextThreadsAndFields[perThread.nextPerThread] = nextChildFields.Keys;
                }

                consumer.flush(childThreadsAndFields, state);

                shrinkFreePostings(threadsAndFields, state);

                if (nextTermsHash != null)
                    nextTermsHash.flush(nextThreadsAndFields, state);
            }
        }

        internal override bool freeRAM()
        {
            lock (this)
            {

                if (!trackAllocations)
                    return false;

                bool any;
                int numToFree;
                if (postingsFreeCount >= postingsFreeChunk)
                    numToFree = postingsFreeChunk;
                else
                    numToFree = postingsFreeCount;
                any = numToFree > 0;
                if (any)
                {
                    SupportClass.CollectionsSupport.ArrayFill(postingsFreeList, postingsFreeCount - numToFree, postingsFreeCount, null);
                    postingsFreeCount -= numToFree;
                    postingsAllocCount -= numToFree;
                    docWriter.BytesAllocated(-numToFree * bytesPerPosting);
                    any = true;
                }

                if (nextTermsHash != null)
                    any |= nextTermsHash.freeRAM();

                return any;
            }
        }

        public void recyclePostings(RawPostingList[] postings, int numPostings)
        {
            lock (this)
            {

                System.Diagnostics.Debug.Assert(postings.Length >= numPostings);

                // Move all Postings from this ThreadState back to our
                // free list.  We pre-allocated this array while we were
                // creating Postings to make sure it's large enough
                System.Diagnostics.Debug.Assert(postingsFreeCount + numPostings <= postingsFreeList.Length);
                System.Array.Copy(postings, 0, postingsFreeList, postingsFreeCount, numPostings);
                postingsFreeCount += numPostings;
            }
        }

        public void getPostings(RawPostingList[] postings)
        {
            lock (this)
            {
                System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("TermsHash.getPostings start"));

                System.Diagnostics.Debug.Assert(postingsFreeCount <= postingsFreeList.Length);
                System.Diagnostics.Debug.Assert(postingsFreeCount <= postingsAllocCount, "postingsFreeCount=" + postingsFreeCount + " postingsAllocCount=" + postingsAllocCount);

                int numToCopy;
                if (postingsFreeCount < postings.Length)
                    numToCopy = postingsFreeCount;
                else
                    numToCopy = postings.Length;
                int start = postingsFreeCount - numToCopy;
                System.Diagnostics.Debug.Assert(start >= 0);
                System.Diagnostics.Debug.Assert(start + numToCopy <= postingsFreeList.Length);
                System.Diagnostics.Debug.Assert(numToCopy <= postings.Length);
                System.Array.Copy(postingsFreeList, start, postings, 0, numToCopy);

                // Directly allocate the remainder if any
                if (numToCopy != postings.Length)
                {
                    int extra = postings.Length - numToCopy;
                    int newPostingsAllocCount = postingsAllocCount + extra;

                    consumer.createPostings(postings, numToCopy, extra);
                    System.Diagnostics.Debug.Assert(docWriter.writer.TestPoint("TermsHash.getPostings after create"));
                    postingsAllocCount += extra;

                    if (trackAllocations)
                        docWriter.BytesAllocated(extra * bytesPerPosting);

                    if (newPostingsAllocCount > postingsFreeList.Length)
                        // Pre-allocate the postingsFreeList so it's large
                        // enough to hold all postings we've given out
                        postingsFreeList = new RawPostingList[ArrayUtil.GetNextSize(newPostingsAllocCount)];
                }

                postingsFreeCount -= numToCopy;

                if (trackAllocations)
                    docWriter.BytesUsed(postings.Length * bytesPerPosting);
            }
        }
    }
}
