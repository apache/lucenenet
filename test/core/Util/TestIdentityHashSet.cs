using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestIdentityHashSet : LuceneTestCase
    {
        public void testCheck()
        {
            var rnd = new Random();
            ISet<object> jdk = Collections.NewSetFromMap(
                new IdentityHashMap<object, bool>());
            RamUsageEstimator.IdentityHashSet<object> us = new RamUsageEstimator.IdentityHashSet<object>();

            var max = 100000;
            var threshold = 256;
            for (var i = 0; i < max; i++)
            {
                // some of these will be interned and some will not so there will be collisions.
                var v = rnd.Next(threshold);

                bool e1 = jdk.Contains(v);
                bool e2 = us.Contains(v);
                Assert.Equals(e1, e2);

                e1 = jdk.Add(v);
                e2 = us.Add(v);
                Assert.Equals(e1, e2);
            }

            ISet<object> collected = Collections.NewSetFromMap(
                new IdentityHashMap<object, bool>());
            foreach (var o in us)
            {
                collected.Add(o);
            }

            Assert.Equals(collected, jdk);
        }
    }
}
