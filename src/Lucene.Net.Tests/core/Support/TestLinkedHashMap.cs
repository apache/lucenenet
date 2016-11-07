using Lucene.Net.Attributes;
using NUnit.Framework;
using System.Collections.Generic;

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
    public class TestLinkedHashMap : TestHashMap
    {
        protected override HashMap<TKey, TValue> GetNewHashMap<TKey, TValue>()
        {
            return new LinkedHashMap<TKey, TValue>();
        }

        private IDictionary<FakeKey<int>, string> GetDefaultHashMap2()
        {
            var dict = GetNewHashMap<FakeKey<int>, string>();

            dict[new FakeKey<int>(2)] = "MyString";
            dict[new FakeKey<int>(0)] = "OtherString";
            dict[new FakeKey<int>(5)] = "NumberFive";
            dict[new FakeKey<int>(int.MaxValue)] = "Maximum";
            dict[null] = "NullValue";
            dict[new FakeKey<int>(int.MinValue)] = "Minimum";

            return dict;
        }

        internal class FakeKey<T>
        {
            private readonly T key;

            public FakeKey(T key)
            {
                this.key = key;
            }

            public override bool Equals(object obj)
            {
                // NOTE: This takes into consideration that value
                // types cannot be null.
                if (key == null && obj == null)
                {
                    return true;
                }

                if (!(obj is FakeKey<T>))
                {
                    return false;
                }

                return key.Equals(((FakeKey<T>)obj).key);
            }

            public override int GetHashCode()
            {
                // NOTE: This takes into consideration that value
                // types cannot be null.
                if (key == null)
                {
                    return 0; // Emulates Objects.hashcode(object) in Java
                }

                return key.GetHashCode();
            }

            public override string ToString()
            {
                // NOTE: This takes into consideration that value
                // types cannot be null.
                if (key == null)
                {
                    return "null"; // Emulates String.valueOf(object) in Java
                }

                return key.ToString();
            }
        }

        [Test, LuceneNetSpecific]
        public void TestInsertionOrderNullFirst()
        {
            var dict = GetNewHashMap<FakeKey<int>, string>();

            dict[null] = "NullValue";
            dict[new FakeKey<int>(2)] = "MyString";
            dict[new FakeKey<int>(0)] = "OtherString";
            dict[new FakeKey<int>(5)] = "NumberFive";
            dict[new FakeKey<int>(int.MaxValue)] = "Maximum";

            var expectedOrder = new List<KeyValuePair<FakeKey<int>, string>>
            {
                new KeyValuePair<FakeKey<int>, string>(null, "NullValue"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(2), "MyString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(0), "OtherString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(5), "NumberFive"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MaxValue), "Maximum"),
            };

            AssertEnumerationOrder(expectedOrder, dict);
        }

        [Test, LuceneNetSpecific]
        public void TestInsertionOrderNullInMiddle()
        {
            var dict = GetNewHashMap<FakeKey<int>, string>();

            dict[new FakeKey<int>(2)] = "MyString";
            dict[new FakeKey<int>(0)] = "OtherString";
            dict[null] = "NullValue";
            dict[new FakeKey<int>(5)] = "NumberFive";
            dict[new FakeKey<int>(int.MaxValue)] = "Maximum";

            var expectedOrder = new List<KeyValuePair<FakeKey<int>, string>>
            {
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(2), "MyString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(0), "OtherString"),
                new KeyValuePair<FakeKey<int>, string>(null, "NullValue"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(5), "NumberFive"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MaxValue), "Maximum"),
            };

            AssertEnumerationOrder(expectedOrder, dict);
        }

        [Test, LuceneNetSpecific]
        public void TestInsertionOrderNullLast()
        {
            var dict = GetNewHashMap<FakeKey<int>, string>();

            dict[new FakeKey<int>(2)] = "MyString";
            dict[new FakeKey<int>(0)] = "OtherString";
            dict[new FakeKey<int>(5)] = "NumberFive";
            dict[new FakeKey<int>(int.MaxValue)] = "Maximum";
            dict[null] = "NullValue";

            var expectedOrder = new List<KeyValuePair<FakeKey<int>, string>>
            {
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(2), "MyString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(0), "OtherString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(5), "NumberFive"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MaxValue), "Maximum"),
                new KeyValuePair<FakeKey<int>, string>(null, "NullValue"),
            };

            AssertEnumerationOrder(expectedOrder, dict);
        }

        [Test, LuceneNetSpecific]
        public void TestInsertionOrderNullAfterRemovingElements()
        {
            var dict = GetDefaultHashMap2();

            // Remove elements before the null value to make sure
            // we track the proper position of the null
            dict.Remove(new FakeKey<int>(5));
            dict.Remove(new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(0), "OtherString"));

            var expectedOrder = new List<KeyValuePair<FakeKey<int>, string>>
            {
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(2), "MyString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MaxValue), "Maximum"),
                new KeyValuePair<FakeKey<int>, string>(null, "NullValue"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MinValue), "Minimum"),
            };

            AssertEnumerationOrder(expectedOrder, dict);
        }


        [Test, LuceneNetSpecific]
        public void TestInsertionOrderNullAfterRefillingBuckets()
        {
            var dict = GetDefaultHashMap2();

            // Remove elements before the null value to make sure
            // we track the proper position of the null
            dict.Remove(new FakeKey<int>(5));
            dict.Remove(new FakeKey<int>(0));

            // This has been verified that it puts the reused key before "new FakeKey<int>(int.MaxValue)"
            // in a standard Dictionary (putting it out of insertion order)
            dict.Add(new FakeKey<int>(5), "Testing");

            var expectedOrder = new List<KeyValuePair<FakeKey<int>, string>>
            {
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(2), "MyString"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MaxValue), "Maximum"),
                new KeyValuePair<FakeKey<int>, string>(null, "NullValue"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(int.MinValue), "Minimum"),
                new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(5), "Testing"),
            };

            AssertEnumerationOrder(expectedOrder, dict);
        }

        private void AssertEnumerationOrder<TKey, TValue>(IList<KeyValuePair<TKey, TValue>> expectedOrder, IDictionary<TKey, TValue> actualOrder)
        {
            // check element order
            int expectedCount = expectedOrder.Count;
            var elementEnumerator = actualOrder.GetEnumerator();
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.IsTrue(elementEnumerator.MoveNext());
                Assert.AreEqual(expectedOrder[i].Key, elementEnumerator.Current.Key);
                Assert.AreEqual(expectedOrder[i].Value, elementEnumerator.Current.Value);
            }

            Assert.IsFalse(elementEnumerator.MoveNext());
            Assert.IsFalse(elementEnumerator.MoveNext());

            // check key order
            var keyEnumerator = actualOrder.Keys.GetEnumerator();
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.IsTrue(keyEnumerator.MoveNext());
                Assert.AreEqual(expectedOrder[i].Key, keyEnumerator.Current);
            }

            Assert.IsFalse(keyEnumerator.MoveNext());
            Assert.IsFalse(keyEnumerator.MoveNext());

            // check value order
            var valueEnumerator = actualOrder.Values.GetEnumerator();
            for (int i = 0; i < expectedCount; i++)
            {
                Assert.IsTrue(valueEnumerator.MoveNext());
                Assert.AreEqual(expectedOrder[i].Value, valueEnumerator.Current);
            }

            Assert.IsFalse(valueEnumerator.MoveNext());
            Assert.IsFalse(valueEnumerator.MoveNext());
        }


        [Test, LuceneNetSpecific]
        public void TestCopyTo()
        {
            var dict = GetDefaultHashMap2();

            KeyValuePair<FakeKey<int>, string>[] elements = new KeyValuePair<FakeKey<int>, string>[dict.Count];

            dict.CopyTo(elements, 0);

            // element insertion order
            var enumerator = elements.GetEnumerator();

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(2), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("MyString", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(0), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("OtherString", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(5), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("NumberFive", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(int.MaxValue), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("Maximum", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(null, ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("NullValue", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(int.MinValue), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("Minimum", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test, LuceneNetSpecific]
        public void TestCopyToWithOffset()
        {
            int offset = 5;
            var dict = GetDefaultHashMap2();

            KeyValuePair<FakeKey<int>, string>[] elements = new KeyValuePair<FakeKey<int>, string>[dict.Count + offset];

            dict.CopyTo(elements, offset);

            // element insertion order
            var enumerator = elements.GetEnumerator();

            for (int i = 0; i < offset; i++)
            {
                Assert.IsTrue(enumerator.MoveNext());
                Assert.AreEqual(null, ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
                Assert.AreEqual(null, ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);
            }

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(2), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("MyString", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(0), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("OtherString", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(5), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("NumberFive", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(int.MaxValue), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("Maximum", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(null, ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("NullValue", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(new FakeKey<int>(int.MinValue), ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Key);
            Assert.AreEqual("Minimum", ((KeyValuePair<FakeKey<int>, string>)enumerator.Current).Value);

            Assert.IsFalse(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());
        }

        [Test, LuceneNetSpecific]
        public void TestContainsKeyValuePair()
        {
            var dict = GetDefaultHashMap2();

            var realTarget = new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(5), "NumberFive");
            Assert.IsTrue(dict.Contains(realTarget));

            var nullTarget = new KeyValuePair<FakeKey<int>, string>(null, "NullValue");
            Assert.IsTrue(dict.Contains(nullTarget));

            var invalidTarget = new KeyValuePair<FakeKey<int>, string>(new FakeKey<int>(543), "NullValue");
            Assert.IsFalse(dict.Contains(invalidTarget));

            dict.Remove(nullTarget);
            Assert.IsFalse(dict.Contains(nullTarget));
        }


        #region TestHashMap
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test, LuceneNetSpecific]
        public override void TestKeyEnumeration()
        {
            base.TestKeyEnumeration();
        }

        [Test, LuceneNetSpecific]
        public override void TestValueEnumeration()
        {
            base.TestValueEnumeration();
        }

        [Test, LuceneNetSpecific]
        public override void TestKeyValuePairEnumeration()
        {
            base.TestKeyValuePairEnumeration();
        }

        [Test, LuceneNetSpecific]
        public override void TestContainsNullKey()
        {
            base.TestContainsNullKey();
        }

        [Test, LuceneNetSpecific]
        public override void TestContainsKey()
        {
            base.TestContainsKey();
        }

        [Test, LuceneNetSpecific]
        public override void TestAdd_NoNullKeys_NullValues()
        {
            base.TestAdd_NoNullKeys_NullValues();
        }

        [Test, LuceneNetSpecific]
        public override void TestAdd_WithNullKeys_NoNullValues()
        {
            base.TestAdd_WithNullKeys_NoNullValues();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetWithNonExistantKey_EmptyCollection()
        {
            base.TestGetWithNonExistantKey_EmptyCollection();
        }

        [Test, LuceneNetSpecific]
        public override void TestGetWithNonExistantKey()
        {
            base.TestGetWithNonExistantKey();
        }

        [Test, LuceneNetSpecific]
        public override void TestAddsUpdate_NotThrowException()
        {
            base.TestAddsUpdate_NotThrowException();
        }

        [Test, LuceneNetSpecific]
        public override void TestIndexersUpdate_NotThrowException()
        {
            base.TestIndexersUpdate_NotThrowException();
        }

        [Test, LuceneNetSpecific]
        public override void TestWithValueType()
        {
            base.TestWithValueType();
        }

        #endregion
    }
}
