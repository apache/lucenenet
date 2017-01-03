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

        internal readonly TermsHashConsumerPerField Consumer;

        internal readonly TermsHash TermsHash;

        internal readonly TermsHashPerField NextPerField;
        internal readonly DocumentsWriterPerThread.DocState DocState;
        internal readonly FieldInvertState FieldState;
        internal ITermToBytesRefAttribute TermAtt;
        internal BytesRef TermBytesRef;

        // Copied from our perThread
        internal readonly IntBlockPool IntPool;

        internal readonly ByteBlockPool BytePool;
        internal readonly ByteBlockPool TermBytePool;

        internal readonly int StreamCount;
        internal readonly int NumPostingInt;

        internal readonly FieldInfo FieldInfo;

        internal readonly BytesRefHash BytesHash;

        internal ParallelPostingsArray PostingsArray;
        private readonly Counter BytesUsed;

        public TermsHashPerField(DocInverterPerField docInverterPerField, TermsHash termsHash, TermsHash nextTermsHash, FieldInfo fieldInfo)
        {
            IntPool = termsHash.IntPool;
            BytePool = termsHash.BytePool;
            TermBytePool = termsHash.TermBytePool;
            DocState = termsHash.DocState;
            this.TermsHash = termsHash;
            BytesUsed = termsHash.BytesUsed;
            FieldState = docInverterPerField.fieldState;
            this.Consumer = termsHash.Consumer.AddField(this, fieldInfo);
            PostingsBytesStartArray byteStarts = new PostingsBytesStartArray(this, BytesUsed);
            BytesHash = new BytesRefHash(TermBytePool, HASH_INIT_SIZE, byteStarts);
            StreamCount = Consumer.StreamCount;
            NumPostingInt = 2 * StreamCount;
            this.FieldInfo = fieldInfo;
            if (nextTermsHash != null)
            {
                NextPerField = (TermsHashPerField)nextTermsHash.AddField(docInverterPerField, fieldInfo);
            }
            else
            {
                NextPerField = null;
            }
        }

        internal void ShrinkHash(int targetSize)
        {
            // Fully free the bytesHash on each flush but keep the pool untouched
            // bytesHash.clear will clear the ByteStartArray and in turn the ParallelPostingsArray too
            BytesHash.Clear(false);
        }

        public void Reset()
        {
            BytesHash.Clear(false);
            if (NextPerField != null)
            {
                NextPerField.Reset();
            }
        }

        public override void Abort()
        {
            Reset();
            if (NextPerField != null)
            {
                NextPerField.Abort();
            }
        }

        public void InitReader(ByteSliceReader reader, int termID, int stream)
        {
            Debug.Assert(stream < StreamCount);
            int intStart = PostingsArray.intStarts[termID];
            int[] ints = IntPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
            int upto = intStart & IntBlockPool.INT_BLOCK_MASK;
            reader.Init(BytePool, PostingsArray.byteStarts[termID] + stream * ByteBlockPool.FIRST_LEVEL_SIZE, ints[upto + stream]);
        }

        /// <summary>
        /// Collapse the hash table & sort in-place. </summary>
        public int[] SortPostings(IComparer<BytesRef> termComp)
        {
            return BytesHash.Sort(termComp);
        }

        private bool DoCall;
        private bool DoNextCall;

        internal override void Start(IIndexableField f)
        {
            TermAtt = FieldState.AttributeSource.GetAttribute<ITermToBytesRefAttribute>();
            TermBytesRef = TermAtt.BytesRef;
            Consumer.Start(f);
            if (NextPerField != null)
            {
                NextPerField.Start(f);
            }
        }

        internal override bool Start(IIndexableField[] fields, int count)
        {
            DoCall = Consumer.Start(fields, count);
            BytesHash.Reinit();
            if (NextPerField != null)
            {
                DoNextCall = NextPerField.Start(fields, count);
            }
            return DoCall || DoNextCall;
        }

        // Secondary entry point (for 2nd & subsequent TermsHash),
        // because token text has already been "interned" into
        // textStart, so we hash by textStart
        public void Add(int textStart)
        {
            int termID = BytesHash.AddByPoolOffset(textStart);
            if (termID >= 0) // New posting
            {
                // First time we are seeing this token since we last
                // flushed the hash.
                // Init stream slices
                if (NumPostingInt + IntPool.IntUpto > IntBlockPool.INT_BLOCK_SIZE)
                {
                    IntPool.NextBuffer();
                }

                if (ByteBlockPool.BYTE_BLOCK_SIZE - BytePool.ByteUpto < NumPostingInt * ByteBlockPool.FIRST_LEVEL_SIZE)
                {
                    BytePool.NextBuffer();
                }

                IntUptos = IntPool.Buffer;
                IntUptoStart = IntPool.IntUpto;
                IntPool.IntUpto += StreamCount;

                PostingsArray.intStarts[termID] = IntUptoStart + IntPool.IntOffset;

                for (int i = 0; i < StreamCount; i++)
                {
                    int upto = BytePool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                    IntUptos[IntUptoStart + i] = upto + BytePool.ByteOffset;
                }
                PostingsArray.byteStarts[termID] = IntUptos[IntUptoStart];

                Consumer.NewTerm(termID);
            }
            else
            {
                termID = (-termID) - 1;
                int intStart = PostingsArray.intStarts[termID];
                IntUptos = IntPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
                IntUptoStart = intStart & IntBlockPool.INT_BLOCK_MASK;
                Consumer.AddTerm(termID);
            }
        }

        // Primary entry point (for first TermsHash)
        internal override void Add()
        {
            TermAtt.FillBytesRef();

            // We are first in the chain so we must "intern" the
            // term text into textStart address
            // Get the text & hash of this term.
            int termID;
            try
            {
                termID = BytesHash.Add(TermBytesRef);
            }
            catch (BytesRefHash.MaxBytesLengthExceededException)
            {
                // Term is too large; record this here (can't throw an
                // exc because DocInverterPerField will then abort the
                // entire segment) and then throw an exc later in
                // DocInverterPerField.java.  LengthFilter can always be
                // used to prune the term before indexing:
                if (DocState.maxTermPrefix == null)
                {
                    int saved = TermBytesRef.Length;
                    try
                    {
                        TermBytesRef.Length = Math.Min(30, DocumentsWriterPerThread.MAX_TERM_LENGTH_UTF8);
                        DocState.maxTermPrefix = TermBytesRef.ToString();
                    }
                    finally
                    {
                        TermBytesRef.Length = saved;
                    }
                }
                Consumer.SkippingLongTerm();
                return;
            }
            if (termID >= 0) // New posting
            {
                BytesHash.ByteStart(termID);
                // Init stream slices
                if (NumPostingInt + IntPool.IntUpto > IntBlockPool.INT_BLOCK_SIZE)
                {
                    IntPool.NextBuffer();
                }

                if (ByteBlockPool.BYTE_BLOCK_SIZE - BytePool.ByteUpto < NumPostingInt * ByteBlockPool.FIRST_LEVEL_SIZE)
                {
                    BytePool.NextBuffer();
                }

                IntUptos = IntPool.Buffer;
                IntUptoStart = IntPool.IntUpto;
                IntPool.IntUpto += StreamCount;

                PostingsArray.intStarts[termID] = IntUptoStart + IntPool.IntOffset;

                for (int i = 0; i < StreamCount; i++)
                {
                    int upto = BytePool.NewSlice(ByteBlockPool.FIRST_LEVEL_SIZE);
                    IntUptos[IntUptoStart + i] = upto + BytePool.ByteOffset;
                }
                PostingsArray.byteStarts[termID] = IntUptos[IntUptoStart];

                Consumer.NewTerm(termID);
            }
            else
            {
                termID = (-termID) - 1;
                int intStart = PostingsArray.intStarts[termID];
                IntUptos = IntPool.Buffers[intStart >> IntBlockPool.INT_BLOCK_SHIFT];
                IntUptoStart = intStart & IntBlockPool.INT_BLOCK_MASK;
                Consumer.AddTerm(termID);
            }

            if (DoNextCall)
            {
                NextPerField.Add(PostingsArray.textStarts[termID]);
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
            var bytes = BytePool.buffers[upto >> ByteBlockPool.BYTE_BLOCK_SHIFT];
            Debug.Assert(bytes != null);
            int offset = upto & ByteBlockPool.BYTE_BLOCK_MASK;
            if (bytes[offset] != 0)
            {
                // End of slice; allocate a new one
                offset = BytePool.AllocSlice(bytes, offset);
                bytes = BytePool.Buffer;
                IntUptos[IntUptoStart + stream] = offset + BytePool.ByteOffset;
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
            Debug.Assert(stream < StreamCount);
            while ((i & ~0x7F) != 0)
            {
                WriteByte(stream, unchecked((sbyte)((i & 0x7f) | 0x80)));
                i = (int)((uint)i >> 7);
            }
            WriteByte(stream, (sbyte)i);
        }

        internal override void Finish()
        {
            Consumer.Finish();
            if (NextPerField != null)
            {
                NextPerField.Finish();
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
                if (perField.PostingsArray == null)
                {
                    perField.PostingsArray = perField.Consumer.CreatePostingsArray(2);
                    bytesUsed.AddAndGet(perField.PostingsArray.size * perField.PostingsArray.BytesPerPosting());
                }
                return perField.PostingsArray.textStarts;
            }

            public override int[] Grow()
            {
                ParallelPostingsArray postingsArray = perField.PostingsArray;
                int oldSize = perField.PostingsArray.size;
                postingsArray = perField.PostingsArray = postingsArray.Grow();
                bytesUsed.AddAndGet((postingsArray.BytesPerPosting() * (postingsArray.size - oldSize)));
                return postingsArray.textStarts;
            }

            public override int[] Clear()
            {
                if (perField.PostingsArray != null)
                {
                    bytesUsed.AddAndGet(-(perField.PostingsArray.size * perField.PostingsArray.BytesPerPosting()));
                    perField.PostingsArray = null;
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