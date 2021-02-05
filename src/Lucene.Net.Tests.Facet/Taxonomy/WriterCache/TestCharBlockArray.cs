// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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

            // This is essentially the equivalent of
            // CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder()
            //     .onUnmappableCharacter(CodingErrorAction.REPLACE)
            //     .onMalformedInput(CodingErrorAction.REPLACE);
            // 
            // Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage, 
            //     new EncoderReplacementFallback("?"), 
            //     new DecoderReplacementFallback("?"));

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage,
                    new EncoderReplacementFallback("?"),
                    new DecoderReplacementFallback("?"));
                string s = decoder.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage,
                    new EncoderReplacementFallback("?"),
                    new DecoderReplacementFallback("?"));
                string s = decoder.GetString(buffer, 0, size);
                array.Append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random.NextBytes(buffer);
                int size = 1 + Random.Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                Encoding decoder = Encoding.GetEncoding(Encoding.UTF8.CodePage,
                    new EncoderReplacementFallback("?"),
                    new DecoderReplacementFallback("?"));
                string s = decoder.GetString(buffer, 0, size);
                for (int j = 0; j < s.Length; j++)
                {
                    array.Append(s[j]);
                }
                builder.Append(s);
            }

            AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch.", builder, array);

            DirectoryInfo tempDir = CreateTempDir("growingchararray");
            FileInfo f = new FileInfo(Path.Combine(tempDir.FullName, "GrowingCharArrayTest.tmp"));
            using (var @out = new FileStream(f.FullName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                array.Flush(@out);
                @out.Flush();
            }

            using (var @in = new FileStream(f.FullName, FileMode.Open, FileAccess.Read))
            {
                array = CharBlockArray.Open(@in);
                AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch after flush/load.", builder, array);
            }
            f.Delete();
        }

        private static void AssertEqualsInternal(string msg, StringBuilder expected, CharBlockArray actual)
        {
            // LUCENENET specific - Indexing a string is much faster than StringBuilder (#295)
            var expected2 = expected.ToString();
            var expected2Len = expected2.Length;
            Assert.AreEqual(expected2Len, actual.Length, msg);
            for (int i = 0; i < expected2Len; i++)
            {
                Assert.AreEqual(expected2[i], actual[i], msg);
            }
        }
    }
}