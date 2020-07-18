using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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
    public class TestEnglishNumberFormatExtensions
    {
        [Test, LuceneNetSpecific]
        public void TestToWords()
        {
            Assert.AreEqual("twenty-one", 21.ToWords());
            Assert.AreEqual("one thousand two hundred thirty-four", 1234.ToWords());
            Assert.AreEqual("six million four hundred ninety-one thousand three hundred forty-eight", 6491348.ToWords());
            Assert.AreEqual("one hundred thirty", 130.ToWords());
            Assert.AreEqual("one hundred thirty-seven", 137.ToWords());
            Assert.AreEqual("seven hundred forty-nine million one hundred thirty-two thousand one hundred forty-six", 749132146.ToWords());
            Assert.AreEqual("nine hundred ninety-nine billion seven hundred forty-nine million one hundred thirty-two thousand one hundred forty-six", 999749132146.ToWords());
        }
    }
}
