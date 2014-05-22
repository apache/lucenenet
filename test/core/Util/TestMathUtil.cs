using System.Diagnostics;

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


	using RandomPicks = com.carrotsearch.randomizedtesting.generators.RandomPicks;

	public class TestMathUtil : LuceneTestCase
	{

	  internal static long[] PRIMES = new long[] {2, 3, 5, 7, 11, 13, 17, 19, 23, 29};

	  internal static long RandomLong()
	  {
		if (random().nextBoolean())
		{
		  long l = 1;
		  if (random().nextBoolean())
		  {
			l *= -1;
		  }
		  foreach (long i in PRIMES)
		  {
			int m = random().Next(3);
			for (int j = 0; j < m; ++j)
			{
			  l *= i;
			}
		  }
		  return l;
		}
		else if (random().nextBoolean())
		{
		  return random().nextLong();
		}
		else
		{
		  return RandomPicks.randomFrom(random(), Arrays.asList(long.MinValue, long.MaxValue, 0L, -1L, 1L));
		}
	  }

	  // slow version used for testing
	  internal static long Gcd(long l1, long l2)
	  {
		System.Numerics.BigInteger gcd = System.Numerics.BigInteger.valueOf(l1).gcd(System.Numerics.BigInteger.valueOf(l2));
		Debug.Assert(gcd.bitCount() <= 64);
		return (long)gcd;
	  }

	  public virtual void TestGCD()
	  {
		int iters = atLeast(100);
		for (int i = 0; i < iters; ++i)
		{
		  long l1 = RandomLong();
		  long l2 = RandomLong();
		  long gcd = MathUtil.gcd(l1, l2);
		  long actualGcd = Gcd(l1, l2);
		  Assert.AreEqual(actualGcd, gcd);
		  if (gcd != 0)
		  {
			Assert.AreEqual(l1, (l1 / gcd) * gcd);
			Assert.AreEqual(l2, (l2 / gcd) * gcd);
		  }
		}
	  }

	  // ported test from commons-math
	  public virtual void TestGCD2()
	  {
		long a = 30;
		long b = 50;
		long c = 77;

		Assert.AreEqual(0, MathUtil.gcd(0, 0));
		Assert.AreEqual(b, MathUtil.gcd(0, b));
		Assert.AreEqual(a, MathUtil.gcd(a, 0));
		Assert.AreEqual(b, MathUtil.gcd(0, -b));
		Assert.AreEqual(a, MathUtil.gcd(-a, 0));

		Assert.AreEqual(10, MathUtil.gcd(a, b));
		Assert.AreEqual(10, MathUtil.gcd(-a, b));
		Assert.AreEqual(10, MathUtil.gcd(a, -b));
		Assert.AreEqual(10, MathUtil.gcd(-a, -b));

		Assert.AreEqual(1, MathUtil.gcd(a, c));
		Assert.AreEqual(1, MathUtil.gcd(-a, c));
		Assert.AreEqual(1, MathUtil.gcd(a, -c));
		Assert.AreEqual(1, MathUtil.gcd(-a, -c));

		Assert.AreEqual(3L * (1L << 45), MathUtil.gcd(3L * (1L << 50), 9L * (1L << 45)));
		Assert.AreEqual(1L << 45, MathUtil.gcd(1L << 45, long.MinValue));

		Assert.AreEqual(long.MaxValue, MathUtil.gcd(long.MaxValue, 0L));
		Assert.AreEqual(long.MaxValue, MathUtil.gcd(-long.MaxValue, 0L));
		Assert.AreEqual(1, MathUtil.gcd(60247241209L, 153092023L));

		Assert.AreEqual(long.MinValue, MathUtil.gcd(long.MinValue, 0));
		Assert.AreEqual(long.MinValue, MathUtil.gcd(0, long.MinValue));
		Assert.AreEqual(long.MinValue, MathUtil.gcd(long.MinValue, long.MinValue));
	  }

	  public virtual void TestAcoshMethod()
	  {
		// acosh(NaN) == NaN
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(double.NaN)));
		// acosh(1) == +0
		Assert.AreEqual(0, double.doubleToLongBits(MathUtil.acosh(1D)));
		// acosh(POSITIVE_INFINITY) == POSITIVE_INFINITY
		Assert.AreEqual(double.doubleToLongBits(double.PositiveInfinity), double.doubleToLongBits(MathUtil.acosh(double.PositiveInfinity)));
		// acosh(x) : x < 1 == NaN
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(0.9D))); // x < 1
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(0D))); // x == 0
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(-0D))); // x == -0
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(-0.9D))); // x < 0
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(-1D))); // x == -1
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(-10D))); // x < -1
		Assert.IsTrue(double.IsNaN(MathUtil.acosh(double.NegativeInfinity))); // x == -Inf

		double epsilon = 0.000001;
		Assert.AreEqual(0, MathUtil.acosh(1), epsilon);
		Assert.AreEqual(1.5667992369724109, MathUtil.acosh(2.5), epsilon);
		Assert.AreEqual(14.719378760739708, MathUtil.acosh(1234567.89), epsilon);
	  }

	  public virtual void TestAsinhMethod()
	  {

		// asinh(NaN) == NaN
		Assert.IsTrue(double.IsNaN(MathUtil.asinh(double.NaN)));
		// asinh(+0) == +0
		Assert.AreEqual(0, double.doubleToLongBits(MathUtil.asinh(0D)));
		// asinh(-0) == -0
		Assert.AreEqual(double.doubleToLongBits(-0D), double.doubleToLongBits(MathUtil.asinh(-0D)));
		// asinh(POSITIVE_INFINITY) == POSITIVE_INFINITY
		Assert.AreEqual(double.doubleToLongBits(double.PositiveInfinity), double.doubleToLongBits(MathUtil.asinh(double.PositiveInfinity)));
		// asinh(NEGATIVE_INFINITY) == NEGATIVE_INFINITY
		Assert.AreEqual(double.doubleToLongBits(double.NegativeInfinity), double.doubleToLongBits(MathUtil.asinh(double.NegativeInfinity)));

		double epsilon = 0.000001;
		Assert.AreEqual(-14.719378760740035, MathUtil.asinh(-1234567.89), epsilon);
		Assert.AreEqual(-1.6472311463710958, MathUtil.asinh(-2.5), epsilon);
		Assert.AreEqual(-0.8813735870195429, MathUtil.asinh(-1), epsilon);
		Assert.AreEqual(0, MathUtil.asinh(0), 0);
		Assert.AreEqual(0.8813735870195429, MathUtil.asinh(1), epsilon);
		Assert.AreEqual(1.6472311463710958, MathUtil.asinh(2.5), epsilon);
		Assert.AreEqual(14.719378760740035, MathUtil.asinh(1234567.89), epsilon);
	  }

	  public virtual void TestAtanhMethod()
	  {
		// atanh(NaN) == NaN
		Assert.IsTrue(double.IsNaN(MathUtil.atanh(double.NaN)));
		// atanh(+0) == +0
		Assert.AreEqual(0, double.doubleToLongBits(MathUtil.atanh(0D)));
		// atanh(-0) == -0
		Assert.AreEqual(double.doubleToLongBits(-0D), double.doubleToLongBits(MathUtil.atanh(-0D)));
		// atanh(1) == POSITIVE_INFINITY
		Assert.AreEqual(double.doubleToLongBits(double.PositiveInfinity), double.doubleToLongBits(MathUtil.atanh(1D)));
		// atanh(-1) == NEGATIVE_INFINITY
		Assert.AreEqual(double.doubleToLongBits(double.NegativeInfinity), double.doubleToLongBits(MathUtil.atanh(-1D)));
		// atanh(x) : Math.abs(x) > 1 == NaN
		Assert.IsTrue(double.IsNaN(MathUtil.atanh(1.1D))); // x > 1
		Assert.IsTrue(double.IsNaN(MathUtil.atanh(double.PositiveInfinity))); // x == Inf
		Assert.IsTrue(double.IsNaN(MathUtil.atanh(-1.1D))); // x < -1
		Assert.IsTrue(double.IsNaN(MathUtil.atanh(double.NegativeInfinity))); // x == -Inf

		double epsilon = 0.000001;
		Assert.AreEqual(double.NegativeInfinity, MathUtil.atanh(-1), 0);
		Assert.AreEqual(-0.5493061443340549, MathUtil.atanh(-0.5), epsilon);
		Assert.AreEqual(0, MathUtil.atanh(0), 0);
		Assert.AreEqual(0.5493061443340549, MathUtil.atanh(0.5), epsilon);
		Assert.AreEqual(double.PositiveInfinity, MathUtil.atanh(1), 0);
	  }

	}

}