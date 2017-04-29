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
    public class TestHeapByteBuffer : TestByteBuffer
    {
        public override void SetUp() 
        {
            base.SetUp();
            buf = ByteBuffer.Allocate(BUFFER_LENGTH);
            baseBuf = buf;
        }

        public override void TearDown() 
        {
            base.TearDown();
            buf = null;
            baseBuf = null;
        }

        [Test, LuceneNetSpecific]
        public void TestAllocatedByteBuffer_IllegalArg()
        {
            try
            {
                ByteBuffer.Allocate(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected 
            }
        }

        [Test, LuceneNetSpecific]
        public override void TestIsDirect()
        {
            assertFalse(buf.IsDirect);
        }

        [Test, LuceneNetSpecific]
        public override void TestHasArray()
        {
            assertTrue(buf.HasArray);
        }

        [Test, LuceneNetSpecific]
        public override void TestIsReadOnly()
        {
            assertFalse(buf.IsReadOnly);
        }

        #region TestByteBuffer
        // LUCENENET NOTE: Tests in an abstract base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public override void TestArray()
        {
            base.TestArray();
        }

        [Test, LuceneNetSpecific]
        public override void TestArrayOffset()
        {
            base.TestArrayOffset();
        }

        [Test, LuceneNetSpecific]
        public override void TestAsReadOnlyBuffer()
        {
            base.TestAsReadOnlyBuffer();
        }

        [Test, LuceneNetSpecific]
        public override void TestCompact()
        {
            base.TestCompact();
        }

        [Test, LuceneNetSpecific]
        public override void TestCompareTo()
        {
            base.TestCompareTo();
        }

        [Test, LuceneNetSpecific]
        public override void TestDuplicate()
        {
            base.TestDuplicate();
        }

        [Test, LuceneNetSpecific]
        public override void TestEquals()
        {
            base.TestEquals();
        }

        /*
         * Class under test for byte get()
         */
        [Test, LuceneNetSpecific]
        public override void TestGet()
        {
            base.TestGet();
        }

        /*
         * Class under test for java.nio.ByteBuffer get(byte[])
         */
        [Test, LuceneNetSpecific]
        public override void TestGetbyteArray()
        {
            base.TestGetbyteArray();
        }

        /*
         * Class under test for java.nio.ByteBuffer get(byte[], int, int)
         */
        [Test, LuceneNetSpecific]
        public override void TestGetbyteArrayintint()
        {
            base.TestGetbyteArrayintint();
        }

        /*
         * Class under test for byte get(int)
         */
        [Test, LuceneNetSpecific]
        public override void TestGetint()
        {
            base.TestGetint();
        }

        //[Test, LuceneNetSpecific]
        //public override void TestHasArray()
        //{
        //    base.TestHasArray();
        //}

        [Test, LuceneNetSpecific]
        public override void TestHashCode()
        {
            base.TestHashCode();
        }

        //[Test, LuceneNetSpecific]
        //public override void TestIsDirect()
        //{
        //    base.TestIsDirect();
        //}

        [Test, LuceneNetSpecific]
        public override void TestOrder()
        {
            base.TestOrder();
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte)
         */
        [Test, LuceneNetSpecific]
        public override void TestPutbyte()
        {
            base.TestPutbyte();
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte[])
         */
        [Test, LuceneNetSpecific]
        public override void TestPutbyteArray()
        {
            base.TestPutbyteArray();
        }

        /*
         * Class under test for java.nio.ByteBuffer put(byte[], int, int)
         */
        [Test, LuceneNetSpecific]
        public override void TestPutbyteArrayintint()
        {
            base.TestPutbyteArrayintint();
        }

        /*
         * Class under test for java.nio.ByteBuffer put(java.nio.ByteBuffer)
         */
        [Test, LuceneNetSpecific]
        public override void TestPutByteBuffer()
        {
            base.TestPutByteBuffer();
        }

        /*
         * Class under test for java.nio.ByteBuffer put(int, byte)
         */
        [Test, LuceneNetSpecific]
        public override void TestPutintbyte()
        {
            base.TestPutintbyte();
        }

        [Test, LuceneNetSpecific]
        public override void TestSlice()
        {
            base.TestSlice();
        }

        [Test, LuceneNetSpecific]
        public override void TestToString()
        {
            base.TestToString();
        }

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public override void TestAsCharBuffer()
        //{
        //    base.TestAsCharBuffer();
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public override void TestAsDoubleBuffer()
        //{
        //    base.TestAsDoubleBuffer();
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public override void TestAsFloatBuffer()
        //{
        //    base.TestAsFloatBuffer();
        //}

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public override void TestAsIntBuffer()
        //{
        //    base.TestAsIntBuffer();
        //}

        [Test, LuceneNetSpecific]
        public override void TestAsLongBuffer()
        {
            base.TestAsLongBuffer();
        }

        // LUCENENET NOTE: Not supported
        //[Test, LuceneNetSpecific]
        //public override void TestAsShortBuffer()
        //{
        //    base.TestAsShortBuffer();
        //}

        [Test, LuceneNetSpecific]
        public override void TestGetChar()
        {
            base.TestGetChar();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetCharint()
        {
            base.TestGetCharint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutChar()
        {
            base.TestPutChar();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutCharint()
        {
            base.TestPutCharint();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetDouble()
        {
            base.TestGetDouble();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetDoubleint()
        {
            base.TestGetDoubleint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutDouble()
        {
            base.TestPutDouble();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutDoubleint()
        {
            base.TestPutDoubleint();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetFloat()
        {
            base.TestGetFloat();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetFloatint()
        {
            base.TestGetFloatint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutFloat()
        {
            base.TestPutFloat();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutFloatint()
        {
            base.TestPutFloatint();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetInt()
        {
            base.TestGetInt();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetIntint()
        {
            base.TestGetIntint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutInt()
        {
            base.TestPutInt();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutIntint()
        {
            base.TestPutIntint();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetLong()
        {
            base.TestGetLong();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetLongint()
        {
            base.TestGetLongint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutLong()
        {
            base.TestPutLong();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutLongint()
        {
            base.TestPutLongint();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetShort()
        {
            base.TestGetShort();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetShortint()
        {
            base.TestGetShortint();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutShort()
        {
            base.TestPutShort();
        }

        [Test, LuceneNetSpecific]
        public override void TestPutShortint()
        {
            base.TestPutShortint();
        }

        /**
         * @tests java.nio.ByteBuffer.Wrap(byte[],int,int)
         */
        [Test, LuceneNetSpecific]
        public override void TestWrappedByteBuffer_null_array()
        {
            base.TestWrappedByteBuffer_null_array();
        }

        #endregion

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

        //[Test, LuceneNetSpecific]
        //public override void TestIsReadOnly()
        //{
        //    base.TestIsReadOnly();
        //}

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
