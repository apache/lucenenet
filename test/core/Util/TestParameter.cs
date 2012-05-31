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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestParameter
    {
        internal class MockParameter : Parameter
        {
            public MockParameter(string name)
                : base(name)
            { }
        }

        [Test]
        public void TestEquals()
        {
            var first = new MockParameter("FIRST");
            var other = new MockParameter("OTHER");

            // Make sure it's equal against itself
            Assert.AreEqual(first, first);
            // Not equal if it has a different name
            Assert.AreNotEqual(first, other);
            
            // Test == operator
            Assert.IsTrue(first == first);
            Assert.IsFalse(first == other);

            // Test != operator
            Assert.IsFalse(first != first);
            Assert.IsTrue(first != other);
        }

        
        [Test]
        public void TestLuceneNet472()
        {
            var thing = new MockParameter("THING");
            var otherThing = new MockParameter("OTHERTHING");

            // LUCENENET-472 - NRE on ==/!= parameter
            Assert.IsTrue(thing != null);
            Assert.IsFalse(thing == null);
            Assert.IsTrue(otherThing != null);
        }
    }
}
