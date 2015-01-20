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
	/// <summary><code>SumFloatFunction</code> returns the sum of it's components.</summary>
	/// <remarks><code>SumFloatFunction</code> returns the sum of it's components.</remarks>
	public class SumFloatFunction : MultiFloatFunction
	{
		public SumFloatFunction(ValueSource[] sources) : base(sources)
		{
		}

		protected internal override string Name()
		{
			return "sum";
		}

		protected internal override float Func(int doc, FunctionValues[] valsArr)
		{
			float val = 0.0f;
			foreach (FunctionValues vals in valsArr)
			{
				val += vals.FloatVal(doc);
			}
			return val;
		}
	}
}
