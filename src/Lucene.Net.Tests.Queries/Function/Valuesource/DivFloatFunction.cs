/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Function to divide "a" by "b"</summary>
	public class DivFloatFunction : DualFloatFunction
	{
		/// <param name="a">the numerator.</param>
		/// <param name="b">the denominator.</param>
		public DivFloatFunction(ValueSource a, ValueSource b) : base(a, b)
		{
		}

		protected internal override string Name()
		{
			return "div";
		}

		protected internal override float Func(int doc, FunctionValues aVals, FunctionValues
			 bVals)
		{
			return aVals.FloatVal(doc) / bVals.FloatVal(doc);
		}
	}
}
