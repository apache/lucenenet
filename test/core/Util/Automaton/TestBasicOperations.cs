using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestBasicOperations : LuceneTestCase
    {
        [Test]
        public void TestStringUnion()
        {
            var strings = new List<BytesRef>();
            for (int i = RandomInts.randomIntBetween(new Random(), 0, 1000); --i >= 0; )
            {
                strings.Add(new BytesRef(_TestUtil.RandomUnicodeString(new Random())));
            }

            strings.Sort();
            //Collections.Sort(strings);
            var union = BasicAutomata.MakeStringUnion(strings);
            Assert.IsTrue(union.Deterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(union, NaiveUnion(strings)));
        }

        private static Lucene.Net.Util.Automaton.Automaton NaiveUnion(List<BytesRef> strings)
        {
            var eachIndividual = new Lucene.Net.Util.Automaton.Automaton[strings.Count];
            int i = 0;
            foreach (var bref in strings)
            {
                eachIndividual[i++] = BasicAutomata.MakeString(bref.Utf8ToString());
            }
            return BasicOperations.Union(eachIndividual.ToList());
        }

        /** Test optimization to Concatenate() */
        [Test]
        public void TestSingletonConcatenate()
        {
            var singleton = BasicAutomata.MakeString("prefix");
            var expandedSingleton = singleton.CloneExpanded();
            var other = BasicAutomata.MakeCharRange('5', '7');
            var concat = BasicOperations.Concatenate(singleton, other);
            Assert.IsTrue(concat.Deterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(BasicOperations.Concatenate(expandedSingleton, other), concat));
        }

        /** Test optimization to Concatenate() to an NFA */
        [Test]
        public void TestSingletonNFAConcatenate()
        {
            var singleton = BasicAutomata.MakeString("prefix");
            var expandedSingleton = singleton.CloneExpanded();
            // an NFA (two transitions for 't' from initial state)
            var nfa = BasicOperations.Union(BasicAutomata.MakeString("this"),
                BasicAutomata.MakeString("three"));
            var concat = BasicOperations.Concatenate(singleton, nfa);
            Assert.IsFalse(concat.Deterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(BasicOperations.Concatenate(expandedSingleton, nfa), concat));
        }

        /** Test optimization to Concatenate() with empty String */
        [Test]
        public void TestEmptySingletonConcatenate()
        {
            var singleton = BasicAutomata.MakeString("");
            var expandedSingleton = singleton.CloneExpanded();
            var other = BasicAutomata.MakeCharRange('5', '7');
            var concat1 = BasicOperations.Concatenate(expandedSingleton, other);
            var concat2 = BasicOperations.Concatenate(singleton, other);
            Assert.IsTrue(concat2.Deterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(concat1, concat2));
            Assert.IsTrue(BasicOperations.SameLanguage(other, concat1));
            Assert.IsTrue(BasicOperations.SameLanguage(other, concat2));
        }

        /** Test concatenation with empty language returns empty */
        [Test]
        public void TestEmptyLanguageConcatenate()
        {
            var a = BasicAutomata.MakeString("a");
            var concat = BasicOperations.Concatenate(a, BasicAutomata.MakeEmpty());
            Assert.IsTrue(BasicOperations.IsEmpty(concat));
        }

        /** Test optimization to Concatenate() with empty String to an NFA */
        [Test]
        public void TestEmptySingletonNFAConcatenate()
        {
            var singleton = BasicAutomata.MakeString("");
            var expandedSingleton = singleton.CloneExpanded();
            // an NFA (two transitions for 't' from initial state)
            var nfa = BasicOperations.Union(BasicAutomata.MakeString("this"),
                BasicAutomata.MakeString("three"));
            var concat1 = BasicOperations.Concatenate(expandedSingleton, nfa);
            var concat2 = BasicOperations.Concatenate(singleton, nfa);
            Assert.IsFalse(concat2.Deterministic);
            Assert.IsTrue(BasicOperations.SameLanguage(concat1, concat2));
            Assert.IsTrue(BasicOperations.SameLanguage(nfa, concat1));
            Assert.IsTrue(BasicOperations.SameLanguage(nfa, concat2));
        }

        /** Test singletons work correctly */
        [Test]
        public void TestSingleton()
        {
            var singleton = BasicAutomata.MakeString("foobar");
            var expandedSingleton = singleton.CloneExpanded();
            Assert.IsTrue(BasicOperations.SameLanguage(singleton, expandedSingleton));

            singleton = BasicAutomata.MakeString("\ud801\udc1c");
            expandedSingleton = singleton.CloneExpanded();
            Assert.IsTrue(BasicOperations.SameLanguage(singleton, expandedSingleton));
        }

        [Test]
        public void TestGetRandomAcceptedString()
        {
            int ITER1 = AtLeast(100);
            int ITER2 = AtLeast(100);
            for (var i = 0; i < ITER1; i++)
            {

                var re = new RegExp(AutomatonTestUtil.randomRegexp(new Random()), RegExp.NONE);
                var a = re.ToAutomaton();
                Assert.IsFalse(BasicOperations.IsEmpty(a));

                AutomatonTestUtil.RandomAcceptedStrings rx = new AutomatonTestUtil.RandomAcceptedStrings(a);
                for (var j = 0; j < ITER2; j++)
                {
                    int[] acc = null;
                    try
                    {
                        acc = rx.GetRandomAcceptedString(new Random());
                        String s = UnicodeUtil.NewString(acc, 0, acc.Length);
                        Assert.IsTrue(BasicOperations.Run(a, s));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("regexp: " + re);
                        if (acc != null)
                        {
                            Console.WriteLine("fail acc re=" + re + " count=" + acc.Length);
                            for (int k = 0; k < acc.Length; k++)
                            {
                                Console.WriteLine("  " + Integer.ToHexString(acc[k]));
                            }
                        }
                        throw;
                    }
                }
            }
        }
    }
}
