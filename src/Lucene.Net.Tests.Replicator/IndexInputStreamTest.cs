using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Replicator;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Tests.Replicator
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

    //Note: LUCENENET specific
    [LuceneNetSpecific]
    public class IndexInputStreamTest : LuceneTestCase
    {
        
        [Test]
        [LuceneNetSpecific]
        public void Read_RemainingIndexInputLargerThanReadCount_ReturnsReadCount()
        {
            byte[] buffer = new byte[8.KiloBytes()];
            Random.NextBytes(buffer);
            IndexInputStream stream = new IndexInputStream(new MockIndexInput(buffer));

            int readBytes = 2.KiloBytes();
            byte[] readBuffer = new byte[readBytes];
            Assert.AreEqual(stream.Read(readBuffer, 0, readBytes), readBytes);
        }

        [Test]
        [LuceneNetSpecific]
        public void Read_RemainingIndexInputLargerThanReadCount_ReturnsExpectedSection([Range(1,8)]int section)
        {
            byte[] buffer = new byte[8.KiloBytes()];
            Random.NextBytes(buffer);
            IndexInputStream stream = new IndexInputStream(new MockIndexInput(buffer));

            int readBytes = 1.KiloBytes();
            byte[] readBuffer = new byte[readBytes];
            for (int i = section; i > 0; i--)
                stream.Read(readBuffer, 0, readBytes);
            Assert.AreEqual(readBuffer, buffer.Skip((section - 1) * readBytes).Take(readBytes).ToArray());
        }

    }

    //Note: LUCENENET specific
    internal static class ByteHelperExtensions
    {
        public static int KiloBytes(this int value)
        {
            return value * 1024;
        }
    }
}
