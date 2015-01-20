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
	/// <summary><code>MaxFloatFunction</code> returns the max of it's components.</summary>
	/// <remarks><code>MaxFloatFunction</code> returns the max of it's components.</remarks>
	public class MaxFloatFunction : MultiFloatFunction
	{
		public MaxFloatFunction(ValueSource[] sources) : base(sources)
		{
		}

		protected internal override string Name()
		{
			return "max";
		}

		protected internal override float Func(int doc, FunctionValues[] valsArr)
		{
			if (valsArr.Length == 0)
			{
				return 0.0f;
			}
			float val = float.NegativeInfinity;
			foreach (FunctionValues vals in valsArr)
			{
				val = Math.Max(vals.FloatVal(doc), val);
			}
			return val;
		}
	}
}
