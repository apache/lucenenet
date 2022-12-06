using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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

    // The test framework will automatically select the Codec.Default property at random from the complete
    // set of codecs. For any codecs that were not registed, they need to be supressed with the SuppressCodecs attribute
    // on each test class (or a base class of them).
    [SuppressCodecs("Lucene3x", "Lucene40", "Lucene41", "Lucene42", "Lucene45")]
    public class TestConfigurationService : LuceneTestCase
    {
        [Test]
        public void TestRetrieveConfiguration()
        {
            Assert.AreEqual("fooValue", ConfigurationSettings.CurrentConfiguration["foo"]);
            Assert.AreEqual("barValue", ConfigurationSettings.CurrentConfiguration["bar"]);
            Assert.AreEqual("bazValue", ConfigurationSettings.CurrentConfiguration["baz"]);
        }

        [Test]
        public void TestCustomMaxStackByteLimit()
        {
            // This custom value is configured in Startup.cs.
            // 5000 chosen because it is not likely to ever be made a default.
            Assert.AreEqual(5000, Constants.MaxStackByteLimit);
        }
    }
}
