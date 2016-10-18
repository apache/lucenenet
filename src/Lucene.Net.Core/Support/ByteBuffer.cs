using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A byte buffer.
    /// <p> This class defines six categories of operations upon
    /// byte buffers:
    /// 
    /// <ul>
    /// 
    ///   <li><p> Absolute and relative {@link #get() <i>get</i>} and
    ///   {@link #put(byte) <i>put</i>} methods that read and write
    ///   single bytes; </p></li>
    /// 
    ///   <li><p> Relative {@link #get(byte[]) <i>bulk get</i>}
    ///   methods that transfer contiguous sequences of bytes from this buffer
    ///   into an array; </p></li>
    /// 
    ///   <li><p> Relative {@link #put(byte[]) <i>bulk put</i>}
    ///   methods that transfer contiguous sequences of bytes from a
    ///   byte array or some other byte
    ///   buffer into this buffer; </p></li>
    /// 
    ///   <li><p> Absolute and relative {@link #getChar() <i>get</i>}
    ///   and {@link #putChar(char) <i>put</i>} methods that read and
    ///   write values of other primitive types, translating them to and from
    ///   sequences of bytes in a particular byte order; </p></li>
    /// 
    ///   <li><p> Methods for creating<i><a href = "#views" > view buffers</a></i>,
    ///   which allow a byte buffer to be viewed as a buffer containing values of
    ///   some other primitive type; and</p></li>
    /// 
    ///   <li><p> Methods for {@link #compact compacting}, {@link
    ///   #duplicate duplicating}, and {@link #slice slicing}
    ///   a byte buffer.  </p></li>
    /// 
    /// </ul>
    /// 
    /// <p> Byte buffers can be created either by {@link #allocate
    /// <i>allocation</i>}, which allocates space for the buffer's
    /// 
    /// content, or by <see cref="Wrap(byte[])"/> <i>wrapping</i>} an
    /// existing byte array into a buffer.
    /// 
    /// <a name="direct"></a>
    /// <h2> Direct <i>vs.</i> non-direct buffers </h2>
    /// 
    /// <p> A byte buffer is either <i>direct</i> or <i>non-direct</i>.  Given a
    /// direct byte buffer, the Java virtual machine will make a best effort to
    /// perform native I/O operations directly upon it.  That is, it will attempt to
    /// avoid copying the buffer's content to (or from) an intermediate buffer
    /// before (or after) each invocation of one of the underlying operating
    /// system's native I/O operations.
    /// 
    /// <p> A direct byte buffer may be created by invoking the <see cref="AllocateDirect(int)"/>
    /// #allocateDirect(int) allocateDirect} factory method of this class.  The
    /// buffers returned by this method typically have somewhat higher allocation
    /// and deallocation costs than non-direct buffers.  The contents of direct
    /// buffers may reside outside of the normal garbage-collected heap, and so
    /// their impact upon the memory footprint of an application might not be
    /// obvious.  It is therefore recommended that direct buffers be allocated
    /// primarily for large, long-lived buffers that are subject to the underlying
    /// system's native I/O operations.  In general it is best to allocate direct
    /// buffers only when they yield a measureable gain in program performance.
    /// 
    /// <p> A direct byte buffer may also be created by {@link
    /// java.nio.channels.FileChannel#map mapping} a region of a file
    /// directly into memory.  An implementation of the Java platform may optionally
    /// support the creation of direct byte buffers from native code via JNI.  If an
    /// instance of one of these kinds of buffers refers to an inaccessible region
    /// of memory then an attempt to access that region will not change the buffer's
    /// content and will cause an unspecified exception to be thrown either at the
    /// time of the access or at some later time.
    /// 
    /// <p> Whether a byte buffer is direct or non-direct may be determined by
    /// invoking its {@link #isDirect isDirect} method.  This method is provided so
    /// that explicit buffer management can be done in performance-critical code.
    /// 
    /// 
    /// <a name="bin"></a>
    /// <h2> Access to binary data </h2>
    /// 
    /// <p> This class defines methods for reading and writing values of all other
    /// primitive types, except <tt>boolean</tt>.  Primitive values are translated
    /// to (or from) sequences of bytes according to the buffer's current byte
    /// order, which may be retrieved and modified via the {@link #order order}
    /// methods.  Specific byte orders are represented by instances of the {@link
    /// ByteOrder} class.  The initial order of a byte buffer is always {@link
    /// ByteOrder#BIG_ENDIAN BIG_ENDIAN}.
    /// 
    /// <p> For access to heterogeneous binary data, that is, sequences of values of
    /// different types, this class defines a family of absolute and relative
    /// <i>get</i> and <i>put</i> methods for each type.  For 32-bit floating-point
    /// values, for example, this class defines:
    /// 
    /// <blockquote><pre>
    /// float  {@link #getFloat()}
    /// float  {@link #getFloat(int) getFloat(int index)}
    ///  void  {@link #putFloat(float) putFloat(float f)}
    ///  void  {@link #putFloat(int,float) putFloat(int index, float f)}</pre></blockquote>
    /// 
    /// <p> Corresponding methods are defined for the types <tt>char</tt>,
    /// <tt>short</tt>, <tt>int</tt>, <tt>long</tt>, and <tt>double</tt>.  The index
    /// parameters of the absolute <i>get</i> and <i>put</i> methods are in terms of
    /// bytes rather than of the type being read or written.
    /// 
    /// <a name="views"></a>
    /// 
    /// <p> For access to homogeneous binary data, that is, sequences of values of
    /// the same type, this class defines methods that can create <i>views</i> of a
    /// given byte buffer.  A <i>view buffer</i> is simply another buffer whose
    /// content is backed by the byte buffer.  Changes to the byte buffer's content
    /// will be visible in the view buffer, and vice versa; the two buffers'
    /// position, limit, and mark values are independent.  The {@link
    /// #asFloatBuffer() asFloatBuffer} method, for example, creates an instance of
    /// the {@link FloatBuffer} class that is backed by the byte buffer upon which
    /// the method is invoked.  Corresponding view-creation methods are defined for
    /// the types <tt>char</tt>, <tt>short</tt>, <tt>int</tt>, <tt>long</tt>, and
    /// <tt>double</tt>.
    /// 
    /// <p> View buffers have three important advantages over the families of
    /// type-specific <i>get</i> and <i>put</i> methods described above:
    /// 
    /// <ul>
    /// 
    ///   <li><p> A view buffer is indexed not in terms of bytes but rather in terms
    ///   of the type-specific size of its values;  </p></li>
    /// 
    ///   <li><p> A view buffer provides relative bulk <i>get</i> and <i>put</i>
    ///   methods that can transfer contiguous sequences of values between a buffer
    ///   and an array or some other buffer of the same type; and  </p></li>
    /// 
    ///   <li><p> A view buffer is potentially much more efficient because it will
    ///   be direct if, and only if, its backing byte buffer is direct.  </p></li>
    /// 
    /// </ul>
    /// 
    /// <p> The byte order of a view buffer is fixed to be that of its byte buffer
    /// at the time that the view is created.  </p>
    /// 
    /// <h2> Invocation chaining </h2>
    /// 
    /// <p> Methods in this class that do not otherwise have a value to return are
    /// specified to return the buffer upon which they are invoked.  This allows
    /// method invocations to be chained.
    /// 
    /// 
    /// The sequence of statements
    /// 
    /// <blockquote><pre>
    /// bb.putInt(0xCAFEBABE);
    /// bb.putShort(3);
    /// bb.putShort(45);</pre></blockquote>
    /// 
    /// can, for example, be replaced by the single statement
    /// 
    /// <blockquote><pre>
    /// bb.putInt(0xCAFEBABE).putShort(3).putShort(45);</pre></blockquote>
    /// 
    /// @author Mark Reinhold
    /// @author JSR-51 Expert Group
    /// @since 1.4
    /// </summary>
    public abstract class ByteBuffer : Buffer, IComparable<ByteBuffer>
    {
        internal byte[] hb; // Non-null only for heap buffers
        private int offset;
        private bool isReadOnly; // Valid only for heap buffers

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, capacity,
        /// backing array, and array offset
        /// </summary>
        internal ByteBuffer(int mark, int pos, int lim, int cap,
            byte[] hb, int offset)
            : base(mark, pos, lim, cap)
        {
            this.hb = hb;
            this.offset = offset;
        }

        /// <summary>
        /// Creates a new buffer with the given mark, position, limit, and capacity
        /// </summary>
        internal ByteBuffer(int mark, int pos, int lim, int cap)
            : this(mark, pos, lim, cap, null, 0)
        {
        }

        /// <summary>
        /// Allocates a new direct byte buffer. (NOT IMPLEMENTED IN LUCENE.NET)
        /// 
        /// <para>
        /// The new buffer's position will be zero, its limit will be its
        /// capacity, its mark will be undefined, and each of its elements will be
        /// initialized to zero.Whether or not it has a
        /// <see cref="HasArray">backing array</see> is unspecified.</para>
        /// </summary>
        /// <param name="capacity">The new buffer's capacity, in bytes</param>
        /// <returns>The new byte buffer</returns>
        /// <exception cref="ArgumentException">If the <c>capacity</c> is a negative integer</exception>
        public static ByteBuffer AllocateDirect(int capacity)
        {
            throw new NotImplementedException();
            //return new DirectByteBuffer(capacity);
        }

        /// <summary>
        /// Allocates a new byte buffer.
        /// 
        /// <para>
        /// The new buffer's position will be zero, its limit will be its
        /// capacity, its mark will be undefined, and each of its elements will be
        /// initialized to zero.It will have a <see cref="Array">backing array</see>
        /// and its <see cref="ArrayOffset">array offset</see>
        ///  will be zero.
        /// </para>
        /// </summary>
        /// <param name="capacity">The new buffer's capacity, in bytes</param>
        /// <returns>The new byte buffer</returns>
        /// <exception cref="ArgumentException">If the <c>capacity</c> is a negative integer</exception>
        public static ByteBuffer Allocate(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentException();
            return new HeapByteBuffer(capacity, capacity);
        }

        /// <summary>
        /// Wraps a byte array into a buffer.
        /// 
        /// <para>
        /// The new buffer will be backed by the given byte array;
        /// that is, modifications to the buffer will cause the array to be modified
        /// and vice versa.  The new buffer's capacity will be
        /// <c>array.Length</c>, its position will be <paramref name="offset"/>, its limit
        /// will be <c>offset + length</c>, and its mark will be undefined.  Its
        /// <see cref="Array">backing array</see> will be the given array, and
        /// its <see cref="ArrayOffset">array offset</see> will be zero.
        /// </para>
        /// </summary>
        /// <param name="array">The array that will back the new buffer</param>
        /// <param name="offset">
        /// The offset of the subarray to be used; must be non-negative and
        /// no larger than <c>array.length</c>.  The new buffer's position
        /// will be set to this value.
        /// </param>
        /// <param name="length">
        /// The length of the subarray to be used;
        /// must be non-negative and no larger than
        /// <c>array.Length - Offset</c>.
        /// The new buffer's limit will be set to <c>Offset + Length</c>.
        /// </param>
        /// <returns>The new byte buffer</returns>
        /// <exception cref="ArgumentOutOfRangeException">If the preconditions on the 
        /// <paramref name="offset"/> and <paramref name="length"/> parameters do not hold</exception>
        public static ByteBuffer Wrap(byte[] array, int offset, int length)
        {
            try
            {
                return new HeapByteBuffer(array, offset, length);
            }
            catch (ArgumentException x)
            {
                throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Wraps a byte array into a buffer.
        /// 
        /// <para>
        /// The new buffer will be backed by the given byte array;
        /// that is, modifications to the buffer will cause the array to be modified
        /// and vice versa.  The new buffer's capacity will be
        /// <c>array.Length</c>, its position will be <paramref name="offset"/>, its limit
        /// will be <c>offset + length</c>, and its mark will be undefined.  Its
        /// <see cref="Array">backing array</see> will be the given array, and
        /// its <see cref="ArrayOffset">array offset</see> will be zero.
        /// </para>
        /// </summary>
        /// <param name="array">The array that will back this buffer</param>
        /// <returns>The new byte buffer</returns>
        public static ByteBuffer Wrap(byte[] array)
        {
            return Wrap(array, 0, array.Length);
        }

        /// <summary>
        /// Creates a new byte buffer whose content is a shared subsequence of
        /// this buffer's content.
        ///
        /// <para> 
        /// The content of the new buffer will start at this buffer's current
        /// position.  Changes to this buffer's content will be visible in the new
        /// buffer, and vice versa; the two buffers' position, limit, and mark
        /// values will be independent.
        ///
        /// <p> The new buffer's position will be zero, its capacity and its limit
        /// will be the number of bytes remaining in this buffer, and its mark
        /// will be undefined.  The new buffer will be direct if, and only if, this
        /// buffer is direct, and it will be read-only if, and only if, this buffer
        /// is read-only.
        /// </para>
        /// </summary>
        /// <returns>The new byte buffer</returns>
        public abstract ByteBuffer Slice();

        /// <summary>
        /// Creates a new byte buffer that shares this buffer's content.
        ///
        /// <para> 
        /// The content of the new buffer will be that of this buffer.  Changes
        /// to this buffer's content will be visible in the new buffer, and vice
        /// versa; the two buffers' position, limit, and mark values will be
        /// independent.
        /// </para>
        ///
        /// <para> 
        /// The new buffer's capacity, limit, position, and mark values will be
        /// identical to those of this buffer.  The new buffer will be direct if,
        /// and only if, this buffer is direct, and it will be read-only if, and
        /// only if, this buffer is read-only.  
        /// </para>
        /// </summary>
        /// <returns>The new byte buffer</returns>
        public abstract ByteBuffer Duplicate();

        public abstract ByteBuffer AsReadOnlyBuffer();

        public abstract byte Get();

        public abstract ByteBuffer Put(byte b);

        public abstract byte Get(int index);

        public abstract ByteBuffer Put(int index, byte b);

        // -- Bulk get operations --

        public virtual ByteBuffer Get(byte[] dst, int offset, int length)
        {
            CheckBounds(offset, length, dst.Length);
            if (length > Remaining)
                throw new BufferUnderflowException();
            int end = offset + length;
            for (int i = offset; i < end; i++)
                dst[i] = Get();

            return this;
        }

        public virtual ByteBuffer Get(byte[] dst)
        {
            return Get(dst, 0, dst.Length);
        }

        // -- Bulk put operations --

        public virtual ByteBuffer Put(ByteBuffer src)
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

        public virtual ByteBuffer Put(byte[] src, int offset, int length)
        {
            CheckBounds(offset, length, src.Length);
            if (length > Remaining)
                throw new BufferOverflowException();
            int end = offset + length;
            for (int i = offset; i < end; i++)
                this.Put(src[i]);
            return this;
        }

        public ByteBuffer Put(byte[] src)
        {
            return Put(src, 0, src.Length);
        }

        // -- Other stuff --

        public sealed override bool HasArray
        {
            get { return (hb != null) && !isReadOnly; }
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

        public abstract ByteBuffer Compact();

        public abstract override bool IsDirect { get; }

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
                h = 31 * h + (int)Get(i);
            return h;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!(obj is ByteBuffer))
                return false;
            ByteBuffer that = (ByteBuffer)obj;
            if (this.Remaining != that.Remaining)
                return false;
            int p = this.Position;
            for (int i = this.Limit - 1, j = that.Limit - 1; i >= p; i--, j--)
                if (!Equals(this.Get(i), that.Get(j)))
                    return false;
            return true;
        }

        private static bool Equals(byte x, byte y)
        {
            return x == y;
        }

        public int CompareTo(ByteBuffer other)
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

        private static int Compare(byte x, byte y)
        {
            // from Byte.compare(x, y)
            return x - y;
        }

        // -- Other char stuff --

        // (empty)

        // -- Other byte stuff: Access to binary data --

        internal bool bigEndian = true;
        //bool nativeByteOrder = (Bits.byteOrder() == ByteOrder.BIG_ENDIAN);

        public ByteOrder Order
        {
            get
            {
                return bigEndian ? ByteOrder.BIG_ENDIAN : ByteOrder.LITTLE_ENDIAN;
            }
            set
            {
                bigEndian = (value == ByteOrder.BIG_ENDIAN);
            }
        }

        // Unchecked accessors, for use by ByteBufferAs-X-Buffer classes
        //
        internal abstract byte _get(int i);                          // package-private
        internal abstract void _put(int i, byte b);                  // package-private



        public abstract char GetChar();

        public abstract ByteBuffer PutChar(char value);

        public abstract char GetChar(int index);

        public abstract ByteBuffer PutChar(int index, char value);

        //public abstract CharBuffer AsCharBuffer();

        public abstract short GetShort();

        public abstract ByteBuffer PutShort(short value);

        public abstract short GetShort(int index);

        public abstract ByteBuffer PutShort(int index, short value);

        // public abstract ShortBuffer AsShortBuffer();

        public abstract int GetInt();

        public abstract ByteBuffer PutInt(int value);

        public abstract int GetInt(int index);

        public abstract ByteBuffer PutInt(int index, int value);

        //public abstract IntBuffer AsIntBuffer();

        public abstract long GetLong();

        public abstract ByteBuffer PutLong(long value);

        public abstract long GetLong(int index);

        public abstract ByteBuffer PutLong(int index, long value);

        public abstract LongBuffer AsLongBuffer();

        public abstract float GetFloat();

        public abstract ByteBuffer PutFloat(float value);

        public abstract float GetFloat(int index);

        public abstract ByteBuffer PutFloat(int index, float value);

        //public abstract FloatBuffer AsFloatBuffer();

        public abstract double GetDouble();

        public abstract ByteBuffer PutDouble(double value);

        public abstract double GetDouble(int index);

        public abstract ByteBuffer PutDouble(int index, double value);

        //public abstract DoubleBuffer AsDoubleBuffer();


        public class HeapByteBuffer : ByteBuffer
        {
            // For speed these fields are actually declared in X-Buffer;
            // these declarations are here as documentation
            //protected readonly byte[] hb;
            //protected readonly int offset;

            internal HeapByteBuffer(int cap, int lim)
                : base(-1, 0, lim, cap, new byte[cap], 0)
            {
                /*
                hb = new byte[cap];
                offset = 0;
                */
            }

            internal HeapByteBuffer(int mark, int pos, int lim, int cap)
                : base(mark, pos, lim, cap, new byte[cap], 0)
            {
                /*
                hb = new byte[cap];
                offset = 0;
                */
            }

            internal HeapByteBuffer(byte[] buf, int off, int len)
                : base(-1, off, off + len, buf.Length, buf, 0)
            {
                /*
                hb = buf;
                offset = 0;
                */
            }

            protected HeapByteBuffer(byte[] buf,
                                           int mark, int pos, int lim, int cap,
                                           int off)
                : base(mark, pos, lim, cap, buf, off)
            {
                /*
                hb = buf;
                offset = off;
                */
            }

            public override ByteBuffer Slice()
            {
                return new HeapByteBuffer(hb,
                                        -1,
                                        0,
                                        this.Remaining,
                                        this.Remaining,
                                        this.Position + offset);
            }

            public override ByteBuffer Duplicate()
            {
                return new HeapByteBuffer(hb,
                                        this.MarkValue,
                                        this.Position,
                                        this.Limit,
                                        this.Capacity,
                                        offset);
            }

            public override ByteBuffer AsReadOnlyBuffer()
            {
                return new HeapByteBufferR(hb,
                                     this.MarkValue,
                                     this.Position,
                                     this.Limit,
                                     this.Capacity,
                                     offset);
            }

            protected int Ix(int i)
            {
                return i + offset;
            }

            public override byte Get()
            {
                return hb[Ix(NextGetIndex())];
            }

            public override byte Get(int i)
            {
                return hb[Ix(CheckIndex(i))];
            }

            public override ByteBuffer Get(byte[] dst, int offset, int length)
            {
                CheckBounds(offset, length, dst.Length);
                if (length > Remaining)
                    throw new BufferUnderflowException();
                System.Buffer.BlockCopy(hb, Ix(Position), dst, offset, length);
                SetPosition(Position + length);
                return this;
            }

            public override bool IsDirect
            {
                get { return false; }
            }

            public override bool IsReadOnly
            {
                get { return false; }
            }

            public override ByteBuffer Put(byte x)
            {
                hb[Ix(NextPutIndex())] = x;
                return this;
            }

            public override ByteBuffer Put(int i, byte x)
            {
                hb[Ix(CheckIndex(i))] = x;
                return this;
            }

            public override ByteBuffer Put(byte[] src, int offset, int length)
            {
                CheckBounds(offset, length, src.Length);
                if (length > Remaining)
                    throw new BufferOverflowException();
                System.Buffer.BlockCopy(src, offset, hb, Ix(Position), length);
                SetPosition(Position + length);
                return this;
            }

            public override ByteBuffer Put(ByteBuffer src)
            {
                if (src is HeapByteBuffer)
                {
                    if (src == this)
                        throw new ArgumentException();
                    HeapByteBuffer sb = (HeapByteBuffer)src;
                    int n = sb.Remaining;
                    if (n > Remaining)
                        throw new BufferOverflowException();
                    System.Buffer.BlockCopy(sb.hb, sb.Ix(sb.Position),
                                     hb, Ix(Position), n);
                    sb.SetPosition(sb.Position + n);
                    SetPosition(Position + n);
                }
                else if (src.IsDirect)
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

            public override ByteBuffer Compact()
            {
                System.Buffer.BlockCopy(hb, Ix(Position), hb, Ix(0), Remaining);
                SetPosition(Remaining);
                SetLimit(Capacity);
                DiscardMark();
                return this;
            }

            internal override byte _get(int i)
            {
                return hb[i];
            }

            internal override void _put(int i, byte b)
            {
                hb[i] = b;
            }


            public override char GetChar()
            {
                var littleEndian = BitConverter.ToChar(hb, Ix(NextGetIndex(2)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override char GetChar(int i)
            {
                var littleEndian = BitConverter.ToChar(hb, Ix(CheckIndex(i, 2)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutChar(char value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];

                return this;
            }

            public override ByteBuffer PutChar(int index, char value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];

                return this;
            }

            //public CharBuffer asCharBuffer()
            //{
            //    int size = this.remaining() >> 1;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (CharBuffer)(new ByteBufferAsCharBufferB(this,
            //                                                           -1,
            //                                                           0,
            //                                                           size,
            //                                                           size,
            //                                                           off))
            //            : (CharBuffer)(new ByteBufferAsCharBufferL(this,
            //                                                           -1,
            //                                                           0,
            //                                                           size,
            //                                                           size,
            //                                                           off)));
            //}

            // short

            public override short GetShort()
            {
                var littleEndian = BitConverter.ToInt16(hb, Ix(NextGetIndex(2)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override short GetShort(int index)
            {
                var littleEndian = BitConverter.ToInt16(hb, Ix(CheckIndex(index, 2)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutShort(short value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];

                return this;
            }



            public override ByteBuffer PutShort(int index, short value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];

                return this;
            }

            //public ShortBuffer asShortBuffer()
            //{
            //    int size = this.remaining() >> 1;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (ShortBuffer)(new ByteBufferAsShortBufferB(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off))
            //            : (ShortBuffer)(new ByteBufferAsShortBufferL(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off)));
            //}

            // int

            public override int GetInt()
            {
                var littleEndian = BitConverter.ToInt32(hb, Ix(NextGetIndex(4)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override int GetInt(int index)
            {
                var littleEndian = BitConverter.ToInt32(hb, Ix(CheckIndex(index, 4)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutInt(int value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];

                return this;
            }

            public override ByteBuffer PutInt(int index, int value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];

                return this;
            }

            //public IntBuffer asIntBuffer()
            //{
            //    int size = this.remaining() >> 2;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (IntBuffer)(new ByteBufferAsIntBufferB(this,
            //                                                         -1,
            //                                                         0,
            //                                                         size,
            //                                                         size,
            //                                                         off))
            //            : (IntBuffer)(new ByteBufferAsIntBufferL(this,
            //                                                         -1,
            //                                                         0,
            //                                                         size,
            //                                                         size,
            //                                                         off)));
            //}

            // long

            public override long GetLong()
            {
                var littleEndian = BitConverter.ToInt64(hb, Ix(NextGetIndex(8)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override long GetLong(int index)
            {
                var littleEndian = BitConverter.ToInt64(hb, Ix(CheckIndex(index, 8)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutLong(long value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];
                hb[Ix(NextPutIndex())] = bytes[4];
                hb[Ix(NextPutIndex())] = bytes[5];
                hb[Ix(NextPutIndex())] = bytes[6];
                hb[Ix(NextPutIndex())] = bytes[7];

                return this;
            }



            public override ByteBuffer PutLong(int index, long value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];
                hb[Ix(NextPutIndex())] = bytes[4];
                hb[Ix(NextPutIndex())] = bytes[5];
                hb[Ix(NextPutIndex())] = bytes[6];
                hb[Ix(NextPutIndex())] = bytes[7];

                return this;
            }

            public override LongBuffer AsLongBuffer()
            {
                int size = this.Remaining >> 3;
                int off = offset + Position;
                return (new ByteBufferAsLongBuffer(bigEndian,
                    this,
                    -1,
                    0,
                    size,
                    size,
                    off));

                //return (bigEndian
                //        ? (LongBuffer)(new ByteBufferAsLongBufferB(this,
                //                                                       -1,
                //                                                       0,
                //                                                       size,
                //                                                       size,
                //                                                       off))
                //        : (LongBuffer)(new ByteBufferAsLongBufferL(this,
                //                                                       -1,
                //                                                       0,
                //                                                       size,
                //                                                       size,
                //                                                       off)));
            }

            public override float GetFloat()
            {
                byte[] temp = new byte[4];
                System.Array.Copy(hb, Ix(NextGetIndex(4)), temp, 0, 4);
                if (bigEndian)
                {
                    System.Array.Reverse(temp);
                }
                return BitConverter.ToSingle(temp, 0);
            }

            public override float GetFloat(int index)
            {
                var littleEndian = BitConverter.ToSingle(hb, Ix(CheckIndex(index, 4)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutFloat(float value)
            {
                var bytes = BitConverter.GetBytes(value);

                if (bigEndian)
                {
                    System.Array.Reverse(bytes);
                }

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];

                return this;
            }



            public override ByteBuffer PutFloat(int index, float value)
            {
                var bytes = BitConverter.GetBytes(value);

                if (bigEndian)
                {
                    System.Array.Reverse(bytes);
                }

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];

                return this;
            }

            //public FloatBuffer asFloatBuffer()
            //{
            //    int size = this.remaining() >> 2;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (FloatBuffer)(new ByteBufferAsFloatBufferB(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off))
            //            : (FloatBuffer)(new ByteBufferAsFloatBufferL(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off)));
            //}


            // double

            public override double GetDouble()
            {
                var littleEndian = BitConverter.ToDouble(hb, Ix(NextGetIndex(8)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override double GetDouble(int index)
            {
                var littleEndian = BitConverter.ToDouble(hb, Ix(CheckIndex(index, 8)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override ByteBuffer PutDouble(double value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex())] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];
                hb[Ix(NextPutIndex())] = bytes[4];
                hb[Ix(NextPutIndex())] = bytes[5];
                hb[Ix(NextPutIndex())] = bytes[6];
                hb[Ix(NextPutIndex())] = bytes[7];

                return this;
            }



            public override ByteBuffer PutDouble(int index, double value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                hb[Ix(NextPutIndex(index))] = bytes[0];
                hb[Ix(NextPutIndex())] = bytes[1];
                hb[Ix(NextPutIndex())] = bytes[2];
                hb[Ix(NextPutIndex())] = bytes[3];
                hb[Ix(NextPutIndex())] = bytes[4];
                hb[Ix(NextPutIndex())] = bytes[5];
                hb[Ix(NextPutIndex())] = bytes[6];
                hb[Ix(NextPutIndex())] = bytes[7];

                return this;
            }

            //public DoubleBuffer asDoubleBuffer()
            //{
            //    int size = this.remaining() >> 3;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (DoubleBuffer)(new ByteBufferAsDoubleBufferB(this,
            //                                                               -1,
            //                                                               0,
            //                                                               size,
            //                                                               size,
            //                                                               off))
            //            : (DoubleBuffer)(new ByteBufferAsDoubleBufferL(this,
            //                                                               -1,
            //                                                               0,
            //                                                               size,
            //                                                               size,
            //                                                               off)));
            //}
        }

        internal class HeapByteBufferR : HeapByteBuffer
        {
            internal HeapByteBufferR(int cap, int lim)
                : base(cap, lim)
            {
                this.isReadOnly = true;
            }

            internal HeapByteBufferR(byte[] buf, int off, int len)
                : base(buf, off, len)
            {
                this.isReadOnly = true;
            }

            protected internal HeapByteBufferR(byte[] buf,
                                           int mark, int pos, int lim, int cap,
                                           int off)
                : base(buf, mark, pos, lim, cap, off)
            {
                this.isReadOnly = true;
            }

            public override ByteBuffer Slice()
            {
                return new HeapByteBufferR(hb,
                                                -1,
                                                0,
                                                this.Remaining,
                                                this.Remaining,
                                                this.Position + offset);
            }

            public override ByteBuffer Duplicate()
            {
                return new HeapByteBufferR(hb,
                                                this.MarkValue,
                                                this.Position,
                                                this.Limit,
                                                this.Capacity,
                                                offset);
            }

            public override ByteBuffer AsReadOnlyBuffer()
            {
                return Duplicate();
            }

            public override bool IsReadOnly
            {
                get { return true; }
            }

            public override ByteBuffer Put(byte x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer Put(int i, byte x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer Put(byte[] src, int offset, int length)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer Put(ByteBuffer src)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer Compact()
            {
                throw new ReadOnlyBufferException();
            }

            internal override byte _get(int i)
            {
                return hb[i];
            }

            internal override void _put(int i, byte b)
            {
                throw new ReadOnlyBufferException();
            }

            // char

            public override ByteBuffer PutChar(char x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutChar(int i, char x)
            {
                throw new ReadOnlyBufferException();
            }

            //public override CharBuffer AsCharBuffer()
            //{
            //    int size = this.remaining() >> 1;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (CharBuffer)(new ByteBufferAsCharBufferRB(this,
            //                                                           -1,
            //                                                           0,
            //                                                           size,
            //                                                           size,
            //                                                           off))
            //            : (CharBuffer)(new ByteBufferAsCharBufferRL(this,
            //                                                           -1,
            //                                                           0,
            //                                                           size,
            //                                                           size,
            //                                                           off)));
            //}

            // short

            public override ByteBuffer PutShort(short x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutShort(int i, short x)
            {
                throw new ReadOnlyBufferException();
            }

            //public override ShortBuffer AsShortBuffer()
            //{
            //    int size = this.remaining() >> 1;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (ShortBuffer)(new ByteBufferAsShortBufferRB(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off))
            //            : (ShortBuffer)(new ByteBufferAsShortBufferRL(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off)));
            //}


            // int

            public override ByteBuffer PutInt(int x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutInt(int i, int x)
            {
                throw new ReadOnlyBufferException();
            }

            //public override IntBuffer AsIntBuffer()
            //{
            //    int size = this.remaining() >> 2;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (IntBuffer)(new ByteBufferAsIntBufferRB(this,
            //                                                         -1,
            //                                                         0,
            //                                                         size,
            //                                                         size,
            //                                                         off))
            //            : (IntBuffer)(new ByteBufferAsIntBufferRL(this,
            //                                                         -1,
            //                                                         0,
            //                                                         size,
            //                                                         size,
            //                                                         off)));
            //}


            // long

            public override ByteBuffer PutLong(long x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutLong(int i, long x)
            {
                throw new ReadOnlyBufferException();
            }

            public override LongBuffer AsLongBuffer()
            {
                throw new NotImplementedException();
                //int size = this.remaining() >> 3;
                //int off = offset + position();
                //return (bigEndian
                //        ? (LongBuffer)(new ByteBufferAsLongBufferRB(this,
                //                                                       -1,
                //                                                       0,
                //                                                       size,
                //                                                       size,
                //                                                       off))
                //        : (LongBuffer)(new ByteBufferAsLongBufferRL(this,
                //                                                       -1,
                //                                                       0,
                //                                                       size,
                //                                                       size,
                //                                                       off)));
            }

            // float

            public override ByteBuffer PutFloat(float x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutFloat(int i, float x)
            {
                throw new ReadOnlyBufferException();
            }

            //public override FloatBuffer AsFloatBuffer()
            //{
            //    int size = this.remaining() >> 2;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (FloatBuffer)(new ByteBufferAsFloatBufferRB(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off))
            //            : (FloatBuffer)(new ByteBufferAsFloatBufferRL(this,
            //                                                             -1,
            //                                                             0,
            //                                                             size,
            //                                                             size,
            //                                                             off)));
            //}


            // double

            public override ByteBuffer PutDouble(double x)
            {
                throw new ReadOnlyBufferException();
            }

            public override ByteBuffer PutDouble(int i, double x)
            {
                throw new ReadOnlyBufferException();
            }

            //public override DoubleBuffer AsDoubleBuffer()
            //{
            //    int size = this.remaining() >> 3;
            //    int off = offset + position();
            //    return (bigEndian
            //            ? (DoubleBuffer)(new ByteBufferAsDoubleBufferRB(this,
            //                                                               -1,
            //                                                               0,
            //                                                               size,
            //                                                               size,
            //                                                               off))
            //            : (DoubleBuffer)(new ByteBufferAsDoubleBufferRL(this,
            //                                                               -1,
            //                                                               0,
            //                                                               size,
            //                                                               size,
            //                                                               off)));
            //}
        }


        internal class ByteBufferAsLongBuffer : LongBuffer
        {
            protected readonly ByteBuffer bb;
            new protected readonly int offset;
            protected readonly bool bigEndian;

            internal ByteBufferAsLongBuffer(bool bigEndian, ByteBuffer bb)
                : base(-1, 0,
                      bb.Remaining >> 3,
                      bb.Remaining >> 3)
            {   // package-private
                this.bb = bb;
                // enforce limit == capacity
                int cap = this.Capacity;
                this.SetLimit(cap);
                int pos = this.Position;
                Debug.Assert(pos <= cap);
                offset = pos;
                this.bigEndian = bigEndian;
            }

            internal ByteBufferAsLongBuffer(bool bigEndian, ByteBuffer bb,
                                             int mark, int pos, int lim, int cap,
                                             int off)
                : base(mark, pos, lim, cap)
            {
                this.bb = bb;
                offset = off;
                this.bigEndian = bigEndian;
            }

            public override LongBuffer Slice()
            {
                int pos = this.Position;
                int lim = this.Limit;
                Debug.Assert(pos <= lim);
                int rem = (pos <= lim ? lim - pos : 0);
                int off = (pos << 3) + offset;
                Debug.Assert(off >= 0);
                return new ByteBufferAsLongBuffer(this.bigEndian, bb, -1, 0, rem, rem, off);
            }

            public override LongBuffer Duplicate()
            {
                return new ByteBufferAsLongBuffer(this.bigEndian,
                                            bb,
                                            this.MarkValue,
                                            this.Position,
                                            this.Limit,
                                            this.Capacity,
                                            offset);
            }

            public override LongBuffer AsReadOnlyBuffer()
            {
                throw new NotImplementedException();
                //return new ByteBufferAsLongBufferRB(bb,
                //                                         this.Mark,
                //                                         this.position(),
                //                                         this.limit(),
                //                                         this.capacity(),
                //                                         offset);
            }

            protected int Ix(int i)
            {
                return (i << 3) + offset;
            }

            public override long Get()
            {
                if (!bb.HasArray)
                    throw new InvalidOperationException();

                var littleEndian = BitConverter.ToInt64(bb.hb, Ix(NextGetIndex()));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override long Get(int index)
            {
                if (!bb.HasArray)
                    throw new InvalidOperationException();

                var littleEndian = BitConverter.ToInt64(bb.hb, Ix(CheckIndex(index)));
                if (bigEndian)
                {
                    return Number.FlipEndian(littleEndian);
                }
                return littleEndian;
            }

            public override LongBuffer Put(long value)
            {
                PutImpl(Ix(NextPutIndex()), value);
                return this;
            }

            public override LongBuffer Put(int index, long value)
            {
                PutImpl(Ix(CheckIndex(index)), value);
                return this;
            }

            private void PutImpl(int index, long value)
            {
                var bytes = BitConverter.GetBytes(bigEndian ? Number.FlipEndian(value) : value);

                bb._put(index, bytes[0]);
                bb._put(index + 1, bytes[1]);
                bb._put(index + 2, bytes[2]);
                bb._put(index + 3, bytes[3]);
                bb._put(index + 4, bytes[4]);
                bb._put(index + 5, bytes[5]);
                bb._put(index + 6, bytes[6]);
                bb._put(index + 7, bytes[7]);
            }


            public override LongBuffer Compact()
            {
                int pos = Position;
                int lim = Limit;
                Debug.Assert(pos <= lim);
                int rem = (pos <= lim ? lim - pos : 0);

                ByteBuffer db = bb.Duplicate();
                db.SetLimit(Ix(lim));
                db.SetPosition(Ix(0));
                ByteBuffer sb = db.Slice();
                sb.SetPosition(pos << 3);
                sb.Compact();
                SetPosition(rem);
                SetLimit(Capacity);
                DiscardMark();
                return this;
            }

            public override bool IsDirect
            {
                get
                {
                    return bb.IsDirect;
                }
            }

            public override bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public override ByteOrder Order
            {
                get { return bigEndian ? ByteOrder.BIG_ENDIAN : ByteOrder.LITTLE_ENDIAN; }
            }
        }
    }
}
