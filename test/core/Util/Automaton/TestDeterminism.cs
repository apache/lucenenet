using System;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestDeterminism : LuceneTestCase
    {
        /** Test a bunch of random regular expressions */
        [Test]
        public void TestRegexps()
        {
            int num = AtLeast(500);
            for (var i = 0; i < num; i++)
                AssertAutomaton(new RegExp(AutomatonTestUtil.RandomRegexp(new Random()), RegExp.NONE).ToAutomaton());
        }

        /** Test against a simple, unoptimized det */
        [Test]
        public void TestAgainstSimple()
        {
            int num = AtLeast(200);
            for (var i = 0; i < num; i++)
            {
                var a = AutomatonTestUtil.RandomAutomaton(new Random());
                var b = a.Clone();
                AutomatonTestUtil.DeterminizeSimple(a);
                b.Deterministic = false; // force det
                b.Determinize();
                // TODO: more verifications possible?
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
            }
        }

        private static void AssertAutomaton(Lucene.Net.Util.Automaton.Automaton a)
        {
            var clone = a.Clone();
            // complement(complement(a)) = a
            var equivalent = BasicOperations.Complement(BasicOperations.Complement(a));
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a union a = a
            equivalent = BasicOperations.Union(a, clone);
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a intersect a = a
            equivalent = BasicOperations.Intersection(a, clone);
            Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));

            // a minus a = empty
            var empty = BasicOperations.Minus(a, clone);
            Assert.IsTrue(BasicOperations.IsEmpty(empty));

            // as long as don't accept the empty string
            // then optional(a) - empty = a
            if (!BasicOperations.Run(a, ""))
            {
                //System.out.println("Test " + a);
                var optional = BasicOperations.Optional(a);
                //System.out.println("optional " + optional);
                equivalent = BasicOperations.Minus(optional, BasicAutomata.MakeEmptyString());
                //System.out.println("equiv " + equivalent);
                Assert.IsTrue(BasicOperations.SameLanguage(a, equivalent));
            }
        }
    }
}
