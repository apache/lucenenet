using Lucene.Net.Analysis.TokenAttributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using ByteBlockPool = Lucene.Net.Util.ByteBlockPool;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using BytesRefHash = Lucene.Net.Util.BytesRefHash;
    using Counter = Lucene.Net.Util.Counter;
    using IntBlockPool = Lucene.Net.Util.IntBlockPool;

    internal sealed class TermsHashPerField : InvertedDocConsumerPerField
    {
        private const int HASH_INIT_SIZE = 4;

        internal readonly TermsHashConsumerPerField consumer;

        internal readonly TermsHash termsHash;

        internal readonly TermsHashPerField nextPerField;
        internal readonly DocumentsWriterPerThread.DocState docState;
        internal readonly FieldInvertState fieldState;
        internal ITermToBytesRefAttribute termAtt;
        internal BytesRef termBytesRef;

        // Copied from our perThread
        internal readonly IntBlockPool intPool;

        internal readonly ByteBlockPool bytePool;
        internal readonly ByteBlockPool termBytePool;

        internal readonly int streamCount;
        internal readonly int numPostingInt;

        internal readonly FieldInfo fieldInfo;

        internal readonly BytesRefHash bytesHash;

        internal ParallelPostingsArray postingsArray;
        private readonly Counter bytesUsed;

        public TermsHashPerField(DocInverterPerField docInverterPerField, TermsHash termsHash, TermsHash nextTermsHash, FieldInfo fieldInfo)
        {
            intPool = termsHash.intPool;
            bytePool = termsHash.bytePool;
            termBytePool = termsHash.termBytePool;
            docState = termsHash.docState;
            this.termsHash = termsHash;
            bytesUsed = termsHash.bytesUsed;
            fieldState = docInverterPerField.fieldState;
            this.consumer = termsHash.consumer.AddField(this, fieldInfo);
            PostingsBytesStartArray byteStarts = new PostingsBytesStartArray(this, bytesUsed);
            bytesHash = new BytesRefHash(termBytePool, HASH_INIT_SIZE, byteStarts);
            streamCount = consumer.StreamCount;
            numPostingInt = 2 * streamCount;
            this.fieldInfo = fieldInfo;
            if (nextTermsHash != null)
            {
                nextPerField = (TermsHashPerField)nextTermsHash.AddField(docInverterPerField, fieldInfo);
            }
            else
            {
                nextPerField = null;
            }
        }

        internal void ShrinkHash(int targetSize)
        {
            // Fully free the bytesHash on each flush but keep the pool untouched
            // bytesHash.clear will clear the ByteStartArray and in turn the ParallelPostingsArray too
            bytesHash.Clear(false);
        }

        public void Reset()
        {
            bytesHash.Clear(false);
            if (nextPerField != null)
            {
                nextPerField.Reset();
            }
        }

        public override void Abort()
        {
            Reset();
            if (nextPerField != null)
            {
                nextPerField.Abort();
            }
        }

        public void InitReader(ByteSliceReader reader, int termID, int stream)
        {
            Debug.Assert(stream < streamCount);
            int intStart = postingsArray.intStarts[termID];
            int[] ints = intPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
            int upto = intStart & IntBlockPool.INT_BLOCK_MASK;
            reader.Init(bytePool, postingsArray.byteStarts[termID] + stream * ByteBlockPool.FIRST_LEVEL_SIZE, ints[upto + stream]);
        }

        /// <summary>
        /// Collapse the hash table & sort in-place. </summary>
        public int[] SortPostings(IComparer<BytesRef> termComp)
        {
            return bytesHash.Sort(termComp);
        }

        private bool DoCall;
        private bool DoNextCall;

        internal override void Start(IIndexableField f)
        {
            termAtt = fieldState.AttributeSource.GetAttribute<ITermToBytesRefAttribute>();
            termBytesRef = termAtt.BytesRef;
            consumer.Start(f);
            if (nextPerField != null)
            {
                nextPerField.Start(f);
            }
        }

        internal override bool Start(IIndexableField[] fields, int count)
        {
            DoCall = consumer.Start(fields, count);
            bytesHash.Reinit();
            if (nextPerField != null)
            {
                DoNextCall = nextPerField.Start(fields, count);
            }
            return DoCall || DoNextCall;
        }

        // Secondary entry point (for 2nd & subsequent TermsHash),
        // because token text has already been "interned" into
        // textStart, so we hash by textStart
        public void Add(int textStart)
        {
            int termID = bytesHash.AddByPoolOffset(textStart);
            if (termID >= 0) // New posting
            {
                // First time we are seeing this token since we last
                // flushed the hash.
                // Init stream slices
                if (numPostingInt + intPool.IntUpto > IntBlockPool.INT_BLOCK_SIZE)
                {
                    intPool.NextBuffer();
                }

                if (ByteBlockPool.BYTE_BLOCK_SIZE - bytePool.ByteUpto < numPostingInt * ByteBlockPool.FIRST_LEVEL_SIZE)
                {
                    bytePool.NextBuffer();
                }

                IntUptos = intPool.Buffer;
                IntUptoStart = intPool.IntUpto;
                intPool.IntUpto += streamCount;

                postingsArray.intStarts[termID] = IntUptoStart + intPool.IntOffset;

                for (int i = 0; i < streamCount; i++)
                {
                    int upto = bytePool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                    IntUptos[IntUptoStart + i] = upto + bytePool.ByteOffset;
                }
                postingsArray.byteStarts[termID] = IntUptos[IntUptoStart];

                consumer.NewTerm(termID);
            }
            else
            {
                termID = (-termID) - 1;
                int intStart = postingsArray.intStarts[termID];
                IntUptos = intPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
                IntUptoStart = intStart & IntBlockPool.INT_BLOCK_MASK;
                consumer.AddTerm(termID);
            }
        }

        // Primary entry point (for first TermsHash)
        internal override void Add()
        {
            termAtt.FillBytesRef();

            // We are first in the chain so we must "intern" the
            // term text into textStart address
            // Get the text & hash of this term.
            int termID;
            try
            {
                termID = bytesHash.Add(termBytesRef);
            }
            catch (BytesRefHash.MaxBytesLengthExceededException)
            {
                // Term is too large; record this here (can't throw an
                // exc because DocInverterPerField will then abort the
                // entire segment) and then throw an exc later in
                // DocInverterPerField.java.  LengthFilter can always be
                // used to prune the term before indexing:
                if (docState.maxTermPrefix == null)
                {
                    int saved = termBytesRef.Length;
                    try
                    {
                        termBytesRef.Length = Math.Min(30, DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8);
                        docState.maxTermPrefix = termBytesRef.ToString();
                    }
                    finally
                    {
                        termBytesRef.Length = saved;
                    }
                }
                consumer.SkippingLongTerm();
                return;
            }
            if (termID >= 0) // New posting
            {
                bytesHash.ByteStart(termID);
                // Init stream slices
                if (numPostingInt + intPool.IntUpto > IntBlockPool.INT_BLOCK_SIZE)
                {
                    intPool.NextBuffer();
                }

                if (ByteBlockPool.BYTE_BLOCK_SIZE - bytePool.ByteUpto < numPostingInt * ByteBlockPool.FIRST_LEVEL_SIZE)
                {
                    bytePool.NextBuffer();
                }

                IntUptos = intPool.Buffer;
                IntUptoStart = intPool.IntUpto;
                intPool.IntUpto += streamCount;

                postingsArray.intStarts[termID] = IntUptoStart + intPool.IntOffset;

                for (int i = 0; i < streamCount; i++)
                {
                    int upto = bytePool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                    IntUptos[IntUptoStart + i] = upto + bytePool.ByteOffset;
                }
                postingsArray.byteStarts[termID] = IntUptos[IntUptoStart];

                consumer.NewTerm(termID);
            }
            else
            {
                termID = (-termID) - 1;
                int intStart = postingsArray.intStarts[termID];
                IntUptos = intPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
                IntUptoStart = intStart & IntBlockPool.INT_BLOCK_MASK;
                consumer.AddTerm(termID);
            }

            if (DoNextCall)
            {
                nextPerField.Add(postingsArray.textStarts[termID]);
            }
        }

        internal int[] IntUptos;
        internal int IntUptoStart;

        internal void WriteByte(int stream, sbyte b)
        {
            WriteByte(stream, (byte)b);
        }

        internal void WriteByte(int stream, byte b)
        {
            int upto = IntUptos[IntUptoStart + stream];
            var bytes = bytePool.buffers[upto >> ByteBlockPool.BYTE_BLOCK_SHIFT];
            Debug.Assert(bytes != null);
            int offset = upto & ByteBlockPool.BYTE_BLOCK_MASK;
            if (bytes[offset] != 0)
            {
                // End of slice; allocate a new one
                offset = bytePool.AllocSlice(bytes, offset);
                bytes = bytePool.Buffer;
                IntUptos[IntUptoStart + stream] = offset + bytePool.ByteOffset;
            }
            bytes[offset] = b;
            (IntUptos[IntUptoStart + stream])++;
        }

        public void WriteBytes(int stream, byte[] b, int offset, int len)
        {
            // TODO: optimize
            int end = offset + len;
            for (int i = offset; i < end; i++)
            {
                WriteByte(stream, b[i]);
            }
        }

        internal void WriteVInt(int stream, int i)
        {
            Debug.Assert(stream < streamCount);
            while ((i & ~0x7F) != 0)
            {
                WriteByte(stream, unchecked((sbyte)((i & 0x7f) | 0x80)));
                i = (int)((uint)i >> 7);
            }
            WriteByte(stream, (sbyte)i);
        }

        internal override void Finish()
        {
            consumer.Finish();
            if (nextPerField != null)
            {
                nextPerField.Finish();
            }
        }

        private sealed class PostingsBytesStartArray : BytesRefHash.BytesStartArray
        {
            private readonly TermsHashPerField perField;
            private readonly Counter bytesUsed;

            internal PostingsBytesStartArray(TermsHashPerField perField, Counter bytesUsed)
            {
                this.perField = perField;
                this.bytesUsed = bytesUsed;
            }

            public override int[] Init()
            {
                if (perField.postingsArray == null)
                {
                    perField.postingsArray = perField.consumer.CreatePostingsArray(2);
                    bytesUsed.AddAndGet(perField.postingsArray.size * perField.postingsArray.BytesPerPosting());
                }
                return perField.postingsArray.textStarts;
            }

            public override int[] Grow()
            {
                ParallelPostingsArray postingsArray = perField.postingsArray;
                int oldSize = perField.postingsArray.size;
                postingsArray = perField.postingsArray = postingsArray.Grow();
                bytesUsed.AddAndGet((postingsArray.BytesPerPosting() * (postingsArray.size - oldSize)));
                return postingsArray.textStarts;
            }

            public override int[] Clear()
            {
                if (perField.postingsArray != null)
                {
                    bytesUsed.AddAndGet(-(perField.postingsArray.size * perField.postingsArray.BytesPerPosting()));
                    perField.postingsArray = null;
                }
                return null;
            }

            public override Counter BytesUsed()
            {
                return bytesUsed;
            }
        }
    }
}