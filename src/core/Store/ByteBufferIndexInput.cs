using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    internal abstract class ByteBufferIndexInput : IndexInput
    {
        private ByteBuffer[] buffers;

        private readonly long chunkSizeMask;
        private readonly int chunkSizePower;

        private int offset;
        private long length;
        private String sliceDescription;

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

        internal ByteBufferIndexInput(String resourceDescription, ByteBuffer[] buffers, long length, int chunkSizePower,
            bool trackClones)
            : base(resourceDescription)
        {

            this.buffers = buffers;
            this.length = length;
            this.chunkSizePower = chunkSizePower;
            this.chunkSizeMask = (1L << chunkSizePower) - 1L;
            this.clones = trackClones ? WeakIdentityMap<ByteBufferIndexInput, BoolRefWrapper>.NewConcurrentHashMap() : null;

            //assert chunkSizePower >= 0 && chunkSizePower <= 30;   
            //assert (length >>> chunkSizePower) < Integer.MAX_VALUE;

            Seek(0L);
        }

        public override byte ReadByte()
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
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            try
            {
                curBuf.Get(b, offset, len);
            }
            catch (BufferUnderflowException e)
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
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override short ReadShort()
        {
            try
            {
                return curBuf.GetShort();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadShort();
            }
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override int ReadInt()
        {
            try
            {
                return curBuf.GetInt();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadInt();
            }
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override long ReadLong()
        {
            try
            {
                return curBuf.GetLong();
            }
            catch (BufferUnderflowException)
            {
                return base.ReadLong();
            }
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override long FilePointer
        {
            get
            {
                try
                {
                    return (((long)curBufIndex) << chunkSizePower) + curBuf.Position - offset;
                }
                catch (NullReferenceException)
                {
                    throw new AlreadyClosedException("Already closed: " + this);
                }
            }
        }

        public override void Seek(long pos)
        {
            // necessary in case offset != 0 and pos < 0, but pos >= -offset
            if (pos < 0L)
            {
                throw new ArgumentException("Seeking to negative position: " + this);
            }
            pos += offset;
            // we use >> here to preserve negative, so we will catch AIOOBE,
            // in case pos + offset overflows.
            int bi = (int)(pos >> chunkSizePower);
            try
            {
                ByteBuffer b = buffers[bi];
                b.Position = (int)(pos & chunkSizeMask);
                // write values, on exception all is unchanged
                this.curBufIndex = bi;
                this.curBuf = b;
            }
            catch (IndexOutOfRangeException)
            {
                throw new System.IO.EndOfStreamException("seek past EOF: " + this);
            }
            catch (ArgumentException)
            {
                throw new System.IO.EndOfStreamException("seek past EOF: " + this);
            }
            catch (NullReferenceException)
            {
                throw new AlreadyClosedException("Already closed: " + this);
            }
        }

        public override long Length
        {
            get { return length; }
        }

        public override object Clone()
        {
            ByteBufferIndexInput clone = BuildSlice(0L, this.length);

            clone.Seek(FilePointer);

            return clone;
        }

        public ByteBufferIndexInput Slice(string sliceDescription, long offset, long length)
        {
            if (isClone)
            { // well we could, but this is stupid
                throw new InvalidOperationException("cannot slice() " + sliceDescription + " from a cloned IndexInput: " + this);
            }
            ByteBufferIndexInput clone = BuildSlice(offset, length);
            clone.sliceDescription = sliceDescription;
            clone.Seek(0L);

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
                throw new ArgumentException("slice() " + sliceDescription + " out of bounds: offset=" + offset + ",length=" + length + ",fileLength=" + this.length + ": " + this);
            }

            // include our own offset into the final offset:
            offset += this.offset;

            ByteBufferIndexInput clone = (ByteBufferIndexInput)base.Clone();
            clone.isClone = true;
            // we keep clone.clones, so it shares the same map with original and we have no additional cost on clones
            //assert clone.clones == this.clones;
            clone.buffers = BuildSlice(buffers, offset, length);
            clone.offset = (int)(offset & chunkSizeMask);
            clone.length = length;

            // register the new clone in our clone list to clean it up on closing:
            if (clones != null)
            {
                this.clones[clone] = new BoolRefWrapper(true);
            }

            return clone;
        }

        private ByteBuffer[] BuildSlice(ByteBuffer[] buffers, long offset, long length)
        {
            long sliceEnd = offset + length;

            int startIndex = (int)Number.URShift(offset, chunkSizePower);
            int endIndex = (int)Number.URShift(sliceEnd, chunkSizePower);

            // we always allocate one more slice, the last one may be a 0 byte one
            ByteBuffer[] slices = new ByteBuffer[endIndex - startIndex + 1];

            for (int i = 0; i < slices.Length; i++)
            {
                slices[i] = buffers[startIndex + i].Duplicate();
            }

            // set the last buffer's limit for the sliced view.
            slices[slices.Length - 1].Limit = (int)(sliceEnd & chunkSizeMask);

            return slices;
        }

        private void UnsetBuffers()
        {
            buffers = null;
            curBuf = null;
            curBufIndex = 0;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (buffers == null) return;

                // make local copy, then un-set early
                ByteBuffer[] bufs = buffers;
                UnsetBuffers();
                if (clones != null)
                {
                    clones.Remove(this);
                }

                if (isClone) return;

                // for extra safety unset also all clones' buffers:
                if (clones != null)
                {
                    foreach (ByteBufferIndexInput clone in clones.Keys)
                    {
                        //assert clone.isClone;
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

        protected abstract void FreeBuffer(ByteBuffer b);

        public override string ToString()
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
