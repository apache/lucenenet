// -----------------------------------------------------------------------
// <copyright file="WeakDictionaryOfTKeyTValueTest.cs" company="Apache">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------


namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else 
    using Gallio.Framework;
    using MbUnit.Framework;
#endif

    using Lucene.Net.Util;
    using Lucene.Net.Internal;

    using Categories = Lucene.Net.TestCategories;
    
    [TestFixture]
    [Category(Categories.Unit)]
    [Parallelizable]
    public class WeakDictionaryOfTKeyTValueTest
    {
        private readonly TimeSpan defaultTimeSpan = new TimeSpan(0, 0, 15);

        [Test]
        public void Contructor_WithCapacity()
        {
            WeakDictionary<string, ReferenceType> weakDictionary = null;


            Assert.DoesNotThrow(() =>
            {
                weakDictionary = new WeakDictionary<string, ReferenceType>(2);
            });

            Assert.AreEqual(this.defaultTimeSpan, weakDictionary.PeriodicRemoval);
            Assert.AreEqual(2, weakDictionary.InitialCapacity);
            Assert.AreEqual(0, weakDictionary.Count);
            Assert.IsNotNull(weakDictionary.Comparer);
            Assert.IsFalse(weakDictionary.IsReadOnly);
        }

        [Test]
        public void Constructor_WithComparer()
        {
            WeakDictionary<string, ReferenceType> weakDictionary = null;
            IEqualityComparer<string> comparer = new String2EqualityComparer();

            Assert.DoesNotThrow(() =>
            {
                weakDictionary = new WeakDictionary<string, ReferenceType>(comparer);
            });

            Assert.AreEqual(this.defaultTimeSpan, weakDictionary.PeriodicRemoval);
            Assert.AreEqual(0, weakDictionary.InitialCapacity);
            Assert.AreEqual(0, weakDictionary.Count);
            Assert.IsNotNull(weakDictionary.Comparer);
            Assert.AreEqual(comparer, weakDictionary.Comparer);
        }

        [Test]
        public void Constructor_WithComparerAndCapacity()
        {

            WeakDictionary<string, ReferenceType> weakDictionary = null;
            IEqualityComparer<string> comparer = new String2EqualityComparer();
            int capacity = 10;

            Assert.DoesNotThrow(() =>
            {
                weakDictionary = new WeakDictionary<string, ReferenceType>(capacity, comparer);
            });

            Assert.AreEqual(this.defaultTimeSpan, weakDictionary.PeriodicRemoval);
            Assert.AreEqual(capacity, weakDictionary.InitialCapacity);
            Assert.AreEqual(0, weakDictionary.Count);
            Assert.IsNotNull(weakDictionary.Comparer);
            Assert.AreEqual(comparer, weakDictionary.Comparer);
        }

    
        


        [Test]
        public void Add()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>();

            foreach (var pair in internalDictionary)
                weakDictionary.Add(pair.Key, pair.Value);

            Assert.AreEqual(5, weakDictionary.Count);

            var value4 = weakDictionary["four"];

            Assert.AreEqual(internalDictionary["four"], value4);

            var entry6 = new ReferenceType() { Name = "six"};

            weakDictionary.Add("six", entry6);

            var value6 = weakDictionary["six"];

            Assert.AreEqual(6, weakDictionary.Count);
            Assert.AreEqual(value6, entry6);
        }

        [Test]
        public void TryGetValue()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);
            ReferenceType value = null;

            bool success = weakDictionary.TryGetValue("one", out value);

            Assert.IsTrue(success, "TryGetValue should have found the key 'one'");
            Assert.AreEqual("one", value.Name);

            value = null;

            success = weakDictionary.TryGetValue("four", out value);

            Assert.AreEqual("four", value.Name);
        }


        [Test]
        public void Clear()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);

            Assert.AreEqual(5, weakDictionary.Count);

            weakDictionary.Clear();

            Assert.AreEqual(0, weakDictionary.Count);
        }

        [Test]
        public void Contains()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);

            bool success = weakDictionary.Contains(
                                            new KeyValuePair<string, ReferenceType>("one",
                                                internalDictionary["one"]));

            Assert.IsTrue(success, "Dictionary should have contained the key pair value.");
        }

        [Test]
        public void ContainsKey()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);

            bool success = weakDictionary.ContainsKey("one");

            Assert.IsTrue(success, "The weak dictionary should have contained key 'one'");
        }

        [Test]
        public void CopyTo()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);

             KeyValuePair<string, ReferenceType>[]  emptyArray = new KeyValuePair<string, ReferenceType>[weakDictionary.Count];

             weakDictionary.CopyTo(emptyArray, 0);
            


             Assert.AreEqual(5, weakDictionary.Count);
             Assert.AreEqual(weakDictionary.Count, emptyArray.Length);
             Assert.AreEqual(weakDictionary.First(), emptyArray.First());
        }

        

        [Test]
        public void Remove()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);
           
            Assert.AreEqual(5, weakDictionary.Count);

            bool success = weakDictionary.Remove("one");

            Assert.IsTrue(success);
            Assert.AreEqual(4, weakDictionary.Count);
        }


        [Test]
        public void RemoveCollectedEntries()
        {
            var internalDictionary = CreateSmallDictionary();
            var weakDictionary = new WeakDictionary<string, ReferenceType>(internalDictionary);

            // Assert State
            Assert.AreEqual(5, weakDictionary.Count);
            Assert.IsTrue(weakDictionary.ContainsKey("one"));

            // Assert GC does not collect dictionary reference 
            // while there is an existing reference.
            GC.Collect();

            weakDictionary.RemoveCollectedEntries();

           
            Assert.AreEqual(5, weakDictionary.Count);
            Assert.IsTrue(weakDictionary.ContainsKey("one"));

            // Assert GC does collect dictionary reference 
            // when the references are removed.
            internalDictionary.Clear();
            internalDictionary = null;


            GC.Collect();

            ReferenceType type =null;
            Assert.IsFalse(weakDictionary.TryGetValue("one", out type));
            Assert.IsTrue(weakDictionary.ContainsKey("one"));

            weakDictionary.RemoveCollectedEntries();

            Assert.IsFalse(weakDictionary.ContainsKey("one"));
            Assert.AreEqual(0, weakDictionary.Count);
        }


        #region Helpers

        internal static Dictionary<string, ReferenceType> CreateSmallDictionary()
        {
             var internalDictionary = new Dictionary<string, ReferenceType>() {
                {"one", new ReferenceType() { Name = "one" }},
                {"two", new ReferenceType() { Name = "two" }},
                {"three", new ReferenceType() { Name = "three" }},
                {"four", new ReferenceType() { Name = "four" }},
                {"five", new ReferenceType() { Name = "one" }},
            };

            return internalDictionary;
        }

        

       

        internal class String2EqualityComparer :EqualityComparer<string>
        {

            public override bool Equals(string x, string y)
            {
                return x.Equals(y);
            }

            public override int GetHashCode(string obj)
            {
                return obj.GetHashCode();
            }
        }

        #endregion
    }
}
