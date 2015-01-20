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
	/// <summary><code>MinFloatFunction</code> returns the min of it's components.</summary>
	/// <remarks><code>MinFloatFunction</code> returns the min of it's components.</remarks>
	public class MinFloatFunction : MultiFloatFunction
	{
		public MinFloatFunction(ValueSource[] sources) : base(sources)
		{
		}

		protected internal override string Name()
		{
			return "min";
		}

		protected internal override float Func(int doc, FunctionValues[] valsArr)
		{
			if (valsArr.Length == 0)
			{
				return 0.0f;
			}
			float val = float.PositiveInfinity;
			foreach (FunctionValues vals in valsArr)
			{
				val = Math.Min(vals.FloatVal(doc), val);
			}
			return val;
		}
	}
}
