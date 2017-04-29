// This class was sourced from the Apache Harmony project
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Attributes;
using Lucene.Net.Util;
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
    public abstract class AbstractBufferTest : LuceneTestCase
    {
        protected Buffer baseBuf;

        public override void SetUp() 
        {
            base.SetUp();
            baseBuf = ByteBuffer.Allocate(10);
        }

        public override void TearDown()
        {
            base.TearDown();
        }

        [Test, LuceneNetSpecific]
        public virtual void TestCapacity()
        {
            assertTrue(0 <= baseBuf.Position && baseBuf.Position <= baseBuf.Limit
                    && baseBuf.Limit <= baseBuf.Capacity);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestClear()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            Buffer ret = baseBuf.Clear();
            assertSame(ret, baseBuf);
            assertEquals(baseBuf.Position, 0);
            assertEquals(baseBuf.Limit, baseBuf.Capacity);
            try
            {
                baseBuf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$S
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestFlip()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            Buffer ret = baseBuf.Flip();
            assertSame(ret, baseBuf);
            assertEquals(baseBuf.Position, 0);
            assertEquals(baseBuf.Limit, oldPosition);
            try
            {
                baseBuf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestHasRemaining()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            assertEquals(baseBuf.HasRemaining, baseBuf.Position < baseBuf.Limit);
            baseBuf.SetPosition(baseBuf.Limit);
            assertFalse(baseBuf.HasRemaining);

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestIsReadOnly()
        {
            var _ = baseBuf.IsReadOnly;
        }

        /*
         * Class under test for int limit()
         */
        [Test, LuceneNetSpecific]
        public virtual void TestLimit()
        {
            assertTrue(0 <= baseBuf.Position && baseBuf.Position <= baseBuf.Limit
                    && baseBuf.Limit <= baseBuf.Capacity);
        }

        /*
         * Class under test for Buffer limit(int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestLimitint()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            Buffer ret = baseBuf.SetLimit(baseBuf.Limit);
            assertSame(ret, baseBuf);

            baseBuf.Mark();
            baseBuf.SetLimit(baseBuf.Capacity);
            assertEquals(baseBuf.Limit, baseBuf.Capacity);
            // position should not change
            assertEquals(baseBuf.Position, oldPosition);
            // mark should be valid
            baseBuf.Reset();

            if (baseBuf.Capacity > 0)
            {
                baseBuf.SetLimit(baseBuf.Capacity);
                baseBuf.SetPosition(baseBuf.Capacity);
                baseBuf.Mark();
                baseBuf.SetLimit(baseBuf.Capacity - 1);
                // position should be the new limit
                assertEquals(baseBuf.Position, baseBuf.Limit);
                // mark should be invalid
                try
                {
                    baseBuf.Reset();
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (InvalidMarkException e)
                {
                    // expected
                }
            }

            try
            {
                baseBuf.SetLimit(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected
            }
            try
            {
                baseBuf.SetLimit(baseBuf.Capacity + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected
            }

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestMark()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            Buffer ret = baseBuf.Mark();
            assertSame(ret, baseBuf);

            baseBuf.Mark();
            baseBuf.SetPosition(baseBuf.Limit);
            baseBuf.Reset();
            assertEquals(baseBuf.Position, oldPosition);

            baseBuf.Mark();
            baseBuf.SetPosition(baseBuf.Limit);
            baseBuf.Reset();
            assertEquals(baseBuf.Position, oldPosition);

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        /*
         * Class under test for int position()
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPosition()
        {
            assertTrue(0 <= baseBuf.Position && baseBuf.Position <= baseBuf.Limit
                    && baseBuf.Limit <= baseBuf.Capacity);
        }

        /*
         * Class under test for Buffer position(int)
         */
        [Test, LuceneNetSpecific]
        public virtual void TestPositionint()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            try
            {
                baseBuf.SetPosition(-1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected
            }
            try
            {
                baseBuf.SetPosition(baseBuf.Limit + 1);
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (ArgumentException e)
            {
                // expected
            }

            baseBuf.Mark();
            baseBuf.SetPosition(baseBuf.Position);
            baseBuf.Reset();
            assertEquals(baseBuf.Position, oldPosition);

            baseBuf.SetPosition(0);
            assertEquals(baseBuf.Position, 0);
            baseBuf.SetPosition(baseBuf.Limit);
            assertEquals(baseBuf.Position, baseBuf.Limit);

            if (baseBuf.Capacity > 0)
            {
                baseBuf.SetLimit(baseBuf.Capacity);
                baseBuf.SetPosition(baseBuf.Limit);
                baseBuf.Mark();
                baseBuf.SetPosition(baseBuf.Limit - 1);
                assertEquals(baseBuf.Position, baseBuf.Limit - 1);
                // mark should be invalid
                try
                {
                    baseBuf.Reset();
                    fail("Should throw Exception"); //$NON-NLS-1$
                }
                catch (InvalidMarkException e)
                {
                    // expected
                }
            }

            Buffer ret = baseBuf.SetPosition(0);
            assertSame(ret, baseBuf);

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestRemaining()
        {
            assertEquals(baseBuf.Remaining, baseBuf.Limit - baseBuf.Position);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestReset()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            baseBuf.Mark();
            baseBuf.SetPosition(baseBuf.Limit);
            baseBuf.Reset();
            assertEquals(baseBuf.Position, oldPosition);

            baseBuf.Mark();
            baseBuf.SetPosition(baseBuf.Limit);
            baseBuf.Reset();
            assertEquals(baseBuf.Position, oldPosition);

            Buffer ret = baseBuf.Reset();
            assertSame(ret, baseBuf);

            baseBuf.Clear();
            try
            {
                baseBuf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestRewind()
        {
            // save state
            int oldPosition = baseBuf.Position;
            int oldLimit = baseBuf.Limit;

            Buffer ret = baseBuf.Rewind();
            assertEquals(baseBuf.Position, 0);
            assertSame(ret, baseBuf);
            try
            {
                baseBuf.Reset();
                fail("Should throw Exception"); //$NON-NLS-1$
            }
            catch (InvalidMarkException e)
            {
                // expected
            }

            // restore state
            baseBuf.SetLimit(oldLimit);
            baseBuf.SetPosition(oldPosition);
        }
    }
}
