using Lucene.Net.Attributes;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Util.Automaton
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
    public class TestUTF32ToUTF8 : LuceneTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private const int MAX_UNICODE = 0x10FFFF;

        internal readonly BytesRef b = new BytesRef(4);

        private bool Matches(ByteRunAutomaton a, int code)
        {
            char[] chars = Character.ToChars(code);
            UnicodeUtil.UTF16toUTF8(chars, 0, chars.Length, b);
            return a.Run(b.Bytes, 0, b.Length);
        }

        private void TestOne(Random r, ByteRunAutomaton a, int startCode, int endCode, int iters)
        {
            // Verify correct ints are accepted
            int nonSurrogateCount;
            bool ovSurStart;
            if (endCode < UnicodeUtil.UNI_SUR_HIGH_START || startCode > UnicodeUtil.UNI_SUR_LOW_END)
            {
                // no overlap w/ surrogates
                nonSurrogateCount = endCode - startCode + 1;
                ovSurStart = false;
            }
            else if (IsSurrogate(startCode))
            {
                // start of range overlaps surrogates
                nonSurrogateCount = endCode - startCode + 1 - (UnicodeUtil.UNI_SUR_LOW_END - startCode + 1);
                ovSurStart = false;
            }
            else if (IsSurrogate(endCode))
            {
                // end of range overlaps surrogates
                ovSurStart = true;
                nonSurrogateCount = endCode - startCode + 1 - (endCode - UnicodeUtil.UNI_SUR_HIGH_START + 1);
            }
            else
            {
                // range completely subsumes surrogates
                ovSurStart = true;
                nonSurrogateCount = endCode - startCode + 1 - (UnicodeUtil.UNI_SUR_LOW_END - UnicodeUtil.UNI_SUR_HIGH_START + 1);
            }

            Debug.Assert(nonSurrogateCount > 0);

            for (int iter = 0; iter < iters; iter++)
            {
                // pick random code point in-range

                int code = startCode + r.Next(nonSurrogateCount);
                if (IsSurrogate(code))
                {
                    if (ovSurStart)
                    {
                        code = UnicodeUtil.UNI_SUR_LOW_END + 1 + (code - UnicodeUtil.UNI_SUR_HIGH_START);
                    }
                    else
                    {
                        code = UnicodeUtil.UNI_SUR_LOW_END + 1 + (code - startCode);
                    }
                }

                Debug.Assert(code >= startCode && code <= endCode, "code=" + code + " start=" + startCode + " end=" + endCode);
                Debug.Assert(!IsSurrogate(code));

                Assert.IsTrue(Matches(a, code), "DFA for range " + startCode + "-" + endCode + " failed to match code=" + code);
            }

            // Verify invalid ints are not accepted
            int invalidRange = MAX_UNICODE - (endCode - startCode + 1);
            if (invalidRange > 0)
            {
                for (int iter = 0; iter < iters; iter++)
                {
                    int x = TestUtil.NextInt(r, 0, invalidRange - 1);
                    int code;
                    if (x >= startCode)
                    {
                        code = endCode + 1 + x - startCode;
                    }
                    else
                    {
                        code = x;
                    }
                    if ((code >= UnicodeUtil.UNI_SUR_HIGH_START && code <= UnicodeUtil.UNI_SUR_HIGH_END) | (code >= UnicodeUtil.UNI_SUR_LOW_START && code <= UnicodeUtil.UNI_SUR_LOW_END))
                    {
                        iter--;
                        continue;
                    }
                    Assert.IsFalse(Matches(a, code), "DFA for range " + startCode + "-" + endCode + " matched invalid code=" + code);
                }
            }
        }

        // Evenly picks random code point from the 4 "buckets"
        // (bucket = same #bytes when encoded to utf8)
        private static int GetCodeStart(Random r)
        {
            switch (r.Next(4))
            {
                case 0:
                    return TestUtil.NextInt(r, 0, 128);

                case 1:
                    return TestUtil.NextInt(r, 128, 2048);

                case 2:
                    return TestUtil.NextInt(r, 2048, 65536);

                default:
                    return TestUtil.NextInt(r, 65536, 1 + MAX_UNICODE);
            }
        }

        private static bool IsSurrogate(int code)
        {
            return code >= UnicodeUtil.UNI_SUR_HIGH_START && code <= UnicodeUtil.UNI_SUR_LOW_END;
        }

        [Test]
        public void TestRandomRanges()
        {
            Random r = Random();
            int ITERS = AtLeast(10);
            int ITERS_PER_DFA = AtLeast(100);
            for (int iter = 0; iter < ITERS; iter++)
            {
                int x1 = GetCodeStart(r);
                int x2 = GetCodeStart(r);
                int startCode, endCode;

                if (x1 < x2)
                {
                    startCode = x1;
                    endCode = x2;
                }
                else
                {
                    startCode = x2;
                    endCode = x1;
                }

                if (IsSurrogate(startCode) && IsSurrogate(endCode))
                {
                    iter--;
                    continue;
                }

                var a = new Automaton();
                var end = new State {Accept = true};
                a.InitialState.AddTransition(new Transition(startCode, endCode, end));
                a.Deterministic = true;

                TestOne(r, new ByteRunAutomaton(a), startCode, endCode, ITERS_PER_DFA);
            }
        }

        [Test]
        public void TestSpecialCase()
        {
            RegExp re = new RegExp(".?");
            Automaton automaton = re.ToAutomaton();
            CharacterRunAutomaton cra = new CharacterRunAutomaton(automaton);
            ByteRunAutomaton bra = new ByteRunAutomaton(automaton);
            // make sure character dfa accepts empty string
            Assert.IsTrue(cra.IsAccept(cra.InitialState));
            Assert.IsTrue(cra.Run(""));
            Assert.IsTrue(cra.Run(new char[0], 0, 0));

            // make sure byte dfa accepts empty string
            Assert.IsTrue(bra.IsAccept(bra.InitialState));
            Assert.IsTrue(bra.Run(new byte[0], 0, 0));
        }

        [Test]
        public void TestSpecialCase2()
        {
            RegExp re = new RegExp(".+\u0775");
            string input = "\ufadc\ufffd\ub80b\uda5a\udc68\uf234\u0056\uda5b\udcc1\ufffd\ufffd\u0775";
            Automaton automaton = re.ToAutomaton();
            CharacterRunAutomaton cra = new CharacterRunAutomaton(automaton);
            ByteRunAutomaton bra = new ByteRunAutomaton(automaton);

            Assert.IsTrue(cra.Run(input));

            var bytes = input.GetBytes(Encoding.UTF8);
            Assert.IsTrue(bra.Run(bytes, 0, bytes.Length)); // this one fails!
        }

        [Test]
        public void TestSpecialCase3()
        {
            RegExp re = new RegExp("(\\鯺)*(.)*\\Ӕ");
            string input = "\u5cfd\ufffd\ub2f7\u0033\ue304\u51d7\u3692\udb50\udfb3\u0576\udae2\udc62\u0053\u0449\u04d4";
            Automaton automaton = re.ToAutomaton();
            CharacterRunAutomaton cra = new CharacterRunAutomaton(automaton);
            ByteRunAutomaton bra = new ByteRunAutomaton(automaton);

            Assert.IsTrue(cra.Run(input));

            var bytes = input.GetBytes(Encoding.UTF8);
            Assert.IsTrue(bra.Run(bytes, 0, bytes.Length));
        }

        [Test]
        public void TestRandomRegexes()
        {
            int num = AtLeast(250);
            for (int i = 0; i < num; i++)
            {
                AssertAutomaton((new RegExp(AutomatonTestUtil.RandomRegexp(Random()), RegExp.NONE)).ToAutomaton());
            }
        }

        private static void AssertAutomaton(Automaton automaton)
        {
            var cra = new CharacterRunAutomaton(automaton);
            var bra = new ByteRunAutomaton(automaton);
            var ras = new AutomatonTestUtil.RandomAcceptedStrings(automaton);

            int num = AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                string s;
                if (Random().NextBoolean())
                {
                    // likely not accepted
                    s = TestUtil.RandomUnicodeString(Random());
                }
                else
                {
                    // will be accepted
                    int[] codepoints = ras.GetRandomAcceptedString(Random());
                    try
                    {
                        s = UnicodeUtil.NewString(codepoints, 0, codepoints.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(codepoints.Length + " codepoints:");
                        for (int j = 0; j < codepoints.Length; j++)
                        {
                            Console.WriteLine("  " + codepoints[j].ToString("x"));
                        }
                        throw e;
                    }
                }
                var bytes = s.GetBytes(Encoding.UTF8);
                Assert.AreEqual(cra.Run(s), bra.Run(bytes, 0, bytes.Length));
            }
        }
    }
}