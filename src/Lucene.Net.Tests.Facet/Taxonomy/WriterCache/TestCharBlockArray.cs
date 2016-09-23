using NUnit.Framework;
using System.IO;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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
    public class TestCharBlockArray : FacetTestCase
    {

        [Test]
        public virtual void TestArray()
        {
            CharBlockArray array = new CharBlockArray();
            StringBuilder builder = new StringBuilder();

            const int n = 100 * 1000;

            byte[] buffer = new byte[50];

            for (int i = 0; i < n; i++)
            {
                Random().NextBytes(buffer);
                int size = 1 + Random().Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.

                string s = Encoding.UTF8.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random().NextBytes(buffer);
                int size = 1 + Random().Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                string s = Encoding.UTF8.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random().NextBytes(buffer);
                int size = 1 + Random().Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                string s = Encoding.UTF8.GetString(buffer, 0, size);
                for (int j = 0; j < s.Length; j++)
                {
                    array.Append(s[j]);
                }
                builder.Append(s);
            }

            AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch.", builder, array);

            DirectoryInfo tempDir = CreateTempDir("growingchararray");
            FileInfo f = new FileInfo(Path.Combine(tempDir.FullName, "GrowingCharArrayTest.tmp"));
            using (Stream @out = new FileStream(f.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                array.Flush(@out);
                @out.Flush();
            }

            using (Stream @in = new FileStream(f.FullName, FileMode.Open, FileAccess.Read))
            {
                array = CharBlockArray.Open(@in);
                AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch after flush/load.", builder, array);
            }
            f.Delete();
        }

        private static void AssertEqualsInternal(string msg, StringBuilder expected, CharBlockArray actual)
        {
            Assert.AreEqual(expected.Length, actual.Length, msg);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual.CharAt(i), msg);
            }
        }
    }
}