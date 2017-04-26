/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Tests.Support
{
    public class TestCollections : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestEqualsTypeMismatch()
        {
            var list = new List<int> { 1, 2, 3, 4, 5 };
            var set = new HashSet<int> { 1, 2, 3, 4, 5 };
            var dictionary = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 }, { 5, 0 } };
            var array = new int[] { 1, 2, 3, 4, 5 };

            Assert.IsFalse(Collections.Equals(list, set));
            Assert.IsFalse(Collections.Equals(list, dictionary));
            Assert.IsTrue(Collections.Equals(list, array)); // Types are compatible - array implements IList<T>

            Assert.IsFalse(Collections.Equals(set, dictionary));
            Assert.IsFalse(Collections.Equals(set, array));
        }

        [Test, LuceneNetSpecific]
        public void TestEqualityDictionary()
        {
            var control = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
            {
                { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
                { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
                { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
                { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
            };
            var equal = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
            {
                { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
                { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
                { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
                { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
            };
            var equalDifferentType = new HashMap<string, IDictionary<HashMap<long, double>, string>>
            {
                { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
                { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
                { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
                { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
            };
            var equalDifferentOrder = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
            {
                { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
                { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
                { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
                { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
            };

            var level1EqualLevel2EqualLevel3Unequal = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
            {
                { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88.1 } }, "qwerty" } } },
                { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
                { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
                { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
            };

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
            Assert.IsTrue(Collections.Equals(control, control));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
            Assert.IsTrue(Collections.Equals(control, equal));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
            Assert.IsTrue(Collections.Equals(control, equalDifferentType));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
            Assert.IsTrue(Collections.Equals(control, equalDifferentOrder));

            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1EqualLevel2EqualLevel3Unequal));
            Assert.IsFalse(Collections.Equals(control, level1EqualLevel2EqualLevel3Unequal));
        }

        [Test, LuceneNetSpecific]
        public void TestEqualityList()
        {
            var control = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equal = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equalDifferentType = new IDictionary<string, string>[]
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equalDifferentOrder = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
            };
            var level1EqualLevel2Unequal = new List<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine99" } },
            };

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
            Assert.IsTrue(Collections.Equals(control, control));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
            Assert.IsTrue(Collections.Equals(control, equal));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
            Assert.IsTrue(Collections.Equals(control, equalDifferentType));

            // Lists and arrays are order-sensitive
            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
            Assert.IsFalse(Collections.Equals(control, equalDifferentOrder));

            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1EqualLevel2Unequal));
            Assert.IsFalse(Collections.Equals(control, level1EqualLevel2Unequal));
        }

        [Test, LuceneNetSpecific]
        public void TestEqualityListSimple()
        {
            var control = new List<IList<string>>
            {
                new List<string> { "one",  "two",  "three" },
                new List<string> { "four",  "five", "six" } ,
                new List<string> { "seven", "eight", "nine" },
            };
            var equal = new List<IList<string>>
            {
                new List<string> { "one",  "two",  "three" },
                new List<string> { "four",  "five", "six" } ,
                new List<string> { "seven", "eight", "nine" },
            };
            var equalDifferentType = new IList<string>[]
            {
                new List<string> { "one",  "two",  "three" },
                new List<string> { "four",  "five", "six" } ,
                new List<string> { "seven", "eight", "nine" },
            };
            var equalDifferentOrder = new List<IList<string>>
            {
                new List<string> { "four",  "five", "six" } ,
                new List<string> { "seven", "eight", "nine" },
                new List<string> { "one",  "two",  "three" },
            };
            var level1EqualLevel2Unequal = new List<IList<string>>
            {
                new List<string> { "one",  "two",  "three" },
                new List<string> { "four",  "five", "six" } ,
                new List<string> { "seven", "eight", "nine-nine" },
            };

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
            Assert.IsTrue(Collections.Equals(control, control));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
            Assert.IsTrue(Collections.Equals(control, equal));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
            Assert.IsTrue(Collections.Equals(control, equalDifferentType));

            // Lists and arrays are order - sensitive
            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
            Assert.IsFalse(Collections.Equals(control, equalDifferentOrder));

            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1EqualLevel2Unequal));
            Assert.IsFalse(Collections.Equals(control, level1EqualLevel2Unequal));
        }


        private class MockHashSet<T> : HashSet<T>
        {
            public override int GetHashCode()
            {
                return Random().nextInt(); // Random garbage to ensure it is not equal
            }

            public override bool Equals(object obj)
            {
                return false;
            }
        }

        [Test, LuceneNetSpecific]
        public void TestEqualitySet()
        {
            var control = new HashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equal = new HashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equalDifferentType = new MockHashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var equalDifferentOrder = new HashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
            };
            var level1EqualLevel2Unequal = new HashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine99" } },
            };

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
            Assert.IsTrue(Collections.Equals(control, control));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
            Assert.IsTrue(Collections.Equals(control, equal));

            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
            Assert.IsTrue(Collections.Equals(control, equalDifferentType));

            // Sets are not order-sensitive
            Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
            Assert.IsTrue(Collections.Equals(control, equalDifferentOrder));

            Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1EqualLevel2Unequal));
            Assert.IsFalse(Collections.Equals(control, level1EqualLevel2Unequal));
        }

        [Test, LuceneNetSpecific]
        public void TestToString()
        {
            var set = new HashSet<IDictionary<string, string>>
            {
                new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
                new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
                new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
            };
            var setExpected = "[{1=one, 2=two, 3=three}, {4=four, 5=five, 6=six}, {7=seven, 8=eight, 9=nine}]";

            Assert.AreEqual(setExpected, Collections.ToString(set));

            var map = new Dictionary<string, IDictionary<int, double>>
            {
                { "first", new Dictionary<int, double> { { 1, 1.23 }, { 2, 2.23 }, { 3, 3.23 } } },
                { "second", new Dictionary<int, double> { { 4, 1.24 }, { 5, 2.24 }, { 6, 3.24 } } },
                { "third", new Dictionary<int, double> { { 7, 1.25 }, { 8, 2.25 }, { 9, 3.25 } } },
            };
            var mapExpectedPortuguese = "{first={1=1,23, 2=2,23, 3=3,23}, second={4=1,24, 5=2,24, 6=3,24}, third={7=1,25, 8=2,25, 9=3,25}}";
            var mapExpectedUSEnglish = "{first={1=1.23, 2=2.23, 3=3.23}, second={4=1.24, 5=2.24, 6=3.24}, third={7=1.25, 8=2.25, 9=3.25}}";

            Assert.AreEqual(mapExpectedPortuguese, Collections.ToString(map, new CultureInfo("pt")));
            Assert.AreEqual(mapExpectedUSEnglish, Collections.ToString(map, new CultureInfo("en-US")));

            var array = new List<Dictionary<string, string>>[]
            {
                new List<Dictionary<string, string>> {
                    new Dictionary<string, string> { { "foo", "bar" }, { "foobar", "barfoo" } }
                },
                new List<Dictionary<string, string>> {
                    new Dictionary<string, string> { { "orange", "yellow" }, { "red", "black" } },
                    new Dictionary<string, string> { { "rain", "snow" }, { "sleet", "sunshine" } }
                },
            };
            var arrayExpected = "[[{foo=bar, foobar=barfoo}], [{orange=yellow, red=black}, {rain=snow, sleet=sunshine}]]";

            Assert.AreEqual(arrayExpected, Collections.ToString(array));
        }



        //[Test]
        //public void TestEqualityDictionaryShallow()
        //{
        //    var control = new Dictionary<string, IDictionary<int, string>>
        //    {
        //        { "a", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        //{ "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //        //{ "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        //{ "t", new Dictionary<int, string> { { 61, "octopus" } } },
        //    };
        //    var equal = new Dictionary<string, IDictionary<int, string>>
        //    {
        //        { "a", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        //{ "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //        //{ "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        //{ "t", new Dictionary<int, string> { { 61, "octopus" } } },
        //    };
        //    var equalDifferentType = new HashMap<string, IDictionary<int, string>>
        //    {
        //        { "a", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        { "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //        { "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        { "t", new Dictionary<int, string> { { 61, "octopus" } } },
        //    };
        //    var equalDifferentOrder = new Dictionary<string, IDictionary<int, string>>
        //    {
        //        { "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        { "t", new Dictionary<int, string> { { 61, "octopus" } } },
        //        { "a", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        { "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //    };
        //    var level1EqualLevel2Unequal = new Dictionary<string, IDictionary<int, string>>
        //    {
        //        { "a", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        { "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //        { "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        { "t", new Dictionary<int, string> { { 7, "octopus" } } },
        //    };
        //    var level1UnequalLevel2Equal = new Dictionary<string, IDictionary<int, string>>
        //    {
        //        { "y", new Dictionary<int, string> { { 9, "qwerty" } } },
        //        { "z", new Dictionary<int, string> { { 23, "hexagon" } } },
        //        { "r", new Dictionary<int, string> { { 4, "parasite" } } },
        //        { "t", new Dictionary<int, string> { { 61, "octopus" } } },
        //    };

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
        //    Assert.IsTrue(Collections.Equals(control, control));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
        //    Assert.IsTrue(Collections.Equals(control, equal));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
        //    Assert.IsTrue(Collections.Equals(control, equalDifferentType));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
        //    Assert.IsTrue(Collections.Equals(control, equalDifferentOrder));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1EqualLevel2Unequal));
        //    Assert.IsTrue(Collections.Equals(control, level1EqualLevel2Unequal));

        //    Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(level1UnequalLevel2Equal));
        //    Assert.IsFalse(Collections.Equals(control, level1UnequalLevel2Equal));
        //}

        //[Test]
        //public void TestEqualityDictionaryDeep()
        //{
        //    var control = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
        //    {
        //        { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
        //        { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
        //        { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
        //        { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
        //    };
        //    var equal = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
        //    {
        //        { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
        //        { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
        //        { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
        //        { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
        //    };
        //    var equalDifferentType = new HashMap<string, IDictionary<HashMap<long, double>, string>>
        //    {
        //        { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
        //        { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
        //        { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
        //        { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
        //    };
        //    var equalDifferentOrder = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
        //    {
        //        { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
        //        { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
        //        { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88 } }, "qwerty" } } },
        //        { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
        //    };

        //    var level1EqualLevel2EqualLevel3Unequal = new Dictionary<string, IDictionary<HashMap<long, double>, string>>
        //    {
        //        { "a", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 123, 9.87 }, { 80, 88.1 } }, "qwerty" } } },
        //        { "z", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 456, 9.86 }, { 81, 88 } }, "hexagon" } } },
        //        { "r", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 789, 9.85 }, { 82, 88 } }, "parasite" } } },
        //        { "t", new Dictionary<HashMap<long, double>, string> { { new HashMap<long, double> { { 101, 9.84 }, { 83, 88 } }, "octopus" } } },
        //    };

        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(control, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equal, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentType, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentOrder, true));
        //    Assert.AreNotEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(level1EqualLevel2EqualLevel3Unequal, true));
        //}

        //[Test]
        //public void TestEqualityListShallow()
        //{
        //    var control = new List<int> { 1, 2, 3, 4, 5 };
        //    var equal = new List<int> { 1, 2, 3, 4, 5 };
        //    var equalDifferentType = new int[] { 1, 2, 3, 4, 5 };
        //    var equalDifferentOrder = new List<int> { 1, 2, 3, 5, 4 };

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
        //    // Lists and arrays are order-sensitive
        //    Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
        //}

        //[Test]
        //public void TestEqualityListDeep()
        //{
        //    var control = new List<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equal = new List<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equalDifferentType = new IDictionary<string, string>[]
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equalDifferentOrder = new List<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //    };
        //    var level1EqualLevel2Unequal = new List<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine99" } },
        //    };

        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(control, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equal, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentType, true));
        //    // Lists and arrays are order-sensitive
        //    Assert.AreNotEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentOrder, true));
        //    Assert.AreNotEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(level1EqualLevel2Unequal, true));
        //}

        //private class MockHashSet<T> : HashSet<T>
        //{
        //    public override int GetHashCode()
        //    {
        //        return Random().nextInt(); // Random garbage to ensure it is not equal
        //    }

        //    public override bool Equals(object obj)
        //    {
        //        return false;
        //    }
        //}

        //[Test]
        //public void TestEqualitySetShallow()
        //{
        //    var control = new HashSet<int> { 1, 2, 3, 4, 5 };
        //    var equal = new HashSet<int> { 1, 2, 3, 4, 5 };
        //    var equalDifferentType = new MockHashSet<int> { 1, 2, 3, 4, 5 };
        //    var equalDifferentOrder = new HashSet<int> { 1, 2, 3, 5, 4 };
        //    var missingItem = new HashSet<int> { 1, 2, 3, 5 };

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(control));
        //    Assert.IsTrue(Collections.Equals(control, control));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equal));
        //    Assert.IsTrue(Collections.Equals(control, equal));

        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentType));
        //    Assert.IsTrue(Collections.Equals(control, equalDifferentType));

        //    // sets are not order-sensitive
        //    Assert.AreEqual(Collections.GetHashCode(control), Collections.GetHashCode(equalDifferentOrder));
        //    Assert.IsTrue(Collections.Equals(control, equalDifferentOrder));

        //    Assert.AreNotEqual(Collections.GetHashCode(control), Collections.GetHashCode(missingItem));
        //    Assert.IsFalse(Collections.Equals(control, missingItem));
        //}

        //[Test]
        //public void TestEqualitySetDeep()
        //{
        //    var control = new HashSet<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equal = new HashSet<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equalDifferentType = new MockHashSet<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //    };
        //    var equalDifferentOrder = new HashSet<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine" } },
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //    };
        //    var level1EqualLevel2Unequal = new HashSet<IDictionary<string, string>>
        //    {
        //        new Dictionary<string, string> { { "1", "one" }, { "2", "two" }, { "3", "three" } },
        //        new Dictionary<string, string> { { "4", "four" }, { "5", "five" }, { "6", "six" } },
        //        new Dictionary<string, string> { { "7", "seven" }, { "8", "eight" }, { "9", "nine99" } },
        //    };

        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(control, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equal, true));
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentType, true));
        //    // Sets are not order-sensitive
        //    Assert.AreEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(equalDifferentOrder, true));
        //    Assert.AreNotEqual(Collections.GetHashCode(control, true), Collections.GetHashCode(level1EqualLevel2Unequal, true));
        //}
    }
}
