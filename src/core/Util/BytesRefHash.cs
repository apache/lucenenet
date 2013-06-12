using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public sealed class BytesRefHash : IDisposable
    {
        public const int DEFAULT_CAPACITY = 16;

        internal readonly ByteBlockPool pool;
        internal int[] bytesStart;

        private readonly BytesRef scratch1 = new BytesRef();
        private int hashSize;
        private int hashHalfSize;
        private int hashMask;
        private int count;
        private int lastCount = -1;
        private int[] ids;
        private readonly BytesStartArray bytesStartArray;
        private Counter bytesUsed;

        public BytesRefHash()
            : this(new ByteBlockPool(new ByteBlockPool.DirectAllocator()))
        {
        }

        public BytesRefHash(ByteBlockPool pool)
            : this(pool, DEFAULT_CAPACITY, new DirectBytesStartArray(DEFAULT_CAPACITY))
        {
        }

        public BytesRefHash(ByteBlockPool pool, int capacity, BytesStartArray bytesStartArray)
        {
            hashSize = capacity;
            hashHalfSize = hashSize >> 1;
            hashMask = hashSize - 1;
            this.pool = pool;
            ids = new int[hashSize];
            Arrays.Fill(ids, -1);
            this.bytesStartArray = bytesStartArray;
            bytesStart = bytesStartArray.Init();
            bytesUsed = bytesStartArray.BytesUsed == null ? Counter.NewCounter() : bytesStartArray.BytesUsed;
            bytesUsed.AddAndGet(hashSize * RamUsageEstimator.NUM_BYTES_INT);
        }

        public int Size
        {
            get
            {
                return count;
            }
        }

        public BytesRef Get(int bytesID, BytesRef @ref)
        {
            if (bytesStart == null)
                throw new InvalidOperationException("bytesStart is not initialized.");

            if (bytesID >= bytesStart.Length)
                throw new ArgumentException("bytesID exceedes bytesStart length: " + bytesStart.Length);

            pool.SetBytesRef(@ref, bytesStart[bytesID]);
            return @ref;
        }

        internal int[] Compact()
        {
            //assert bytesStart != null : "bytesStart is null - not initialized";
            int upto = 0;
            for (int i = 0; i < hashSize; i++)
            {
                if (ids[i] != -1)
                {
                    if (upto < i)
                    {
                        ids[upto] = ids[i];
                        ids[i] = -1;
                    }
                    upto++;
                }
            }

            //assert upto == count;
            lastCount = count;
            return ids;
        }

        private class CustomSortSorterTemplate : SorterTemplate
        {
            private readonly int[] compact;
            private readonly IComparer<BytesRef> comp;
            private readonly BytesRefHash parent;
            private readonly BytesRef pivot = new BytesRef(),
                scratch1 = new BytesRef(), scratch2 = new BytesRef();

            public CustomSortSorterTemplate(int[] compact, IComparer<BytesRef> comp, BytesRefHash parent)
            {
                this.compact = compact;
                this.comp = comp;
                this.parent = parent;
            }

            protected internal override void Swap(int i, int j)
            {
                int o = compact[i];
                compact[i] = compact[j];
                compact[j] = o;
            }

            protected internal override int Compare(int i, int j)
            {
                int id1 = compact[i], id2 = compact[j];
                //assert bytesStart.length > id1 && bytesStart.length > id2;
                parent.pool.SetBytesRef(scratch1, parent.bytesStart[id1]);
                parent.pool.SetBytesRef(scratch2, parent.bytesStart[id2]);
                return comp.Compare(scratch1, scratch2);
            }

            protected internal override void SetPivot(int i)
            {
                int id = compact[i];
                parent.pool.SetBytesRef(pivot, parent.bytesStart[id]);
            }

            protected internal override int ComparePivot(int j)
            {
                int id = compact[j];
                parent.pool.SetBytesRef(scratch2, parent.bytesStart[id]);
                return comp.Compare(pivot, scratch2);
            }
        }

        public int[] Sort(IComparer<BytesRef> comp)
        {
            int[] compact = Compact();

            new CustomSortSorterTemplate(compact, comp, this).QuickSort(0, count - 1);

            return compact;
        }

        private bool Equals(int id, BytesRef b)
        {
            pool.SetBytesRef(scratch1, bytesStart[id]);
            return scratch1.BytesEquals(b);
        }

        private bool Shrink(int targetSize)
        {
            // Cannot use ArrayUtil.shrink because we require power
            // of 2:
            int newSize = hashSize;
            while (newSize >= 8 && newSize / 4 > targetSize)
            {
                newSize /= 2;
            }
            if (newSize != hashSize)
            {
                bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * -(hashSize - newSize));
                hashSize = newSize;
                ids = new int[hashSize];
                Arrays.Fill(ids, -1);
                hashHalfSize = newSize / 2;
                hashMask = newSize - 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Clear(bool resetPool)
        {
            lastCount = count;
            count = 0;
            if (resetPool)
            {
                pool.Reset(false, false); // we don't need to 0-fill the buffers
            }
            bytesStart = bytesStartArray.Clear();
            if (lastCount != -1 && Shrink(lastCount))
            {
                // shrink clears the hash entries
                return;
            }
            Arrays.Fill(ids, -1);
        }

        public void Clear()
        {
            Clear(true);
        }

        public void Dispose()
        {
            Clear(true);
            ids = null;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * -hashSize);
        }

        public int Add(BytesRef bytes)
        {
            return Add(bytes, bytes.GetHashCode());
        }

        public int Add(BytesRef bytes, int code)
        {
            //assert bytesStart != null : "Bytesstart is null - not initialized";
            int length = bytes.length;
            // final position
            int hashPos = FindHash(bytes, code);
            int e = ids[hashPos];

            if (e == -1)
            {
                // new entry
                int len2 = 2 + bytes.length;
                if (len2 + pool.byteUpto > ByteBlockPool.BYTE_BLOCK_SIZE)
                {
                    if (len2 > ByteBlockPool.BYTE_BLOCK_SIZE)
                    {
                        throw new MaxBytesLengthExceededException("bytes can be at most "
                            + (ByteBlockPool.BYTE_BLOCK_SIZE - 2) + " in length; got " + bytes.length);
                    }
                    pool.NextBuffer();
                }
                sbyte[] buffer = pool.buffer;
                int bufferUpto = pool.byteUpto;
                if (count >= bytesStart.Length)
                {
                    bytesStart = bytesStartArray.Grow();
                    //assert count < bytesStart.length + 1 : "count: " + count + " len: "
                    //    + bytesStart.length;
                }
                e = count++;

                bytesStart[e] = bufferUpto + pool.byteOffset;

                // We first encode the length, followed by the
                // bytes. Length is encoded as vInt, but will consume
                // 1 or 2 bytes at most (we reject too-long terms,
                // above).
                if (length < 128)
                {
                    // 1 byte to store length
                    buffer[bufferUpto] = (sbyte)length;
                    pool.byteUpto += length + 1;
                    //assert length >= 0: "Length must be positive: " + length;
                    Array.Copy(bytes.bytes, bytes.offset, buffer, bufferUpto + 1,
                        length);
                }
                else
                {
                    // 2 byte to store length
                    buffer[bufferUpto] = (sbyte)(0x80 | (length & 0x7f));
                    buffer[bufferUpto + 1] = (sbyte)((length >> 7) & 0xff);
                    pool.byteUpto += length + 2;
                    Array.Copy(bytes.bytes, bytes.offset, buffer, bufferUpto + 2,
                        length);
                }
                //assert ids[hashPos] == -1;
                ids[hashPos] = e;

                if (count == hashHalfSize)
                {
                    Rehash(2 * hashSize, true);
                }
                return e;
            }
            return -(e + 1);
        }

        public int Find(BytesRef bytes)
        {
            return Find(bytes, bytes.GetHashCode());
        }

        public int Find(BytesRef bytes, int code)
        {
            return ids[FindHash(bytes, code)];
        }

        private int FindHash(BytesRef bytes, int code)
        {
            //assert bytesStart != null : "bytesStart is null - not initialized";
            // final position
            int hashPos = code & hashMask;
            int e = ids[hashPos];
            if (e != -1 && !Equals(e, bytes))
            {
                // Conflict: keep searching different locations in
                // the hash table.
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    hashPos = code & hashMask;
                    e = ids[hashPos];
                } while (e != -1 && !Equals(e, bytes));
            }

            return hashPos;
        }

        public int AddByPoolOffset(int offset)
        {
            //assert bytesStart != null : "Bytesstart is null - not initialized";
            // final position
            int code = offset;
            int hashPos = offset & hashMask;
            int e = ids[hashPos];
            if (e != -1 && bytesStart[e] != offset)
            {
                // Conflict: keep searching different locations in
                // the hash table.
                int inc = ((code >> 8) + code) | 1;
                do
                {
                    code += inc;
                    hashPos = code & hashMask;
                    e = ids[hashPos];
                } while (e != -1 && bytesStart[e] != offset);
            }
            if (e == -1)
            {
                // new entry
                if (count >= bytesStart.Length)
                {
                    bytesStart = bytesStartArray.Grow();
                    //assert count < bytesStart.length + 1 : "count: " + count + " len: "
                    //    + bytesStart.length;
                }
                e = count++;
                bytesStart[e] = offset;
                //assert ids[hashPos] == -1;
                ids[hashPos] = e;

                if (count == hashHalfSize)
                {
                    Rehash(2 * hashSize, false);
                }
                return e;
            }
            return -(e + 1);
        }

        private void Rehash(int newSize, bool hashOnData)
        {
            int newMask = newSize - 1;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * (newSize));
            int[] newHash = new int[newSize];
            Arrays.Fill(newHash, -1);
            for (int i = 0; i < hashSize; i++)
            {
                int e0 = ids[i];
                if (e0 != -1)
                {
                    int code;
                    if (hashOnData)
                    {
                        int off = bytesStart[e0];
                        int start = off & ByteBlockPool.BYTE_BLOCK_MASK;
                        sbyte[] bytes = pool.buffers[off >> ByteBlockPool.BYTE_BLOCK_SHIFT];
                        code = 0;
                        int len;
                        int pos;
                        if ((bytes[start] & 0x80) == 0)
                        {
                            // length is 1 byte
                            len = bytes[start];
                            pos = start + 1;
                        }
                        else
                        {
                            len = (bytes[start] & 0x7f) + ((bytes[start + 1] & 0xff) << 7);
                            pos = start + 2;
                        }

                        int endPos = pos + len;
                        while (pos < endPos)
                        {
                            code = 31 * code + bytes[pos++];
                        }
                    }
                    else
                    {
                        code = bytesStart[e0];
                    }

                    int hashPos = code & newMask;
                    //assert hashPos >= 0;
                    if (newHash[hashPos] != -1)
                    {
                        int inc = ((code >> 8) + code) | 1;
                        do
                        {
                            code += inc;
                            hashPos = code & newMask;
                        } while (newHash[hashPos] != -1);
                    }
                    newHash[hashPos] = e0;
                }
            }

            hashMask = newMask;
            bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * (-ids.Length));
            ids = newHash;
            hashSize = newSize;
            hashHalfSize = newSize / 2;
        }

        public void Reinit()
        {
            if (bytesStart == null)
            {
                bytesStart = bytesStartArray.Init();
            }

            if (ids == null)
            {
                ids = new int[hashSize];
                bytesUsed.AddAndGet(RamUsageEstimator.NUM_BYTES_INT * hashSize);
            }
        }

        public int ByteStart(int bytesID)
        {
            //assert bytesStart != null : "bytesStart is null - not initialized";
            //assert bytesID >= 0 && bytesID < count : bytesID;
            return bytesStart[bytesID];
        }

        public class MaxBytesLengthExceededException : Exception
        {
            public MaxBytesLengthExceededException(String message)
                : base(message)
            {
            }
        }

        public abstract class BytesStartArray
        {
            public abstract int[] Init();

            public abstract int[] Grow();

            public abstract int[] Clear();

            public abstract Counter BytesUsed { get; }
        }

        public class DirectBytesStartArray : BytesStartArray
        {
            // TODO: can't we just merge this w/
            // TrackingDirectBytesStartArray...?  Just add a ctor
            // that makes a private bytesUsed?

            protected readonly int initSize;
            private int[] bytesStart;
            private readonly Counter bytesUsed;

            public DirectBytesStartArray(int initSize, Counter counter)
            {
                this.bytesUsed = counter;
                this.initSize = initSize;
            }

            public DirectBytesStartArray(int initSize)
                : this(initSize, Counter.NewCounter())
            {
            }

            public override int[] Clear()
            {
                return bytesStart = null;
            }

            public override int[] Grow()
            {
                if (bytesStart == null)
                    throw new InvalidOperationException("This object has been cleared or never Init()'ed.");

                return bytesStart = ArrayUtil.Grow(bytesStart, bytesStart.Length + 1);
            }

            public override int[] Init()
            {
                return bytesStart = new int[ArrayUtil.Oversize(initSize,
                    RamUsageEstimator.NUM_BYTES_INT)];
            }

            public override Counter BytesUsed
            {
                get { return bytesUsed; }
            }
        }


    }
}
