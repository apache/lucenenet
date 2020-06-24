using NUnit.Framework;
using System;
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
    /// Tests Rule.
    /// <para/>
    /// since 1.6
    /// </summary>
    public class RuleTest
    {
        //    private static class NegativeIntegerBaseMatcher : BaseMatcher<Integer> {
        //        @Override
        //    public void describeTo(final Description description)
        //    {
        //        description.appendText("value should be negative");
        //    }

        //    @Override
        //    public boolean matches(final Object item)
        //    {
        //        return ((Integer)item).intValue() < 0;
        //    }
        //}

        private Phoneme[][] MakePhonemes()
        {
            String[][]
        words = {
               new string[] { "rinD", "rinDlt", "rina", "rinalt", "rino", "rinolt", "rinu", "rinult" },
               new string[] { "dortlaj", "dortlej", "ortlaj", "ortlej", "ortlej-dortlaj" } };
            Phoneme[][] phonemes = new Phoneme[words.Length][];

            for (int i = 0; i < words.Length; i++)
            {
                String[] words_i = words[i];
                Phoneme[] phonemes_i = phonemes[i] = new Phoneme[words_i.Length];
                for (int j = 0; j < words_i.Length; j++)
                {
                    phonemes_i[j] = new Phoneme(words_i[j], Languages.NO_LANGUAGES);
                }
            }

            return phonemes;
        }

        [Test]
        public void TestPhonemeComparedToLaterIsNegative()
        {
            foreach (Phoneme[] phs in MakePhonemes())
            {
                for (int i = 0; i < phs.Length; i++)
                {
                    for (int j = i + 1; j < phs.Length; j++)
                    {
                        int c = Phoneme.COMPARER.Compare(phs[i], phs[j]);

                        Assert.True(c < 0,
                                "Comparing " + phs[i].GetPhonemeText() + " to " + phs[j].GetPhonemeText() + " should be negative");
                    }
                }
            }
        }

        [Test]
        public void TestPhonemeComparedToSelfIsZero()
        {
            foreach (Phoneme[] phs in MakePhonemes())
            {
                foreach (Phoneme ph in phs)
                {
                    Assert.AreEqual(0,
                            Phoneme.COMPARER.Compare(ph, ph),
                            "Phoneme compared to itself should be zero: " + ph.GetPhonemeText());
                }
            }
        }

        [Test]
        public void TestSubSequenceWorks()
        {
            // AppendableCharSequence is private to Rule. We can only make it through a Phoneme.

            Phoneme a = new Phoneme("a", null);
            Phoneme b = new Phoneme("b", null);
            Phoneme cd = new Phoneme("cd", null);
            Phoneme ef = new Phoneme("ef", null);
            Phoneme ghi = new Phoneme("ghi", null);
            Phoneme jkl = new Phoneme("jkl", null);

            Assert.AreEqual('a', a.GetPhonemeText()[0]);
            Assert.AreEqual('b', b.GetPhonemeText()[0]);
            Assert.AreEqual('c', cd.GetPhonemeText()[0]);
            Assert.AreEqual('d', cd.GetPhonemeText()[1]);
            Assert.AreEqual('e', ef.GetPhonemeText()[0]);
            Assert.AreEqual('f', ef.GetPhonemeText()[1]);
            Assert.AreEqual('g', ghi.GetPhonemeText()[0]);
            Assert.AreEqual('h', ghi.GetPhonemeText()[1]);
            Assert.AreEqual('i', ghi.GetPhonemeText()[2]);
            Assert.AreEqual('j', jkl.GetPhonemeText()[0]);
            Assert.AreEqual('k', jkl.GetPhonemeText()[1]);
            Assert.AreEqual('l', jkl.GetPhonemeText()[2]);

            Phoneme a_b = new Phoneme(a, b);
            Assert.AreEqual('a', a_b.GetPhonemeText()[0]);
            Assert.AreEqual('b', a_b.GetPhonemeText()[1]);
            Assert.AreEqual("ab", a_b.GetPhonemeText().Substring(0, 2 - 0).toString());
            Assert.AreEqual("a", a_b.GetPhonemeText().Substring(0, 1 - 0).toString());
            Assert.AreEqual("b", a_b.GetPhonemeText().Substring(1, 2 - 1).toString());

            Phoneme cd_ef = new Phoneme(cd, ef);
            Assert.AreEqual('c', cd_ef.GetPhonemeText()[0]);
            Assert.AreEqual('d', cd_ef.GetPhonemeText()[1]);
            Assert.AreEqual('e', cd_ef.GetPhonemeText()[2]);
            Assert.AreEqual('f', cd_ef.GetPhonemeText()[3]);
            Assert.AreEqual("c", cd_ef.GetPhonemeText().Substring(0, 1 - 0).toString());
            Assert.AreEqual("d", cd_ef.GetPhonemeText().Substring(1, 2 - 1).toString());
            Assert.AreEqual("e", cd_ef.GetPhonemeText().Substring(2, 3 - 2).toString());
            Assert.AreEqual("f", cd_ef.GetPhonemeText().Substring(3, 4 - 3).toString());
            Assert.AreEqual("cd", cd_ef.GetPhonemeText().Substring(0, 2 - 0).toString());
            Assert.AreEqual("de", cd_ef.GetPhonemeText().Substring(1, 3 - 1).toString());
            Assert.AreEqual("ef", cd_ef.GetPhonemeText().Substring(2, 4 - 2).toString());
            Assert.AreEqual("cde", cd_ef.GetPhonemeText().Substring(0, 3 - 0).toString());
            Assert.AreEqual("def", cd_ef.GetPhonemeText().Substring(1, 4 - 1).toString());
            Assert.AreEqual("cdef", cd_ef.GetPhonemeText().Substring(0, 4 - 0).toString());

            var test = new Phoneme(a, b);
            Phoneme a_b_cd = new Phoneme(test, cd);
            Assert.AreEqual('a', a_b_cd.GetPhonemeText()[0]);
            Assert.AreEqual('b', a_b_cd.GetPhonemeText()[1]);
            Assert.AreEqual('c', a_b_cd.GetPhonemeText()[2]);
            Assert.AreEqual('d', a_b_cd.GetPhonemeText()[3]);
            Assert.AreEqual("a", a_b_cd.GetPhonemeText().Substring(0, 1 - 0).toString());
            Assert.AreEqual("b", a_b_cd.GetPhonemeText().Substring(1, 2 - 1).toString());
            Assert.AreEqual("c", a_b_cd.GetPhonemeText().Substring(2, 3 - 2).toString());
            Assert.AreEqual("d", a_b_cd.GetPhonemeText().Substring(3, 4 - 3).toString());
            Assert.AreEqual("ab", a_b_cd.GetPhonemeText().Substring(0, 2 - 0).toString());
            Assert.AreEqual("bc", a_b_cd.GetPhonemeText().Substring(1, 3 - 1).toString());
            Assert.AreEqual("cd", a_b_cd.GetPhonemeText().Substring(2, 4 - 2).toString());
            Assert.AreEqual("abc", a_b_cd.GetPhonemeText().Substring(0, 3 - 0).toString());
            Assert.AreEqual("bcd", a_b_cd.GetPhonemeText().Substring(1, 4 - 1).toString());
            Assert.AreEqual("abcd", a_b_cd.GetPhonemeText().Substring(0, 4 - 0).toString());
        }
    }
}
