using Lucene.Net.Attributes;
using NUnit.Framework;
using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Tests from JDK/nio/BasicLong.java
    /// </summary>
    public class TestLongBuffer : BaseBufferTestCase
    {
        private static readonly long[] VALUES = {
            long.MinValue,
            (long) -1,
            (long) 0,
            (long) 1,
            long.MaxValue,
        };


        private static void relGet(LongBuffer b)
        {
            int n = b.Capacity;
            long v;
            for (int i = 0; i < n; i++)
                ck(b, (long)b.Get(), (long)((long)Ic(i)));
            b.Rewind();
        }

        private static void relGet(LongBuffer b, int start)
        {
            int n = b.Remaining;
            long v;
            for (int i = start; i < n; i++)
                ck(b, (long)b.Get(), (long)((long)Ic(i)));
            b.Rewind();
        }

        private static void absGet(LongBuffer b)
        {
            int n = b.Capacity;
            long v;
            for (int i = 0; i < n; i++)
                ck(b, (long)b.Get(), (long)((long)Ic(i)));
            b.Rewind();
        }

        private static void bulkGet(LongBuffer b)
        {
            int n = b.Capacity;
            long[] a = new long[n + 7];
            b.Get(a, 7, n);
            for (int i = 0; i < n; i++)
                ck(b, (long)a[i + 7], (long)((long)Ic(i)));
        }

        private static void relPut(LongBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            for (int i = 0; i < n; i++)
                b.Put((long)Ic(i));
            b.Flip();
        }

        private static void absPut(LongBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            for (int i = 0; i < n; i++)
                b.Put(i, (long)Ic(i));
            b.Limit = (n);
            b.Position = (0);
        }

        private static void bulkPutArray(LongBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            long[] a = new long[n + 7];
            for (int i = 0; i < n; i++)
                a[i + 7] = (long)Ic(i);
            b.Put(a, 7, n);
            b.Flip();
        }

        private static void bulkPutBuffer(LongBuffer b)
        {
            int n = b.Capacity;
            b.Clear();
            LongBuffer c = LongBuffer.Allocate(n + 7);
            c.Position = (7);
            for (int i = 0; i < n; i++)
                c.Put((long)Ic(i));
            c.Flip();
            c.Position = (7);
            b.Put(c);
            b.Flip();
        }

        //6231529
        private static void callReset(LongBuffer b)
        {
            b.Position = (0);
            b.Mark();

            b.Duplicate().Reset();

            // LUCENENET: AsReadOnlyBuffer() not implemented
            //b.AsReadOnlyBuffer().Reset();
        }



        // 6221101-6234263

        private static void putBuffer()
        {
            int cap = 10;

            // LUCENENET: AllocateDirect not implemented

            //LongBuffer direct1 = ByteBuffer.AllocateDirect(cap).AsLongBuffer();
            LongBuffer nondirect1 = ByteBuffer.Allocate(cap).AsLongBuffer();
            //direct1.Put(nondirect1);

            //LongBuffer direct2 = ByteBuffer.AllocateDirect(cap).AsLongBuffer();
            LongBuffer nondirect2 = ByteBuffer.Allocate(cap).AsLongBuffer();
            //nondirect2.Put(direct2);

            //LongBuffer direct3 = ByteBuffer.AllocateDirect(cap).AsLongBuffer();
            //LongBuffer direct4 = ByteBuffer.AllocateDirect(cap).AsLongBuffer();
            //direct3.Put(direct4);

            LongBuffer nondirect3 = ByteBuffer.Allocate(cap).AsLongBuffer();
            LongBuffer nondirect4 = ByteBuffer.Allocate(cap).AsLongBuffer();
            nondirect3.Put(nondirect4);
        }

        private static void checkSlice(LongBuffer b, LongBuffer slice)
        {
            ck(slice, 0, slice.Position);
            ck(slice, b.Remaining, slice.Limit);
            ck(slice, b.Remaining, slice.Capacity);
            if (b.IsDirect != slice.IsDirect)
                fail("Lost direction", slice);
            if (b.IsReadOnly != slice.IsReadOnly)
                fail("Lost read-only", slice);
        }

        private static void fail(string problem,
                                 LongBuffer xb, LongBuffer yb,
                                 long x, long y)
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

        private static void tryCatch(long[] t, Type ex, Action thunk)
        {
            tryCatch(LongBuffer.Wrap(t), ex, thunk);
        }

        public static void test(int level, LongBuffer b, bool direct)
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
                b.Put((long)42);
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
                b.Put(b.Limit, (long)42);
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
            b.Put((long)0);
            b.Put((long)-1);
            b.Put((long)1);
            b.Put(long.MaxValue);
            b.Put(long.MinValue);

            long v;
            b.Flip();
            ck(b, b.Get(), 0);
            ck(b, b.Get(), (long)-1);
            ck(b, b.Get(), 1);
            ck(b, b.Get(), long.MaxValue);
            ck(b, b.Get(), long.MinValue);


            // Comparison
            b.Rewind();
            LongBuffer b2 = Lucene.Net.Support.LongBuffer.Allocate(b.Capacity);
            b2.Put(b);
            b2.Flip();
            b.Position = (2);
            b2.Position = (2);
            if (!b.equals(b2))
            {
                for (int i = 2; i < b.Limit; i++)
                {
                    long x = b.Get(i);
                    long y = b2.Get(i);
                    if (x != y)
                        output.WriteLine("[" + i + "] " + x + " != " + y);
                }
                fail("Identical buffers not equal", b, b2);
            }
            if (b.CompareTo(b2) != 0)
                fail("Comparison to identical buffer != 0", b, b2);

            b.Limit = (b.Limit + 1);
            b.Position = (b.Limit - 1);
            b.Put((long)99);
            b.Rewind();
            b2.Rewind();
            if (b.Equals(b2))
                fail("Non-identical buffers equal", b, b2);
            if (b.CompareTo(b2) <= 0)
                fail("Comparison to shorter buffer <= 0", b, b2);
            b.Limit = (b.Limit - 1);

            b.Put(2, (long)42);
            if (b.equals(b2))
                fail("Non-identical buffers equal", b, b2);
            if (b.CompareTo(b2) <= 0)
                fail("Comparison to lesser buffer <= 0", b, b2);

            // Check equals and compareTo with interesting values
            foreach (long x in VALUES)
            {
                LongBuffer xb = Lucene.Net.Support.LongBuffer.Wrap(new long[] { x });
                if (xb.CompareTo(xb) != 0)
                {
                    fail("compareTo not reflexive", xb, xb, x, x);
                }
                if (!xb.equals(xb))
                {
                    fail("equals not reflexive", xb, xb, x, x);
                }
                foreach (long y in VALUES)
                {
                    LongBuffer yb = Lucene.Net.Support.LongBuffer.Wrap(new long[] { y });
                    if (xb.CompareTo(yb) != -yb.CompareTo(xb))
                    {
                        fail("compareTo not anti-symmetric",
                             xb, yb, x, y);
                    }
                    if ((xb.CompareTo(yb) == 0) != xb.equals(yb))
                    {
                        fail("compareTo inconsistent with equals",
                             xb, yb, x, y);
                    }
                    // from Long.compare(x, y)
                    if (xb.CompareTo(yb) != ((x < y) ? -1 : ((x == y) ? 0 : 1)))
                    {

                        fail("Incorrect results for LongBuffer.compareTo",
                             xb, yb, x, y);
                    }
                    if (xb.equals(yb) != ((x == y) || (x != x) && (y != y)))
                    {
                        fail("Incorrect results for LongBuffer.equals",
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
            LongBuffer sb = b.Slice();
            checkSlice(b, sb);
            b.Position = (0);
            LongBuffer sb2 = sb.Slice();
            checkSlice(sb, sb2);

            if (!sb.equals(sb2))
                fail("Sliced slices do not match", sb, sb2);
            if ((sb.HasArray) && (sb.ArrayOffset != sb2.ArrayOffset))
                fail("Array offsets do not match: "
                     + sb.ArrayOffset + " != " + sb2.ArrayOffset, sb, sb2);


            // Read-only views

            b.Rewind();

            // LUCENENET: AsReadOnlyBuffer() not implemented
            tryCatch(b, typeof(NotImplementedException), () =>
            {
                b.AsReadOnlyBuffer();
            });

            // LUCENENET: AsReadOnlyBuffer() not implemented
            //LongBuffer rb = b.AsReadOnlyBuffer();
            //if (!b.Equals(rb))
            //    fail("Buffer not equal to read-only view", b, rb);
            //Show(level + 1, rb);

            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    relPut(rb);
            //});

            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    absPut(rb);
            //});

            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    bulkPutArray(rb);
            //});

            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    bulkPutBuffer(rb);
            //});

            //// put(LongBuffer) should not change source position
            //LongBuffer src = LongBuffer.Allocate(1);
            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    rb.Put(src);
            //});
            //ck(src, src.Position, 0);

            //tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //{
            //    rb.Compact();
            //});


            //if (rb.GetType().Name.StartsWith("Heap"))
            //{

            //    tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //    {
            //        var x = rb.Array;
            //    });

            //    tryCatch(b, typeof(ReadOnlyBufferException), () =>
            //    {
            //        var x = rb.ArrayOffset;
            //    });

            //    if (rb.HasArray)
            //        fail("Read-only heap buffer's backing array is accessible",
            //             rb);

            //}

            // Bulk puts from read-only buffers

            b.Clear();
            //rb.Rewind();
            //b.Put(rb);

            relPut(b);                       // Required by testViews

        }


        public static void test(long[] ba)
        {
            int offset = 47;
            int length = 900;
            LongBuffer b = LongBuffer.Wrap(ba, offset, length);
            Show(0, b);
            ck(b, b.Capacity, ba.Length);
            ck(b, b.Position, offset);
            ck(b, b.Limit, offset + length);

            // The offset must be non-negative and no larger than <array.length>.
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                LongBuffer.Wrap(ba, -1, ba.Length);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                LongBuffer.Wrap(ba, ba.Length + 1, ba.Length);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                LongBuffer.Wrap(ba, 0, -1);
            });
            tryCatch(ba, typeof(ArgumentOutOfRangeException), () =>
            {
                LongBuffer.Wrap(ba, 0, ba.Length + 1);
            });

            // A NullPointerException will be thrown if the array is null.
            tryCatch(ba, typeof(NullReferenceException), () =>
            {
                LongBuffer.Wrap((long[])null, 0, 5);
            });
            tryCatch(ba, typeof(NullReferenceException), () =>
            {
                LongBuffer.Wrap((long[])null);
            });
        }


        public static void TestAllocate()
        {
            // An IllegalArgumentException will be thrown for negative capacities.
            tryCatch((Buffer)null, typeof(ArgumentException), () =>
            {
                LongBuffer.Allocate(-1);
            });
        }

        [Test, LuceneNetSpecific]
        public static void Test()
        {
            TestAllocate();
            test(0, LongBuffer.Allocate(7 * 1024), false);
            test(0, LongBuffer.Wrap(new long[7 * 1024], 0, 7 * 1024), false);
            test(new long[1024]);

            callReset(LongBuffer.Allocate(10));
            putBuffer();

        }
    }
}
