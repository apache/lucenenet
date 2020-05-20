using Lucene.Net.Util;
using NUnit.Framework;
using After = NUnit.Framework.TearDownAttribute;

namespace Lucene.Net.Configuration
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
    class TestDefaultSystemProperties : LuceneTestCase
    {        /// <summary>
             /// For subclasses to override. Overrides must call <c>base.TearDown()</c>.
             /// </summary>
        [After]
#pragma warning disable xUnit1013
        public virtual void TearDown()
#pragma warning restore xUnit1013
        {
            ConfigurationSettings.CurrentConfiguration.Reload();
            base.TearDown();
        }
        [Test]
        public virtual void ReadEnvironmentTest()
        {
            string testKey = "lucene:tests:setting";
            string testValue = "test.success";
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }
        [Test]
        public virtual void SetEnvironmentTest()
        {
            string testKey  = "lucene:tests:setting";
            string testValue = "test.success";
            Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey] = testValue;
            Assert.AreEqual(testValue, Lucene.Net.Configuration.ConfigurationSettings.CurrentConfiguration[testKey]);
            Assert.AreEqual(testValue, SystemProperties.GetProperty(testKey));
        }

    }
}