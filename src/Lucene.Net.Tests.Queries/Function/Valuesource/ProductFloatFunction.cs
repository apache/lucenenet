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
	/// <summary><code>ProductFloatFunction</code> returns the product of it's components.
	/// 	</summary>
	/// <remarks><code>ProductFloatFunction</code> returns the product of it's components.
	/// 	</remarks>
	public class ProductFloatFunction : MultiFloatFunction
	{
		public ProductFloatFunction(ValueSource[] sources) : base(sources)
		{
		}

		protected internal override string Name()
		{
			return "product";
		}

		protected internal override float Func(int doc, FunctionValues[] valsArr)
		{
			float val = 1.0f;
			foreach (FunctionValues vals in valsArr)
			{
				val *= vals.FloatVal(doc);
			}
			return val;
		}
	}
}
