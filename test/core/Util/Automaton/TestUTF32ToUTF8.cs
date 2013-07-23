using System;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestUTF32ToUTF8 : LuceneTestCase
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
        }

        private static readonly int MAX_UNICODE = 0x10FFFF;

        internal readonly BytesRef b = new BytesRef(4);

        private bool Matches(ByteRunAutomaton a, int code)
        {
            char[] chars = Character.ToChars(code);
            UnicodeUtil.UTF16toUTF8(chars, 0, chars.Length, b);
            return a.Run(b.bytes, 0, b.length);
        }

        private void TestOne(Random r, ByteRunAutomaton a, int startCode, int endCode, int iters)
        {

            // Verify correct ints are accepted
            int nonSurrogateCount;
            bool ovSurStart;
            if (endCode < UnicodeUtil.UNI_SUR_HIGH_START ||
                startCode > UnicodeUtil.UNI_SUR_LOW_END)
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

            //assert nonSurrogateCount > 0;

            for (var iter = 0; iter < iters; iter++)
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

                //assert code >= startCode && code <= endCode: "code=" + code + " start=" + startCode + " end=" + endCode;
                //assert !IsSurrogate(code);

                assertTrue("DFA for range " + startCode + "-" + endCode + " failed to match code=" + code,
                           Matches(a, code));
            }

            // Verify invalid ints are not accepted
            var invalidRange = MAX_UNICODE - (endCode - startCode + 1);
            if (invalidRange > 0)
            {
                for (var iter = 0; iter < iters; iter++)
                {
                    int x = _TestUtil.NextInt(r, 0, invalidRange - 1);
                    int code;
                    if (x >= startCode)
                    {
                        code = endCode + 1 + x - startCode;
                    }
                    else
                    {
                        code = x;
                    }
                    if ((code >= UnicodeUtil.UNI_SUR_HIGH_START && code <= UnicodeUtil.UNI_SUR_HIGH_END) |
                        (code >= UnicodeUtil.UNI_SUR_LOW_START && code <= UnicodeUtil.UNI_SUR_LOW_END))
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
        private int GetCodeStart(Random r)
        {
            switch (r.Next(4))
            {
                case 0:
                    return _TestUtil.NextInt(r, 0, 128);
                case 1:              
                    return _TestUtil.NextInt(r, 128, 2048);
                case 2:              
                    return _TestUtil.NextInt(r, 2048, 65536);
                default:             
                    return _TestUtil.NextInt(r, 65536, 1 + MAX_UNICODE);
            }
        }

        private static bool IsSurrogate(int code)
        {
            return code >= UnicodeUtil.UNI_SUR_HIGH_START && code <= UnicodeUtil.UNI_SUR_LOW_END;
        }

        [Test]
        public void TestRandomRanges()
        {
            var r = new Random();
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

                var a = new Lucene.Net.Util.Automaton.Automaton();
                var end = new State {Accept = true};
                a.InitialState.AddTransition(new Transition(startCode, endCode, end));
                a.Deterministic = true;

                TestOne(r, new ByteRunAutomaton(a), startCode, endCode, ITERS_PER_DFA);
            }
        }

        [Test]
        public void TestSpecialCase()
        {
            var re = new RegExp(".?");
            var automaton = re.ToAutomaton();
            var cra = new CharacterRunAutomaton(automaton);
            var bra = new ByteRunAutomaton(automaton);
            // make sure character dfa accepts empty string
            assertTrue(cra.IsAccept(cra.InitialState));
            assertTrue(cra.Run(""));
            assertTrue(cra.Run(new char[0], 0, 0));

            // make sure byte dfa accepts empty string
            assertTrue(bra.IsAccept(bra.InitialState));
            assertTrue(bra.Run(new sbyte[0], 0, 0));
        }

        [Test]
        public void TestSpecialCase2()
        {
            var re = new RegExp(".+\u0775");
            var input = "\ufadc\ufffd\ub80b\uda5a\udc68\uf234\u0056\uda5b\udcc1\ufffd\ufffd\u0775";
            var automaton = re.ToAutomaton();
            var cra = new CharacterRunAutomaton(automaton);
            var bra = new ByteRunAutomaton(automaton);

            assertTrue(cra.Run(input));

            byte[] bytes = input.GetBytes("UTF-8");
            assertTrue(bra.Run(bytes, 0, bytes.Length)); // this one fails!
        }

        [Test]
        public void TestSpecialCase3()
        {
            var re = new RegExp("(\\鯺)*(.)*\\Ӕ");
            var input = "\u5cfd\ufffd\ub2f7\u0033\ue304\u51d7\u3692\udb50\udfb3\u0576\udae2\udc62\u0053\u0449\u04d4";
            var automaton = re.ToAutomaton();
            var cra = new CharacterRunAutomaton(automaton);
            var bra = new ByteRunAutomaton(automaton);

            assertTrue(cra.Run(input));

            byte[] bytes = input.GetBytes("UTF-8");
            assertTrue(bra.Run(bytes, 0, bytes.Length));
        }

        [Test]
        public void TestRandomRegexes()
        {
            int num = AtLeast(250);
            for (var i = 0; i < num; i++)
            {
                AssertAutomaton(new RegExp(AutomatonTestUtil.randomRegexp(new Random()), RegExp.NONE).ToAutomaton());
            }
        }

        private void AssertAutomaton(Lucene.Net.Util.Automaton.Automaton automaton)
        {
            var cra = new CharacterRunAutomaton(automaton);
            var bra = new ByteRunAutomaton(automaton);
            AutomatonTestUtil.RandomAcceptedStrings ras = new AutomatonTestUtil.RandomAcceptedStrings(automaton);

            int num = AtLeast(1000);
            for (int i = 0; i < num; i++)
            {
                string str;
                if (new Random().NextBool())
                {
                    // likely not accepted
                    str = _TestUtil.RandomUnicodeString(new Random());
                }
                else
                {
                    // will be accepted
                    int[] codepoints = ras.GetRandomAcceptedString(new Random());
                    try
                    {
                        str = UnicodeUtil.NewString(codepoints, 0, codepoints.Length);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(codepoints.Length + " codepoints:");
                        for (int j = 0; j < codepoints.Length; j++)
                        {
                            Console.WriteLine("  " + Integer.toHexString(codepoints[j]));
                        }
                        throw e;
                    }
                }
                var bytes = str.GetBytes("UTF-8");
                assertEquals(cra.Run(str), bra.Run(bytes, 0, bytes.Length));
            }
        }
    }
}
