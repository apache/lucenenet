using System;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestRamUsageEstimatorOnWildAnimals : LuceneTestCase
    {
        public class ListElement
        {
            internal ListElement next;
        }

        [Test]
        public void TestOverflowMaxChainLength()
        {
            var UPPERLIMIT = 100000;
            var lower = 0;
            var upper = UPPERLIMIT;

            while (lower + 1 < upper)
            {
                var mid = (lower + upper) / 2;
                try
                {
                    var first = new ListElement();
                    var last = first;
                    for (var i = 0; i < mid; i++)
                    {
                        last = (last.next = new ListElement());
                    }
                    RamUsageEstimator.SizeOf(first); // cause SOE or pass.
                    lower = mid;
                }
                catch (StackOverflowException e)
                {
                    upper = mid;
                }
            }

            if (lower + 1 < UPPERLIMIT)
            {
                Assert.Fail("Max object chain length till stack overflow: " + lower);
            }
        }  
    }
}
