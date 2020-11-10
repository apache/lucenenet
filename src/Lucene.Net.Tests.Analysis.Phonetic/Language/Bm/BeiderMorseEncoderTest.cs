using J2N.Text;
using NUnit.Framework;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Tests BeiderMorseEncoder.
    /// </summary>
    public class BeiderMorseEncoderTest : StringEncoderAbstractTest<BeiderMorseEncoder>
    {
        private static readonly char[] TEST_CHARS = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'o', 'u' };

        private void AssertNotEmpty(BeiderMorseEncoder bmpm, string value)
        {
            Assert.False(bmpm.Encode(value).Length == 0, value); // LUCENENET: CA1820: Test for empty strings using string length
        }

        private BeiderMorseEncoder CreateGenericApproxEncoder()
        {
            BeiderMorseEncoder encoder = new BeiderMorseEncoder();
            encoder.NameType=(NameType.GENERIC);
            encoder.RuleType=(RuleType.APPROX);
            return encoder;
        }

        protected override BeiderMorseEncoder CreateStringEncoder()
        {
            return new BeiderMorseEncoder();
        }

        /**
         * Tests we do not blow up.
         *
         * @throws EncoderException
         */
        [Test]
        public void TestAllChars()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                bmpm.Encode(c.ToString());
            }
        }

        [Test]
        public void TestAsciiEncodeNotEmpty1Letter()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            for (char c = 'a'; c <= 'z'; c++)
            {
                string value = c.ToString();
                string valueU = value.ToUpperInvariant();
                AssertNotEmpty(bmpm, value);
                AssertNotEmpty(bmpm, valueU);
            }
        }

        [Test]
        public void TestAsciiEncodeNotEmpty2Letters()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            for (char c1 = 'a'; c1 <= 'z'; c1++)
            {
                for (char c2 = 'a'; c2 <= 'z'; c2++)
                {
                    String value = new String(new char[] { c1, c2 });
                    String valueU = value.ToUpperInvariant();
                    AssertNotEmpty(bmpm, value);
                    AssertNotEmpty(bmpm, valueU);
                }
            }
        }

        [Test]
        public void TestEncodeAtzNotEmpty()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            //String[] names = { "ácz", "átz", "Ignácz", "Ignátz", "Ignác" };
            String[]
           names = { "\u00e1cz", "\u00e1tz", "Ign\u00e1cz", "Ign\u00e1tz", "Ign\u00e1c" };
            foreach (String name in names)
            {
                AssertNotEmpty(bmpm, name);
            }
        }

        /**
         * Tests https://issues.apache.org/jira/browse/CODEC-125?focusedCommentId=13071566&page=com.atlassian.jira.plugin.system.issuetabpanels:
         * comment-tabpanel#comment-13071566
         *
         * @throws EncoderException
         */
        [Test]
        public void TestEncodeGna()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            bmpm.Encode("gna");
        }

        [Test]//@Test(expected = IllegalArgumentException.class)
        public void TestInvalidLangIllegalArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Rule.GetInstance(NameType.GENERIC, RuleType.APPROX, "noSuchLanguage"));
        }

        [Test]//@Test(expected = IllegalStateException.class)
        public void TestInvalidLangIllegalStateException()
        {
            Assert.Throws<InvalidOperationException>(() => Lang.LoadFromResource("thisIsAMadeUpResourceName", Languages.GetInstance(NameType.GENERIC)));
        }

        [Test]//@Test(expected = IllegalArgumentException.class)
        public void TestInvalidLanguageIllegalArgumentException()
        {
            Assert.Throws<ArgumentException>(() => Languages.GetInstance("thereIsNoSuchLanguage"));
        }

        [Test]//@Test(timeout = 10000L)
        public void TestLongestEnglishSurname()
        {
            BeiderMorseEncoder bmpm = CreateGenericApproxEncoder();
            bmpm.Encode("MacGhilleseatheanaich");
        }

        [Test]//@Test(expected = IndexOutOfBoundsException.class)
        public void TestNegativeIndexForRuleMatchIndexOutOfBoundsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                Rule r = new Rule("a", "", "", new Phoneme("", Languages.ANY_LANGUAGE));
                r.PatternAndContextMatches("bob", -1);
            });
        }

        [Test]
        public void TestOOM()
        {
            String phrase = "200697900'-->&#1913348150;</  bceaeef >aadaabcf\"aedfbff<!--\'-->?>cae"
                       + "cfaaa><?&#<!--</script>&lang&fc;aadeaf?>>&bdquo<    cc =\"abff\"    /></   afe  >"
                       + "<script><!-- f(';<    cf aefbeef = \"bfabadcf\" ebbfeedd = fccabeb >";

            BeiderMorseEncoder encoder = new BeiderMorseEncoder();
            encoder.NameType=(NameType.GENERIC);
            encoder.RuleType=(RuleType.EXACT);
            encoder.SetMaxPhonemes(10);

            String phonemes = encoder.Encode(phrase);
            Assert.True(phonemes.Length > 0);

            String[] phonemeArr = new Regex("\\|").Split(phonemes).TrimEnd();
            Assert.True(phonemeArr.Length <= 10);
        }

        [Test]
        public void TestSetConcat()
        {
            BeiderMorseEncoder bmpm = new BeiderMorseEncoder();
            bmpm.IsConcat=(false);
            Assert.False(bmpm.IsConcat, "Should be able to set concat to false");
        }

        [Test]
        public void TestSetNameTypeAsh()
        {
            BeiderMorseEncoder bmpm = new BeiderMorseEncoder();
            bmpm.NameType=(NameType.ASHKENAZI);
            Assert.AreEqual(NameType.ASHKENAZI, bmpm.NameType, "Name type should have been set to ash");
        }

        [Test]
        public void TestSetRuleTypeExact()
        {
            BeiderMorseEncoder bmpm = new BeiderMorseEncoder();
            bmpm.RuleType=(RuleType.EXACT);
            Assert.AreEqual(RuleType.EXACT, bmpm.RuleType, "Rule type should have been set to exact");
        }

        [Test]//@Test(expected = IllegalArgumentException.class)
        public void TestSetRuleTypeToRulesIllegalArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                BeiderMorseEncoder bmpm = new BeiderMorseEncoder();
                bmpm.RuleType=(RuleType.RULES);
            });
        }

        /**
         * (Un)luckily, the worse performing test because of the data in <see cref="TEST_CHARS"/>
         *
         * @throws EncoderException
         */
        [Test]/* timeout = 20000L */
        public void TestSpeedCheck()
        {
            BeiderMorseEncoder bmpm = this.CreateGenericApproxEncoder();
            StringBuilder stringBuffer = new StringBuilder();
            stringBuffer.append(TEST_CHARS[0]);
            for (int i = 0, j = 1; i < 40; i++, j++)
            {
                if (j == TEST_CHARS.Length)
                {
                    j = 0;
                }
                bmpm.Encode(stringBuffer.toString());
                stringBuffer.append(TEST_CHARS[j]);
            }
        }

        [Test]
        public void TestSpeedCheck2()
        {
            BeiderMorseEncoder bmpm = this.CreateGenericApproxEncoder();
            String phrase = "ItstheendoftheworldasweknowitandIfeelfine";

            for (int i = 1; i <= phrase.Length; i++)
            {
                bmpm.Encode(phrase.Substring(0, i));
            }
        }

        [Test]
        public void TestSpeedCheck3()
        {
            BeiderMorseEncoder bmpm = this.CreateGenericApproxEncoder();
            String phrase = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz";

            for (int i = 1; i <= phrase.Length; i++)
            {
                bmpm.Encode(phrase.Substring(0, i));
            }
        }
    }
}
