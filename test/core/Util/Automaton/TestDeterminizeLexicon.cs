using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestDeterminizeLexicon : LuceneTestCase
    {
        private List<Lucene.Net.Util.Automaton.Automaton> automata = new List<Lucene.Net.Util.Automaton.Automaton>();
        private List<string> terms = new List<string>();

        public void testLexicon()
        {
            int num = AtLeast(1);
            for (var i = 0; i < num; i++)
            {
                automata.Clear();
                terms.Clear();
                for (var j = 0; j < 5000; j++)
                {
                    string randomString = _TestUtil.RandomUnicodeString(new Random());
                    terms.Add(randomString);
                    automata.Add(BasicAutomata.MakeString(randomString));
                }
                assertLexicon();
            }
        }

        public void assertLexicon()
        {
            Collections.Shuffle(automata, new Random());
            var lex = BasicOperations.Union(automata);
            lex.Determinize();
            assertTrue(SpecialOperations.IsFinite(lex));
            foreach (var s in terms)
            {
                assertTrue(BasicOperations.Run(lex, s));
            }
            var lexByte = new ByteRunAutomaton(lex);
            foreach (var s in terms)
            {
                var bytes = s.GetBytes("UTF-8");
                assertTrue(lexByte.Run(bytes, 0, bytes.Length));
            }
        }
    }
}
