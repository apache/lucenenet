using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Diagnostics;

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
    /// Base IndexInput implementation that uses an array
    /// of ByteBuffers to represent a file.
    /// <p>
    /// Because Java's ByteBuffer uses an int to address the
    /// values, it's necessary to access a file greater
    /// Integer.MAX_VALUE in size using multiple byte buffers.
    /// <p>
    /// For efficiency, this class requires that the buffers
    /// are a power-of-two (<code>chunkSizePower</code>).
    /// </summary>
    public abstract class ByteBufferIndexInput : IndexInput
    {
        private ByteBuffer[] buffers;

        private readonly long ChunkSizeMask;
        private readonly int ChunkSizePower;

        private int Offset;
        private long Length_Renamed;
        private string SliceDescription;

        private int CurBufIndex;

        private ByteBuffer CurBuf; // redundant for speed: buffers[curBufIndex]
        private bool IsClone = false;
        private readonly WeakIdentityMap<ByteBufferIndexInput, BoolRefWrapper> Clones;

        private class BoolRefWrapper
        {
            // .NET port: this is needed as bool is not a reference type
            public BoolRefWrapper(bool value)
            {
                this.Value = value;
            }

            public bool Value { get; private set; }
        }

        internal ByteBufferIndexInput(string resourceDescription, ByteBuffer[] buffers, long length, int chunkSizePower, bool trackClones)
            : base(resourceDescription)
        {
            this.buffers = buffers;
            this.Length_Renamed = length;
            this.ChunkSizePower = chunkSizePower;
            this.ChunkSizeMask = (1L << chunkSizePower) - 1L;
            this.Clones = trackClones ? WeakIdentityMap<ByteBufferIndexInput, BoolRefWrapper>.NewConcurrentHashMap() : null;

            Debug.Assert(chunkSizePower >= 0 && chunkSizePower <= 30);
            //assert((long)((ulong)length >> chunkSizePower)) < int.MaxValue; // LUCENENET TODO: why isn't this in place?

            //Seek(0L); // LUCENENET TODO: why isn't this in place?
        }

        internal ByteBuffer[] Buffers // LUCENENET TODO: this shouldn't be a property - perhaps make a SetBuffers() internal method
        {
            get { return buffers; }
            set
            {
                buffers = value; // necessary for MMapIndexInput
            }
        }

        public override sealed byte ReadByte()
        {
            try
            {
                return CurBuf.Get();
            }
            catch (BufferUnderflowException)
            {
                do
                {
                    CurBufIndex++;
                    if (CurBufIndex >= buffers.Length)
                    {
                        throw new System.IO.EndOfStreamException("read past EOF: " + this);
                    }
                    CurBuf = buffers[CurBufIndex];
                    CurBuf.Position = 0;
                } while (!CurBuf.HasRemaining);
                return CurBuf.Get();
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed void ReadBytes(byte[] b, int offset, int len)
        {
            try
            {
                CurBuf.Get(b, offset, len);
            }
            catch (BufferUnderflowException)
            {
                int curAvail = CurBuf.Remaining;
                while (len > curAvail)
                {
                    CurBuf.Get(b, offset, curAvail);
                    len -= curAvail;
                    offset += curAvail;
                    CurBufIndex++;
                    if (CurBufIndex >= buffers.Length)
                    {
                        throw new System.IO.EndOfStreamException("read past EOF: " + this);
                    }
                    CurBuf = buffers[CurBufIndex];
                    CurBuf.Position = 0;
                    curAvail = CurBuf.Remaining;
                }
                CurBuf.Get(b, offset, len);
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed short ReadShort()
        {
            try
            {
                return CurBuf.GetShort();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadShort();
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed int ReadInt()
        {
            try
            {
                return CurBuf.GetInt();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadInt();
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed long ReadLong()
        {
            try
            {
                return CurBuf.GetLong();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadLong();
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed long FilePointer
        {
            get
            {
                try
                {
                    return (((long)CurBufIndex) << ChunkSizePower) + CurBuf.Position - Offset;
                }
                catch (System.NullReferenceException)
                {
                    throw new AlreadyClosedException("Already closed: " + this);
                }
            }
        }

        public override sealed void Seek(long pos)
        {
            // necessary in case offset != 0 and pos < 0, but pos >= -offset
            if (pos < 0L)
            {
                throw new System.ArgumentException("Seeking to negative position: " + this);
            }
            pos += Offset;
            // we use >> here to preserve negative, so we will catch AIOOBE,
            // in case pos + offset overflows.
            int bi = (int)(pos >> ChunkSizePower);
            try
            {
                ByteBuffer b = buffers[bi];
                b.Position = ((int)(pos & ChunkSizeMask));
                // write values, on exception all is unchanged
                this.CurBufIndex = bi;
                this.CurBuf = b;
            }
            catch (System.IndexOutOfRangeException)
            {
                throw new System.IO.EndOfStreamException("seek past EOF: " + this);
            }
            catch (System.ArgumentException)
            {
                throw new System.IO.EndOfStreamException("seek past EOF: " + this);
            }
            catch (System.NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override sealed long Length()
        {
            return Length_Renamed;
        }

        public override sealed object Clone()
        {
            ByteBufferIndexInput clone = BuildSlice(0L, this.Length_Renamed);
            try
            {
                clone.Seek(FilePointer);
            }
            catch (System.IO.IOException ioe)
            {
                throw new Exception("Should never happen: " + this, ioe);
            }

            return clone;
        }

        /// <summary>
        /// Creates a slice of this index input, with the given description, offset, and length. The slice is seeked to the beginning.
        /// </summary>
        public ByteBufferIndexInput Slice(string sliceDescription, long offset, long length)
        {
            if (IsClone) // well we could, but this is stupid
            {
                throw new InvalidOperationException("cannot slice() " + sliceDescription + " from a cloned IndexInput: " + this);
            }
            ByteBufferIndexInput clone = BuildSlice(offset, length);
            clone.SliceDescription = sliceDescription;
            try
            {
                clone.Seek(0L);
            }
            catch (System.IO.IOException ioe)
            {
                throw new Exception("Should never happen: " + this, ioe);
            }

            return clone;
        }

        private ByteBufferIndexInput BuildSlice(long offset, long length)
        {
            if (buffers == null)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
            if (offset < 0 || length < 0 || offset + length > this.Length_Renamed)
            {
                throw new System.ArgumentException("slice() " + SliceDescription + " out of bounds: offset=" + offset + ",length=" + length + ",fileLength=" + this.Length_Renamed + ": " + this);
            }

            // include our own offset into the final offset:
            offset += this.Offset;

            ByteBufferIndexInput clone = (ByteBufferIndexInput)base.Clone();
            clone.IsClone = true;
            // we keep clone.clones, so it shares the same map with original and we have no additional cost on clones
            Debug.Assert(clone.Clones == this.Clones);
            clone.buffers = BuildSlice(buffers, offset, length);
            clone.Offset = (int)(offset & ChunkSizeMask);
            clone.Length_Renamed = length;

            // register the new clone in our clone list to clean it up on closing:
            if (Clones != null)
            {
                this.Clones.Put(clone, new BoolRefWrapper(true));
            }

            return clone;
        }

        /// <summary>
        /// Returns a sliced view from a set of already-existing buffers:
        ///  the last buffer's limit() will be correct, but
        ///  you must deal with offset separately (the first buffer will not be adjusted)
        /// </summary>
        private ByteBuffer[] BuildSlice(ByteBuffer[] buffers, long offset, long length)
        {
            long sliceEnd = offset + length;

            int startIndex = (int)((long)((ulong)offset >> ChunkSizePower));
            int endIndex = (int)((long)((ulong)sliceEnd >> ChunkSizePower));

            // we always allocate one more slice, the last one may be a 0 byte one
            ByteBuffer[] slices = new ByteBuffer[endIndex - startIndex + 1];

            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = buffers[startIndex + i].Duplicate();
            }

            // set the last buffer's limit for the sliced view.
            slices[slices.Length - 1].Limit = ((int)(sliceEnd & ChunkSizeMask));

            return slices;
        }

        private void UnsetBuffers()
        {
            buffers = null;
            CurBuf = null;
            CurBufIndex = 0;
        }

        public override void Dispose()
        {
            try
            {
                if (buffers == null)
                {
                    return;
                }

                // make local copy, then un-set early
                ByteBuffer[] bufs = buffers;
                UnsetBuffers();
                if (Clones != null)
                {
                    Clones.Remove(this);
                }

                if (IsClone)
                {
                    return;
                }

                // for extra safety unset also all clones' buffers:
                if (Clones != null)
                {
                    foreach (ByteBufferIndexInput clone in Clones.Keys)
                    {
                        clone.UnsetBuffers();
                    }
                    this.Clones.Clear();
                }

                foreach (ByteBuffer b in bufs)
                {
                    FreeBuffer(b);
                }
            }
            finally
            {
                UnsetBuffers();
            }
        }

        /// <summary>
        /// Called when the contents of a buffer will be no longer needed.
        /// </summary>
        protected abstract void FreeBuffer(ByteBuffer b);

        public override sealed string ToString()
        {
            if (SliceDescription != null)
            {
                return base.ToString() + " [slice=" + SliceDescription + "]";
            }
            else
            {
                return base.ToString();
            }
        }
    }
}