using System.Text;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
{


    using TestUtil = Lucene.Net.Util.TestUtil;

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

        /* not finished yet because of missing charset decoder */

        /*
        public virtual void testArray()
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

                CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onUnmappableCharacter(CodingErrorAction.REPLACE).onMalformedInput(CodingErrorAction.REPLACE);
                string s = decoder.Decode(ByteBuffer.Wrap(buffer, 0, size)).ToString();
                array.append(s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random().NextBytes(buffer);
                int size = 1 + Random().Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onUnmappableCharacter(CodingErrorAction.REPLACE).onMalformedInput(CodingErrorAction.REPLACE);
                string s = decoder.decode(ByteBuffer.Wrap(buffer, 0, size)).ToString();
                array.append((CharSequence)s);
                builder.Append(s);
            }

            for (int i = 0; i < n; i++)
            {
                Random().NextBytes(buffer);
                int size = 1 + Random().Next(50);
                // This test is turning random bytes into a string,
                // this is asking for trouble.
                CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onUnmappableCharacter(CodingErrorAction.REPLACE).onMalformedInput(CodingErrorAction.REPLACE);
                string s = decoder.decode(ByteBuffer.Wrap(buffer, 0, size)).ToString();
                for (int j = 0; j < s.Length; j++)
                {
                    array.append(s[j]);
                }
                builder.Append(s);
            }

            AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch.", builder, array);

            File tempDir = CreateTempDir("growingchararray");
            File f = new File(tempDir, "GrowingCharArrayTest.tmp");
            BufferedOutputStream @out = new BufferedOutputStream(new FileOutputStream(f));
            array.flush(@out);
            @out.flush();
            @out.Close();

            BufferedInputStream @in = new BufferedInputStream(new FileInputStream(f));
            array = CharBlockArray.open(@in);
            AssertEqualsInternal("GrowingCharArray<->StringBuilder mismatch after flush/load.", builder, array);
            @in.Close();
            f.delete();
        }

        private static void AssertEqualsInternal(string msg, StringBuilder expected, CharBlockArray actual)
        {
            Assert.AreEqual(expected.Length, actual.Length, msg);
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], actual.CharAt(i), msg);
            }
        }
        */
    }

}