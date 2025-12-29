using System;
using Lucene.Net.Attributes;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    [LuceneNetSpecific]
    public class TestByteArrayDataOutput : LuceneTestCase
    {
        [Test]
        public virtual void TestWriteString()
        {
            byte[] bytes = new byte[10];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(bytes);
            @out.WriteString("ABC");

            ByteArrayDataInput @in = new ByteArrayDataInput(bytes);
            Assert.AreEqual("ABC", @in.ReadString());
        }

        [Test]
        public virtual void TestWriteChars()
        {
            byte[] bytes = new byte[10];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(bytes);
            @out.WriteChars("ABC".AsSpan());

            ByteArrayDataInput @in = new ByteArrayDataInput(bytes);
            Assert.AreEqual("ABC", @in.ReadString());
        }
    }
}
