using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support
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
    public class TestCRC32
    {
        /// <summary></summary>
        /// <throws></throws>
        [Test, LuceneNetSpecific]
        public virtual void TestCRC32_()
        {
            byte[] b = new byte[256];
            for (int i = 0; i < b.Length; i++)
                b[i] = (byte)i;

            IChecksum digest = new CRC32();
            digest.Update(b, 0, b.Length);

            Int64 expected = 688229491;
            Assert.AreEqual(expected, digest.Value);
        }
    }
}