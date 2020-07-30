using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Configuration.Custom
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

    public class TestCustomConfigurationFactory : LuceneTestCase
    {
        [Test]
        public void TestCustomConfigurationSettings()
        {
            Assert.AreEqual("banana", ConfigurationSettings.CurrentConfiguration["fruit"]);
            Assert.AreEqual("banana", SystemProperties.GetProperty("fruit"));

            Assert.AreEqual("lettuce", ConfigurationSettings.CurrentConfiguration["vegetable"]);
            Assert.AreEqual("lettuce", SystemProperties.GetProperty("vegetable"));

            Assert.AreEqual("yogurt", ConfigurationSettings.CurrentConfiguration["tests:goo"]);
            Assert.AreEqual("yogurt", SystemProperties.GetProperty("tests:goo"));

            Assert.AreEqual("pizza", ConfigurationSettings.CurrentConfiguration["tests:junk"]);
            Assert.AreEqual("pizza", SystemProperties.GetProperty("tests:junk"));
        }

        [Test]
        public void TestInitializedOnlyOnce()
        {
            Assert.AreEqual(1, Startup.initializationCount);
            Assert.AreEqual(false, Startup.initilizationReset);
        }
    }
}
