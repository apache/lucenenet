// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Attributes;
using NUnit.Framework;
using System;

namespace Lucene.Net.Support.IO
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

    [TestFixture]
    public class TestByteBuffer : AbstractBufferTest
    {
        protected static readonly int SMALL_TEST_LENGTH = 5;
        protected static readonly int BUFFER_LENGTH = 250;

        protected ByteBuffer buf;

        public override void SetUp() 
        {
            buf = ByteBuffer.Allocate(10);
            loadTestData1(buf);
            baseBuf = buf;
        }

        public override void TearDown() 
        {
            base.TearDown();
        }

        [Test, LuceneNetSpecific]
        public virtual void TestArray()
        {
            if (buf.HasArray)
            {
                byte[] array = buf.Array;
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData1(array, buf.ArrayOffset, buf.Capacity);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData2(array, buf.ArrayOffset, buf.Capacity);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData1(buf);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData2(buf);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);
            }
            else
            {
                if (buf.IsReadOnly)
                {
                    try
                    {
                        var _ = buf.Array;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                        // Note:can not tell when to throw 
                        // NotSupportedException
                        // or ReadOnlyBufferException, so catch all.
                    }
                    // LUCENENET specific - ReadOnlyBufferException doesn't inherit
                    // NotSupportedException.
                    catch (ReadOnlyBufferException)
                    {
                        // expected
                    }
                }
                else
                {
                    try
                    {
                        var _ = buf.Array;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                    }
                    // LUCENENET specific - ReadOnlyBufferException doesn't inherit
                    // NotSupportedException.
                    catch (ReadOnlyBufferException)
                    {
                        // expected
                    }
                }
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestArrayOffset()
        {
            if (buf.HasArray)
            {
                byte[] array = buf.Array;
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData1(array, buf.ArrayOffset, buf.Capacity);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData2(array, buf.ArrayOffset, buf.Capacity);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData1(buf);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);

                loadTestData2(buf);
                assertContentEquals(buf, array, buf.ArrayOffset, buf.Capacity);
            }
            else
            {
                if (buf.IsReadOnly)
                {
                    try
                    {
                        var _ = buf.ArrayOffset;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                        // Note:can not tell when to throw 
                        // NotSupportedException
                        // or ReadOnlyBufferException, so catch all.
                    }
                    // LUCENENET specific - ReadOnlyBufferException doesn't inherit
                    // NotSupportedException.
                    catch (ReadOnlyBufferException)
                    {
                        // expected
                    }
                }
                else
                {
                    try
                    {
                        var _ = buf.ArrayOffset;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                    }
                    // LUCENENET specific - ReadOnlyBufferException doesn't inherit
                    // NotSupportedException.
                    catch (ReadOnlyBufferException)
                    {
                        // expected
                    }
                }
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAsReadOnlyBuffer()
        {
            buf.Clear();
            buf.Mark();
            buf.SetPosition(buf.Limit);

            // readonly's contents should be the same as buf
            ByteBuffer @readonly = buf.AsReadOnlyBuffer();
            assertNotSame(buf, @readonly);
            assertTrue(@readonly.IsReadOnly);
            assertEquals(buf.Position, @readonly.Position);
            assertEquals(buf.Limit, @readonly.Limit);
            assertEquals(buf.IsDirect, @readonly.IsDirect);
            assertEquals(buf.Order, @readonly.Order);
            assertContentEquals(buf, @readonly);

            // readonly's position, mark, and limit should be independent to buf
            @readonly.Reset();
            assertEquals(@readonly.Position, 0);
            @readonly.Clear();
            assertEquals(buf.Position, buf.Limit);
            buf.Reset();
            assertEquals(buf.Position, 0);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCompact()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Compact();
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            // case: buffer is full
            buf.Clear();
            buf.Mark();
            loadTestData1(buf);
            ByteBuffer ret = buf.Compact();
            assertSame(ret, buf);
            assertEquals(buf.Position, buf.Capacity);
            assertEquals(buf.Limit, buf.Capacity);
            assertContentLikeTestData1(buf, 0, (byte)0, buf.Capacity);
            try
            {
                buf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // case: buffer is empty
            buf.SetPosition(0);
            buf.SetLimit(0);
            buf.Mark();
            ret = buf.Compact();
            assertSame(ret, buf);
            assertEquals(buf.Position, 0);
            assertEquals(buf.Limit, buf.Capacity);
            assertContentLikeTestData1(buf, 0, (byte)0, buf.Capacity);
            try
            {
                buf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // case: normal
            assertTrue(buf.Capacity > SMALL_TEST_LENGTH);
            buf.SetPosition(1);
            buf.SetLimit(SMALL_TEST_LENGTH);
            buf.Mark();
            ret = buf.Compact();
            assertSame(ret, buf);
            assertEquals(buf.Position, 4);
            assertEquals(buf.Limit, buf.Capacity);
            assertContentLikeTestData1(buf, 0, (byte)1, 4);
            try
            {
                buf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCompareTo()
        {
            // compare to self
            assertEquals(0, buf.CompareTo(buf));

            // normal cases
            if (!buf.IsReadOnly)
            {
                assertTrue(buf.Capacity > SMALL_TEST_LENGTH);
                buf.Clear();
                ByteBuffer other = ByteBuffer.Allocate(buf.Capacity);
                loadTestData1(buf);
                loadTestData1(other);
                assertEquals(0, buf.CompareTo(other));
                assertEquals(0, other.CompareTo(buf));
                buf.SetPosition(1);
                assertTrue(buf.CompareTo(other) > 0);
                assertTrue(other.CompareTo(buf) < 0);
                other.SetPosition(2);
                assertTrue(buf.CompareTo(other) < 0);
                assertTrue(other.CompareTo(buf) > 0);
                buf.SetPosition(2);
                other.SetLimit(SMALL_TEST_LENGTH);
                assertTrue(buf.CompareTo(other) > 0);
                assertTrue(other.CompareTo(buf) < 0);
            }

            // LUCENENET NOTE: AllocateDirect() not supported
            //assertTrue(ByteBuffer.Wrap(new byte[21]).CompareTo(ByteBuffer.AllocateDirect(21)) == 0);
            assertTrue(ByteBuffer.Wrap(new byte[21]).CompareTo(ByteBuffer.Allocate(21)) == 0);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestDuplicate()
        {
            buf.Clear();
            buf.Mark();
            buf.SetPosition(buf.Limit);

            // duplicate's contents should be the same as buf
            ByteBuffer duplicate = buf.Duplicate();
            assertNotSame(buf, duplicate);
            assertEquals(buf.Position, duplicate.Position);
            assertEquals(buf.Limit, duplicate.Limit);
            assertEquals(buf.IsReadOnly, duplicate.IsReadOnly);
            assertEquals(buf.IsDirect, duplicate.IsDirect);
            assertEquals(buf.Order, duplicate.Order);
            assertContentEquals(buf, duplicate);

            // duplicate's position, mark, and limit should be independent to buf
            duplicate.Reset();
            assertEquals(duplicate.Position, 0);
            duplicate.Clear();
            assertEquals(buf.Position, buf.Limit);
            buf.Reset();
            assertEquals(buf.Position, 0);

            // duplicate share the same content with buf
            if (!duplicate.IsReadOnly)
            {
                loadTestData1(buf);
                assertContentEquals(buf, duplicate);
                loadTestData2(duplicate);
                assertContentEquals(buf, duplicate);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestEquals()
        {
            // equal to self
            assertTrue(buf.equals(buf));
            ByteBuffer @readonly = buf.AsReadOnlyBuffer();
            assertTrue(buf.equals(@readonly));
            ByteBuffer duplicate = buf.Duplicate();
            assertTrue(buf.equals(duplicate));

            // always false, if type mismatch
            assertFalse(buf.equals(Boolean.TrueString));

            assertTrue(buf.Capacity > SMALL_TEST_LENGTH);

            buf.SetLimit(buf.Capacity).SetPosition(0);
            @readonly.SetLimit(@readonly.Capacity).SetPosition(1);
            assertFalse(buf.equals(@readonly));

            buf.SetLimit(buf.Capacity - 1).SetPosition(0);
            duplicate.SetLimit(duplicate.Capacity).SetPosition(0);
            assertFalse(buf.equals(duplicate));
        }

        /*
         * Class under test for byte get()
         */
        [Test, LuceneNetSpecific]
        public virtual void TestGet()
        {
            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, i);
                assertEquals(buf.Get(), buf.Get(i));
            }
            try
            {
                buf.Get();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }
        }

        /*
         * Class under test for java.nio.ByteBuffer get(byte[])
         */
        [Test, LuceneNetSpecific]
        public virtual void TestGetbyteArray()
        {
            byte[] array = new byte[1];
            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, i);
                ByteBuffer ret = buf.Get(array);
                assertEquals(array[0], buf.Get(i));
                assertSame(ret, buf);
            }
            try
            {
                buf.Get(array);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }
            try
            {
                buf.Get((byte[])null);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
                // expected
            }
        }

        /*
         * Class under test for java.nio.ByteBuffer get(byte[], int, int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestGetbyteArrayintint()
        {
            buf.Clear();
            byte[] array = new byte[buf.Capacity];

            try
            {
                buf.Get(new byte[buf.Capacity + 1], 0, buf.Capacity + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }
            assertEquals(buf.Position, 0);
            try
            {
                buf.Get(array, -1, array.Length);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            buf.Get(array, array.Length, 0);
            try
            {
                buf.Get(array, array.Length + 1, 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            assertEquals(buf.Position, 0);
            try
            {
                buf.Get(array, 2, -1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Get(array, 2, array.Length);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Get((byte[])null, -1, 0);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
                // expected
            }
            try
            {
                buf.Get(array, 1, int.MaxValue);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Get(array, int.MaxValue, 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            assertEquals(buf.Position, 0);

            buf.Clear();
            ByteBuffer ret = buf.Get(array, 0, array.Length);
            assertEquals(buf.Position, buf.Capacity);
            assertContentEquals(buf, array, 0, array.Length);
            assertSame(ret, buf);
        }

        /*
         * Class under test for byte get(int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestGetint()
        {
            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, i);
                assertEquals(buf.Get(), buf.Get(i));
            }
            try
            {
                buf.Get(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Get(buf.Limit);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestHasArray()
        {
            if (buf.HasArray)
            {
                assertNotNull(buf.Array);
            }
            else
            {
                if (buf.IsReadOnly)
                {
                    try
                    {
                        var _ = buf.Array;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                        // Note:can not tell when to throw 
                        // NotSupportedException
                        // or ReadOnlyBufferException, so catch all.
                    }
                }
                else
                {
                    try
                    {
                        var _ = buf.Array;
                        fail("Should throw Exception"); //$NON-NLS-1$
                    }
                    catch (NotSupportedException e)
                    {
                        // expected
                    }
                }
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestHashCode()
        {
            buf.Clear();
            loadTestData1(buf);
            ByteBuffer @readonly = buf.AsReadOnlyBuffer();
            ByteBuffer duplicate = buf.Duplicate();
            assertTrue(buf.GetHashCode() == @readonly.GetHashCode());
            assertTrue(buf.Capacity > SMALL_TEST_LENGTH);
            duplicate.SetPosition(buf.Capacity / 2);
            assertTrue(buf.GetHashCode() != duplicate.GetHashCode());
        }

        //for the testHashCode() method of readonly subclasses
        protected void readOnlyHashCode()
        {
            //create a new buffer initiated with some data 
            ByteBuffer buf = ByteBuffer.Allocate(BUFFER_LENGTH);
            loadTestData1(buf);
            buf = buf.AsReadOnlyBuffer();
            buf.Clear();
            ByteBuffer @readonly = buf.AsReadOnlyBuffer();
            ByteBuffer duplicate = buf.Duplicate();
            assertEquals(buf.GetHashCode(),@readonly.GetHashCode());
            duplicate.SetPosition(buf.Capacity / 2);
            assertTrue(buf.GetHashCode() != duplicate.GetHashCode());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsDirect()
        {
            var _ = buf.IsDirect;
        }

        [Test, LuceneNetSpecific]
        public virtual void TestOrder()
        {
            // BIG_ENDIAN is the default byte order
            assertEquals(ByteOrder.BIG_ENDIAN, buf.Order);

            buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
            assertEquals(ByteOrder.LITTLE_ENDIAN, buf.Order);

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
            assertEquals(ByteOrder.BIG_ENDIAN, buf.Order);

            // Regression test for HARMONY-798
            buf.SetOrder((ByteOrder)null);
            assertEquals(ByteOrder.LITTLE_ENDIAN, buf.Order);

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPutbyte()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.Put((byte)0);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, i);
                ByteBuffer ret = buf.Put((byte)i);
                assertEquals(buf.Get(i), (byte)i);
                assertSame(ret, buf);
            }
            try
            {
                buf.Put((byte)0);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte[])
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPutbyteArray()
        {
            byte[] array = new byte[1];
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Put(array);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, i);
                array[0] = (byte)i;
                ByteBuffer ret = buf.Put(array);
                assertEquals(buf.Get(i), (byte)i);
                assertSame(ret, buf);
            }
            try
            {
                buf.Put(array);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }
            try
            {
                buf.Put((byte[])null);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
                // expected
            }
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte[], int, int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPutbyteArrayintint()
        {
            buf.Clear();
            byte[] array = new byte[buf.Capacity];
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Put(array, 0, array.Length);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            try
            {
                buf.Put(new byte[buf.Capacity + 1], 0, buf.Capacity + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }
            assertEquals(buf.Position, 0);
            try
            {
                buf.Put(array, -1, array.Length);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Put(array, array.Length + 1, 0);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            buf.Put(array, array.Length, 0);
            assertEquals(buf.Position, 0);
            try
            {
                buf.Put(array, 0, -1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Put(array, 2, array.Length);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            try
            {
                buf.Put(array, 2, int.MaxValue);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Put(array, int.MaxValue, 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Put((byte[])null, 2, int.MaxValue);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
                // expected
            }

            assertEquals(buf.Position, 0);

            loadTestData2(array, 0, array.Length);
            ByteBuffer ret = buf.Put(array, 0, array.Length);
            assertEquals(buf.Position, buf.Capacity);
            assertContentEquals(buf, array, 0, array.Length);
            assertSame(ret, buf);
        }

        /*
         * Class under test for java.nio.ByteBuffer put(java.nio.ByteBuffer)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPutByteBuffer()
        {
            ByteBuffer other = ByteBuffer.Allocate(buf.Capacity);
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.Put(other);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                try
                {
                    buf.Clear();
                    buf.Put((ByteBuffer)null);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            try
            {
                buf.Put(buf);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected
            }
            try
            {
                buf.Put(ByteBuffer.Allocate(buf.Capacity + 1));
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            try
            {
                buf.Put((ByteBuffer)null);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
                // expected
            }
            loadTestData2(other);
            other.Clear();
            buf.Clear();
            ByteBuffer ret = buf.Put(other);
            assertEquals(other.Position, other.Capacity);
            assertEquals(buf.Position, buf.Capacity);
            assertContentEquals(other, buf);
            assertSame(ret, buf);
        }

        /*
         * Class under test for java.nio.ByteBuffer put(int, byte)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPutintbyte()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Put(0, (byte)0);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Position, 0);
                ByteBuffer ret = buf.Put(i, (byte)i);
                assertEquals(buf.Get(i), (byte)i);
                assertSame(ret, buf);
            }
            try
            {
                buf.Put(-1, (byte)0);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.Put(buf.Limit, (byte)0);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestSlice()
        {
            assertTrue(buf.Capacity > SMALL_TEST_LENGTH);
            buf.SetPosition(1);
            buf.SetLimit(buf.Capacity - 1);

            ByteBuffer slice = buf.Slice();
            assertEquals(buf.IsReadOnly, slice.IsReadOnly);
            assertEquals(buf.IsDirect, slice.IsDirect);
            assertEquals(buf.Order, slice.Order);
            assertEquals(slice.Position, 0);
            assertEquals(slice.Limit, buf.Remaining);
            assertEquals(slice.Capacity, buf.Remaining);
            try
            {
                slice.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // slice share the same content with buf
            if (!slice.IsReadOnly)
            {
                loadTestData1(slice);
                assertContentLikeTestData1(buf, 1, (byte)0, slice.Capacity);
                buf.Put(2, (byte)100);
                assertEquals(slice.Get(1), 100);
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestToString()
        {
            String str = buf.toString();
            assertTrue(str.IndexOf("Byte") >= 0 || str.IndexOf("byte") >= 0);
            assertTrue(str.IndexOf("" + buf.Position) >= 0);
            assertTrue(str.IndexOf("" + buf.Limit) >= 0);
            assertTrue(str.IndexOf("" + buf.Capacity) >= 0);
        }

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public virtual void TestAsCharBuffer()
        //{
        //    CharBuffer charBuffer;
        //    byte[] bytes = new byte[2];
        //    char value;

        //    // test BIG_ENDIAN char buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //    charBuffer = buf.asCharBuffer();
        //    assertSame(ByteOrder.BIG_ENDIAN, charBuffer.Order);
        //    while (charBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = charBuffer.Get();
        //        assertEquals(bytes2char(bytes, buf.Order), value);
        //    }

        //    // test LITTLE_ENDIAN char buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //    charBuffer = buf.asCharBuffer();
        //    assertSame(ByteOrder.LITTLE_ENDIAN, charBuffer.Order);
        //    while (charBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = charBuffer.Get();
        //        assertEquals(bytes2char(bytes, buf.Order), value);
        //    }

        //    if (!buf.IsReadOnly)
        //    {
        //        // test BIG_ENDIAN char buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //        charBuffer = buf.asCharBuffer();
        //        assertSame(ByteOrder.BIG_ENDIAN, charBuffer.Order);
        //        while (charBuffer.Remaining > 0)
        //        {
        //            value = (char)charBuffer.Remaining;
        //            charBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, char2bytes(value, buf.Order)));
        //        }

        //        // test LITTLE_ENDIAN char buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //        charBuffer = buf.asCharBuffer();
        //        assertSame(ByteOrder.LITTLE_ENDIAN, charBuffer.Order);
        //        while (charBuffer.Remaining > 0)
        //        {
        //            value = (char)charBuffer.Remaining;
        //            charBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, char2bytes(value, buf.Order)));
        //        }
        //    }
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public virtual void TestAsDoubleBuffer()
        //{
        //    DoubleBuffer doubleBuffer;
        //    byte[] bytes = new byte[8];
        //    double value;

        //    // test BIG_ENDIAN double buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //    doubleBuffer = buf.AsDoubleBuffer();
        //    assertSame(ByteOrder.BIG_ENDIAN, doubleBuffer.Order);
        //    while (doubleBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = doubleBuffer.Get();
        //        if (!(Double.IsNaN(bytes2double(bytes, buf.Order)) && Double
        //                .IsNaN(value)))
        //        {
        //            assertEquals(bytes2double(bytes, buf.Order), value, 0.00);
        //        }
        //    }

        //    // test LITTLE_ENDIAN double buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //    doubleBuffer = buf.AsDoubleBuffer();
        //    assertSame(ByteOrder.LITTLE_ENDIAN, doubleBuffer.Order);
        //    while (doubleBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = doubleBuffer.Get();
        //        if (!(Double.IsNaN(bytes2double(bytes, buf.Order)) && Double
        //                .IsNaN(value)))
        //        {
        //            assertEquals(bytes2double(bytes, buf.Order), value, 0.00);
        //        }
        //    }

        //    if (!buf.IsReadOnly)
        //    {
        //        // test BIG_ENDIAN double buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //        doubleBuffer = buf.AsDoubleBuffer();
        //        assertSame(ByteOrder.BIG_ENDIAN, doubleBuffer.Order);
        //        while (doubleBuffer.Remaining > 0)
        //        {
        //            value = (double)doubleBuffer.Remaining;
        //            doubleBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, double2bytes(value, buf.Order)));
        //        }

        //        // test LITTLE_ENDIAN double buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //        doubleBuffer = buf.AsDoubleBuffer();
        //        assertSame(ByteOrder.LITTLE_ENDIAN, doubleBuffer.Order);
        //        while (doubleBuffer.Remaining > 0)
        //        {
        //            value = (double)doubleBuffer.Remaining;
        //            doubleBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, double2bytes(value, buf.Order)));
        //        }
        //    }

        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public virtual void TestAsFloatBuffer()
        //{
        //    SingleBuffer floatBuffer;
        //    byte[] bytes = new byte[4];
        //    float value;

        //    // test BIG_ENDIAN float buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //    floatBuffer = buf.AsSingleBuffer();
        //    assertSame(ByteOrder.BIG_ENDIAN, floatBuffer.Order);
        //    while (floatBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = floatBuffer.Get();
        //        if (!(float.IsNaN(bytes2float(bytes, buf.Order)) && float
        //                .IsNaN(value)))
        //        {
        //            assertEquals(bytes2float(bytes, buf.Order), value, 0.00);
        //        }
        //    }

        //    // test LITTLE_ENDIAN float buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //    floatBuffer = buf.AsSingleBuffer();
        //    assertSame(ByteOrder.LITTLE_ENDIAN, floatBuffer.Order);
        //    while (floatBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = floatBuffer.Get();
        //        if (!(float.IsNaN(bytes2float(bytes, buf.Order)) && float
        //                .IsNaN(value)))
        //        {
        //            assertEquals(bytes2float(bytes, buf.Order), value, 0.00);
        //        }
        //    }

        //    if (!buf.IsReadOnly)
        //    {
        //        // test BIG_ENDIAN float buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //        floatBuffer = buf.AsSingleBuffer();
        //        assertSame(ByteOrder.BIG_ENDIAN, floatBuffer.Order);
        //        while (floatBuffer.Remaining > 0)
        //        {
        //            value = (float)floatBuffer.Remaining;
        //            floatBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, float2bytes(value, buf.Order)));
        //        }

        //        // test LITTLE_ENDIAN float buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //        floatBuffer = buf.AsSingleBuffer();
        //        assertSame(ByteOrder.LITTLE_ENDIAN, floatBuffer.Order);
        //        while (floatBuffer.Remaining > 0)
        //        {
        //            value = (float)floatBuffer.Remaining;
        //            floatBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, float2bytes(value, buf.Order)));
        //        }
        //    }

        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public virtual void TestAsIntBuffer()
        //{
        //    Int32Buffer intBuffer;
        //    byte[] bytes = new byte[4];
        //    int value;

        //    // test BIG_ENDIAN int buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //    intBuffer = buf.AsInt32Buffer();
        //    assertSame(ByteOrder.BIG_ENDIAN, intBuffer.Order);
        //    while (intBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = intBuffer.Get();
        //        assertEquals(bytes2int(bytes, buf.Order), value);
        //    }

        //    // test LITTLE_ENDIAN int buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //    intBuffer = buf.AsInt32Buffer();
        //    assertSame(ByteOrder.LITTLE_ENDIAN, intBuffer.Order);
        //    while (intBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = intBuffer.Get();
        //        assertEquals(bytes2int(bytes, buf.Order), value);
        //    }

        //    if (!buf.IsReadOnly)
        //    {
        //        // test BIG_ENDIAN int buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //        intBuffer = buf.AsInt32Buffer();
        //        assertSame(ByteOrder.BIG_ENDIAN, intBuffer.Order);
        //        while (intBuffer.Remaining > 0)
        //        {
        //            value = (int)intBuffer.Remaining;
        //            intBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, int2bytes(value, buf.Order)));
        //        }

        //        // test LITTLE_ENDIAN int buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //        intBuffer = buf.AsInt32Buffer();
        //        assertSame(ByteOrder.LITTLE_ENDIAN, intBuffer.Order);
        //        while (intBuffer.Remaining > 0)
        //        {
        //            value = (int)intBuffer.Remaining;
        //            intBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, int2bytes(value, buf.Order)));
        //        }
        //    }

        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //}

        [Test, LuceneNetSpecific]
        public virtual void TestAsLongBuffer()
        {
            Int64Buffer longBuffer;
            byte[] bytes = new byte[8];
            long value;

            // test BIG_ENDIAN long buffer, read
            buf.Clear();
            buf.SetOrder(ByteOrder.BIG_ENDIAN);
            longBuffer = buf.AsInt64Buffer();
            assertSame(ByteOrder.BIG_ENDIAN, longBuffer.Order);
            while (longBuffer.Remaining > 0)
            {
                buf.Get(bytes);
                value = longBuffer.Get();
                assertEquals(bytes2long(bytes, buf.Order), value);
            }

            // test LITTLE_ENDIAN long buffer, read
            buf.Clear();
            buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
            longBuffer = buf.AsInt64Buffer();
            assertSame(ByteOrder.LITTLE_ENDIAN, longBuffer.Order);
            while (longBuffer.Remaining > 0)
            {
                buf.Get(bytes);
                value = longBuffer.Get();
                assertEquals(bytes2long(bytes, buf.Order), value);
            }

            if (!buf.IsReadOnly)
            {
                // test BIG_ENDIAN long buffer, write
                buf.Clear();
                buf.SetOrder(ByteOrder.BIG_ENDIAN);
                longBuffer = buf.AsInt64Buffer();
                assertSame(ByteOrder.BIG_ENDIAN, longBuffer.Order);
                while (longBuffer.Remaining > 0)
                {
                    value = (long)longBuffer.Remaining;
                    longBuffer.Put(value);
                    buf.Get(bytes);
                    assertTrue(Arrays.Equals(bytes, long2bytes(value, buf.Order)));
                }

                // test LITTLE_ENDIAN long buffer, write
                buf.Clear();
                buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
                longBuffer = buf.AsInt64Buffer();
                assertSame(ByteOrder.LITTLE_ENDIAN, longBuffer.Order);
                while (longBuffer.Remaining > 0)
                {
                    value = (long)longBuffer.Remaining;
                    longBuffer.Put(value);
                    buf.Get(bytes);
                    assertTrue(Arrays.Equals(bytes, long2bytes(value, buf.Order)));
                }
            }

            buf.Clear();
            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public virtual void TestAsShortBuffer()
        //{
        //    Int16Buffer shortBuffer;
        //    byte[] bytes = new byte[2];
        //    short value;

        //    // test BIG_ENDIAN short buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //    shortBuffer = buf.AsInt16Buffer();
        //    assertSame(ByteOrder.BIG_ENDIAN, shortBuffer.Order);
        //    while (shortBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = shortBuffer.Get();
        //        assertEquals(bytes2short(bytes, buf.Order), value);
        //    }

        //    // test LITTLE_ENDIAN short buffer, read
        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //    shortBuffer = buf.AsInt16Buffer();
        //    assertSame(ByteOrder.LITTLE_ENDIAN, shortBuffer.Order);
        //    while (shortBuffer.Remaining > 0)
        //    {
        //        buf.Get(bytes);
        //        value = shortBuffer.Get();
        //        assertEquals(bytes2short(bytes, buf.Order), value);
        //    }

        //    if (!buf.IsReadOnly)
        //    {
        //        // test BIG_ENDIAN short buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //        shortBuffer = buf.AsInt16Buffer();
        //        assertSame(ByteOrder.BIG_ENDIAN, shortBuffer.Order);
        //        while (shortBuffer.Remaining > 0)
        //        {
        //            value = (short)shortBuffer.Remaining;
        //            shortBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, short2bytes(value, buf.Order)));
        //        }

        //        // test LITTLE_ENDIAN short buffer, write
        //        buf.Clear();
        //        buf.SetOrder(ByteOrder.LITTLE_ENDIAN);
        //        shortBuffer = buf.AsInt16Buffer();
        //        assertSame(ByteOrder.LITTLE_ENDIAN, shortBuffer.Order);
        //        while (shortBuffer.Remaining > 0)
        //        {
        //            value = (short)shortBuffer.Remaining;
        //            shortBuffer.Put(value);
        //            buf.Get(bytes);
        //            assertTrue(Arrays.Equals(bytes, short2bytes(value, buf.Order)));
        //        }
        //    }

        //    buf.Clear();
        //    buf.SetOrder(ByteOrder.BIG_ENDIAN);
        //}

        [Test, LuceneNetSpecific]
        public virtual void TestGetChar()
        {
            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            char value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetChar();
                assertEquals(bytes2char(bytes, buf.Order), value);
            }

            try
            {
                buf.GetChar();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetCharint()
        {
            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            char value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetChar(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertEquals(bytes2char(bytes, buf.Order), value);
            }

            try
            {
                buf.GetChar(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetChar(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutChar()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutChar((char)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            char value = (char)0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (char)i;
                buf.Mark();
                buf.PutChar(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(char2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutChar(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutCharint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutChar(0, (char)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            char value = (char)0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (char)i;
                buf.SetPosition(i);
                buf.PutChar(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(char2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutChar(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutChar(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);

            try
            {
                // LUCENENET NOTE: AllocateDirect() not implemented
                //ByteBuffer.AllocateDirect(16).PutChar(int.MaxValue, 'h');
                ByteBuffer.Allocate(16).PutChar(int.MaxValue, 'h');
            }
            catch (IndexOutOfRangeException e)
            {
                //expected 
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetDouble()
        {
            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            double value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetDouble();
                if (!(Double.IsNaN(bytes2double(bytes, buf.Order)) && Double
                        .IsNaN(value)))
                {
                    assertEquals(bytes2double(bytes, buf.Order), value, 0.00);
                }
            }

            try
            {
                buf.GetDouble();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetDoubleint()
        {
            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            double value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetDouble(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                if (!(Double.IsNaN(bytes2double(bytes, buf.Order)) && Double
                        .IsNaN(value)))
                {
                    assertEquals(bytes2double(bytes, buf.Order), value, 0.00);
                }
            }

            try
            {
                buf.GetDouble(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetDouble(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);

            try
            {
                // LUCENENET NOTE: AllocateDirect() not supported
                //ByteBuffer.AllocateDirect(16).GetDouble(int.MaxValue);
                ByteBuffer.Allocate(16).GetDouble(int.MaxValue);
            }
            catch (IndexOutOfRangeException e)
            {
                //expected 
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutDouble()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutDouble((double)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            double value = 0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (double)i;
                buf.Mark();
                buf.PutDouble(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(double2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutDouble(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutDoubleint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutDouble(0, (double)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            double value = 0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (double)i;
                buf.SetPosition(i);
                buf.PutDouble(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(double2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutDouble(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutDouble(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetFloat()
        {
            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            float value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetSingle();
                if (!(float.IsNaN(bytes2float(bytes, buf.Order)) && float
                        .IsNaN(value)))
                {
                    assertEquals(bytes2float(bytes, buf.Order), value, 0.00);
                }
            }

            try
            {
                buf.GetSingle();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetFloatint()
        {
            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            float value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetSingle(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                if (!(float.IsNaN(bytes2float(bytes, buf.Order)) && float
                        .IsNaN(value)))
                {
                    assertEquals(bytes2float(bytes, buf.Order), value, 0.00);
                }
            }

            try
            {
                buf.GetSingle(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetSingle(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutFloat()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutSingle((float)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            float value = 0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (float)i;
                buf.Mark();
                buf.PutSingle(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(float2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutSingle(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutFloatint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutSingle(0, (float)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            float value = 0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (float)i;
                buf.SetPosition(i);
                buf.PutSingle(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(float2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutSingle(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutSingle(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetInt()
        {
            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            int value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetInt32();
                assertEquals(bytes2int(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt32();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetIntint()
        {
            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            int value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetInt32(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertEquals(bytes2int(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt32(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetInt32(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
            try
            {
                // LUCENENET NOTE: AllocateDirect() not implemented
                //ByteBuffer.AllocateDirect(16).GetInt32(int.MaxValue);
                ByteBuffer.Allocate(16).GetInt32(int.MaxValue);
            }
            catch (IndexOutOfRangeException e)
            {
                //expected 
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutInt()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutInt32((int)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            int value = 0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (int)i;
                buf.Mark();
                buf.PutInt32(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(int2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt32(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutIntint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutInt32(0, (int)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 4;
            byte[] bytes = new byte[nbytes];
            int value = 0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (int)i;
                buf.SetPosition(i);
                buf.PutInt32(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(int2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt32(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutInt32(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetLong()
        {
            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            long value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetInt64();
                assertEquals(bytes2long(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt64();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetLongint()
        {
            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            long value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetInt64(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertEquals(bytes2long(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt64(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetInt64(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutLong()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutInt64((long)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            long value = 0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (long)i;
                buf.Mark();
                buf.PutInt64(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(long2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt64(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutLongint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutInt64(0, (long)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 8;
            byte[] bytes = new byte[nbytes];
            long value = 0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (long)i;
                buf.SetPosition(i);
                buf.PutInt64(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(long2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt64(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutInt64(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetShort()
        {
            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            short value;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                assertEquals(i * nbytes, buf.Position);
                buf.Mark();
                buf.Get(bytes);
                buf.Reset();
                value = buf.GetInt16();
                assertEquals(bytes2short(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt16();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferUnderflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestGetShortint()
        {
            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            short value;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                buf.SetPosition(i);
                value = buf.GetInt16(i);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertEquals(bytes2short(bytes, buf.Order), value);
            }

            try
            {
                buf.GetInt16(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.GetInt16(buf.Limit - nbytes + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutShort()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.Clear();
                    buf.PutInt16((short)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            short value = 0;
            buf.Clear();
            for (int i = 0; buf.Remaining >= nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (short)i;
                buf.Mark();
                buf.PutInt16(value);
                assertEquals((i + 1) * nbytes, buf.Position);
                buf.Reset();
                buf.Get(bytes);
                assertTrue(Arrays.Equals(short2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt16(value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (BufferOverflowException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestPutShortint()
        {
            if (buf.IsReadOnly)
            {
                try
                {
                    buf.PutInt16(0, (short)1);
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (ReadOnlyBufferException e)
                {
                    // expected
                }
                return;
            }

            int nbytes = 2;
            byte[] bytes = new byte[nbytes];
            short value = 0;
            buf.Clear();
            for (int i = 0; i <= buf.Limit - nbytes; i++)
            {
                buf.SetOrder(i % 2 == 0 ? ByteOrder.BIG_ENDIAN
                        : ByteOrder.LITTLE_ENDIAN);
                value = (short)i;
                buf.SetPosition(i);
                buf.PutInt16(i, value);
                assertEquals(i, buf.Position);
                buf.Get(bytes);
                assertTrue(Arrays.Equals(short2bytes(value, buf.Order), bytes));
            }

            try
            {
                buf.PutInt16(-1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }
            try
            {
                buf.PutInt16(buf.Limit - nbytes + 1, value);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
                // expected
            }

            buf.SetOrder(ByteOrder.BIG_ENDIAN);
        }

        /**
         * @tests java.nio.ByteBuffer.Wrap(byte[],int,int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestWrappedByteBuffer_null_array()
        {
            // Regression for HARMONY-264
            byte[] array = null;
            try
            {
                ByteBuffer.Wrap(array, -1, 0);
                fail("Should throw NPE"); //$NON-NLS-1$
            }
            catch (NullReferenceException e)
            {
            }
            try
            {
                ByteBuffer.Wrap(new byte[10], int.MaxValue, 2);
                fail("Should throw IndexOutOfRangeException"); //$NON-NLS-1$
            }
            catch (IndexOutOfRangeException e)
            {
            }
        }

        private void loadTestData1(byte[] array, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                array[offset + i] = (byte)i;
            }
        }

        private void loadTestData2(byte[] array, int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                array[offset + i] = (byte)(length - i);
            }
        }

        private void loadTestData1(ByteBuffer buf)
        {
            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                buf.Put(i, (byte)i);
            }
        }

        private void loadTestData2(ByteBuffer buf)
        {
            buf.Clear();
            for (int i = 0; i < buf.Capacity; i++)
            {
                buf.Put(i, (byte)(buf.Capacity - i));
            }
        }

        private void assertContentEquals(ByteBuffer buf, byte[] array,
                int offset, int length)
        {
            for (int i = 0; i < length; i++)
            {
                assertEquals(buf.Get(i), array[offset + i]);
            }
        }

        private void assertContentEquals(ByteBuffer buf, ByteBuffer other)
        {
            assertEquals(buf.Capacity, other.Capacity);
            for (int i = 0; i < buf.Capacity; i++)
            {
                assertEquals(buf.Get(i), other.Get(i));
            }
        }

        private void assertContentLikeTestData1(ByteBuffer buf,
                int startIndex, byte startValue, int length)
        {
            byte value = startValue;
            for (int i = 0; i < length; i++)
            {
                assertEquals(buf.Get(startIndex + i), value);
                value = (byte)(value + 1);
            }
        }

        private int bytes2int(byte[] bytes, ByteOrder order)
        {
            int nbytes = 4, bigHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                bigHead = 0;
                step = 1;
            }
            else
            {
                bigHead = nbytes - 1;
                step = -1;
            }
            int result = 0;
            int p = bigHead;
            for (int i = 0; i < nbytes; i++)
            {
                result = result << 8;
                result = result | (bytes[p] & 0xff);
                p += step;
            }
            return result;
        }

        private long bytes2long(byte[] bytes, ByteOrder order)
        {
            int nbytes = 8, bigHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                bigHead = 0;
                step = 1;
            }
            else
            {
                bigHead = nbytes - 1;
                step = -1;
            }
            long result = 0;
            int p = bigHead;
            for (int i = 0; i < nbytes; i++)
            {
                result = result << 8;
                result = result | (uint)(bytes[p] & 0xff);
                p += step;
            }
            return result;
        }

        private short bytes2short(byte[] bytes, ByteOrder order)
        {
            int nbytes = 2, bigHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                bigHead = 0;
                step = 1;
            }
            else
            {
                bigHead = nbytes - 1;
                step = -1;
            }
            short result = 0;
            int p = bigHead;
            for (int i = 0; i < nbytes; i++)
            {
                result = (short)(result << 8);
                result = (short)((ushort)result | (uint)(bytes[p] & 0xff));
                p += step;
            }
            return result;
        }

        private char bytes2char(byte[] bytes, ByteOrder order)
        {
            return (char)bytes2short(bytes, order);
        }

        private float bytes2float(byte[] bytes, ByteOrder order)
        {
            return Number.Int32BitsToSingle(bytes2int(bytes, order));
        }

        private double bytes2double(byte[] bytes, ByteOrder order)
        {
            return BitConverter.Int64BitsToDouble(bytes2long(bytes, order));
        }

        private byte[] int2bytes(int value, ByteOrder order)
        {
            int nbytes = 4, smallHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                smallHead = nbytes - 1;
                step = -1;
            }
            else
            {
                smallHead = 0;
                step = 1;
            }
            byte[] bytes = new byte[nbytes];
            int p = smallHead;
            for (int i = 0; i < nbytes; i++)
            {
                bytes[p] = (byte)(value & 0xff);
                value = value >> 8;
                p += step;
            }
            return bytes;
        }

        private byte[] long2bytes(long value, ByteOrder order)
        {
            int nbytes = 8, smallHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                smallHead = nbytes - 1;
                step = -1;
            }
            else
            {
                smallHead = 0;
                step = 1;
            }
            byte[] bytes = new byte[nbytes];
            int p = smallHead;
            for (int i = 0; i < nbytes; i++)
            {
                bytes[p] = (byte)(value & 0xff);
                value = value >> 8;
                p += step;
            }
            return bytes;
        }

        private byte[] short2bytes(short value, ByteOrder order)
        {
            int nbytes = 2, smallHead, step;
            if (order == ByteOrder.BIG_ENDIAN)
            {
                smallHead = nbytes - 1;
                step = -1;
            }
            else
            {
                smallHead = 0;
                step = 1;
            }
            byte[] bytes = new byte[nbytes];
            int p = smallHead;
            for (int i = 0; i < nbytes; i++)
            {
                bytes[p] = (byte)(value & 0xff);
                value = (short)(value >> 8);
                p += step;
            }
            return bytes;
        }

        private byte[] char2bytes(char value, ByteOrder order)
        {
            return short2bytes((short)value, order);
        }

        private byte[] float2bytes(float value, ByteOrder order)
        {
            return int2bytes(Number.SingleToRawInt32Bits(value), order);
        }

        private byte[] double2bytes(double value, ByteOrder order)
        {
            return long2bytes(BitConverter.DoubleToInt64Bits(value), order);
        }



        #region AbstractBufferTest
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public override void TestCapacity()
        {
            base.TestCapacity();
        }

        [Test, LuceneNetSpecific]
        public override void TestClear()
        {
            base.TestClear();
        }

        [Test, LuceneNetSpecific]
        public override void TestFlip()
        {
            base.TestFlip();
        }

        [Test, LuceneNetSpecific]
        public override void TestHasRemaining()
        {
            base.TestHasRemaining();
        }

        [Test, LuceneNetSpecific]
        public override void TestIsReadOnly()
        {
            base.TestIsReadOnly();
        }

        /*
         * Class under test for int limit()
         */
        [Test, LuceneNetSpecific]
        public override void TestLimit()
        {
            base.TestLimit();
        }

        /*
         * Class under test for Buffer limit(int)
         */
        [Test, LuceneNetSpecific]
        public override void TestLimitint()
        {
            base.TestLimitint();
        }

        [Test, LuceneNetSpecific]
        public override void TestMark()
        {
            base.TestMark();
        }

        /*
         * Class under test for int position()
         */
        [Test, LuceneNetSpecific]
        public override void TestPosition()
        {
            base.TestPosition();
        }

        /*
         * Class under test for Buffer position(int)
         */
        [Test, LuceneNetSpecific]
        public override void TestPositionint()
        {
            base.TestPositionint();
        }

        [Test, LuceneNetSpecific]
        public override void TestRemaining()
        {
            base.TestRemaining();
        }

        [Test, LuceneNetSpecific]
        public override void TestReset()
        {
            base.TestReset();
        }

        [Test, LuceneNetSpecific]
        public override void TestRewind()
        {
            base.TestRewind();
        }

        #endregion
    }
}
