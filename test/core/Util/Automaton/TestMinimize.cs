using System;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestMinimize : LuceneTestCase
    {
        /** the minimal and non-minimal are compared to ensure they are the same. */
        [Test]
        public void Test()
        {
            int num = AtLeast(200);
            for (var i = 0; i < num; i++)
            {
                var a = AutomatonTestUtil.RandomAutomaton(new Random());
                var b = a.Clone();
                MinimizationOperations.Minimize(b);
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
            }
        }

        /** compare minimized against minimized with a slower, simple impl.
         * we check not only that they are the same, but that #states/#transitions
         * are the same. */
        [Test]
        public void TestAgainstBrzozowski()
        {
            int num = AtLeast(200);
            for (var i = 0; i < num; i++)
            {
                var a = AutomatonTestUtil.RandomAutomaton(new Random());
                AutomatonTestUtil.MinimizeSimple(a);
                var b = a.Clone();
                MinimizationOperations.Minimize(b);
                Assert.IsTrue(BasicOperations.SameLanguage(a, b));
                Assert.Equals(a.GetNumberOfStates(), b.GetNumberOfStates());
                Assert.Equals(a.GetNumberOfTransitions(), b.GetNumberOfTransitions());
            }
        }

        /** n^2 space usage in Hopcroft minimization? */
        [Test]
        public void TestMinimizeHuge()
        {
            new RegExp("+-*(A|.....|BC)*]", RegExp.NONE).ToAutomaton();
        }
    }
}
