using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using RandomInts = RandomizedTesting.Generators.RandomNumbers;

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

    using Lucene.Net.Util;

    [TestFixture]
    public class TestBasicOperations : LuceneTestCase
    {
        /// <summary>
        /// Test string union. </summary>
        [Test]
        public virtual void TestStringUnion()
        {
            IList<BytesRef> strings = new JCG.List<BytesRef>();
            for (int i = RandomInts.RandomInt32Between(Random, 0, 1000); --i >= 0; )
            {
                strings.Add(new BytesRef(TestUtil.RandomUnicodeString(Random)));
            }

            strings.Sort();
            Automaton union = BasicAutomata.MakeStringUnion(strings);
            Assert.IsTrue(union.IsDeterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(union, NaiveUnion(strings)));
        }

        private static Automaton NaiveUnion(IList<BytesRef> strings)
        {
            Automaton[] eachIndividual = new Automaton[strings.Count];
            int i = 0;
            foreach (BytesRef bref in strings)
            {
                eachIndividual[i++] = BasicAutomata.MakeString(bref.Utf8ToString());
            }
            return BasicOperations.Union(eachIndividual);
        }

        /// <summary>
        /// Test optimization to concatenate() </summary>
        [Test]
        public virtual void TestSingletonConcatenate()
        {
            Automaton singleton = BasicAutomata.MakeString("prefix");
            Automaton expandedSingleton = singleton.CloneExpanded();
            Automaton other = BasicAutomata.MakeCharRange('5', '7');
            Automaton concat = BasicOperations.Concatenate(singleton, other);
            Assert.IsTrue(concat.IsDeterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(BasicOperations.Concatenate(expandedSingleton, other), concat));
        }

        /// <summary>
        /// Test optimization to concatenate() to an NFA </summary>
        [Test]
        public virtual void TestSingletonNFAConcatenate()
        {
            Automaton singleton = BasicAutomata.MakeString("prefix");
            Automaton expandedSingleton = singleton.CloneExpanded();
            // an NFA (two transitions for 't' from initial state)
            Automaton nfa = BasicOperations.Union(BasicAutomata.MakeString("this"), BasicAutomata.MakeString("three"));
            Automaton concat = BasicOperations.Concatenate(singleton, nfa);
            Assert.IsFalse(concat.IsDeterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(BasicOperations.Concatenate(expandedSingleton, nfa), concat));
        }

        /// <summary>
        /// Test optimization to concatenate() with empty String </summary>
        [Test]
        public virtual void TestEmptySingletonConcatenate()
        {
            Automaton singleton = BasicAutomata.MakeString("");
            Automaton expandedSingleton = singleton.CloneExpanded();
            Automaton other = BasicAutomata.MakeCharRange('5', '7');
            Automaton concat1 = BasicOperations.Concatenate(expandedSingleton, other);
            Automaton concat2 = BasicOperations.Concatenate(singleton, other);
            Assert.IsTrue(concat2.IsDeterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(concat1, concat2));
            Assert.IsTrue(BasicOperations.SameLanguage(other, concat1));
            Assert.IsTrue(BasicOperations.SameLanguage(other, concat2));
        }

        /// <summary>
        /// Test concatenation with empty language returns empty </summary>
        [Test]
        public virtual void TestEmptyLanguageConcatenate()
        {
            Automaton a = BasicAutomata.MakeString("a");
            Automaton concat = BasicOperations.Concatenate(a, BasicAutomata.MakeEmpty());
            Assert.IsTrue(BasicOperations.IsEmpty(concat));
        }

        /// <summary>
        /// Test optimization to concatenate() with empty String to an NFA </summary>
        [Test]
        public virtual void TestEmptySingletonNFAConcatenate()
        {
            Automaton singleton = BasicAutomata.MakeString("");
            Automaton expandedSingleton = singleton.CloneExpanded();
            // an NFA (two transitions for 't' from initial state)
            Automaton nfa = BasicOperations.Union(BasicAutomata.MakeString("this"), BasicAutomata.MakeString("three"));
            Automaton concat1 = BasicOperations.Concatenate(expandedSingleton, nfa);
            Automaton concat2 = BasicOperations.Concatenate(singleton, nfa);
            Assert.IsFalse(concat2.IsDeterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(concat1, concat2));
            Assert.IsTrue(BasicOperations.SameLanguage(nfa, concat1));
            Assert.IsTrue(BasicOperations.SameLanguage(nfa, concat2));
        }

        /// <summary>
        /// Test singletons work correctly </summary>
        [Test]
        public virtual void TestSingleton()
        {
            Automaton singleton = BasicAutomata.MakeString("foobar");
            Automaton expandedSingleton = singleton.CloneExpanded();
            Assert.IsTrue(BasicOperations.SameLanguage(singleton, expandedSingleton));

            singleton = BasicAutomata.MakeString("\ud801\udc1c");
            expandedSingleton = singleton.CloneExpanded();
            Assert.IsTrue(BasicOperations.SameLanguage(singleton, expandedSingleton));
        }

        [Test]
        public virtual void TestGetRandomAcceptedString()
        {
            int ITER1 = AtLeast(100);
            int ITER2 = AtLeast(100);
            for (int i = 0; i < ITER1; i++)
            {
                RegExp re = new RegExp(AutomatonTestUtil.RandomRegexp(Random), RegExpSyntax.NONE);
                Automaton a = re.ToAutomaton();
                Assert.IsFalse(BasicOperations.IsEmpty(a));

                RandomAcceptedStrings rx = new RandomAcceptedStrings(a);
                for (int j = 0; j < ITER2; j++)
                {
                    int[] acc = null;
                    try
                    {
                        acc = rx.GetRandomAcceptedString(Random);
                        string s = UnicodeUtil.NewString(acc, 0, acc.Length);
                        Assert.IsTrue(BasicOperations.Run(a, s));
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        Console.WriteLine("regexp: " + re);
                        if (acc != null)
                        {
                            Console.WriteLine("fail acc re=" + re + " count=" + acc.Length);
                            for (int k = 0; k < acc.Length; k++)
                            {
                                Console.WriteLine("  " + acc[k].ToString("x"));
                            }
                        }
                        throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                    }
                }
            }
        }
    }
}