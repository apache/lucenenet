

namespace Lucene.Net.Test.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using NUnit.Framework;
    using Lucene.Net.Util;

    [TestFixture]
    public class LuceneTestCaseTests : LuceneTestCase
    {
        [Test]
        public void  TestAtLeast()
        {
            var random = new Random();

            Assert.DoesNotThrow(() => {
                AtLeast(10);
            });
            
            13.Times(() => {
                var least = random.Next(1, 5000);
                var result = AtLeast(least);

                Assert.True(result >= least, string.Format(" {0} result should be at least {1}", result, least));
            });


            13.Times(() => {
                var least = random.Next(1, 5000);
                var localRandom = new Random();
                var localResult = AtLeast(localRandom, least);

                Assert.True(localResult >= least, string.Format(" {0} result should be at least {1}", localResult, least));
            });
        }
    }
}
