using Lucene.Net.Codecs.Lucene46;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Codecs
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

    public class TestCodecServices : LuceneTestCase
    {
        [Test]
        public void TestRetrieveCodecs()
        {
            var lucene46 = Codec.ForName("Lucene46");
            Assert.AreEqual(typeof(Lucene46Codec), lucene46.GetType());
            Assert.AreEqual("Lucene46", lucene46.Name);

            var myCodec = Codec.ForName("MyCodec");
            Assert.AreEqual(typeof(MyCodec), myCodec.GetType());
            Assert.AreEqual("MyCodec", myCodec.Name);
        }
    }
}
