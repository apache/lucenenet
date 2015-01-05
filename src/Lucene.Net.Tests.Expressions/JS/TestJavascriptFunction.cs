/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Expressions;
using Org.Apache.Lucene.Expressions.JS;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Expressions.JS
{
	public class TestJavascriptFunction : LuceneTestCase
	{
		private static double DELTA = 0.0000001;

		/// <exception cref="System.Exception"></exception>
		private void AssertEvaluatesTo(string expression, double expected)
		{
			Expression evaluator = JavascriptCompiler.Compile(expression);
			double actual = evaluator.Evaluate(0, null);
			NUnit.Framework.Assert.AreEqual(expected, actual, DELTA);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAbsMethod()
		{
			AssertEvaluatesTo("abs(0)", 0);
			AssertEvaluatesTo("abs(119)", 119);
			AssertEvaluatesTo("abs(119)", 119);
			AssertEvaluatesTo("abs(1)", 1);
			AssertEvaluatesTo("abs(-1)", 1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAcosMethod()
		{
			AssertEvaluatesTo("acos(-1)", Math.PI);
			AssertEvaluatesTo("acos(-0.8660254)", Math.PI * 5 / 6);
			AssertEvaluatesTo("acos(-0.7071068)", Math.PI * 3 / 4);
			AssertEvaluatesTo("acos(-0.5)", Math.PI * 2 / 3);
			AssertEvaluatesTo("acos(0)", Math.PI / 2);
			AssertEvaluatesTo("acos(0.5)", Math.PI / 3);
			AssertEvaluatesTo("acos(0.7071068)", Math.PI / 4);
			AssertEvaluatesTo("acos(0.8660254)", Math.PI / 6);
			AssertEvaluatesTo("acos(1)", 0);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAcoshMethod()
		{
			AssertEvaluatesTo("acosh(1)", 0);
			AssertEvaluatesTo("acosh(2.5)", 1.5667992369724109);
			AssertEvaluatesTo("acosh(1234567.89)", 14.719378760739708);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAsinMethod()
		{
			AssertEvaluatesTo("asin(-1)", -Math.PI / 2);
			AssertEvaluatesTo("asin(-0.8660254)", -Math.PI / 3);
			AssertEvaluatesTo("asin(-0.7071068)", -Math.PI / 4);
			AssertEvaluatesTo("asin(-0.5)", -Math.PI / 6);
			AssertEvaluatesTo("asin(0)", 0);
			AssertEvaluatesTo("asin(0.5)", Math.PI / 6);
			AssertEvaluatesTo("asin(0.7071068)", Math.PI / 4);
			AssertEvaluatesTo("asin(0.8660254)", Math.PI / 3);
			AssertEvaluatesTo("asin(1)", Math.PI / 2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAsinhMethod()
		{
			AssertEvaluatesTo("asinh(-1234567.89)", -14.719378760740035);
			AssertEvaluatesTo("asinh(-2.5)", -1.6472311463710958);
			AssertEvaluatesTo("asinh(-1)", -0.8813735870195429);
			AssertEvaluatesTo("asinh(0)", 0);
			AssertEvaluatesTo("asinh(1)", 0.8813735870195429);
			AssertEvaluatesTo("asinh(2.5)", 1.6472311463710958);
			AssertEvaluatesTo("asinh(1234567.89)", 14.719378760740035);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAtanMethod()
		{
			AssertEvaluatesTo("atan(-1.732050808)", -Math.PI / 3);
			AssertEvaluatesTo("atan(-1)", -Math.PI / 4);
			AssertEvaluatesTo("atan(-0.577350269)", -Math.PI / 6);
			AssertEvaluatesTo("atan(0)", 0);
			AssertEvaluatesTo("atan(0.577350269)", Math.PI / 6);
			AssertEvaluatesTo("atan(1)", Math.PI / 4);
			AssertEvaluatesTo("atan(1.732050808)", Math.PI / 3);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAtan2Method()
		{
			AssertEvaluatesTo("atan2(+0,+0)", +0.0);
			AssertEvaluatesTo("atan2(+0,-0)", +Math.PI);
			AssertEvaluatesTo("atan2(-0,+0)", -0.0);
			AssertEvaluatesTo("atan2(-0,-0)", -Math.PI);
			AssertEvaluatesTo("atan2(2,2)", Math.PI / 4);
			AssertEvaluatesTo("atan2(-2,2)", -Math.PI / 4);
			AssertEvaluatesTo("atan2(2,-2)", Math.PI * 3 / 4);
			AssertEvaluatesTo("atan2(-2,-2)", -Math.PI * 3 / 4);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestAtanhMethod()
		{
			AssertEvaluatesTo("atanh(-1)", double.NegativeInfinity);
			AssertEvaluatesTo("atanh(-0.5)", -0.5493061443340549);
			AssertEvaluatesTo("atanh(0)", 0);
			AssertEvaluatesTo("atanh(0.5)", 0.5493061443340549);
			AssertEvaluatesTo("atanh(1)", double.PositiveInfinity);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCeilMethod()
		{
			AssertEvaluatesTo("ceil(0)", 0);
			AssertEvaluatesTo("ceil(0.1)", 1);
			AssertEvaluatesTo("ceil(0.9)", 1);
			AssertEvaluatesTo("ceil(25.2)", 26);
			AssertEvaluatesTo("ceil(-0.1)", 0);
			AssertEvaluatesTo("ceil(-0.9)", 0);
			AssertEvaluatesTo("ceil(-1.1)", -1);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCosMethod()
		{
			AssertEvaluatesTo("cos(0)", 1);
			AssertEvaluatesTo("cos(" + Math.PI / 2 + ")", 0);
			AssertEvaluatesTo("cos(" + -Math.PI / 2 + ")", 0);
			AssertEvaluatesTo("cos(" + Math.PI / 4 + ")", 0.7071068);
			AssertEvaluatesTo("cos(" + -Math.PI / 4 + ")", 0.7071068);
			AssertEvaluatesTo("cos(" + Math.PI * 2 / 3 + ")", -0.5);
			AssertEvaluatesTo("cos(" + -Math.PI * 2 / 3 + ")", -0.5);
			AssertEvaluatesTo("cos(" + Math.PI / 6 + ")", 0.8660254);
			AssertEvaluatesTo("cos(" + -Math.PI / 6 + ")", 0.8660254);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestCoshMethod()
		{
			AssertEvaluatesTo("cosh(0)", 1);
			AssertEvaluatesTo("cosh(-1)", 1.5430806348152437);
			AssertEvaluatesTo("cosh(1)", 1.5430806348152437);
			AssertEvaluatesTo("cosh(-0.5)", 1.1276259652063807);
			AssertEvaluatesTo("cosh(0.5)", 1.1276259652063807);
			AssertEvaluatesTo("cosh(-12.3456789)", 114982.09728671524);
			AssertEvaluatesTo("cosh(12.3456789)", 114982.09728671524);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestExpMethod()
		{
			AssertEvaluatesTo("exp(0)", 1);
			AssertEvaluatesTo("exp(-1)", 0.36787944117);
			AssertEvaluatesTo("exp(1)", 2.71828182846);
			AssertEvaluatesTo("exp(-0.5)", 0.60653065971);
			AssertEvaluatesTo("exp(0.5)", 1.6487212707);
			AssertEvaluatesTo("exp(-12.3456789)", 0.0000043485);
			AssertEvaluatesTo("exp(12.3456789)", 229964.194569);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestFloorMethod()
		{
			AssertEvaluatesTo("floor(0)", 0);
			AssertEvaluatesTo("floor(0.1)", 0);
			AssertEvaluatesTo("floor(0.9)", 0);
			AssertEvaluatesTo("floor(25.2)", 25);
			AssertEvaluatesTo("floor(-0.1)", -1);
			AssertEvaluatesTo("floor(-0.9)", -1);
			AssertEvaluatesTo("floor(-1.1)", -2);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestHaversinMethod()
		{
			AssertEvaluatesTo("haversin(40.7143528,-74.0059731,40.759011,-73.9844722)", 5.284299568309
				);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLnMethod()
		{
			AssertEvaluatesTo("ln(0)", double.NegativeInfinity);
			AssertEvaluatesTo("ln(" + Math.E + ")", 1);
			AssertEvaluatesTo("ln(-1)", double.NaN);
			AssertEvaluatesTo("ln(1)", 0);
			AssertEvaluatesTo("ln(0.5)", -0.69314718056);
			AssertEvaluatesTo("ln(12.3456789)", 2.51330611521);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLog10Method()
		{
			AssertEvaluatesTo("log10(0)", double.NegativeInfinity);
			AssertEvaluatesTo("log10(1)", 0);
			AssertEvaluatesTo("log10(-1)", double.NaN);
			AssertEvaluatesTo("log10(0.5)", -0.3010299956639812);
			AssertEvaluatesTo("log10(12.3456789)", 1.0915149771692705);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestLognMethod()
		{
			AssertEvaluatesTo("logn(2, 0)", double.NegativeInfinity);
			AssertEvaluatesTo("logn(2, 1)", 0);
			AssertEvaluatesTo("logn(2, -1)", double.NaN);
			AssertEvaluatesTo("logn(2, 0.5)", -1);
			AssertEvaluatesTo("logn(2, 12.3456789)", 3.6259342686489378);
			AssertEvaluatesTo("logn(2.5, 0)", double.NegativeInfinity);
			AssertEvaluatesTo("logn(2.5, 1)", 0);
			AssertEvaluatesTo("logn(2.5, -1)", double.NaN);
			AssertEvaluatesTo("logn(2.5, 0.5)", -0.75647079736603);
			AssertEvaluatesTo("logn(2.5, 12.3456789)", 2.7429133874016745);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMaxMethod()
		{
			AssertEvaluatesTo("max(0, 0)", 0);
			AssertEvaluatesTo("max(1, 0)", 1);
			AssertEvaluatesTo("max(0, -1)", 0);
			AssertEvaluatesTo("max(-1, 0)", 0);
			AssertEvaluatesTo("max(25, 23)", 25);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestMinMethod()
		{
			AssertEvaluatesTo("min(0, 0)", 0);
			AssertEvaluatesTo("min(1, 0)", 0);
			AssertEvaluatesTo("min(0, -1)", -1);
			AssertEvaluatesTo("min(-1, 0)", -1);
			AssertEvaluatesTo("min(25, 23)", 23);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestPowMethod()
		{
			AssertEvaluatesTo("pow(0, 0)", 1);
			AssertEvaluatesTo("pow(0.1, 2)", 0.01);
			AssertEvaluatesTo("pow(0.9, -1)", 1.1111111111111112);
			AssertEvaluatesTo("pow(2.2, -2.5)", 0.13929749224447147);
			AssertEvaluatesTo("pow(5, 3)", 125);
			AssertEvaluatesTo("pow(-0.9, 5)", -0.59049);
			AssertEvaluatesTo("pow(-1.1, 2)", 1.21);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSinMethod()
		{
			AssertEvaluatesTo("sin(0)", 0);
			AssertEvaluatesTo("sin(" + Math.PI / 2 + ")", 1);
			AssertEvaluatesTo("sin(" + -Math.PI / 2 + ")", -1);
			AssertEvaluatesTo("sin(" + Math.PI / 4 + ")", 0.7071068);
			AssertEvaluatesTo("sin(" + -Math.PI / 4 + ")", -0.7071068);
			AssertEvaluatesTo("sin(" + Math.PI * 2 / 3 + ")", 0.8660254);
			AssertEvaluatesTo("sin(" + -Math.PI * 2 / 3 + ")", -0.8660254);
			AssertEvaluatesTo("sin(" + Math.PI / 6 + ")", 0.5);
			AssertEvaluatesTo("sin(" + -Math.PI / 6 + ")", -0.5);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSinhMethod()
		{
			AssertEvaluatesTo("sinh(0)", 0);
			AssertEvaluatesTo("sinh(-1)", -1.1752011936438014);
			AssertEvaluatesTo("sinh(1)", 1.1752011936438014);
			AssertEvaluatesTo("sinh(-0.5)", -0.52109530549);
			AssertEvaluatesTo("sinh(0.5)", 0.52109530549);
			AssertEvaluatesTo("sinh(-12.3456789)", -114982.09728236674);
			AssertEvaluatesTo("sinh(12.3456789)", 114982.09728236674);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestSqrtMethod()
		{
			AssertEvaluatesTo("sqrt(0)", 0);
			AssertEvaluatesTo("sqrt(-1)", double.NaN);
			AssertEvaluatesTo("sqrt(0.49)", 0.7);
			AssertEvaluatesTo("sqrt(49)", 7);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTanMethod()
		{
			AssertEvaluatesTo("tan(0)", 0);
			AssertEvaluatesTo("tan(-1)", -1.55740772465);
			AssertEvaluatesTo("tan(1)", 1.55740772465);
			AssertEvaluatesTo("tan(-0.5)", -0.54630248984);
			AssertEvaluatesTo("tan(0.5)", 0.54630248984);
			AssertEvaluatesTo("tan(-1.3)", -3.60210244797);
			AssertEvaluatesTo("tan(1.3)", 3.60210244797);
		}

		/// <exception cref="System.Exception"></exception>
		public virtual void TestTanhMethod()
		{
			AssertEvaluatesTo("tanh(0)", 0);
			AssertEvaluatesTo("tanh(-1)", -0.76159415595);
			AssertEvaluatesTo("tanh(1)", 0.76159415595);
			AssertEvaluatesTo("tanh(-0.5)", -0.46211715726);
			AssertEvaluatesTo("tanh(0.5)", 0.46211715726);
			AssertEvaluatesTo("tanh(-12.3456789)", -0.99999999996);
			AssertEvaluatesTo("tanh(12.3456789)", 0.99999999996);
		}
	}
}
