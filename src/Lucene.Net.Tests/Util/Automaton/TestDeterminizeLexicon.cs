using J2N.Collections.Generic.Extensions;
using J2N.Text;
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;

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

    /// <summary>
    /// Not thorough, but tries to test determinism correctness
    /// somewhat randomly, by determinizing a huge random lexicon.
    /// </summary>
    [TestFixture]
    public class TestDeterminizeLexicon : LuceneTestCase
    {
        private IList<Automaton> automata = new JCG.List<Automaton>();
        private IList<string> terms = new JCG.List<string>();

        [Test]
        public void TestLexicon()
        {
            int num = AtLeast(1);
            for (int i = 0; i < num; i++)
            {
                automata.Clear();
                terms.Clear();
                for (int j = 0; j < 5000; j++)
                {
                    string randomString = TestUtil.RandomUnicodeString(Random);
                    terms.Add(randomString);
                    automata.Add(BasicAutomata.MakeString(randomString));
                }
                AssertLexicon();
            }
        }

        public void AssertLexicon()
        {
            automata.Shuffle(Random);
            var lex = BasicOperations.Union(automata);
            lex.Determinize();
            Assert.IsTrue(SpecialOperations.IsFinite(lex));
            foreach (string s in terms)
            {
                assertTrue(BasicOperations.Run(lex, s));
            }
            var lexByte = new ByteRunAutomaton(lex);
            foreach (string s in terms)
            {
                var bytes = s.GetBytes(Encoding.UTF8);
                assertTrue(lexByte.Run(bytes, 0, bytes.Length));
            }
        }
    }
}