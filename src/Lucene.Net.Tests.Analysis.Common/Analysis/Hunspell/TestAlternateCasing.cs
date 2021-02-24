// Lucene version compatibility level 4.10.4
using NUnit.Framework;

namespace Lucene.Net.Analysis.Hunspell
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

    public class TestAlternateCasing : StemmerTestBase
    {
        public override void BeforeClass()
        {
            base.BeforeClass();
            Init("alternate-casing.aff", "alternate-casing.dic");
        }

        [Test]
        public void TestPossibilities()
        {
            AssertStemsTo("drink", "drink");
            AssertStemsTo("DRİNK", "drink");
            AssertStemsTo("DRINK");
            AssertStemsTo("drinki", "drink");
            AssertStemsTo("DRİNKİ", "drink");
            AssertStemsTo("DRİNKI");
            AssertStemsTo("DRINKI");
            AssertStemsTo("DRINKİ");
            AssertStemsTo("idrink", "drink");
            AssertStemsTo("İDRİNK", "drink");
            AssertStemsTo("IDRİNK");
            AssertStemsTo("IDRINK");
            AssertStemsTo("İDRINK");
            AssertStemsTo("idrinki", "drink");
            AssertStemsTo("İDRİNKİ", "drink");
            AssertStemsTo("rıver", "rıver");
            AssertStemsTo("RIVER", "rıver");
            AssertStemsTo("RİVER");
            AssertStemsTo("rıverı", "rıver");
            AssertStemsTo("RIVERI", "rıver");
            AssertStemsTo("RİVERI");
            AssertStemsTo("RİVERİ");
            AssertStemsTo("RIVERİ");
            AssertStemsTo("ırıver", "rıver");
            AssertStemsTo("IRIVER", "rıver");
            AssertStemsTo("IRİVER");
            AssertStemsTo("İRİVER");
            AssertStemsTo("İRIVER");
            AssertStemsTo("ırıverı", "rıver");
            AssertStemsTo("IRIVERI", "rıver");
            AssertStemsTo("Irıverı", "rıver");
        }
    }
}
