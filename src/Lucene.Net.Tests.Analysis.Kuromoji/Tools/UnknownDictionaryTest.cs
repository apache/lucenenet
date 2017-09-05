using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ja.Util
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

    public class UnknownDictionaryTest : LuceneTestCase
    {
        public static readonly string FILENAME = "unk-tokeninfo-dict.obj";

        [Test]
        public void TestPutCharacterCategory()
        {
            UnknownDictionaryWriter unkDic = new UnknownDictionaryWriter(10 * 1024 * 1024);

            try
            {
                unkDic.PutCharacterCategory(0, "DUMMY_NAME");
                fail();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {

            }

            try
            {
                unkDic.PutCharacterCategory(-1, "KATAKANA");
                fail();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {

            }

            unkDic.PutCharacterCategory(0, "DEFAULT");
            unkDic.PutCharacterCategory(1, "GREEK");
            unkDic.PutCharacterCategory(2, "HIRAGANA");
            unkDic.PutCharacterCategory(3, "KATAKANA");
            unkDic.PutCharacterCategory(4, "KANJI");
        }

        [Test]
        public void TestPut()
        {
            UnknownDictionaryWriter unkDic = new UnknownDictionaryWriter(10 * 1024 * 1024);
            try
            {
                unkDic.Put(CSVUtil.Parse("KANJI,1285,11426,名詞,一般,*,*,*,*,*,*,*"));
                fail();
            }
#pragma warning disable 168
            catch (Exception e)
#pragma warning restore 168
            {

            }

            String entry1 = "ALPHA,1285,1285,13398,名詞,一般,*,*,*,*,*,*,*";
            String entry2 = "HIRAGANA,1285,1285,13069,名詞,一般,*,*,*,*,*,*,*";
            String entry3 = "KANJI,1285,1285,11426,名詞,一般,*,*,*,*,*,*,*";

            unkDic.PutCharacterCategory(0, "ALPHA");
            unkDic.PutCharacterCategory(1, "HIRAGANA");
            unkDic.PutCharacterCategory(2, "KANJI");

            unkDic.Put(CSVUtil.Parse(entry1));
            unkDic.Put(CSVUtil.Parse(entry2));
            unkDic.Put(CSVUtil.Parse(entry3));
        }
    }
}
