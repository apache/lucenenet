using System;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Ported from Java's nio.LongBuffer
    /// </summary>
    public abstract class LongBuffer : Buffer, IComparable<LongBuffer>
    {
        // These fields are declared here rather than in Heap-X-Buffer in order to
        // reduce the number of virtual method invocations needed to access these
        // values, which is especially costly when coding small buffers.
        //
        internal readonly long[] hb;                  // Non-null only for heap buffers
        internal readonly int offset;
        internal bool isReadOnly;                 // Valid only for heap buffers

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, capacity, backing array, and array offset
        /// </summary>
        public LongBuffer(int mark, int pos, int lim, int cap,
            long[] hb, int offset) 
            : base(mark, pos, lim, cap)
        {
            this.hb = hb;
            this.offset = offset;
        }

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, and capacity
        /// </summary>
        public LongBuffer(int mark, int pos, int lim, int cap)
            : this(mark, pos, lim, cap, null, 0)
        {
        }

        public static LongBuffer Allocate(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException();
            return new HeapLongBuffer(capacity, capacity);
        }


        public static LongBuffer Wrap(long[] array,
                                    int offset, int length)
        {
            try
            {
                return new HeapLongBuffer(array, offset, length);
            }
            catch (ArgumentException x)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        public static LongBuffer Wrap(long[] array)
        {
            return Wrap(array, 0, array.Length);
        }


        public abstract LongBuffer Slice();

        public abstract LongBuffer Duplicate();

        public abstract LongBuffer AsReadOnlyBuffer();

        public abstract long Get();

        public abstract LongBuffer Put(long l);

        public abstract long Get(int index);

        public abstract LongBuffer Put(int index, long l);

        // -- Bulk get operations --

        public virtual LongBuffer Get(long[] dst, int offset, int length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            int end = offset + length;
            for (int i = offset; i < end; i++)
                dst[i] = Get();
            return this;
        }

        public virtual LongBuffer Get(long[] dst)
        {
            return Get(dst, 0, dst.Length);
        }

        // -- Bulk put operations --

        public virtual LongBuffer Put(LongBuffer src)
        {
            if (src == this)
                throw new ArgumentException();
            if (IsReadOnly)
                throw new ReadOnlyBufferException();
            int n = src.Remaining;
            if (n > Remaining)
                throw new BufferOverflowException();
            for (int i = 0; i < n; i++)
                Put(src.Get());
            return this;
        }

        public virtual LongBuffer Put(long[] src, int offset, int length)
        {
            CheckBounds(offset, length, src.Length);
            if (length > Remaining)
                throw new BufferOverflowException();
            int end = offset + length;
            for (int i = offset; i < end; i++)
                this.Put(src[i]);
            return this;
        }

        public LongBuffer Put(long[] src)
        {
            return Put(src, 0, src.Length);
        }

        public override bool HasArray
        {
            get
            {
                return (hb != null) && !isReadOnly;
            }
        }

        public override object Array
        {
            get
            {
                if (hb == null)
                    throw new InvalidOperationException();
                if (isReadOnly)
                    throw new ReadOnlyBufferException();
                return hb;
            }
        }

        public override int ArrayOffset
        {
            get
            {
                if (hb == null)
                    throw new InvalidOperationException();
                if (isReadOnly)
                    throw new ReadOnlyBufferException();
                return offset;
            }
        }

        public abstract LongBuffer Compact();

        //public override bool IsDirect { get; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetType().Name);
            sb.Append("[pos=");
            sb.Append(Position);
            sb.Append(" lim=");
            sb.Append(Limit);
            sb.Append(" cap=");
            sb.Append(Capacity);
            sb.Append("]");
            return sb.ToString();
        }

        public override int GetHashCode()
        {
            int h = 1;
            int p = Position;
            for (int i = Limit - 1; i >= p; i--)
            {
                h = 31 * h + (int)Get(i);
            }
            return h;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!(obj is LongBuffer))
            return false;
            LongBuffer that = (LongBuffer)obj;
            if (this.Remaining != that.Remaining)
                return false;
            int p = this.Position;
            for (int i = this.Limit - 1, j = that.Limit - 1; i >= p; i--, j--)
                if (!Equals(this.Get(i), that.Get(j)))
                    return false;
            return true;
        }

        private static bool Equals(long x, long y)
        {
            return x == y;
        }

        public int CompareTo(LongBuffer other)
        {
            int n = this.Position + Math.Min(this.Remaining, other.Remaining);
            for (int i = this.Position, j = other.Position; i < n; i++, j++)
            {
                int cmp = Compare(this.Get(i), other.Get(j));
                if (cmp != 0)
                    return cmp;
            }
            return this.Remaining - other.Remaining;
        }

        private static int Compare(long x, long y)
        {
            // from Long.compare(x, y)
            return (x < y) ? -1 : ((x == y) ? 0 : 1);
        }

        // -- Other char stuff --

        // (empty)

        // -- Other byte stuff: Access to binary data --

        public abstract ByteOrder Order { get; }


        public class HeapLongBuffer : LongBuffer
        {
            // For speed these fields are actually declared in X-Buffer;
            // these declarations are here as documentation
            /*

            protected final long[] hb;
            protected final int offset;

            */

            internal HeapLongBuffer(int cap, int lim)
                : base(-1, 0, lim, cap, new long[cap], 0)
            {
                /*
                hb = new long[cap];
                offset = 0;
                */
            }

            internal HeapLongBuffer(long[] buf, int off, int len)
                : base(-1, off, off + len, buf.Length, buf, 0)
            {
                /*
                hb = buf;
                offset = 0;
                */
            }

            protected HeapLongBuffer(long[] buf,
                                   int mark, int pos, int lim, int cap,
                                   int off)
                : base(mark, pos, lim, cap, buf, off)
            {
                /*
                hb = buf;
                offset = off;
                */
            }

            public override LongBuffer Slice()
            {
                return new HeapLongBuffer(hb,
                                        -1,
                                        0,
                                        this.Remaining,
                                        this.Remaining,
                                        this.Position + offset);
            }

            public override LongBuffer Duplicate()
            {
                return new HeapLongBuffer(hb,
                                        this.MarkValue,
                                        this.Position,
                                        this.Limit,
                                        this.Capacity,
                                        offset);
            }

            public override LongBuffer AsReadOnlyBuffer()
            {
                throw new NotImplementedException();
                //return new HeapLongBufferR(hb,
                //                     this.MarkValue(),
                //                     this.Position,
                //                     this.Limit,
                //                     this.Capacity,
                //                     offset);
            }

            protected virtual int Ix(int i)
            {
                return i + offset;
            }

            public override long Get()
            {
                return hb[Ix(NextGetIndex())];
            }

            public override long Get(int index)
            {
                return hb[Ix(CheckIndex(index))];
            }

            public override LongBuffer Get(long[] dst, int offset, int length)
            {
                CheckBounds(offset, length, dst.Length);
                if (length > Remaining)
                    throw new BufferUnderflowException();
                System.Array.Copy(hb, Ix(Position), dst, offset, length);
                SetPosition(Position + length);
                return this;
            }


            public override bool IsDirect
            {
                get
                {
                    return false;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override LongBuffer Put(long l)
            {
                hb[Ix(NextPutIndex())] = l;
                return this;
            }

            public override LongBuffer Put(int index, long l)
            {
                hb[Ix(CheckIndex(index))] = l;
                return this;
            }

            public override LongBuffer Put(long[] src, int offset, int length)
            {

                CheckBounds(offset, length, src.Length);
                if (length > Remaining)
                    throw new BufferOverflowException();
                System.Array.Copy(src, offset, hb, Ix(Position), length);
                SetPosition(Position + length);
                return this;
            }

            public override LongBuffer Put(LongBuffer src)
            {

                if (src is HeapLongBuffer) {
                    if (src == this)
                        throw new ArgumentException();
                    HeapLongBuffer sb = (HeapLongBuffer)src;
                    int n = sb.Remaining;
                    if (n > Remaining)
                        throw new BufferOverflowException();
                    System.Array.Copy(sb.hb, sb.Ix(sb.Position),
                                     hb, Ix(Position), n);
                    sb.SetPosition(sb.Position + n);
                    SetPosition(Position + n);
                } else if (src.IsDirect)
                {
                    int n = src.Remaining;
                    if (n > Remaining)
                        throw new BufferOverflowException();
                    src.Get(hb, Ix(Position), n);
                    SetPosition(Position + n);
                }
                else
                {
                    base.Put(src);
                }
                return this;
            }

            public override LongBuffer Compact()
            {
                System.Array.Copy(hb, Ix(Position), hb, Ix(0), Remaining);
                SetPosition(Remaining);
                SetLimit(Capacity);
                DiscardMark();
                return this;
            }

            public override ByteOrder Order
            {
                get
                {
                    throw new NotImplementedException();
                    //return ByteOrder.nativeOrder();
                }
            }
        }
    }
}
