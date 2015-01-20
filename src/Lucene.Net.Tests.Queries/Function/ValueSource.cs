/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function
{
	/// <summary>
	/// Instantiates
	/// <see cref="FunctionValues">FunctionValues</see>
	/// for a particular reader.
	/// <br />
	/// Often used when creating a
	/// <see cref="FunctionQuery">FunctionQuery</see>
	/// .
	/// </summary>
	public abstract class ValueSource
	{
		/// <summary>
		/// Gets the values for this reader and the context that was previously
		/// passed to createWeight()
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		public abstract FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext);

		public abstract override bool Equals(object o);

		public abstract override int GetHashCode();

		/// <summary>description of field, used in explain()</summary>
		public abstract string Description();

		public override string ToString()
		{
			return Description();
		}

		/// <summary>
		/// Implementations should propagate createWeight to sub-ValueSources which can optionally store
		/// weight info in the context.
		/// </summary>
		/// <remarks>
		/// Implementations should propagate createWeight to sub-ValueSources which can optionally store
		/// weight info in the context. The context object will be passed to getValues()
		/// where this info can be retrieved.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual void CreateWeight(IDictionary context, IndexSearcher searcher)
		{
		}

		/// <summary>Returns a new non-threadsafe context map.</summary>
		/// <remarks>Returns a new non-threadsafe context map.</remarks>
		public static IDictionary NewContext(IndexSearcher searcher)
		{
			IDictionary context = new IdentityHashMap();
			context.Put("searcher", searcher);
			return context;
		}

		//
		// Sorting by function
		//
		/// <summary>EXPERIMENTAL: This method is subject to change.</summary>
		/// <remarks>
		/// EXPERIMENTAL: This method is subject to change.
		/// <p>
		/// Get the SortField for this ValueSource.  Uses the
		/// <see cref="GetValues(System.Collections.IDictionary{K, V}, Org.Apache.Lucene.Index.AtomicReaderContext)
		/// 	">GetValues(System.Collections.IDictionary&lt;K, V&gt;, Org.Apache.Lucene.Index.AtomicReaderContext)
		/// 	</see>
		/// to populate the SortField.
		/// </remarks>
		/// <param name="reverse">true if this is a reverse sort.</param>
		/// <returns>
		/// The
		/// <see cref="Org.Apache.Lucene.Search.SortField">Org.Apache.Lucene.Search.SortField
		/// 	</see>
		/// for the ValueSource
		/// </returns>
		public virtual SortField GetSortField(bool reverse)
		{
			return new ValueSource.ValueSourceSortField(this, reverse);
		}

		internal class ValueSourceSortField : SortField
		{
			public ValueSourceSortField(ValueSource _enclosing, bool reverse) : base(this._enclosing
				.Description(), SortField.Type.REWRITEABLE, reverse)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override SortField Rewrite(IndexSearcher searcher)
			{
				IDictionary context = ValueSource.NewContext(searcher);
				this._enclosing.CreateWeight(context, searcher);
				return new SortField(this.GetField(), new ValueSource.ValueSourceComparatorSource
					(this, context), this.GetReverse());
			}

			private readonly ValueSource _enclosing;
		}

		internal class ValueSourceComparatorSource : FieldComparatorSource
		{
			private readonly IDictionary context;

			public ValueSourceComparatorSource(ValueSource _enclosing, IDictionary context)
			{
				this._enclosing = _enclosing;
				this.context = context;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldComparator<object> NewComparator(string fieldname, int numHits
				, int sortPos, bool reversed)
			{
				return new ValueSource.ValueSourceComparator(this, this.context, numHits);
			}

			private readonly ValueSource _enclosing;
		}

		/// <summary>
		/// Implement a
		/// <see cref="Org.Apache.Lucene.Search.FieldComparator{T}">Org.Apache.Lucene.Search.FieldComparator&lt;T&gt;
		/// 	</see>
		/// that works
		/// off of the
		/// <see cref="FunctionValues">FunctionValues</see>
		/// for a ValueSource
		/// instead of the normal Lucene FieldComparator that works off of a FieldCache.
		/// </summary>
		internal class ValueSourceComparator : FieldComparator<double>
		{
			private readonly double[] values;

			private FunctionValues docVals;

			private double bottom;

			private readonly IDictionary fcontext;

			private double topValue;

			internal ValueSourceComparator(ValueSource _enclosing, IDictionary fcontext, int 
				numHits)
			{
				this._enclosing = _enclosing;
				this.fcontext = fcontext;
				this.values = new double[numHits];
			}

			public override int Compare(int slot1, int slot2)
			{
				return double.Compare(this.values[slot1], this.values[slot2]);
			}

			public override int CompareBottom(int doc)
			{
				return double.Compare(this.bottom, this.docVals.DoubleVal(doc));
			}

			public override void Copy(int slot, int doc)
			{
				this.values[slot] = this.docVals.DoubleVal(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldComparator<double> SetNextReader(AtomicReaderContext context
				)
			{
				this.docVals = this._enclosing.GetValues(this.fcontext, context);
				return this;
			}

			public override void SetBottom(int bottom)
			{
				this.bottom = this.values[bottom];
			}

			public override void SetTopValue(double value)
			{
				this.topValue = value;
			}

			public override double Value(int slot)
			{
				return this.values[slot];
			}

			public override int CompareTop(int doc)
			{
				double docValue = this.docVals.DoubleVal(doc);
				return double.Compare(this.topValue, docValue);
			}

			private readonly ValueSource _enclosing;
		}
	}
}
