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

namespace Lucene.Net.Util
{
    public class TestBytesRef : LuceneTestCase
    {
        [Test]
        public void TestEmpty()
        {
            var b = new BytesRef();
            Equal(BytesRef.EMPTY_BYTES, b.Bytes);
            Equal(0, b.Offset);
            Equal(0, b.Length);
        }

        [Test]
        public void TestFromBytes()
        {

            byte[] bytes = new byte[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            var bytesRef = new BytesRef(bytes);
            Equal(bytes, bytesRef.Bytes);
            Equal(0, bytesRef.Offset);
            Equal(bytes.Length, bytesRef.Length);

            var bytesRef2 = new BytesRef(bytes, 1, 3);
            Equal("bcd", bytesRef2.Utf8ToString());

            Ok(!bytesRef.Equals(bytesRef2), "bytesRef should not equal bytesRef2");
        }


        [Test]
        public void TestFromChars()
        {
            100.Times((i) =>
            {
                var utf8Str1 = Random.ToUnicodeString();
                var utf8Str2 = new BytesRef(utf8Str1).Utf8ToString();
                Equal(utf8Str1, utf8Str2);
            });
            const string value = "\uFFFF";

            Equal(value, new BytesRef(value).Utf8ToString());
        }


    }
}
