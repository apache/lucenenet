using J2N.Numerics;
using System;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// Math static utility methods.
    /// </summary>
    public static class MathUtil // LUCENENET: Changed to static
    {
        /// <summary>
        /// Returns <c>x &lt;= 0 ? 0 : Math.Floor(Math.Log(x) / Math.Log(base))</c>. </summary>
        /// <param name="base"> Must be <c>&gt; 1</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Log(long x, int @base)
        {
            if (@base <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(@base), "base must be > 1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            int ret = 0;
            while (x >= @base)
            {
                x /= @base;
                ret++;
            }
            return ret;
        }

        /// <summary>
        /// Calculates logarithm in a given <paramref name="base"/> with doubles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Log(double @base, double x)
        {
            return Math.Log(x) / Math.Log(@base);
        }

        /// <summary>
        /// Return the greatest common divisor of <paramref name="a"/> and <paramref name="b"/>,
        /// consistently with <c>System.Numerics.BigInteger.GreatestCommonDivisor(System.Numerics.BigInteger, System.Numerics.BigInteger)</c>.
        /// <para/><b>NOTE</b>: A greatest common divisor must be positive, but
        /// <c>2^64</c> cannot be expressed as a <see cref="long"/> although it
        /// is the GCD of <see cref="long.MinValue"/> and <c>0</c> and the GCD of
        /// <see cref="long.MinValue"/> and <see cref="long.MinValue"/>. So in these 2 cases,
        /// and only them, this method will return <see cref="long.MinValue"/>.
        /// </summary>
        // see http://en.wikipedia.org/wiki/Binary_GCD_algorithm#Iterative_version_in_C.2B.2B_using_ctz_.28count_trailing_zeros.29
        public static long Gcd(long a, long b)
        {
            // LUCENENET: Math.Abs and BigInteger.Abs get an OverflowException, so we resort to this.
            a = a < 0 ? -a : a;
            b = b < 0 ? -b : b;
            if (a == 0)
            {
                return b;
            }
            else if (b == 0)
            {
                return a;
            }
            int commonTrailingZeros = (a | b).TrailingZeroCount();
            a = a.TripleShift(a.TrailingZeroCount());
            while (true)
            {
                b = b.TripleShift(b.TrailingZeroCount());
                if (a == b)
                {
                    break;
                } // MIN_VALUE is treated as 2^64
                else if (a > b || a == long.MinValue)
                {
                    long tmp = a;
                    a = b;
                    b = tmp;
                }
                if (a == 1)
                {
                    break;
                }
                b -= a;
            }
            return a << commonTrailingZeros;
        }

        /// <summary>
        /// Calculates inverse hyperbolic sine of a <see cref="double"/> value.
        /// <para/>
        /// Special cases:
        /// <list type="bullet">
        ///    <item><description>If the argument is NaN, then the result is NaN.</description></item>
        ///    <item><description>If the argument is zero, then the result is a zero with the same sign as the argument.</description></item>
        ///    <item><description>If the argument is infinite, then the result is infinity with the same sign as the argument.</description></item>
        /// </list>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Asinh(double a)
        {
            double sign;
            // check the sign bit of the raw representation to handle -0
            if (J2N.BitConversion.DoubleToRawInt64Bits(a) < 0)
            {
                a = Math.Abs(a);
                sign = -1.0d;
            }
            else
            {
                sign = 1.0d;
            }

            return sign * Math.Log(Math.Sqrt(a * a + 1.0d) + a);
        }

        /// <summary>
        /// Calculates inverse hyperbolic cosine of a <see cref="double"/> value.
        /// <para/>
        /// Special cases:
        /// <list type="bullet">
        ///    <item><description>If the argument is NaN, then the result is NaN.</description></item>
        ///    <item><description>If the argument is +1, then the result is a zero.</description></item>
        ///    <item><description>If the argument is positive infinity, then the result is positive infinity.</description></item>
        ///    <item><description>If the argument is less than 1, then the result is NaN.</description></item>
        /// </list>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Acosh(double a)
        {
            return Math.Log(Math.Sqrt(a * a - 1.0d) + a);
        }

        /// <summary>
        /// Calculates inverse hyperbolic tangent of a <see cref="double"/> value.
        /// <para/>
        /// Special cases:
        /// <list type="bullet">
        ///    <item><description>If the argument is NaN, then the result is NaN.</description></item>
        ///    <item><description>If the argument is zero, then the result is a zero with the same sign as the argument.</description></item>
        ///    <item><description>If the argument is +1, then the result is positive infinity.</description></item>
        ///    <item><description>If the argument is -1, then the result is negative infinity.</description></item>
        ///    <item><description>If the argument's absolute value is greater than 1, then the result is NaN.</description></item>
        /// </list>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Atanh(double a)
        {
            double mult;
            // check the sign bit of the raw representation to handle -0
            if (J2N.BitConversion.DoubleToRawInt64Bits(a) < 0)
            {
                a = Math.Abs(a);
                mult = -0.5d;
            }
            else
            {
                mult = 0.5d;
            }
            return mult * Math.Log((1.0d + a) / (1.0d - a));
        }
    }
}