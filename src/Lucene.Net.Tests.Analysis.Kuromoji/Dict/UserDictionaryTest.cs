using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja.Dict
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

    public class UserDictionaryTest : LuceneTestCase
    {
        [Test]
        public void TestLookup()
        {
            UserDictionary dictionary = TestJapaneseTokenizer.ReadDict();
            String s = "関西国際空港に行った";
            int[][] dictionaryEntryResult = dictionary.Lookup(s.toCharArray(), 0, s.Length);
            // Length should be three 関西, 国際, 空港
            assertEquals(3, dictionaryEntryResult.Length);

            // Test positions
            assertEquals(0, dictionaryEntryResult[0][1]); // index of 関西
            assertEquals(2, dictionaryEntryResult[1][1]); // index of 国際
            assertEquals(4, dictionaryEntryResult[2][1]); // index of 空港

            // Test lengths
            assertEquals(2, dictionaryEntryResult[0][2]); // length of 関西
            assertEquals(2, dictionaryEntryResult[1][2]); // length of 国際
            assertEquals(2, dictionaryEntryResult[2][2]); // length of 空港

            s = "関西国際空港と関西国際空港に行った";
            int[][] dictionaryEntryResult2 = dictionary.Lookup(s.toCharArray(), 0, s.Length);
            // Length should be six 
            assertEquals(6, dictionaryEntryResult2.Length);
        }

        [Test]
        public void TestReadings()
        {
            UserDictionary dictionary = TestJapaneseTokenizer.ReadDict();
            int[]
                []
                result = dictionary.Lookup("日本経済新聞".toCharArray(), 0, 6);
            assertEquals(3, result.Length);
            int wordIdNihon = result[0]
                [0]; // wordId of 日本 in 日本経済新聞
            assertEquals("ニホン", dictionary.GetReading(wordIdNihon, "日本".toCharArray(), 0, 2));

            result = dictionary.Lookup("朝青龍".toCharArray(), 0, 3);
            assertEquals(1, result.Length);
            int wordIdAsashoryu = result[0]
                [0]; // wordId for 朝青龍
            assertEquals("アサショウリュウ", dictionary.GetReading(wordIdAsashoryu, "朝青龍".toCharArray(), 0, 3));
        }

        [Test]
        public void TestPartOfSpeech()
        {
            UserDictionary dictionary = TestJapaneseTokenizer.ReadDict();
            int[]
                []
                result = dictionary.Lookup("日本経済新聞".toCharArray(), 0, 6);
            assertEquals(3, result.Length);
            int wordIdKeizai = result[1]
                [0]; // wordId of 経済 in 日本経済新聞
            assertEquals("カスタム名詞", dictionary.GetPartOfSpeech(wordIdKeizai));
        }

        [Test]
        public void TestRead()
        {
            UserDictionary dictionary = TestJapaneseTokenizer.ReadDict();
            assertNotNull(dictionary);
        }
    }
}
