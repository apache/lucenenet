using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Store
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

    /// <summary>
    /// Base <see cref="IndexInput"/> implementation that uses an array
    /// of <see cref="ByteBuffer"/>s to represent a file.
    /// <para/>
    /// Because Java's <see cref="ByteBuffer"/> uses an <see cref="int"/> to address the
    /// values, it's necessary to access a file greater
    /// <see cref="int.MaxValue"/> in size using multiple byte buffers.
    /// <para/>
    /// For efficiency, this class requires that the buffers
    /// are a power-of-two (<c>chunkSizePower</c>).
    /// </summary>
    public abstract class ByteBufferIndexInput : IndexInput
    {
        private ByteBuffer[] buffers;

        private readonly long chunkSizeMask;
        private readonly int chunkSizePower;

        private int offset;
        private long length;
        private string sliceDescription;

        private int curBufIndex;

        private ByteBuffer curBuf; // redundant for speed: buffers[curBufIndex]
        private bool isClone = false;
        // LUCENENET: Using ConditionalWeakTable rather than WeakIdenityMap. ConditionalWeakTable
        // uses RuntimeHelpers.GetHashCode() to find the item, so technically, it IS an identity collection.
        private ConditionalWeakTable<ByteBufferIndexInput, BoolRefWrapper> clones;

        private class BoolRefWrapper
        {
            private readonly bool value;

            // .NET port: this is needed as bool is not a reference type
            public BoolRefWrapper(bool value)
            {
                this.value = value;
            }

            public static implicit operator bool(BoolRefWrapper value)
            {
                return value.value;
            }

            public static implicit operator BoolRefWrapper(bool value)
            {
                return new BoolRefWrapper(value);
            }
        }

        private protected ByteBufferIndexInput(string resourceDescription, ByteBuffer[] buffers, long length, int chunkSizePower, bool trackClones) // LUCENENET: Changed from internal to private protected
            : base(resourceDescription)
        {
            //this.buffers = buffers; // LUCENENET: this is set in SetBuffers()
            this.length = length;
            this.chunkSizePower = chunkSizePower;
            this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            // LUCENENET: Using ConditionalWeakTable rather than WeakIdenityMap. ConditionalWeakTable
            // uses RuntimeHelpers.GetHashCode() to find the item, so technically, it IS an identity collection.
            this.clones = trackClones ? new ConditionalWeakTable<ByteBufferIndexInput, BoolRefWrapper>() : null;

            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(chunkSizePower >= 0 && chunkSizePower <= 30);
                Debugging.Assert(length.TripleShift(chunkSizePower) < int.MaxValue);
            }

            // LUCENENET specific: MMapIndexInput calls SetBuffers() to populate
            // the buffers, so we need to skip that call if it is null here, and
            // do the seek inside SetBuffers()
            if (buffers != null)
            {
                SetBuffers(buffers);
            }
        }

        // LUCENENET specific for encapsulating buffers field.
        internal void SetBuffers(ByteBuffer[] buffers) // necessary for MMapIndexInput
        {
            this.buffers = buffers;
            Seek(0L);
        }

        public override sealed byte ReadByte()
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            if (curBuf.HasRemaining)
            {
                return curBuf.Get();
            }

            do
            {
                curBufIndex++;
                if (curBufIndex >= buffers.Length)
                {
                    throw EOFException.Create("read past EOF: " + this);
                }
                curBuf = buffers[curBufIndex];
                curBuf.Position = 0;
            } while (!curBuf.HasRemaining);
            return curBuf.Get();
        }

        public override sealed void ReadBytes(byte[] b, int offset, int len)
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            int curAvail = curBuf.Remaining;
            if (len <= curAvail)
            {
                curBuf.Get(b, offset, len);
            }
            else
            {
                while (len > curAvail)
                {
                    curBuf.Get(b, offset, curAvail);
                    len -= curAvail;
                    offset += curAvail;
                    curBufIndex++;
                    if (curBufIndex >= buffers.Length)
                    {
                        throw EOFException.Create("read past EOF: " + this);
                    }
                    curBuf = buffers[curBufIndex];
                    curBuf.Position = 0;
                    curAvail = curBuf.Remaining;
                }
                curBuf.Get(b, offset, len);
            }
        }

        /// <summary>
        /// NOTE: this was readShort() in Lucene
        /// </summary>
        public override sealed short ReadInt16()
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            if (curBuf.Remaining >= 2)
            {
                return curBuf.GetInt16();
            }
            return base.ReadInt16();
        }

        /// <summary>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        public override sealed int ReadInt32()
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            if (curBuf.Remaining >= 4)
            {
                return curBuf.GetInt32();
            }
            return base.ReadInt32();
        }

        /// <summary>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        public override sealed long ReadInt64()
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            if (curBuf.Remaining >= 8)
            {
                return curBuf.GetInt64();
            }
            return base.ReadInt64();
        }

        public override sealed long Position // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        {
            get
            {
                // LUCENENET: Refactored to avoid calls on invalid conditions instead of
                // catching and re-throwing exceptions in the normal workflow.
                EnsureOpen();
                return (((long)curBufIndex) << chunkSizePower) + curBuf.Position - offset;
            }
        }

        public override sealed void Seek(long pos)
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            // necessary in case offset != 0 and pos < 0, but pos >= -offset
            if (pos < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(pos), "Seeking to negative position: " + this); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            pos += offset;
            // we use >> here to preserve negative, so we will catch AIOOBE,
            // in case pos + offset overflows.
            int bi = (int)(pos >> chunkSizePower);

            // LUCENENET: Defensive programming so we don't get an IndexOutOfRangeException
            // when reading from buffers.
            if (bi < 0 || bi >= buffers.Length)
                throw EOFException.Create("seek past EOF: " + this);
            
            ByteBuffer b = buffers[bi];
            int newPosition = (int)(pos & chunkSizeMask);

            // LUCENENET: Defensive programming so we don't get an ArgumentOutOfRangeException
            // when setting b.Position.
            if (newPosition < 0 || newPosition > b.Limit)
                throw EOFException.Create("seek past EOF: " + this);

            b.Position = newPosition;
            // write values, on exception all is unchanged
            this.curBufIndex = bi;
            this.curBuf = b;

            // LUCENENET: Already checked buffers to see if it was null in EnsureOpen().
            // If we get a NullReferenceException we definitely need it to be thrown so it is known.
        }

        public override sealed long Length => length;

        public override sealed object Clone()
        {
            ByteBufferIndexInput clone = BuildSlice(0L, this.length);
            try
            {
                clone.Seek(Position); // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw RuntimeException.Create("Should never happen: " + this, ioe);
            }

            return clone;
        }

        /// <summary>
        /// Creates a slice of this index input, with the given description, offset, and length. The slice is seeked to the beginning.
        /// </summary>
        public ByteBufferIndexInput Slice(string sliceDescription, long offset, long length)
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            if (isClone) // well we could, but this is stupid
            {
                throw IllegalStateException.Create("cannot Slice() " + sliceDescription + " from a cloned IndexInput: " + this);
            }
            ByteBufferIndexInput clone = BuildSlice(offset, length);
            clone.sliceDescription = sliceDescription;
            try
            {
                clone.Seek(0L);
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw RuntimeException.Create("Should never happen: " + this, ioe);
            }

            return clone;
        }

        private ByteBufferIndexInput BuildSlice(long offset, long length)
        {
            // LUCENENET: Refactored to avoid calls on invalid conditions instead of
            // catching and re-throwing exceptions in the normal workflow.
            EnsureOpen();
            // LUCENENET: Added .NET-sytle guard clauses that throw ArgumementOutOfRangeException
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "offset may not be negative.");
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length may not be negative.");
            if (offset > this.length - length) // LUCENENET: Checks for int overflow
            {
                throw new ArgumentException("slice() " + sliceDescription + " out of bounds: offset=" + offset + ",length=" + length + ",fileLength=" + this.length + ": " + this);
            }

            // include our own offset into the final offset:
            offset += this.offset;

            ByteBufferIndexInput clone = (ByteBufferIndexInput)base.Clone();
            clone.isClone = true;
            // we keep clone.clones, so it shares the same map with original and we have no additional cost on clones
            if (Debugging.AssertsEnabled) Debugging.Assert(clone.clones == this.clones);
            clone.buffers = BuildSlice(buffers, offset, length);
            clone.offset = (int)(offset & chunkSizeMask);
            clone.length = length;

            // register the new clone in our clone list to clean it up on closing:
            if (clones != null)
            {
                this.clones.Add(clone, true);
            }

            return clone;
        }

        /// <summary>
        /// Returns a sliced view from a set of already-existing buffers:
        /// the last buffer's <see cref="J2N.IO.Buffer.Limit"/> will be correct, but
        /// you must deal with <paramref name="offset"/> separately (the first buffer will not be adjusted)
        /// </summary>
        private ByteBuffer[] BuildSlice(ByteBuffer[] buffers, long offset, long length)
        {
            long sliceEnd = offset + length;

            int startIndex = (int)(offset.TripleShift(chunkSizePower));
            int endIndex = (int)(sliceEnd.TripleShift(chunkSizePower));

            // we always allocate one more slice, the last one may be a 0 byte one
            ByteBuffer[] slices = new ByteBuffer[endIndex - startIndex + 1];

            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = buffers[startIndex + i].Duplicate();
            }

            // set the last buffer's limit for the sliced view.
            slices[slices.Length - 1].Limit = ((int)(sliceEnd & chunkSizeMask));

            return slices;
        }

        private void UnsetBuffers()
        {
            buffers = null;
            curBuf = null;
            curBufIndex = 0;
        }

        // LUCENENET specific - rather than using all of this exception catching nonsense 
        // for control flow, we check whether we are disposed first.
        private void EnsureOpen()
        {
            if (buffers is null)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "Already disposed: " + this);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (buffers is null)
                    {
                        return;
                    }

                    // make local copy, then un-set early
                    ByteBuffer[] bufs = buffers;
                    UnsetBuffers();
                    clones?.Remove(this);

                    if (isClone)
                    {
                        return;
                    }

                    // for extra safety unset also all clones' buffers:
                    if (clones != null)
                    {
                        // LUCENENET: Since .NET will GC types that go out of scope automatically,
                        // this isn't strictly necessary. However, we are doing it anyway when
                        // the enumerator is available (.NET Standard 2.1+)
#if FEATURE_CONDITIONALWEAKTABLE_ENUMERATOR
                        foreach (var pair in clones)
                        {
                            if (Debugging.AssertsEnabled) Debugging.Assert(pair.Key.isClone);
                            pair.Key.UnsetBuffers();
                        }
                        this.clones.Clear();
#endif
                        this.clones = null; // LUCENENET: de-reference the table so it can be GC'd
                    }

                    foreach (ByteBuffer b in bufs)
                    {
                        FreeBuffer(b); // LUCENENET: This calls Dispose() when necessary
                    }
                }
                finally
                {
                    UnsetBuffers();
                }
            }
        }

        /// <summary>
        /// Called when the contents of a buffer will be no longer needed.
        /// </summary>
        protected abstract void FreeBuffer(ByteBuffer b);

        public override sealed string ToString()
        {
            if (sliceDescription != null)
            {
                return base.ToString() + " [slice=" + sliceDescription + "]";
            }
            else
            {
                return base.ToString();
            }
        }
    }
}