using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language
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

    /// <summary>
    /// Tests RefinedSoundex.
    /// </summary>
    public class RefinedSoundexTest : StringEncoderAbstractTest<RefinedSoundex>
    {
        protected override RefinedSoundex CreateStringEncoder()
        {
            return new RefinedSoundex();
        }

        [Test]
        public void TestDifference()
        {
            // Edge cases
            Assert.AreEqual(0, this.StringEncoder.Difference(null, null));
            Assert.AreEqual(0, this.StringEncoder.Difference("", ""));
            Assert.AreEqual(0, this.StringEncoder.Difference(" ", " "));
            // Normal cases
            Assert.AreEqual(6, this.StringEncoder.Difference("Smith", "Smythe"));
            Assert.AreEqual(3, this.StringEncoder.Difference("Ann", "Andrew"));
            Assert.AreEqual(1, this.StringEncoder.Difference("Margaret", "Andrew"));
            Assert.AreEqual(1, this.StringEncoder.Difference("Janet", "Margaret"));
            // Examples from
            // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp
            Assert.AreEqual(5, this.StringEncoder.Difference("Green", "Greene"));
            Assert.AreEqual(1, this.StringEncoder.Difference("Blotchet-Halls", "Greene"));
            // Examples from
            // http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_setu-sus_3o6w.asp
            Assert.AreEqual(6, this.StringEncoder.Difference("Smith", "Smythe"));
            Assert.AreEqual(8, this.StringEncoder.Difference("Smithers", "Smythers"));
            Assert.AreEqual(5, this.StringEncoder.Difference("Anothers", "Brothers"));
        }

        [Test]
        public void TestEncode()
        {
            Assert.AreEqual("T6036084", this.StringEncoder.Encode("testing"));
            Assert.AreEqual("T6036084", this.StringEncoder.Encode("TESTING"));
            Assert.AreEqual("T60", this.StringEncoder.Encode("The"));
            Assert.AreEqual("Q503", this.StringEncoder.Encode("quick"));
            Assert.AreEqual("B1908", this.StringEncoder.Encode("brown"));
            Assert.AreEqual("F205", this.StringEncoder.Encode("fox"));
            Assert.AreEqual("J408106", this.StringEncoder.Encode("jumped"));
            Assert.AreEqual("O0209", this.StringEncoder.Encode("over"));
            Assert.AreEqual("T60", this.StringEncoder.Encode("the"));
            Assert.AreEqual("L7050", this.StringEncoder.Encode("lazy"));
            Assert.AreEqual("D6043", this.StringEncoder.Encode("dogs"));

            // Testing CODEC-56
            Assert.AreEqual("D6043", RefinedSoundex.US_ENGLISH.Encode("dogs"));
        }

        [Test]
        public void TestGetMappingCodeNonLetter()
        {
            char code = this.StringEncoder.GetMappingCode('#');
            Assert.AreEqual(0, code, "Code does not equals zero");
        }

        [Test]
        public void TestNewInstance()
        {
            Assert.AreEqual("D6043", new RefinedSoundex().GetSoundex("dogs"));
        }

        [Test]
        public void TestNewInstance2()
        {
            Assert.AreEqual("D6043", new RefinedSoundex(RefinedSoundex.US_ENGLISH_MAPPING_STRING.toCharArray()).GetSoundex("dogs"));
        }

        [Test]
        public void TestNewInstance3()
        {
            Assert.AreEqual("D6043", new RefinedSoundex(RefinedSoundex.US_ENGLISH_MAPPING_STRING).GetSoundex("dogs"));
        }
    }
}
