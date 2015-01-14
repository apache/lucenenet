/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Expressions;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions.JS
{
	public class TestJavascriptOperations : LuceneTestCase
	{
		/// <exception cref="System.Exception"></exception>
		private void AssertEvaluatesTo(string expression, long expected)
		{
			Expression evaluator = JavascriptCompiler.Compile(expression);
			long actual = (long)evaluator.Evaluate(0, null);
			AreEqual(expected, actual);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNegationOperation()
		{
			AssertEvaluatesTo("-1", -1);
			AssertEvaluatesTo("--1", 1);
			AssertEvaluatesTo("-(-1)", 1);
			AssertEvaluatesTo("-0", 0);
			AssertEvaluatesTo("--0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAddOperation()
		{
			AssertEvaluatesTo("1+1", 2);
			AssertEvaluatesTo("1+0.5+0.5", 2);
			AssertEvaluatesTo("5+10", 15);
			AssertEvaluatesTo("1+1+2", 4);
			AssertEvaluatesTo("(1+1)+2", 4);
			AssertEvaluatesTo("1+(1+2)", 4);
			AssertEvaluatesTo("0+1", 1);
			AssertEvaluatesTo("1+0", 1);
			AssertEvaluatesTo("0+0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSubtractOperation()
		{
			AssertEvaluatesTo("1-1", 0);
			AssertEvaluatesTo("5-10", -5);
			AssertEvaluatesTo("1-0.5-0.5", 0);
			AssertEvaluatesTo("1-1-2", -2);
			AssertEvaluatesTo("(1-1)-2", -2);
			AssertEvaluatesTo("1-(1-2)", 2);
			AssertEvaluatesTo("0-1", -1);
			AssertEvaluatesTo("1-0", 1);
			AssertEvaluatesTo("0-0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMultiplyOperation()
		{
			AssertEvaluatesTo("1*1", 1);
			AssertEvaluatesTo("5*10", 50);
			AssertEvaluatesTo("50*0.1", 5);
			AssertEvaluatesTo("1*1*2", 2);
			AssertEvaluatesTo("(1*1)*2", 2);
			AssertEvaluatesTo("1*(1*2)", 2);
			AssertEvaluatesTo("10*0", 0);
			AssertEvaluatesTo("0*0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDivisionOperation()
		{
			AssertEvaluatesTo("1*1", 1);
			AssertEvaluatesTo("10/5", 2);
			AssertEvaluatesTo("10/0.5", 20);
			AssertEvaluatesTo("10/5/2", 1);
			AssertEvaluatesTo("(27/9)/3", 1);
			AssertEvaluatesTo("27/(9/3)", 9);
			AssertEvaluatesTo("1/0", 9223372036854775807L);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestModuloOperation()
		{
			AssertEvaluatesTo("1%1", 0);
			AssertEvaluatesTo("10%3", 1);
			AssertEvaluatesTo("10%3%2", 1);
			AssertEvaluatesTo("(27%10)%4", 3);
			AssertEvaluatesTo("27%(9%5)", 3);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLessThanOperation()
		{
			AssertEvaluatesTo("1 < 1", 0);
			AssertEvaluatesTo("2 < 1", 0);
			AssertEvaluatesTo("1 < 2", 1);
			AssertEvaluatesTo("2 < 1 < 3", 1);
			AssertEvaluatesTo("2 < (1 < 3)", 0);
			AssertEvaluatesTo("(2 < 1) < 1", 1);
			AssertEvaluatesTo("-1 < -2", 0);
			AssertEvaluatesTo("-1 < 0", 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLessThanEqualsOperation()
		{
			AssertEvaluatesTo("1 <= 1", 1);
			AssertEvaluatesTo("2 <= 1", 0);
			AssertEvaluatesTo("1 <= 2", 1);
			AssertEvaluatesTo("1 <= 1 <= 0", 0);
			AssertEvaluatesTo("-1 <= -1", 1);
			AssertEvaluatesTo("-1 <= 0", 1);
			AssertEvaluatesTo("-1 <= -2", 0);
			AssertEvaluatesTo("-1 <= 0", 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGreaterThanOperation()
		{
			AssertEvaluatesTo("1 > 1", 0);
			AssertEvaluatesTo("2 > 1", 1);
			AssertEvaluatesTo("1 > 2", 0);
			AssertEvaluatesTo("2 > 1 > 3", 0);
			AssertEvaluatesTo("2 > (1 > 3)", 1);
			AssertEvaluatesTo("(2 > 1) > 1", 0);
			AssertEvaluatesTo("-1 > -2", 1);
			AssertEvaluatesTo("-1 > 0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestGreaterThanEqualsOperation()
		{
			AssertEvaluatesTo("1 >= 1", 1);
			AssertEvaluatesTo("2 >= 1", 1);
			AssertEvaluatesTo("1 >= 2", 0);
			AssertEvaluatesTo("1 >= 1 >= 0", 1);
			AssertEvaluatesTo("-1 >= -1", 1);
			AssertEvaluatesTo("-1 >= 0", 0);
			AssertEvaluatesTo("-1 >= -2", 1);
			AssertEvaluatesTo("-1 >= 0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestEqualsOperation()
		{
			AssertEvaluatesTo("1 == 1", 1);
			AssertEvaluatesTo("0 == 0", 1);
			AssertEvaluatesTo("-1 == -1", 1);
			AssertEvaluatesTo("1.1 == 1.1", 1);
			AssertEvaluatesTo("0.9 == 0.9", 1);
			AssertEvaluatesTo("-0 == 0", 1);
			AssertEvaluatesTo("0 == 1", 0);
			AssertEvaluatesTo("1 == 2", 0);
			AssertEvaluatesTo("-1 == 1", 0);
			AssertEvaluatesTo("-1 == 0", 0);
			AssertEvaluatesTo("-2 == 1", 0);
			AssertEvaluatesTo("-2 == -1", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestNotEqualsOperation()
		{
			AssertEvaluatesTo("1 != 1", 0);
			AssertEvaluatesTo("0 != 0", 0);
			AssertEvaluatesTo("-1 != -1", 0);
			AssertEvaluatesTo("1.1 != 1.1", 0);
			AssertEvaluatesTo("0.9 != 0.9", 0);
			AssertEvaluatesTo("-0 != 0", 0);
			AssertEvaluatesTo("0 != 1", 1);
			AssertEvaluatesTo("1 != 2", 1);
			AssertEvaluatesTo("-1 != 1", 1);
			AssertEvaluatesTo("-1 != 0", 1);
			AssertEvaluatesTo("-2 != 1", 1);
			AssertEvaluatesTo("-2 != -1", 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoolNotOperation()
		{
			AssertEvaluatesTo("!1", 0);
			AssertEvaluatesTo("!!1", 1);
			AssertEvaluatesTo("!0", 1);
			AssertEvaluatesTo("!!0", 0);
			AssertEvaluatesTo("!-1", 0);
			AssertEvaluatesTo("!2", 0);
			AssertEvaluatesTo("!-2", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoolAndOperation()
		{
			AssertEvaluatesTo("1 && 1", 1);
			AssertEvaluatesTo("1 && 0", 0);
			AssertEvaluatesTo("0 && 1", 0);
			AssertEvaluatesTo("0 && 0", 0);
			AssertEvaluatesTo("-1 && -1", 1);
			AssertEvaluatesTo("-1 && 0", 0);
			AssertEvaluatesTo("0 && -1", 0);
			AssertEvaluatesTo("-0 && -0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBoolOrOperation()
		{
			AssertEvaluatesTo("1 || 1", 1);
			AssertEvaluatesTo("1 || 0", 1);
			AssertEvaluatesTo("0 || 1", 1);
			AssertEvaluatesTo("0 || 0", 0);
			AssertEvaluatesTo("-1 || -1", 1);
			AssertEvaluatesTo("-1 || 0", 1);
			AssertEvaluatesTo("0 || -1", 1);
			AssertEvaluatesTo("-0 || -0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestConditionalOperation()
		{
			AssertEvaluatesTo("1 ? 2 : 3", 2);
			AssertEvaluatesTo("-1 ? 2 : 3", 2);
			AssertEvaluatesTo("0 ? 2 : 3", 3);
			AssertEvaluatesTo("1 ? 2 ? 3 : 4 : 5", 3);
			AssertEvaluatesTo("0 ? 2 ? 3 : 4 : 5", 5);
			AssertEvaluatesTo("1 ? 0 ? 3 : 4 : 5", 4);
			AssertEvaluatesTo("1 ? 2 : 3 ? 4 : 5", 2);
			AssertEvaluatesTo("0 ? 2 : 3 ? 4 : 5", 4);
			AssertEvaluatesTo("0 ? 2 : 0 ? 4 : 5", 5);
			AssertEvaluatesTo("(1 ? 1 : 0) ? 3 : 4", 3);
			AssertEvaluatesTo("(0 ? 1 : 0) ? 3 : 4", 4);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitShiftLeft()
		{
			AssertEvaluatesTo("1 << 1", 2);
			AssertEvaluatesTo("2 << 1", 4);
			AssertEvaluatesTo("-1 << 31", -2147483648);
			AssertEvaluatesTo("3 << 5", 96);
			AssertEvaluatesTo("-5 << 3", -40);
			AssertEvaluatesTo("4195 << 7", 536960);
			AssertEvaluatesTo("4195 << 66", 16780);
			AssertEvaluatesTo("4195 << 6", 268480);
			AssertEvaluatesTo("4195 << 70", 268480);
			AssertEvaluatesTo("-4195 << 70", -268480);
			AssertEvaluatesTo("-15 << 62", 4611686018427387904L);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitShiftRight()
		{
			AssertEvaluatesTo("1 >> 1", 0);
			AssertEvaluatesTo("2 >> 1", 1);
			AssertEvaluatesTo("-1 >> 5", -1);
			AssertEvaluatesTo("-2 >> 30", -1);
			AssertEvaluatesTo("-5 >> 1", -3);
			AssertEvaluatesTo("536960 >> 7", 4195);
			AssertEvaluatesTo("16780 >> 66", 4195);
			AssertEvaluatesTo("268480 >> 6", 4195);
			AssertEvaluatesTo("268480 >> 70", 4195);
			AssertEvaluatesTo("-268480 >> 70", -4195);
			AssertEvaluatesTo("-2147483646 >> 1", -1073741823);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitShiftRightUnsigned()
		{
			AssertEvaluatesTo("1 >>> 1", 0);
			AssertEvaluatesTo("2 >>> 1", 1);
			AssertEvaluatesTo("-1 >>> 37", 134217727);
			AssertEvaluatesTo("-2 >>> 62", 3);
			AssertEvaluatesTo("-5 >>> 33", 2147483647);
			AssertEvaluatesTo("536960 >>> 7", 4195);
			AssertEvaluatesTo("16780 >>> 66", 4195);
			AssertEvaluatesTo("268480 >>> 6", 4195);
			AssertEvaluatesTo("268480 >>> 70", 4195);
			AssertEvaluatesTo("-268480 >>> 102", 67108863);
			AssertEvaluatesTo("2147483648 >>> 1", 1073741824);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitwiseAnd()
		{
			AssertEvaluatesTo("4 & 4", 4);
			AssertEvaluatesTo("3 & 2", 2);
			AssertEvaluatesTo("7 & 3", 3);
			AssertEvaluatesTo("-1 & -1", -1);
			AssertEvaluatesTo("-1 & 25", 25);
			AssertEvaluatesTo("3 & 7", 3);
			AssertEvaluatesTo("0 & 1", 0);
			AssertEvaluatesTo("1 & 0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitwiseOr()
		{
			AssertEvaluatesTo("4 | 4", 4);
			AssertEvaluatesTo("5 | 2", 7);
			AssertEvaluatesTo("7 | 3", 7);
			AssertEvaluatesTo("-1 | -5", -1);
			AssertEvaluatesTo("-1 | 25", -1);
			AssertEvaluatesTo("-100 | 15", -97);
			AssertEvaluatesTo("0 | 1", 1);
			AssertEvaluatesTo("1 | 0", 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitwiseXor()
		{
			AssertEvaluatesTo("4 ^ 4", 0);
			AssertEvaluatesTo("5 ^ 2", 7);
			AssertEvaluatesTo("15 ^ 3", 12);
			AssertEvaluatesTo("-1 ^ -5", 4);
			AssertEvaluatesTo("-1 ^ 25", -26);
			AssertEvaluatesTo("-100 ^ 15", -109);
			AssertEvaluatesTo("0 ^ 1", 1);
			AssertEvaluatesTo("1 ^ 0", 1);
			AssertEvaluatesTo("0 ^ 0", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestBitwiseNot()
		{
			AssertEvaluatesTo("~-5", 4);
			AssertEvaluatesTo("~25", -26);
			AssertEvaluatesTo("~0", -1);
			AssertEvaluatesTo("~-1", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestDecimalConst()
		{
			AssertEvaluatesTo("0", 0);
			AssertEvaluatesTo("1", 1);
			AssertEvaluatesTo("123456789", 123456789);
			AssertEvaluatesTo("5.6E2", 560);
			AssertEvaluatesTo("5.6E+2", 560);
			AssertEvaluatesTo("500E-2", 5);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHexConst()
		{
			AssertEvaluatesTo("0x0", 0);
			AssertEvaluatesTo("0x1", 1);
			AssertEvaluatesTo("0xF", 15);
			AssertEvaluatesTo("0x1234ABCDEF", 78193085935L);
			AssertEvaluatesTo("1 << 0x1", 1 << unchecked((int)(0x1)));
			AssertEvaluatesTo("1 << 0xA", 1 << unchecked((int)(0xA)));
			AssertEvaluatesTo("0x1 << 2", unchecked((int)(0x1)) << 2);
			AssertEvaluatesTo("0xA << 2", unchecked((int)(0xA)) << 2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHexConst2()
		{
			AssertEvaluatesTo("0X0", 0);
			AssertEvaluatesTo("0X1", 1);
			AssertEvaluatesTo("0XF", 15);
			AssertEvaluatesTo("0X1234ABCDEF", 78193085935L);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestOctalConst()
		{
			AssertEvaluatesTo("00", 0);
			AssertEvaluatesTo("01", 1);
			AssertEvaluatesTo("010", 8);
			AssertEvaluatesTo("0123456777", 21913087);
			AssertEvaluatesTo("1 << 01", 1 << 0x1);
			AssertEvaluatesTo("1 << 010", 1 << 0x8);
			AssertEvaluatesTo("01 << 2", 0x1 << 2);
			AssertEvaluatesTo("010 << 2", 0x8 << 2);
		}
	}
}
