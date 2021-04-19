using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
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
    public class TestMathUtil : LuceneTestCase
    {

        internal static readonly long[] PRIMES = new long[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29 };

        internal static long RandomLong()
        {
            if (Random.NextBoolean())
            {
                long l = 1;
                if (Random.NextBoolean())
                {
                    l *= -1;
                }
                foreach (long i in PRIMES)
                {
                    int m = Random.Next(3);
                    for (int j = 0; j < m; ++j)
                    {
                        l *= i;
                    }
                }
                return l;
            }
            else if (Random.NextBoolean())
            {
                return Random.NextInt64();
            }
            else
            {
                return RandomPicks.RandomFrom(Random, new long[] { long.MinValue, long.MaxValue, 0L, -1L, 1L });
            }
        }

        // slow version used for testing
        private static bool TryGetGcd(long a, long b, out long result)
        {
            result = 0;
            var c = System.Numerics.BigInteger.GreatestCommonDivisor(a, b);
            if (c <= long.MaxValue && c >= long.MinValue)
            {
                result = (long)c;
                return true;
            }
            return false; // would overflow
        }

        [Test]
        public virtual void TestGCD()
        {
            int iters = AtLeast(100);
            for (int i = 0; i < iters; ++i)
            {
                long l1 = RandomLong();
                long l2 = RandomLong();
                long gcd = MathUtil.Gcd(l1, l2);
                if (TryGetGcd(l1, l2, out long actualGcd))
                {
                    Assert.AreEqual(actualGcd, gcd);
                    if (gcd != 0)
                    {
                        Assert.AreEqual(l1, (l1 / gcd) * gcd);
                        Assert.AreEqual(l2, (l2 / gcd) * gcd);
                    }
                }
                else
                {
                    // GCD cast to long would fail, try again
                    i--;
                }
            }
        }

        // ported test from commons-math
        [Test]
        public virtual void TestGCD2()
        {
            long a = 30;
            long b = 50;
            long c = 77;

            Assert.AreEqual(0, MathUtil.Gcd(0, 0));
            Assert.AreEqual(b, MathUtil.Gcd(0, b));
            Assert.AreEqual(a, MathUtil.Gcd(a, 0));
            Assert.AreEqual(b, MathUtil.Gcd(0, -b));
            Assert.AreEqual(a, MathUtil.Gcd(-a, 0));

            Assert.AreEqual(10, MathUtil.Gcd(a, b));
            Assert.AreEqual(10, MathUtil.Gcd(-a, b));
            Assert.AreEqual(10, MathUtil.Gcd(a, -b));
            Assert.AreEqual(10, MathUtil.Gcd(-a, -b));

            Assert.AreEqual(1, MathUtil.Gcd(a, c));
            Assert.AreEqual(1, MathUtil.Gcd(-a, c));
            Assert.AreEqual(1, MathUtil.Gcd(a, -c));
            Assert.AreEqual(1, MathUtil.Gcd(-a, -c));

            Assert.AreEqual(3L * (1L << 45), MathUtil.Gcd(3L * (1L << 50), 9L * (1L << 45)));
            Assert.AreEqual(1L << 45, MathUtil.Gcd(1L << 45, long.MinValue));

            Assert.AreEqual(long.MaxValue, MathUtil.Gcd(long.MaxValue, 0L));
            Assert.AreEqual(long.MaxValue, MathUtil.Gcd(-long.MaxValue, 0L));
            Assert.AreEqual(1, MathUtil.Gcd(60247241209L, 153092023L));

            Assert.AreEqual(long.MinValue, MathUtil.Gcd(long.MinValue, 0));
            Assert.AreEqual(long.MinValue, MathUtil.Gcd(0, long.MinValue));
            Assert.AreEqual(long.MinValue, MathUtil.Gcd(long.MinValue, long.MinValue));
        }

        [Test]
        public virtual void TestAcoshMethod()
        {
            // acosh(NaN) == NaN
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(double.NaN)));
            // acosh(1) == +0
            Assert.AreEqual(0, J2N.BitConversion.DoubleToInt64Bits(MathUtil.Acosh(1D)));
            // acosh(POSITIVE_INFINITY) == POSITIVE_INFINITY
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(double.PositiveInfinity), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Acosh(double.PositiveInfinity)));
            // acosh(x) : x < 1 == NaN
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(0.9D))); // x < 1
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(0D))); // x == 0
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(-0D))); // x == -0
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(-0.9D))); // x < 0
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(-1D))); // x == -1
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(-10D))); // x < -1
            Assert.IsTrue(double.IsNaN(MathUtil.Acosh(double.NegativeInfinity))); // x == -Inf

            double epsilon = 0.000001;
            Assert.AreEqual(0, MathUtil.Acosh(1), epsilon);
            Assert.AreEqual(1.5667992369724109, MathUtil.Acosh(2.5), epsilon);
            Assert.AreEqual(14.719378760739708, MathUtil.Acosh(1234567.89), epsilon);
        }

        [Test]
        public virtual void TestAsinhMethod()
        {

            // asinh(NaN) == NaN
            Assert.IsTrue(double.IsNaN(MathUtil.Asinh(double.NaN)));
            // asinh(+0) == +0
            Assert.AreEqual(0, J2N.BitConversion.DoubleToInt64Bits(MathUtil.Asinh(0D)));
            // asinh(-0) == -0
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(-0D), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Asinh(-0D)));
            // asinh(POSITIVE_INFINITY) == POSITIVE_INFINITY
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(double.PositiveInfinity), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Asinh(double.PositiveInfinity)));
            // asinh(NEGATIVE_INFINITY) == NEGATIVE_INFINITY
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(double.NegativeInfinity), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Asinh(double.NegativeInfinity)));

            double epsilon = 0.000001;
            Assert.AreEqual(-14.719378760740035, MathUtil.Asinh(-1234567.89), epsilon);
            Assert.AreEqual(-1.6472311463710958, MathUtil.Asinh(-2.5), epsilon);
            Assert.AreEqual(-0.8813735870195429, MathUtil.Asinh(-1), epsilon);
            Assert.AreEqual(0, MathUtil.Asinh(0), 0);
            Assert.AreEqual(0.8813735870195429, MathUtil.Asinh(1), epsilon);
            Assert.AreEqual(1.6472311463710958, MathUtil.Asinh(2.5), epsilon);
            Assert.AreEqual(14.719378760740035, MathUtil.Asinh(1234567.89), epsilon);
        }

        [Test]
        public virtual void TestAtanhMethod()
        {
            // atanh(NaN) == NaN
            Assert.IsTrue(double.IsNaN(MathUtil.Atanh(double.NaN)));
            // atanh(+0) == +0
            Assert.AreEqual(0, J2N.BitConversion.DoubleToInt64Bits(MathUtil.Atanh(0D)));
            // atanh(-0) == -0
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(-0D), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Atanh(-0D)));
            // atanh(1) == POSITIVE_INFINITY
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(double.PositiveInfinity), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Atanh(1D)));
            // atanh(-1) == NEGATIVE_INFINITY
            Assert.AreEqual(J2N.BitConversion.DoubleToInt64Bits(double.NegativeInfinity), J2N.BitConversion.DoubleToInt64Bits(MathUtil.Atanh(-1D)));
            // atanh(x) : Math.abs(x) > 1 == NaN
            Assert.IsTrue(double.IsNaN(MathUtil.Atanh(1.1D))); // x > 1
            Assert.IsTrue(double.IsNaN(MathUtil.Atanh(double.PositiveInfinity))); // x == Inf
            Assert.IsTrue(double.IsNaN(MathUtil.Atanh(-1.1D))); // x < -1
            Assert.IsTrue(double.IsNaN(MathUtil.Atanh(double.NegativeInfinity))); // x == -Inf

            double epsilon = 0.000001;
            Assert.AreEqual(double.NegativeInfinity, MathUtil.Atanh(-1), 0);
            Assert.AreEqual(-0.5493061443340549, MathUtil.Atanh(-0.5), epsilon);
            Assert.AreEqual(0, MathUtil.Atanh(0), 0);
            Assert.AreEqual(0.5493061443340549, MathUtil.Atanh(0.5), epsilon);
            Assert.AreEqual(double.PositiveInfinity, MathUtil.Atanh(1), 0);
        }

    }

}