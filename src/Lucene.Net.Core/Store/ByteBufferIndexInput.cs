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

        private readonly long chunkSizeMask;
        private readonly int chunkSizePower;

        private int offset;
        private long length;
        private string sliceDescription;

        private int curBufIndex;

        private ByteBuffer curBuf; // redundant for speed: buffers[curBufIndex]
        private bool isClone = false;
        private readonly WeakIdentityMap<ByteBufferIndexInput, BoolRefWrapper> clones;

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
            this.length = length;
            this.chunkSizePower = chunkSizePower;
            this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            this.clones = trackClones ? WeakIdentityMap<ByteBufferIndexInput, BoolRefWrapper>.NewConcurrentHashMap() : null;

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
                return curBuf.Get();
            }
            catch (BufferUnderflowException)
            {
                do
                {
                    curBufIndex++;
                    if (curBufIndex >= buffers.Length)
                    {
                        throw new System.IO.EndOfStreamException("read past EOF: " + this);
                    }
                    curBuf = buffers[curBufIndex];
                    curBuf.Position = 0;
                } while (!curBuf.HasRemaining);
                return curBuf.Get();
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
                curBuf.Get(b, offset, len);
            }
            catch (BufferUnderflowException)
            {
                int curAvail = curBuf.Remaining;
                while (len > curAvail)
                {
                    curBuf.Get(b, offset, curAvail);
                    len -= curAvail;
                    offset += curAvail;
                    curBufIndex++;
                    if (curBufIndex >= buffers.Length)
                    {
                        throw new System.IO.EndOfStreamException("read past EOF: " + this);
                    }
                    curBuf = buffers[curBufIndex];
                    curBuf.Position = 0;
                    curAvail = curBuf.Remaining;
                }
                curBuf.Get(b, offset, len);
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
                return curBuf.GetShort();
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
                return curBuf.GetInt();
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
                return curBuf.GetLong();
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
                    return (((long)curBufIndex) << chunkSizePower) + curBuf.Position - offset;
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
            pos += offset;
            // we use >> here to preserve negative, so we will catch AIOOBE,
            // in case pos + offset overflows.
            int bi = (int)(pos >> chunkSizePower);
            try
            {
                ByteBuffer b = buffers[bi];
                b.Position = ((int)(pos & chunkSizeMask));
                // write values, on exception all is unchanged
                this.curBufIndex = bi;
                this.curBuf = b;
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

        public override sealed long Length
        {
            get { return length; }
        }

        public override sealed object Clone()
        {
            ByteBufferIndexInput clone = BuildSlice(0L, this.length);
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
            if (isClone) // well we could, but this is stupid
            {
                throw new InvalidOperationException("cannot slice() " + sliceDescription + " from a cloned IndexInput: " + this);
            }
            ByteBufferIndexInput clone = BuildSlice(offset, length);
            clone.sliceDescription = sliceDescription;
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
            if (offset < 0 || length < 0 || offset + length > this.length)
            {
                throw new System.ArgumentException("slice() " + sliceDescription + " out of bounds: offset=" + offset + ",length=" + length + ",fileLength=" + this.length + ": " + this);
            }

            // include our own offset into the final offset:
            offset += this.offset;

            ByteBufferIndexInput clone = (ByteBufferIndexInput)base.Clone();
            clone.isClone = true;
            // we keep clone.clones, so it shares the same map with original and we have no additional cost on clones
            Debug.Assert(clone.clones == this.clones);
            clone.buffers = BuildSlice(buffers, offset, length);
            clone.offset = (int)(offset & chunkSizeMask);
            clone.length = length;

            // register the new clone in our clone list to clean it up on closing:
            if (clones != null)
            {
                this.clones.Put(clone, new BoolRefWrapper(true));
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

            int startIndex = (int)((long)((ulong)offset >> chunkSizePower));
            int endIndex = (int)((long)((ulong)sliceEnd >> chunkSizePower));

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
                if (clones != null)
                {
                    clones.Remove(this);
                }

                if (isClone)
                {
                    return;
                }

                // for extra safety unset also all clones' buffers:
                if (clones != null)
                {
                    foreach (ByteBufferIndexInput clone in clones.Keys)
                    {
                        clone.UnsetBuffers();
                    }
                    this.clones.Clear();
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