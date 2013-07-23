using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestIntsRef : LuceneTestCase
    {
        [Test]
        public virtual void TestEmpty()
        {
            var i = new IntsRef();
            Assert.Equals(IntsRef.EMPTY_INTS, i.ints);
            Assert.Equals(0, i.offset);
            Assert.Equals(0, i.length);
        }

        [Test]
        public virtual void TestFromInts()
        {
            var ints = new int[] { 1, 2, 3, 4 };
            var i = new IntsRef(ints, 0, 4);
            Assert.Equals(ints, i.ints);
            Assert.Equals(0, i.offset);
            Assert.Equals(4, i.length);

            var i2 = new IntsRef(ints, 1, 3);
            Assert.Equals(new IntsRef(new int[] { 2, 3, 4 }, 0, 3), i2);

            Assert.IsFalse(i.Equals(i2));
        }
    }
}
