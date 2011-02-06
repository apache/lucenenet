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
    internal sealed class TermsHashPerThread : InvertedDocConsumerPerThread
    {

        internal readonly TermsHash termsHash;
        internal readonly TermsHashConsumerPerThread consumer;
        internal readonly TermsHashPerThread nextPerThread;

        internal readonly CharBlockPool charPool;
        internal readonly IntBlockPool intPool;
        internal readonly ByteBlockPool bytePool;
        internal readonly bool primary;
        internal readonly DocumentsWriter.DocState docState;

        internal readonly RawPostingList[] freePostings = new RawPostingList[256];
        internal int freePostingsCount;

        public TermsHashPerThread(DocInverterPerThread docInverterPerThread, TermsHash termsHash, TermsHash nextTermsHash, TermsHashPerThread primaryPerThread)
        {
            docState = docInverterPerThread.docState;

            this.termsHash = termsHash;
            this.consumer = termsHash.consumer.addThread(this);

            if (nextTermsHash != null)
            {
                // We are primary
                charPool = new CharBlockPool(termsHash.docWriter);
                primary = true;
            }
            else
            {
                charPool = primaryPerThread.charPool;
                primary = false;
            }

            intPool = new IntBlockPool(termsHash.docWriter, termsHash.trackAllocations);
            bytePool = new ByteBlockPool(termsHash.docWriter.byteBlockAllocator, termsHash.trackAllocations);

            if (nextTermsHash != null)
                nextPerThread = nextTermsHash.addThread(docInverterPerThread, this);
            else
                nextPerThread = null;
        }

        internal override InvertedDocConsumerPerField addField(DocInverterPerField docInverterPerField, FieldInfo fieldInfo)
        {
            return new TermsHashPerField(docInverterPerField, this, nextPerThread, fieldInfo);
        }

        internal override void abort()
        {
            lock (this)
            {
                reset(true);
                consumer.abort();
                if (nextPerThread != null)
                    nextPerThread.abort();
            }
        }

        // perField calls this when it needs more postings:
        internal void morePostings()
        {
            System.Diagnostics.Debug.Assert(freePostingsCount == 0);
            termsHash.getPostings(freePostings);
            freePostingsCount = freePostings.Length;
            System.Diagnostics.Debug.Assert(noNullPostings(freePostings, freePostingsCount, "consumer=" + consumer));
        }

        private static bool noNullPostings(RawPostingList[] postings, int count, string details)
        {
            for (int i = 0; i < count; i++)
                System.Diagnostics.Debug.Assert(postings[i] != null, "postings[" + i + "] of " + count + " is null: " + details);
            return true;
        }

        internal override void startDocument()
        {
            consumer.startDocument();
            if (nextPerThread != null)
                nextPerThread.consumer.startDocument();
        }

        internal override DocumentsWriter.DocWriter finishDocument()
        {
            DocumentsWriter.DocWriter doc = consumer.finishDocument();

            DocumentsWriter.DocWriter doc2;
            if (nextPerThread != null)
                doc2 = nextPerThread.consumer.finishDocument();
            else
                doc2 = null;
            if (doc == null)
                return doc2;
            else
            {
                doc.SetNext(doc2);
                return doc;
            }
        }

        // Clear all state
        internal void reset(bool recyclePostings)
        {
            intPool.reset();
            bytePool.Reset();

            if (primary)
                charPool.reset();

            if (recyclePostings)
            {
                termsHash.recyclePostings(freePostings, freePostingsCount);
                freePostingsCount = 0;
            }
        }
    }
}
