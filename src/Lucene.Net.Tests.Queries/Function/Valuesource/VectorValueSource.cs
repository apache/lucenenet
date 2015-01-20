/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// Converts individual ValueSource instances to leverage the FunctionValues *Val functions that work with multiple values,
	/// i.e.
	/// </summary>
	/// <remarks>
	/// Converts individual ValueSource instances to leverage the FunctionValues *Val functions that work with multiple values,
	/// i.e.
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues.DoubleVal(int, double[])
	/// 	">Org.Apache.Lucene.Queries.Function.FunctionValues.DoubleVal(int, double[])</see>
	/// </remarks>
	public class VectorValueSource : MultiValueSource
	{
		protected internal readonly IList<ValueSource> sources;

		public VectorValueSource(IList<ValueSource> sources)
		{
			//Not crazy about the name, but...
			this.sources = sources;
		}

		public virtual IList<ValueSource> GetSources()
		{
			return sources;
		}

		public override int Dimension()
		{
			return sources.Count;
		}

		public virtual string Name()
		{
			return "vector";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			int size = sources.Count;
			// special-case x,y and lat,lon since it's so common
			if (size == 2)
			{
				FunctionValues x = sources[0].GetValues(context, readerContext);
				FunctionValues y = sources[1].GetValues(context, readerContext);
				return new _FunctionValues_63(this, x, y);
			}
			FunctionValues[] valsArr = new FunctionValues[size];
			for (int i = 0; i < size; i++)
			{
				valsArr[i] = sources[i].GetValues(context, readerContext);
			}
			return new _FunctionValues_113(this, valsArr);
		}

		private sealed class _FunctionValues_63 : FunctionValues
		{
			public _FunctionValues_63(VectorValueSource _enclosing, FunctionValues x, FunctionValues
				 y)
			{
				this._enclosing = _enclosing;
				this.x = x;
				this.y = y;
			}

			public override void ByteVal(int doc, byte[] vals)
			{
				vals[0] = x.ByteVal(doc);
				vals[1] = y.ByteVal(doc);
			}

			public override void ShortVal(int doc, short[] vals)
			{
				vals[0] = x.ShortVal(doc);
				vals[1] = y.ShortVal(doc);
			}

			public override void IntVal(int doc, int[] vals)
			{
				vals[0] = x.IntVal(doc);
				vals[1] = y.IntVal(doc);
			}

			public override void LongVal(int doc, long[] vals)
			{
				vals[0] = x.LongVal(doc);
				vals[1] = y.LongVal(doc);
			}

			public override void FloatVal(int doc, float[] vals)
			{
				vals[0] = x.FloatVal(doc);
				vals[1] = y.FloatVal(doc);
			}

			public override void DoubleVal(int doc, double[] vals)
			{
				vals[0] = x.DoubleVal(doc);
				vals[1] = y.DoubleVal(doc);
			}

			public override void StrVal(int doc, string[] vals)
			{
				vals[0] = x.StrVal(doc);
				vals[1] = y.StrVal(doc);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Name() + "(" + x.ToString(doc) + "," + y.ToString(doc) + ")";
			}

			private readonly VectorValueSource _enclosing;

			private readonly FunctionValues x;

			private readonly FunctionValues y;
		}

		private sealed class _FunctionValues_113 : FunctionValues
		{
			public _FunctionValues_113(VectorValueSource _enclosing, FunctionValues[] valsArr
				)
			{
				this._enclosing = _enclosing;
				this.valsArr = valsArr;
			}

			public override void ByteVal(int doc, byte[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].ByteVal(doc);
				}
			}

			public override void ShortVal(int doc, short[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].ShortVal(doc);
				}
			}

			public override void FloatVal(int doc, float[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].FloatVal(doc);
				}
			}

			public override void IntVal(int doc, int[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].IntVal(doc);
				}
			}

			public override void LongVal(int doc, long[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].LongVal(doc);
				}
			}

			public override void DoubleVal(int doc, double[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].DoubleVal(doc);
				}
			}

			public override void StrVal(int doc, string[] vals)
			{
				for (int i = 0; i < valsArr.Length; i++)
				{
					vals[i] = valsArr[i].StrVal(doc);
				}
			}

			public override string ToString(int doc)
			{
				StringBuilder sb = new StringBuilder();
				sb.Append(this._enclosing.Name()).Append('(');
				bool firstTime = true;
				foreach (FunctionValues vals in valsArr)
				{
					if (firstTime)
					{
						firstTime = false;
					}
					else
					{
						sb.Append(',');
					}
					sb.Append(vals.ToString(doc));
				}
				sb.Append(')');
				return sb.ToString();
			}

			private readonly VectorValueSource _enclosing;

			private readonly FunctionValues[] valsArr;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			foreach (ValueSource source in sources)
			{
				source.CreateWeight(context, searcher);
			}
		}

		public override string Description()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(Name()).Append('(');
			bool firstTime = true;
			foreach (ValueSource source in sources)
			{
				if (firstTime)
				{
					firstTime = false;
				}
				else
				{
					sb.Append(',');
				}
				sb.Append(source);
			}
			sb.Append(")");
			return sb.ToString();
		}

		public override bool Equals(object o)
		{
			if (this == o)
			{
				return true;
			}
			if (!(o is Org.Apache.Lucene.Queries.Function.Valuesource.VectorValueSource))
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.VectorValueSource that = (Org.Apache.Lucene.Queries.Function.Valuesource.VectorValueSource
				)o;
			return sources.Equals(that.sources);
		}

		public override int GetHashCode()
		{
			return sources.GetHashCode();
		}
	}
}
