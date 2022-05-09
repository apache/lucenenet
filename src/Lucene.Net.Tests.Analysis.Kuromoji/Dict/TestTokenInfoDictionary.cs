using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using NUnit.Framework;
using System;
using Console = Lucene.Net.Util.SystemConsole;
using Int64 = J2N.Numerics.Int64;

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

    public class TestTokenInfoDictionary : LuceneTestCase
    {
        /// <summary>enumerates the entire FST/lookup data and just does basic sanity checks</summary>
        [Test]
        public void TestEnumerateAll()
        {
            // just for debugging
            int numTerms = 0;
            int numWords = 0;
            int lastWordId = -1;
            int lastSourceId = -1;
            TokenInfoDictionary tid = TokenInfoDictionary.Instance;
            ConnectionCosts matrix = ConnectionCosts.Instance;
            FST<Int64> fst = tid.FST.InternalFST;
            Int32sRefFSTEnum<Int64> fstEnum = new Int32sRefFSTEnum<Int64>(fst);
            Int32sRefFSTEnum.InputOutput<Int64> mapping;
            Int32sRef scratch = new Int32sRef();
            while (fstEnum.MoveNext())
            {
                mapping = fstEnum.Current;
                numTerms++;
                Int32sRef input = mapping.Input;
                char[] chars = new char[input.Length];
                for (int i = 0; i < chars.Length; i++)
                {
                    chars[i] = (char)input.Int32s[input.Offset + i];
                }
                assertTrue(UnicodeUtil.ValidUTF16String(new string(chars)));

                long? output = mapping.Output;
                int sourceId = (int)output.Value;
                // we walk in order, terms, sourceIds, and wordIds should always be increasing
                assertTrue(sourceId > lastSourceId);
                lastSourceId = sourceId;
                tid.LookupWordIds(sourceId, scratch);
                for (int i = 0; i < scratch.Length; i++)
                {
                    numWords++;
                    int wordId = scratch.Int32s[scratch.Offset + i];
                    assertTrue(wordId > lastWordId);
                    lastWordId = wordId;

                    String baseForm = tid.GetBaseForm(wordId, chars, 0, chars.Length);
                    assertTrue(baseForm is null || UnicodeUtil.ValidUTF16String(baseForm));

                    String inflectionForm = tid.GetInflectionForm(wordId);
                    assertTrue(inflectionForm is null || UnicodeUtil.ValidUTF16String(inflectionForm));
                    if (inflectionForm != null)
                    {
                        // check that its actually an ipadic inflection form
                        assertNotNull(ToStringUtil.GetInflectedFormTranslation(inflectionForm));
                    }

                    String inflectionType = tid.GetInflectionType(wordId);
                    assertTrue(inflectionType is null || UnicodeUtil.ValidUTF16String(inflectionType));
                    if (inflectionType != null)
                    {
                        // check that its actually an ipadic inflection type
                        assertNotNull(ToStringUtil.GetInflectionTypeTranslation(inflectionType));
                    }

                    int leftId = tid.GetLeftId(wordId);
                    int rightId = tid.GetRightId(wordId);

                    matrix.Get(rightId, leftId);

                    tid.GetWordCost(wordId);

                    String pos = tid.GetPartOfSpeech(wordId);
                    assertNotNull(pos);
                    assertTrue(UnicodeUtil.ValidUTF16String(pos));
                    // check that its actually an ipadic pos tag
                    assertNotNull(ToStringUtil.GetPOSTranslation(pos));

                    String pronunciation = tid.GetPronunciation(wordId, chars, 0, chars.Length);
                    assertNotNull(pronunciation);
                    assertTrue(UnicodeUtil.ValidUTF16String(pronunciation));

                    String reading = tid.GetReading(wordId, chars, 0, chars.Length);
                    assertNotNull(reading);
                    assertTrue(UnicodeUtil.ValidUTF16String(reading));
                }
            }
            if (Verbose)
            {
                Console.WriteLine("checked " + numTerms + " terms, " + numWords + " words.");
            }
        }
    }
}
