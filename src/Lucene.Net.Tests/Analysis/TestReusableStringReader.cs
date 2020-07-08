using System.Text;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Analysis
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
    public class TestReusableStringReader : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            char[] buf = new char[4];

            using (ReusableStringReader reader = new ReusableStringReader())
            {
                Assert.AreEqual(-1, reader.Read());
                Assert.AreEqual(-1, reader.Read(new char[1], 0, 1));
                Assert.AreEqual(-1, reader.Read(new char[2], 1, 1));
                //Assert.AreEqual(-1, reader.Read(CharBuffer.wrap(new char[2])));

                reader.SetValue("foobar");
                Assert.AreEqual(4, reader.Read(buf, 0, 4));
                Assert.AreEqual("foob", new string(buf));
                Assert.AreEqual(2, reader.Read(buf, 0, 2));
                Assert.AreEqual("ar", new string(buf, 0, 2));
                Assert.AreEqual(-1, reader.Read(buf, 2, 0));
            }

            using (ReusableStringReader reader = new ReusableStringReader())
            {
                reader.SetValue("foobar");
                Assert.AreEqual(0, reader.Read(buf, 1, 0));
                Assert.AreEqual(3, reader.Read(buf, 1, 3));
                Assert.AreEqual("foo", new string(buf, 1, 3));
                Assert.AreEqual(2, reader.Read(buf, 2, 2));
                Assert.AreEqual("ba", new string(buf, 2, 2));
                Assert.AreEqual('r', (char)reader.Read());
                Assert.AreEqual(-1, reader.Read(buf, 2, 0));
                reader.Dispose();

                reader.SetValue("foobar");
                StringBuilder sb = new StringBuilder();
                int ch;
                while ((ch = reader.Read()) != -1)
                {
                    sb.Append((char)ch);
                }
                Assert.AreEqual("foobar", sb.ToString());
            }
        }
    }
}