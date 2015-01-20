/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>Scales values to be between min and max.</summary>
	/// <remarks>
	/// Scales values to be between min and max.
	/// <p>This implementation currently traverses all of the source values to obtain
	/// their min and max.
	/// <p>This implementation currently cannot distinguish when documents have been
	/// deleted or documents that have no value, and 0.0 values will be used for
	/// these cases.  This means that if values are normally all greater than 0.0, one can
	/// still end up with 0.0 as the min value to map from.  In these cases, an
	/// appropriate map() function could be used as a workaround to change 0.0
	/// to a value in the real range.
	/// </remarks>
	public class ScaleFloatFunction : ValueSource
	{
		protected internal readonly ValueSource source;

		protected internal readonly float min;

		protected internal readonly float max;

		public ScaleFloatFunction(ValueSource source, float min, float max)
		{
			this.source = source;
			this.min = min;
			this.max = max;
		}

		public override string Description()
		{
			return "scale(" + source.Description() + "," + min + "," + max + ")";
		}

		private class ScaleInfo
		{
			internal float minVal;

			internal float maxVal;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private ScaleFloatFunction.ScaleInfo CreateScaleInfo(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			IList<AtomicReaderContext> leaves = ReaderUtil.GetTopLevelContext(readerContext).
				Leaves();
			float minVal = float.PositiveInfinity;
			float maxVal = float.NegativeInfinity;
			foreach (AtomicReaderContext leaf in leaves)
			{
				int maxDoc = ((AtomicReader)leaf.Reader()).MaxDoc();
				FunctionValues vals = source.GetValues(context, leaf);
				for (int i = 0; i < maxDoc; i++)
				{
					float val = vals.FloatVal(i);
					if ((float.FloatToRawIntBits(val) & (unchecked((int)(0xff)) << 23)) == unchecked(
						(int)(0xff)) << 23)
					{
						// if the exponent in the float is all ones, then this is +Inf, -Inf or NaN
						// which don't make sense to factor into the scale function
						continue;
					}
					if (val < minVal)
					{
						minVal = val;
					}
					if (val > maxVal)
					{
						maxVal = val;
					}
				}
			}
			if (minVal == float.PositiveInfinity)
			{
				// must have been an empty index
				minVal = maxVal = 0;
			}
			ScaleFloatFunction.ScaleInfo scaleInfo = new ScaleFloatFunction.ScaleInfo();
			scaleInfo.minVal = minVal;
			scaleInfo.maxVal = maxVal;
			context.Put(this, scaleInfo);
			return scaleInfo;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			ScaleFloatFunction.ScaleInfo scaleInfo = (ScaleFloatFunction.ScaleInfo)context.Get
				(this);
			if (scaleInfo == null)
			{
				scaleInfo = CreateScaleInfo(context, readerContext);
			}
			float scale = (scaleInfo.maxVal - scaleInfo.minVal == 0) ? 0 : (max - min) / (scaleInfo
				.maxVal - scaleInfo.minVal);
			float minSource = scaleInfo.minVal;
			float maxSource = scaleInfo.maxVal;
			FunctionValues vals = source.GetValues(context, readerContext);
			return new _FloatDocValues_115(this, vals, minSource, scale, maxSource, this);
		}

		private sealed class _FloatDocValues_115 : FloatDocValues
		{
			public _FloatDocValues_115(ScaleFloatFunction _enclosing, FunctionValues vals, float
				 minSource, float scale, float maxSource, ValueSource baseArg1) : base(baseArg1)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
				this.minSource = minSource;
				this.scale = scale;
				this.maxSource = maxSource;
			}

			public override float FloatVal(int doc)
			{
				return (vals.FloatVal(doc) - minSource) * scale + this._enclosing.min;
			}

			public override string ToString(int doc)
			{
				return "scale(" + vals.ToString(doc) + ",toMin=" + this._enclosing.min + ",toMax="
					 + this._enclosing.max + ",fromMin=" + minSource + ",fromMax=" + maxSource + ")";
			}

			private readonly ScaleFloatFunction _enclosing;

			private readonly FunctionValues vals;

			private readonly float minSource;

			private readonly float scale;

			private readonly float maxSource;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}

		public override int GetHashCode()
		{
			int h = Sharpen.Runtime.FloatToIntBits(min);
			h = h * 29;
			h += Sharpen.Runtime.FloatToIntBits(max);
			h = h * 29;
			h += source.GetHashCode();
			return h;
		}

		public override bool Equals(object o)
		{
			if (typeof(ScaleFloatFunction) != o.GetType())
			{
				return false;
			}
			ScaleFloatFunction other = (ScaleFloatFunction)o;
			return this.min == other.min && this.max == other.max && this.source.Equals(other
				.source);
		}
	}
}
