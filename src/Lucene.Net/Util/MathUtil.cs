using Lucene.Net.Support;
using System;

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
    public sealed class MathUtil
    {
        // No instance:
        private MathUtil()
        {
        }

        /// <summary>
        /// Returns {@code x <= 0 ? 0 : Math.floor(Math.log(x) / Math.log(base))} </summary>
        /// <param name="base"> must be {@code > 1} </param>
        public static int Log(long x, int @base)
        {
            if (@base <= 1)
            {
                throw new System.ArgumentException("base must be > 1");
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
        /// Calculates logarithm in a given base with doubles.
        /// </summary>
        public static double Log(double @base, double x)
        {
            return Math.Log(x) / Math.Log(@base);
        }

        /// <summary>
        /// Return the greatest common divisor of <code>a</code> and <code>b</code>,
        ///  consistently with <seealso cref="BigInteger#gcd(BigInteger)"/>.
        ///  <p><b>NOTE</b>: A greatest common divisor must be positive, but
        ///  <code>2^64</code> cannot be expressed as a long although it
        ///  is the GCD of <seealso cref="Long#MIN_VALUE"/> and <code>0</code> and the GCD of
        ///  <seealso cref="Long#MIN_VALUE"/> and <seealso cref="Long#MIN_VALUE"/>. So in these 2 cases,
        ///  and only them, this method will return <seealso cref="Long#MIN_VALUE"/>.
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
            int commonTrailingZeros = Number.NumberOfTrailingZeros(a | b);
            a = (long)((ulong)a >> Number.NumberOfTrailingZeros(a));
            while (true)
            {
                b = (long)((ulong)b >> Number.NumberOfTrailingZeros(b));
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
        /// Calculates inverse hyperbolic sine of a {@code double} value.
        /// <p>
        /// Special cases:
        /// <ul>
        ///    <li>If the argument is NaN, then the result is NaN.
        ///    <li>If the argument is zero, then the result is a zero with the same sign as the argument.
        ///    <li>If the argument is infinite, then the result is infinity with the same sign as the argument.
        /// </ul>
        /// </summary>
        public static double Asinh(double a)
        {
            double sign;
            // check the sign bit of the raw representation to handle -0
            if (BitConverter.DoubleToInt64Bits(a) < 0)
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
        /// Calculates inverse hyperbolic cosine of a {@code double} value.
        /// <p>
        /// Special cases:
        /// <ul>
        ///    <li>If the argument is NaN, then the result is NaN.
        ///    <li>If the argument is +1, then the result is a zero.
        ///    <li>If the argument is positive infinity, then the result is positive infinity.
        ///    <li>If the argument is less than 1, then the result is NaN.
        /// </ul>
        /// </summary>
        public static double Acosh(double a)
        {
            return Math.Log(Math.Sqrt(a * a - 1.0d) + a);
        }

        /// <summary>
        /// Calculates inverse hyperbolic tangent of a {@code double} value.
        /// <p>
        /// Special cases:
        /// <ul>
        ///    <li>If the argument is NaN, then the result is NaN.
        ///    <li>If the argument is zero, then the result is a zero with the same sign as the argument.
        ///    <li>If the argument is +1, then the result is positive infinity.
        ///    <li>If the argument is -1, then the result is negative infinity.
        ///    <li>If the argument's absolute value is greater than 1, then the result is NaN.
        /// </ul>
        /// </summary>
        public static double Atanh(double a)
        {
            double mult;
            // check the sign bit of the raw representation to handle -0
            if (BitConverter.DoubleToInt64Bits(a) < 0)
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