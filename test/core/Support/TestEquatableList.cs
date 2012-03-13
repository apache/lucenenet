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

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestEquatableList
    {
        /// <summary>
        /// This test shows that System.Collections.Generic.List is not suitable for determining uniqueness 
        /// when used in a HashSet. This is a difference between the Java and .NET BCL Lists. Since the equatable 
        /// behaviour of java.util.List.hashCode() in Java is relied upon for the the Java Lucene implementation, 
        /// a correct port must use an equatable list with the same behaviour.
        /// 
        /// We include this unit test here, to prove the problem with the .NET List type. If this test fails, we 
        /// can remove the SupportClass.EquatableList class, and replace it with System.Collection.Generic.List. 
        /// </summary>
        [Test]
        public void System_Collections_Generic_List_Not_Suitable_For_Determining_Uniqueness_In_HashSet()
        {
            // reference equality 
            var foo = new Object();
            var bar = new Object();

            var list1 = new List<Object> {foo, bar};
            var list2 = new List<Object> {foo, bar};

            var hashSet = new HashSet<List<Object>>();

            Assert.IsTrue(hashSet.Add(list1));

            // note: compare this assertion to the assertion in Suitable_For_Determining_Uniqueness_In_HashSet
            Assert.IsTrue(hashSet.Add(list2),
                          "BCL List changed equality behaviour and is now suitable for use in HashSet! Yay!");
        }

        /// <summary>
        /// This test shows that System.Collections.Generic.List is not suitable for determining uniqueness 
        /// when used in a Hashtable. This is a difference between the Java and .NET BCL Lists. Since the equatable 
        /// behaviour of java.util.List.hashCode() in Java is relied upon for the the Java Lucene implementation, 
        /// a correct port must use an equatable list with the same behaviour.
        /// 
        /// We include this unit test here, to prove the problem with the .NET List type. If this test fails, we 
        /// can remove the SupportClass.EquatableList class, and replace it with System.Collection.Generic.List. 
        /// </summary>
        [Test]
        public void System_Collections_Generic_List_Not_Suitable_For_Determining_Uniqueness_In_Hashtable()
        {
            // reference equality 
            var foo = new Object();
            var bar = new Object();

            var list1 = new List<Object> {foo, bar};
            var list2 = new List<Object> {foo, bar};

            var hashTable = new Hashtable();

            Assert.IsFalse(hashTable.ContainsKey(list1));
            hashTable.Add(list1, list1);

            // note: compare this assertion to the assertion in Suitable_For_Determining_Uniqueness_In_Hashtable
            Assert.IsFalse(
                hashTable.ContainsKey(list2),
                "BCL List changed behaviour and is now suitable for use as a replacement for Java's List! Yay!");
        }

        /// <summary>
        /// There is a interesting problem with .NET's String.GetHashCode() for certain strings. 
        /// This unit test displays the problem, and in the event that this is changed in the 
        /// .NET runtime, the test will fail. 
        /// 
        /// This is one of the reasons that the EquatableList implementation does not use GetHashCode() 
        /// (which is a divergence from the List.equals implementation in Java). EquatableList should have 
        /// the same overall results as Java's List however.
        /// 
        /// For an explanation of this issue see: 
        /// http://blogs.msdn.com/b/ericlippert/archive/2011/07/12/what-curious-property-does-this-string-have.aspx
        /// For a description of the GetHashCode implementation see: 
        /// http://www.dotnetperls.com/gethashcode
        /// For documentation on List.getHashCode(), see: 
        /// http://download.oracle.com/javase/6/docs/api/java/util/List.html#hashCode()
        /// And in the general case, see:
        /// http://download.oracle.com/javase/6/docs/api/java/lang/Object.html#hashCode()
        /// </summary>
        [Test]
        public void System_String_GetHashCode_Exhibits_Inconsistent_Inequality_For_Some_Values()
        {
            var val1 = "\uA0A2\uA0A2";
            var val2 = string.Empty;

            Assert.IsFalse(val1.Equals(val2));

            var hash1 = val1.GetHashCode();
            var hash2 = val2.GetHashCode();

            // note: this is counter-intuative, but technically allowed by the contract for GetHashCode()
            // this only works in 32 bit processes. 

            // if 32 bit process
            if (IntPtr.Size == 4)
            { 
                // TODO: determine if there is an similar issue when in a 64 bit process.
                Assert.IsTrue(
                    hash1.Equals(hash2),
                    "BCL string.GetHashCode() no longer exhibits inconsistent inequality for certain strings."
                    );
            }
        }

        [Test]
        public void Suitable_For_Determining_Uniqueness_In_HashSet()
        {
            var foo = new Object();
            var bar = new Object();

            var list1 = new EquatableList<Object> {foo, bar};
            var list2 = new EquatableList<Object> {foo, bar};

            Assert.AreEqual(list1, list2);

            var hashSet = new HashSet<List<Object>>();

            Assert.IsTrue(hashSet.Add(list1));
            Assert.IsFalse(hashSet.Add(list2));
        }

        [Test]
        public void Suitable_For_Determining_Uniqueness_In_Hashtable()
        {
            var foo = new Object();
            var bar = new Object();

            var list1 = new EquatableList<Object> { foo, bar };
            var list2 = new EquatableList<Object> { foo, bar };

            var hashTable = new Hashtable();

            Assert.IsFalse(hashTable.ContainsKey(list1));
            hashTable.Add(list1, list1);

            Assert.IsTrue(hashTable.ContainsKey(list2));
        }
    }
}
