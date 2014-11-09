using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Java.Lang
{
    using global::Java.Lang;

    public class LongTests : TestClass
    {
        [Test]
        public void NumberOfLeadingZeros()
        {
            // 0x1FL = (Long) 31
            // 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0001 1111
            const long value1 = 0x1FL;
            Equal(59, Long.NumberOfLeadingZeros(value1), "The number of leading zeros must be 59");

            // 0x1F00F0f00F111L = (Long)545422443606289;
            // 0000 0000 0000 0001 1111 0000 0000 1111 0000 1111 0000 0000 1111 0001 0001 0001
            const long value2 = 0x1F00F0f00F111L;
            Equal(15, Long.NumberOfLeadingZeros(value2), "The number of leading zeros must be 15");
        }

        [Test]
        public void NumberOfTrailingZeros()
        {
            // 0x8000L = (Long) 32768;
            // 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 1000 0000 0000 0000
            const long value1 = 0x8000L;
            Equal(15, Long.NumberOfTrailingZeros(value1), "The number of trailing zeros must be 15");

            // 0x1F00F0F00F100L = (Long)545422443606272
            // 0000 0000 0000 0001 1111 0000 0000 1111 0000 1111 0000 0000 1111 0001 0000 0000
            const long value2 = 0x1F00F0F00F100L;
            Equal(8, Long.NumberOfTrailingZeros(value2), "The number of trailing zeros must be 8");

            // 0x80000L = (Long) 524288
            // 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 0000 1000 0000 0000 0000 0000
            const long value3 = 0x80000L;
            Equal(19, Long.NumberOfTrailingZeros(value3), "The number of trailing zeros must be 19");
        }
    }
}
