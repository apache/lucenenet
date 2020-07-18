using NUnit.Framework;
using System;
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
    /// Tests Caverphone1.
    /// </summary>
    public class Caverphone1Test : StringEncoderAbstractTest<Caverphone1>
    {
        protected override Caverphone1 CreateStringEncoder()
        {
            return new Caverphone1();
        }

        /**
         * Tests example adapted from version 2.0  http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * AT1111 words: add, aid, at, art, eat, earth, head, hit, hot, hold, hard, heart, it, out, old
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedCommonCodeAT1111()
        {
            this.CheckEncodingVariations("AT1111", new String[]{
            "add",
            "aid",
            "at",
            "art",
            "eat",
            "earth",
            "head",
            "hit",
            "hot",
            "hold",
            "hard",
            "heart",
            "it",
            "out",
            "old"});
        }

        [Test]
        public void TestEndMb()
        {
            String[]
            []
            data = { new string[] { "mb", "M11111" }, new string[] { "mbmb", "MPM111" } };
            this.CheckEncodings(data);
        }

        /**
         * Tests some examples from version 2.0 http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestIsCaverphoneEquals()
        {
            Caverphone1 caverphone = new Caverphone1();
            Assert.False(caverphone.IsEncodeEqual("Peter", "Stevenson"), "Caverphone encodings should not be equal");
            Assert.True(caverphone.IsEncodeEqual("Peter", "Peady"), "Caverphone encodings should be equal");
        }

        /**
         * Tests example from http://caversham.otago.ac.nz/files/working/ctp060902.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestSpecificationV1Examples()
        {
            String[]
            []
            data = { new string[] { "David", "TFT111" }, new string[] { "Whittle", "WTL111" } };
            this.CheckEncodings(data);
        }

        /**
         * Tests examples from http://en.wikipedia.org/wiki/Caverphone
         *
         * @throws EncoderException
         */
        [Test]
        public void TestWikipediaExamples()
        {
            String[][] data = { new string[] { "Lee", "L11111" }, new string[] { "Thompson", "TMPSN1" } };
            this.CheckEncodings(data);
        }
    }
}
