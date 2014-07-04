using System.Collections.Generic;
using System.Text;
using Lucene.Net.Support;
using NUnit.Framework;

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
        private IList<Automaton> Automata = new List<Automaton>();
        private IList<string> Terms = new List<string>();

        [Test]
        public virtual void TestLexicon()
        {
            int num = AtLeast(1);
            for (int i = 0; i < num; i++)
            {
                Automata.Clear();
                Terms.Clear();
                for (int j = 0; j < 5000; j++)
                {
                    string randomString = TestUtil.RandomUnicodeString(Random());
                    Terms.Add(randomString);
                    Automata.Add(BasicAutomata.MakeString(randomString));
                }
                AssertLexicon();
            }
        }

        public virtual void AssertLexicon()
        {
            Automata = CollectionsHelper.Shuffle(Automata);
            Automaton lex = BasicOperations.Union(Automata);
            lex.Determinize();
            Assert.IsTrue(SpecialOperations.IsFinite(lex));
            foreach (string s in Terms)
            {
                Assert.IsTrue(BasicOperations.Run(lex, s));
            }
            ByteRunAutomaton lexByte = new ByteRunAutomaton(lex);
            foreach (string s in Terms)
            {
                sbyte[] bytes = s.GetBytes(Encoding.UTF8);
                Assert.IsTrue(lexByte.Run(bytes, 0, bytes.Length));
            }
        }
    }

}