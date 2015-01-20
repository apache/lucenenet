/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// <code>RangeMapFloatFunction</code> implements a map function over
	/// another
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// whose values fall within min and max inclusive to target.
	/// <br />
	/// Normally Used as an argument to a
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionQuery">Org.Apache.Lucene.Queries.Function.FunctionQuery
	/// 	</see>
	/// </summary>
	public class RangeMapFloatFunction : ValueSource
	{
		protected internal readonly ValueSource source;

		protected internal readonly float min;

		protected internal readonly float max;

		protected internal readonly ValueSource target;

		protected internal readonly ValueSource defaultVal;

		public RangeMapFloatFunction(ValueSource source, float min, float max, float target
			, float def) : this(source, min, max, new ConstValueSource(target), def == null ? 
			null : new ConstValueSource(def))
		{
		}

		public RangeMapFloatFunction(ValueSource source, float min, float max, ValueSource
			 target, ValueSource def)
		{
			this.source = source;
			this.min = min;
			this.max = max;
			this.target = target;
			this.defaultVal = def;
		}

		public override string Description()
		{
			return "map(" + source.Description() + "," + min + "," + max + "," + target.Description
				() + ")";
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FunctionValues vals = source.GetValues(context, readerContext);
			FunctionValues targets = target.GetValues(context, readerContext);
			FunctionValues defaults = (this.defaultVal == null) ? null : defaultVal.GetValues
				(context, readerContext);
			return new _FloatDocValues_66(this, vals, targets, defaults, this);
		}

		private sealed class _FloatDocValues_66 : FloatDocValues
		{
			public _FloatDocValues_66(RangeMapFloatFunction _enclosing, FunctionValues vals, 
				FunctionValues targets, FunctionValues defaults, ValueSource baseArg1) : base(baseArg1
				)
			{
				this._enclosing = _enclosing;
				this.vals = vals;
				this.targets = targets;
				this.defaults = defaults;
			}

			public override float FloatVal(int doc)
			{
				float val = vals.FloatVal(doc);
				return (val >= this._enclosing.min && val <= this._enclosing.max) ? targets.FloatVal
					(doc) : (this._enclosing.defaultVal == null ? val : defaults.FloatVal(doc));
			}

			public override string ToString(int doc)
			{
				return "map(" + vals.ToString(doc) + ",min=" + this._enclosing.min + ",max=" + this
					._enclosing.max + ",target=" + targets.ToString(doc) + ")";
			}

			private readonly RangeMapFloatFunction _enclosing;

			private readonly FunctionValues vals;

			private readonly FunctionValues targets;

			private readonly FunctionValues defaults;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
			source.CreateWeight(context, searcher);
		}

		public override int GetHashCode()
		{
			int h = source.GetHashCode();
			h ^= (h << 10) | ((int)(((uint)h) >> 23));
			h += Sharpen.Runtime.FloatToIntBits(min);
			h ^= (h << 14) | ((int)(((uint)h) >> 19));
			h += Sharpen.Runtime.FloatToIntBits(max);
			h += target.GetHashCode();
			if (defaultVal != null)
			{
				h += defaultVal.GetHashCode();
			}
			return h;
		}

		public override bool Equals(object o)
		{
			if (typeof(Org.Apache.Lucene.Queries.Function.Valuesource.RangeMapFloatFunction) 
				!= o.GetType())
			{
				return false;
			}
			Org.Apache.Lucene.Queries.Function.Valuesource.RangeMapFloatFunction other = (Org.Apache.Lucene.Queries.Function.Valuesource.RangeMapFloatFunction
				)o;
			return this.min == other.min && this.max == other.max && this.target.Equals(other
				.target) && this.source.Equals(other.source) && (this.defaultVal == other.defaultVal
				 || (this.defaultVal != null && this.defaultVal.Equals(other.defaultVal)));
		}
	}
}
