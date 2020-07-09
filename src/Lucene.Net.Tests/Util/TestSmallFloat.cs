using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    [TestFixture]
    public class TestSmallFloat : LuceneTestCase
    {
        // original lucene byteToFloat
        internal static float Orig_byteToFloat(sbyte b)
        {
            if (b == 0) // zero is a special case
            {
                return 0.0f;
            }
            int mantissa = b & 7;
            int exponent = (b >> 3) & 31;
            int bits = ((exponent + (63 - 15)) << 24) | (mantissa << 21);
            return J2N.BitConversion.Int32BitsToSingle(bits);
        }

        // original lucene floatToByte (since lucene 1.3)
        internal static sbyte Orig_floatToByte_v13(float f)
        {
            if (f < 0.0f) // round negatives up to zero
            {
                f = 0.0f;
            }

            if (f == 0.0f) // zero is a special case
            {
                return 0;
            }

            int bits = J2N.BitConversion.SingleToInt32Bits(f); // parse float into parts
            int mantissa = (bits & 0xffffff) >> 21;
            int exponent = (((bits >> 24) & 0x7f) - 63) + 15;

            if (exponent > 31) // overflow: use max value
            {
                exponent = 31;
                mantissa = 7;
            }

            if (exponent < 0) // underflow: use min value
            {
                exponent = 0;
                mantissa = 1;
            }

            return (sbyte)((exponent << 3) | mantissa); // pack into a byte
        }

        // this is the original lucene floatToBytes (from v1.3)
        // except with the underflow detection bug fixed for values like 5.8123817E-10f
        internal static sbyte Orig_floatToByte(float f)
        {
            if (f < 0.0f) // round negatives up to zero
            {
                f = 0.0f;
            }

            if (f == 0.0f) // zero is a special case
            {
                return 0;
            }

            int bits = J2N.BitConversion.SingleToInt32Bits(f); // parse float into parts
            int mantissa = (bits & 0xffffff) >> 21;
            int exponent = (((bits >> 24) & 0x7f) - 63) + 15;

            if (exponent > 31) // overflow: use max value
            {
                exponent = 31;
                mantissa = 7;
            }

            if (exponent < 0 || exponent == 0 && mantissa == 0) // underflow: use min value
            {
                exponent = 0;
                mantissa = 1;
            }

            return (sbyte)((exponent << 3) | mantissa); // pack into a byte
        }

        [Test]
        public virtual void TestByteToFloat()
        {
            for (int i = 0; i < 256; i++)
            {
                float f1 = Orig_byteToFloat((sbyte)i);
                float f2 = SmallSingle.SByteToSingle((sbyte)i, 3, 15);
                float f3 = SmallSingle.SByte315ToSingle((sbyte)i);
                Assert.AreEqual(f1, f2, 0.0);
                Assert.AreEqual(f2, f3, 0.0);

                float f4 = SmallSingle.SByteToSingle((sbyte)i, 5, 2);
                float f5 = SmallSingle.SByte52ToSingle((sbyte)i);
                Assert.AreEqual(f4, f5, 0.0);
            }
        }

        [Test]
        public virtual void TestFloatToByte()
        {
            Assert.AreEqual(0, Orig_floatToByte_v13(5.8123817E-10f)); // verify the old bug (see LUCENE-2937)
            Assert.AreEqual(1, Orig_floatToByte(5.8123817E-10f)); // verify it's fixed in this test code
            Assert.AreEqual(1, SmallSingle.SingleToSByte315(5.8123817E-10f)); // verify it's fixed

            // test some constants
            Assert.AreEqual(0, SmallSingle.SingleToSByte315(0));
            //Java's Float.MIN_VALUE equals C#'s float.Epsilon
            Assert.AreEqual(1, SmallSingle.SingleToSByte315(float.Epsilon)); // underflow rounds up to smallest positive
            Assert.AreEqual(255, SmallSingle.SingleToSByte315(float.MaxValue) & 0xff); // overflow rounds down to largest positive
            Assert.AreEqual(255, SmallSingle.SingleToSByte315(float.PositiveInfinity) & 0xff);

            // all negatives map to 0
            Assert.AreEqual(0, SmallSingle.SingleToSByte315(-float.Epsilon));
            Assert.AreEqual(0, SmallSingle.SingleToSByte315(-float.MaxValue));
            Assert.AreEqual(0, SmallSingle.SingleToSByte315(float.NegativeInfinity));

            // up iterations for more exhaustive test after changing something
            int num = AtLeast(100000);
            for (int i = 0; i < num; i++)
            {
                float f = J2N.BitConversion.Int32BitsToSingle(Random.Next());
                if (float.IsNaN(f)) // skip NaN
                {
                    continue;
                }
                sbyte b1 = Orig_floatToByte(f);
                sbyte b2 = SmallSingle.SingleToSByte(f, 3, 15);
                sbyte b3 = SmallSingle.SingleToSByte315(f);
                Assert.AreEqual(b1, b2);
                Assert.AreEqual(b2, b3);

                sbyte b4 = SmallSingle.SingleToSByte(f, 5, 2);
                sbyte b5 = SmallSingle.SingleToSByte52(f);
                Assert.AreEqual(b4, b5);
            }
        }
    }
}