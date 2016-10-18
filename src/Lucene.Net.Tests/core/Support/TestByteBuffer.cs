using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Reflection;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Tests from JDK/nio/BasicByte.java
    /// </summary>
    [TestFixture]
    public class TestByteBuffer : BaseBufferTestCase
    {
        private static readonly sbyte[] VALUES = {
            sbyte.MinValue,
            (sbyte) -1,
            (sbyte) 0,
            (sbyte) 1,
            sbyte.MaxValue,
        };


        private static void relGet(ByteBuffer b)
        {
            int n = b.Capacity;
            byte v;
            for (int i = 0; i < n; i++)
                ck(b, (long)b.Get(), (long)((byte)Ic(i)));
            b.Rewind();
        }

        private static void relGet(ByteBuffer b, int start)
        {
            int n = b.Remaining;
            byte v;
            for (int i = start; i < n; i++)
                ck(b, (long)b.Get(), (long)((byte)Ic(i)));
            b.Rewind();
        }

        private static void absGet(ByteBuffer b)
        {
            int n = b.Capacity;
            byte v;
            for (int i = 0; i < n; i++)
                ck(b, (long)b.Get(), (long)((byte)Ic(i)));
            b.Rewind();
        }

        private static void bulkGet(ByteBuffer b)
        {
            int n = b.Capacity;
            byte[] a = new byte[n + 7];
            b.Get(a, 7, n);
            for (int i = 0; i < n; i++)
                ck(b, (long)a[i + 7], (long)((byte)Ic(i)));
        }

        private static void relPut(ByteBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            for (int i = 0; i < n; i++)
                b.Put((byte)Ic(i));
            b.Flip();
        }

        private static void absPut(ByteBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            for (int i = 0; i < n; i++)
                b.Put(i, (byte)Ic(i));
            b.Limit = (n);
            b.Position = (0);
        }

        private static void bulkPutArray(ByteBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            byte[] a = new byte[n + 7];
            for (int i = 0; i < n; i++)
                a[i + 7] = (byte)Ic(i);
            b.Put(a, 7, n);
            b.Flip();
        }

        private static void bulkPutBuffer(ByteBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            ByteBuffer c = ByteBuffer.Allocate(n + 7);
            c.Position = (7);
            for (int i = 0; i < n; i++)
                c.Put((byte)Ic(i));
            c.Flip();
            c.Position = (7);
            b.Put(c);
            b.Flip();
        }

        //6231529
        private static void callReset(ByteBuffer b)
        {
            b.Position = (0);
            b.Mark();

            b.Duplicate().Reset();
            // LUCENENET: AsReadOnlyBuffer() not implemented
            //b.AsReadOnlyBuffer().Reset();
        }

        private static void checkSlice(ByteBuffer b, ByteBuffer slice)
        {
            ck(slice, 0, slice.Position);
            ck(slice, b.Remaining, slice.Limit);
            ck(slice, b.Remaining, slice.Capacity);
            if (b.IsDirect != slice.IsDirect)
                fail("Lost direction", slice);
            if (b.IsReadOnly != slice.IsReadOnly)
                fail("Lost read-only", slice);
        }

        private static void checkBytes(ByteBuffer b, byte[] bs)
        {
            int n = bs.Length;
            int p = b.Position;
            byte v;
            if (b.Order == ByteOrder.BIG_ENDIAN)
            {
                for (int i = 0; i < n; i++)
                ck(b, b.Get(), bs[i]);
            }
            else
            {
                for (int i = n - 1; i >= 0; i--)
                    ck(b, b.Get(), bs[i]);
            }
            b.Position = (p);
        }

        private static void compact(Buffer b)
        {
            try
            {
                Type cl = b.GetType();
                MethodInfo m = cl.GetMethod("Compact", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                m.Invoke(b, new object[0]);
            }
            catch (Exception e)
            {
                fail(e.ToString(), b);
            }
        }

        private static void checkInvalidMarkException(Buffer b)
        {
            tryCatch(b, typeof(InvalidMarkException), () =>
                {
                    b.Mark();
                    compact(b);
                    b.Reset();
                });
        }

        private static void testViews(int level, ByteBuffer b, bool direct)
        {
            //ShortBuffer sb = b.asShortBuffer();
            //BasicShort.test(level, sb, direct);
            //checkBytes(b, new byte[] { 0, (byte)ic(0) });
            //checkInvalidMarkException(sb);

            //CharBuffer cb = b.asCharBuffer();
            //BasicChar.test(level, cb, direct);
            //checkBytes(b, new byte[] { 0, (byte)ic(0) });
            //checkInvalidMarkException(cb);

            //IntBuffer ib = b.asIntBuffer();
            //BasicInt.test(level, ib, direct);
            //checkBytes(b, new byte[] { 0, 0, 0, (byte)ic(0) });
            //checkInvalidMarkException(ib);

            LongBuffer lb = b.AsLongBuffer();
            TestLongBuffer.test(level, lb, direct);
            checkBytes(b, new byte[] { 0, 0, 0, 0, 0, 0, 0, (byte)Ic(0) });
            checkInvalidMarkException(lb);

            //FloatBuffer fb = b.asFloatBuffer();
            //BasicFloat.test(level, fb, direct);
            //checkBytes(b, new byte[] { 0x42, (byte)0xc2, 0, 0 });
            //checkInvalidMarkException(fb);

            //DoubleBuffer db = b.asDoubleBuffer();
            //BasicDouble.test(level, db, direct);
            //checkBytes(b, new byte[] { 0x40, 0x58, 0x40, 0, 0, 0, 0, 0 });
            //checkInvalidMarkException(db);
        }

        private static void testHet(int level, ByteBuffer b)
        {

            int p = b.Position;
            b.Limit = (b.Capacity);
            Show(level, b);
            output.Write("    put:");

            b.PutChar((char)1);
            b.PutChar((char)char.MaxValue);
            output.Write(" char");

            b.PutShort((short)1);
            b.PutShort((short)short.MaxValue);
            output.Write(" short");

            b.PutInt(1);
            b.PutInt(int.MaxValue);
            output.Write(" int");

            b.PutLong((long)1);
            b.PutLong((long)long.MaxValue);
            output.Write(" long");

            b.PutFloat((float)1);
            b.PutFloat((float)float.MinValue);
            b.PutFloat((float)float.MaxValue);
            output.Write(" float");

            b.PutDouble((double)1);
            b.PutDouble((double)double.MinValue);
            b.PutDouble((double)double.MaxValue);
            output.Write(" double");

            output.WriteLine();
            b.Limit = (b.Position);
            b.Position = (p);
            Show(level, b);
            output.Write("    get:");

            ck(b, b.GetChar(), 1);
            ck(b, b.GetChar(), char.MaxValue);
            output.Write(" char");

            ck(b, b.GetShort(), 1);
            ck(b, b.GetShort(), short.MaxValue);
            output.Write(" short");

            ck(b, b.GetInt(), 1);
            ck(b, b.GetInt(), int.MaxValue);
            output.Write(" int");

            ck(b, b.GetLong(), 1);
            ck(b, b.GetLong(), long.MaxValue);
            output.Write(" long");

            ck(b, (long)b.GetFloat(), 1);
            ck(b, (long)b.GetFloat(), unchecked((long)float.MinValue));
            ck(b, (long)b.GetFloat(), unchecked((long)float.MaxValue));
            output.Write(" float");

            ck(b, (long)b.GetDouble(), 1);
            ck(b, (long)b.GetDouble(), unchecked((long)double.MinValue));
            ck(b, (long)b.GetDouble(), unchecked((long)double.MaxValue));
            output.Write(" double");

            output.WriteLine();
        }

        private static void fail(string problem,
                                 ByteBuffer xb, ByteBuffer yb,
                                 byte x, byte y)
        {
            fail(problem + string.Format(": x={0} y={1}", x, y), xb, yb);
        }

        private static void tryCatch(Buffer b, Type ex, Action thunk)
        {
            bool caught = false;
            try
            {
                thunk();
            }
            catch (Exception x)
            {
                if (ex.IsAssignableFrom(x.GetType()))
                {
                    caught = true;
                }
                else
                {
                    fail(x.Message + " not expected");
                }
            }
            if (!caught)
                fail(ex.Name + " not thrown", b);
        }

        private static void tryCatch(byte[] t, Type ex, Action thunk)
        {
            tryCatch(ByteBuffer.Wrap(t), ex, thunk);
        }

        public static void test(int level, ByteBuffer b, bool direct)
        {
            Show(level, b);

            if (direct != b.IsDirect)
                fail("Wrong direction", b);

            // Gets and puts

            relPut(b);
            relGet(b);
            absGet(b);
            bulkGet(b);

            absPut(b);
            relGet(b);
            absGet(b);
            bulkGet(b);

            bulkPutArray(b);
            relGet(b);

            bulkPutBuffer(b);
            relGet(b);


            // Compact

            relPut(b);
            b.Position = (13);
            b.Compact();
            b.Flip();
            relGet(b, 13);

            // Exceptions

            relPut(b);
            b.Limit = (b.Capacity / 2);
            b.Position = (b.Limit);

            tryCatch(b, typeof(BufferUnderflowException), () =>
            {
                b.Get();
            });

            tryCatch(b, typeof(BufferOverflowException), () =>
            {
                b.Put((byte)42);
            });

            // The index must be non-negative and lesss than the buffer's limit.
            tryCatch(b, typeof(IndexOutOfRangeException), () =>
            {
                b.Get(b.Limit);
            });
            tryCatch(b, typeof(IndexOutOfRangeException), () =>
            {
                b.Get(-1);
            });

            tryCatch(b, typeof(IndexOutOfRangeException), () =>
            {
                b.Put(b.Limit, (byte)42);
            });

            tryCatch(b, typeof(InvalidMarkException), () =>
            {
                b.Position = (0);
                b.Mark();
                b.Compact();
                b.Reset();
            });

            // Values

            b.Clear();
            b.Put((byte)0);
            b.Put(unchecked((byte)-1));
            b.Put((byte)1);
            b.Put(unchecked((byte)sbyte.MaxValue));
            b.Put(unchecked((byte)sbyte.MinValue));

            byte v;
            b.Flip();
            ck(b, b.Get(), 0);
            ck(b, b.Get(), unchecked((byte)-1));
            ck(b, b.Get(), 1);
            ck(b, b.Get(), unchecked((byte)sbyte.MaxValue));
            ck(b, b.Get(), unchecked((byte)sbyte.MinValue));


            // Comparison
            b.Rewind();
            ByteBuffer b2 = Lucene.Net.Support.ByteBuffer.Allocate(b.Capacity);
            b2.Put(b);
            b2.Flip();
            b.Position = (2);
            b2.Position = (2);
            if (!b.Equals(b2))
            {
                for (int i = 2; i < b.Limit; i++)
                {
                    byte x = b.Get(i);
                    byte y = b2.Get(i);
                    if (x != y)
                        output.WriteLine("[" + i + "] " + x + " != " + y);
                }
                fail("Identical buffers not equal", b, b2);
            }
            if (b.CompareTo(b2) != 0)
                fail("Comparison to identical buffer != 0", b, b2);

            b.Limit = (b.Limit + 1);
            b.Position = (b.Limit - 1);
            b.Put((byte)99);
            b.Rewind();
            b2.Rewind();
            if (b.Equals(b2))
                fail("Non-identical buffers equal", b, b2);
            if (b.CompareTo(b2) <= 0)
                fail("Comparison to shorter buffer <= 0", b, b2);
            b.Limit = (b.Limit - 1);

            b.Put(2, (byte)42);
            if (b.equals(b2))
                fail("Non-identical buffers equal", b, b2);
            if (b.CompareTo(b2) <= 0)
                fail("Comparison to lesser buffer <= 0", b, b2);

            // Check equals and compareTo with interesting values
            foreach (byte x in VALUES)
            {
                ByteBuffer xb = Lucene.Net.Support.ByteBuffer.Wrap(new byte[] { x });
                if (xb.CompareTo(xb) != 0)
                {
                    fail("compareTo not reflexive", xb, xb, x, x);
                }
                if (!xb.Equals(xb))
                {
                    fail("equals not reflexive", xb, xb, x, x);
                }
                foreach (byte y in VALUES)
                {
                    ByteBuffer yb = Lucene.Net.Support.ByteBuffer.Wrap(new byte[] { y });
                    if (xb.CompareTo(yb) != -yb.CompareTo(xb))
                    {
                        fail("compareTo not anti-symmetric",
                             xb, yb, x, y);
                    }
                    if ((xb.CompareTo(yb) == 0) != xb.Equals(yb))
                    {
                        fail("compareTo inconsistent with equals",
                             xb, yb, x, y);
                    }
                    // from Byte.compare(x, y)
                    //return x - y;
                    if (xb.CompareTo(yb) != (x - y) /* Byte.Compare(x, y)*/)
                    {
                        fail("Incorrect results for ByteBuffer.compareTo",
                             xb, yb, x, y);
                    }
                    if (xb.equals(yb) != ((x == y) || ((x != x) && (y != y))))
                    {
                        fail("Incorrect results for ByteBuffer.equals",
                             xb, yb, x, y);
                    }
                }
            }

            // Sub, dup

            relPut(b);
            relGet(b.Duplicate());
            b.Position = (13);
            relGet(b.Duplicate(), 13);
            relGet(b.Duplicate().Slice(), 13);
            relGet(b.Slice(), 13);
            relGet(b.Slice().Duplicate(), 13);

            // Slice

            b.Position = (5);
            ByteBuffer sb = b.Slice();
            checkSlice(b, sb);
            b.Position = (0);
            ByteBuffer sb2 = sb.Slice();
            checkSlice(sb, sb2);

            if (!sb.Equals(sb2))
                fail("Sliced slices do not match", sb, sb2);
            if ((sb.HasArray) && (sb.ArrayOffset != sb2.ArrayOffset))
                fail("Array offsets do not match: "
                     + sb.ArrayOffset + " != " + sb2.ArrayOffset, sb, sb2);

            // Views

            b.Clear();
            b.Order = (ByteOrder.BIG_ENDIAN);
            testViews(level + 1, b, direct);

            for (int i = 1; i <= 9; i++)
            {
                b.Position = (i);
                Show(level + 1, b);
                testViews(level + 2, b, direct);
            }

            b.Position=(0);
            b.Order=(ByteOrder.LITTLE_ENDIAN);
            testViews(level + 1, b, direct);

            // Heterogeneous accessors

            b.Order = (ByteOrder.BIG_ENDIAN);
            for (int i = 0; i <= 9; i++)
            {
                b.Position = (i);
                testHet(level + 1, b);
            }
            b.Order = (ByteOrder.LITTLE_ENDIAN);
            b.Position = (3);
            testHet(level + 1, b);

            // Read-only views

            b.Rewind();
            ByteBuffer rb = b.AsReadOnlyBuffer();
            if (!b.equals(rb))
                fail("Buffer not equal to read-only view", b, rb);
            Show(level + 1, rb);

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                relPut(rb);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                absPut(rb);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                bulkPutArray(rb);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                bulkPutBuffer(rb);
            });

            // put(ByteBuffer) should not change source position
            ByteBuffer src = ByteBuffer.Allocate(1);
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.Put(src);
            });
            ck(src, src.Position, 0);

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.Compact();
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutChar((char)1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutChar(0, (char)1);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutShort((short)1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutShort(0, (short)1);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutInt(1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutInt(0, 1);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutLong((long)1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutLong(0, (long)1);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutFloat((float)1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutFloat(0, (float)1);
            });

            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutDouble((double)1);
            });
            tryCatch(b, typeof(ReadOnlyBufferException), () =>
            {
                rb.PutDouble(0, (double)1);
            });


            if (rb.GetType().Name.StartsWith("java.nio.Heap"))
            {

                tryCatch(b, typeof(ReadOnlyBufferException), () =>
                {
                    var x = rb.Array;
                });

                tryCatch(b, typeof(ReadOnlyBufferException), () =>
                {
                    var x = rb.ArrayOffset;
                });

                if (rb.HasArray)
                    fail("Read-only heap buffer's backing array is accessible",
                         rb);

            }

            // Bulk puts from read-only buffers

            b.Clear();
            rb.Rewind();
            b.Put(rb);

            // LUCENENET: AllocateDirect not implemented

            // For byte buffers, test both the direct and non-direct cases
            //ByteBuffer ob
            //    = (b.IsDirect
            //       ? ByteBuffer.Allocate(rb.Capacity)
            //       : ByteBuffer.AllocateDirect(rb.Capacity));
            ByteBuffer ob = ByteBuffer.Allocate(rb.Capacity);
            rb.Rewind();
            ob.Put(rb);


            relPut(b);                       // Required by testViews
        }

        public static void test(byte[] ba)
        {
            int offset = 47;
            int length = 900;
            ByteBuffer b = ByteBuffer.Wrap(ba, offset, length);
            Show(0, b);
            ck(b, b.Capacity, ba.Length);
            ck(b, b.Position, offset);
            ck(b, b.Limit, offset + length);

            // The offset must be non-negative and no larger than <array.length>.
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                ByteBuffer.Wrap(ba, -1, ba.Length);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                ByteBuffer.Wrap(ba, ba.Length + 1, ba.Length);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                ByteBuffer.Wrap(ba, 0, -1);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                ByteBuffer.Wrap(ba, 0, ba.Length + 1);
            });

            // A NullPointerException will be thrown if the array is null.
            tryCatch(ba, typeof(NullReferenceException), () =>
            {
                ByteBuffer.Wrap((byte[])null, 0, 5);
            });
            tryCatch(ba, typeof(NullReferenceException), () =>
            {
                ByteBuffer.Wrap((byte[])null);
            });
        }

        private static void testAllocate()
        {
            // An IllegalArgumentException will be thrown for negative capacities.
            tryCatch((Buffer)null, typeof(ArgumentException), () =>
            {
                ByteBuffer.Allocate(-1);
            });

            // LUCENENET: AllocateDirect not implemented
            //tryCatch((Buffer)null, typeof(ArgumentException), () =>
            //{
            //    ByteBuffer.AllocateDirect(-1);
            //});

            // LUCENENET: AllocateDirect not implemented
            tryCatch((Buffer)null, typeof(NotImplementedException), () =>
            {
                ByteBuffer.AllocateDirect(-1);
            });
        }

        [Test, LuceneNetSpecific]
        public static void Test()
        {
            testAllocate();
            test(0, ByteBuffer.Allocate(7 * 1024), false);
            test(0, ByteBuffer.Wrap(new byte[7 * 1024], 0, 7 * 1024), false);
            test(new byte[1024]);

            // LUCENENET: AllocateDirect not implemented
            //ByteBuffer b = ByteBuffer.AllocateDirect(7 * 1024);
            //for (b.Position = (0); b.Position < b.Limit;)
            //    ck(b, b.Get(), 0);
            //test(0, b, true);

            callReset(ByteBuffer.Allocate(10));
        }
    }
}
