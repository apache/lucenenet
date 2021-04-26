using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    using Codec = Lucene.Net.Codecs.Codec;

    // TODO: maybe we should test this with mocks, but its easy
    // enough to test the basics via Codec
    [TestFixture]
    public class TestNamedSPILoader : LuceneTestCase
    {
        [Test]
        public virtual void TestLookup()
        {
            Codec codec = Codec.ForName("Lucene46");
            Assert.AreEqual("Lucene46", codec.Name);
        }

        // we want an exception if its not found.
        [Test]
        public virtual void TestBogusLookup()
        {
            try
            {
                Codec.ForName("dskfdskfsdfksdfdsf");
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
            }
        }

        [Test]
        public virtual void TestAvailableServices()
        {
            var codecs = Codec.AvailableCodecs;
            Assert.IsTrue(codecs.Contains("Lucene46"));
        }
    }
}