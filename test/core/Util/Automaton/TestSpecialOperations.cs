using System;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using NUnit.Framework;

namespace Lucene.Net.Test.Util.Automaton
{
    [TestFixture]
    public class TestSpecialOperations : LuceneTestCase
    {
        [Test]
        public void TestIsFinite()
        {
            int num = AtLeast(200);
            for (var i = 0; i < num; i++)
            {
                var a = AutomatonTestUtil.randomAutomaton(new Random());
                var b = a.clone();
                Assert.Equals(AutomatonTestUtil.isFiniteSlow(a), SpecialOperations.IsFinite(b));
            }
        }

        [Test]
        public void TestFiniteStrings()
        {
            var a = BasicOperations.Union(BasicAutomata.MakeString("dog"), BasicAutomata.MakeString("duck"));
            MinimizationOperations.Minimize(a);
            var strings = SpecialOperations.GetFiniteStrings(a, -1);
            assertEquals(2, strings.Count);
            var dog = new IntsRef();
            Util.ToIntsRef(new BytesRef("dog"), dog);
            assertTrue(strings.Contains(dog));
            var duck = new IntsRef();
            Util.ToIntsRef(new BytesRef("duck"), duck);
            assertTrue(strings.Contains(duck));
        }
    }
}
