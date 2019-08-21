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
#pragma warning disable 168
            catch (ArgumentException e)
#pragma warning restore 168
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
    }
}
