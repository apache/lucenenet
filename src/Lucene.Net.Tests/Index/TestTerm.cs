using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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
    public class TestTerm : LuceneTestCase
    {
        [Test]
        public virtual void TestEquals()
        {
            Term @base = new Term("same", "same");
            Term same = new Term("same", "same");
            Term differentField = new Term("different", "same");
            Term differentText = new Term("same", "different");
            const string differentType = "AString";
            Assert.AreEqual(@base, @base);
            Assert.AreEqual(@base, same);
            Assert.IsFalse(@base.Equals(differentField));
            Assert.IsFalse(@base.Equals(differentText));
            Assert.IsFalse(@base.Equals(differentType));
        }

        [Test, LuceneNetSpecific]
        public void TestToString_ValidUtf8Data()
        {
            // Arrange
            var validUtf8 = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
            var bytesRef = new BytesRef(validUtf8, 0, validUtf8.Length);

            // Act
            string result = Term.ToString(bytesRef);

            // Assert
            Assert.AreEqual("Hello", result);
        }

        [Test, LuceneNetSpecific]
        public void TestToString_InvalidUtf8Data()
        {
            // Arrange
            var invalidUtf8 = new byte[] { 0xC3, 0x28 }; // Invalid UTF-8 sequence
            var bytesRef = new BytesRef(invalidUtf8, 0, invalidUtf8.Length);

            // Act
            string result = Term.ToString(bytesRef);

            // Assert
            Assert.AreEqual("[c3 28]", result); // Should match BytesRef.ToString()
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Utf8WithBom()
        {
            // Arrange
            var utf8WithBom = new byte[] { 0xEF, 0xBB, 0xBF, 0x48, 0x69 }; // BOM + "Hi"
            var bytesRef = new BytesRef(utf8WithBom, 0, utf8WithBom.Length);

            // Act
            string result = Term.ToString(bytesRef);

            // Assert
            Assert.AreEqual("\uFEFFHi", result); // BOM is preserved in the string
        }

        [Test, LuceneNetSpecific]
        public void TestToString_Utf8WithoutBom()
        {
            // Arrange
            var utf8WithoutBom = new byte[] { 0x48, 0x69 }; // "Hi"
            var bytesRef = new BytesRef(utf8WithoutBom, 0, utf8WithoutBom.Length);

            // Act
            string result = Term.ToString(bytesRef);

            // Assert
            Assert.AreEqual("Hi", result);
        }
    }
}
