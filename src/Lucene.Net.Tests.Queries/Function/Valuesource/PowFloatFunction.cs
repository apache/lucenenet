/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Function to raise the base "a" to the power "b"</summary>
	public class PowFloatFunction : DualFloatFunction
	{
		/// <param name="a">the base.</param>
		/// <param name="b">the exponent.</param>
		public PowFloatFunction(ValueSource a, ValueSource b) : base(a, b)
		{
		}

		protected internal override string Name()
		{
			return "pow";
		}

		protected internal override float Func(int doc, FunctionValues aVals, FunctionValues
			 bVals)
		{
			return (float)Math.Pow(aVals.FloatVal(doc), bVals.FloatVal(doc));
		}
	}
}
