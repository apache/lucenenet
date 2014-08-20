using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    using Util;

    public class TestNumberExtensionMethods : LuceneTestCase
    {

        [Test]
        public void NumberOfLeadingZerosForInt()
        {
            Equal(32, 0x0.NumberOfLeadingZeros());
            Equal(24, 0xff.NumberOfLeadingZeros());
        }

        [Test]
        public void NumberOfLeadingZerosForLong()
        {
            Equal(64, ((long)0x0).NumberOfLeadingZeros());
            Equal(56, ((long)0xff).NumberOfLeadingZeros());
        }

        [Test]
        public void NumberOfTrailingZerosForInt()
        {
            Equal(4, 10000.NumberOfTrailingZeros());
            Equal(6, 1000000.NumberOfTrailingZeros());
        }


        [Test]
        public void NumberOfTrailingZerosForLong()
        {
            Equal(3, 1000L.NumberOfTrailingZeros());
            Equal(5, 100000L.NumberOfTrailingZeros());
        }
    }
}
