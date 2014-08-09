

namespace Lucene.Net.Util
{
    using System;

    using Lucene.Net.Support;


    public class TestBroadWord : LuceneTestCase
    {


        [Test]
        public void TestRank()
        {
            AssertRank(0L);
            AssertRank(1L);
            AssertRank(3L);
            AssertRank(0x100L);
            AssertRank(0x300L);
            AssertRank(unchecked((long)0x8000000000000001L));
        }



        private void AssertRank(long value)
        {
            Equal(BitUtil.BitCount(value), BroadWord.BitCount(value));
        }
    }
}
