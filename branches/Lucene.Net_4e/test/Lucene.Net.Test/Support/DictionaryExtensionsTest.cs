// -----------------------------------------------------------------------
// <copyright file="DictionaryExtensionsTest.cs" company="Apache">
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
 
    using Categories = Lucene.Net.TestCategories;

    [TestFixture]
    [Category(Categories.Unit)]
    [Parallelizable]
    public class DictionaryExtensionsTest
    {

        [Test]
        public void GetDefaultValue()
        {
            var dictionary = new Dictionary<string, string>();
            var dictionaryWithInt = new Dictionary<string, int>();

            var defaultedValue1 = dictionary.GetDefaultedValue("key does not exist");

            Assert.IsFalse(dictionary.ContainsKey("key does not exist"));
            Assert.IsNull(defaultedValue1, "The value should return null when the key does not exist");

            dictionary["key"] = "value";

            Assert.IsNotNull(dictionary["key"]);
            Assert.AreEqual("value", dictionary["key"]);
          
            Assert.IsNotNull(dictionaryWithInt.GetDefaultedValue("key does not exist"));
            Assert.AreEqual(0, dictionaryWithInt.GetDefaultedValue("key does not exist"));
        }
    }
}
