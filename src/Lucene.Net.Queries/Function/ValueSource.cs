using System.Collections;

namespace org.apache.lucene.queries.function
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using FieldComparator = org.apache.lucene.search.FieldComparator;
	using FieldComparatorSource = org.apache.lucene.search.FieldComparatorSource;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using SortField = org.apache.lucene.search.SortField;


	/// <summary>
	/// Instantiates <seealso cref="FunctionValues"/> for a particular reader.
	/// <br>
	/// Often used when creating a <seealso cref="FunctionQuery"/>.
	/// 
	/// 
	/// </summary>
	public abstract class ValueSource
	{

	  /// <summary>
	  /// Gets the values for this reader and the context that was previously
	  /// passed to createWeight()
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public abstract FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException;
	  public abstract FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext);

	  public override abstract bool Equals(object o);

	  public override abstract int GetHashCode();

	  /// <summary>
	  /// description of field, used in explain()
	  /// </summary>
	  public abstract string description();

	  public override string ToString()
	  {
		return description();
	  }


	  /// <summary>
	  /// Implementations should propagate createWeight to sub-ValueSources which can optionally store
	  /// weight info in the context. The context object will be passed to getValues()
	  /// where this info can be retrieved.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public virtual void createWeight(IDictionary context, IndexSearcher searcher)
	  {
	  }

	  /// <summary>
	  /// Returns a new non-threadsafe context map.
	  /// </summary>
	  public static IDictionary newContext(IndexSearcher searcher)
	  {
		IDictionary context = new IdentityHashMap();
		context["searcher"] = searcher;
		return context;
	  }


	  //
	  // Sorting by function
	  //

	  /// <summary>
	  /// EXPERIMENTAL: This method is subject to change.
	  /// <para>
	  /// Get the SortField for this ValueSource.  Uses the <seealso cref="#getValues(java.util.Map, AtomicReaderContext)"/>
	  /// to populate the SortField.
	  /// 
	  /// </para>
	  /// </summary>
	  /// <param name="reverse"> true if this is a reverse sort. </param>
	  /// <returns> The <seealso cref="org.apache.lucene.search.SortField"/> for the ValueSource </returns>
	  public virtual SortField getSortField(bool reverse)
	  {
		return new ValueSourceSortField(this, reverse);
	  }

	  internal class ValueSourceSortField : SortField
	  {
		  private readonly ValueSource outerInstance;

		public ValueSourceSortField(ValueSource outerInstance, bool reverse) : base(outerInstance.description(), SortField.Type.REWRITEABLE, reverse)
		{
			this.outerInstance = outerInstance;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.SortField rewrite(org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
		public override SortField rewrite(IndexSearcher searcher)
		{
		  IDictionary context = newContext(searcher);
		  outerInstance.createWeight(context, searcher);
		  return new SortField(Field, new ValueSourceComparatorSource(outerInstance, context), Reverse);
		}
	  }

	  internal class ValueSourceComparatorSource : FieldComparatorSource
	  {
		  private readonly ValueSource outerInstance;

		internal readonly IDictionary context;

		public ValueSourceComparatorSource(ValueSource outerInstance, IDictionary context)
		{
			this.outerInstance = outerInstance;
		  this.context = context;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.FieldComparator<Double> newComparator(String fieldname, int numHits, int sortPos, boolean reversed) throws java.io.IOException
		public override FieldComparator<double?> newComparator(string fieldname, int numHits, int sortPos, bool reversed)
		{
		  return new ValueSourceComparator(outerInstance, context, numHits);
		}
	  }

	  /// <summary>
	  /// Implement a <seealso cref="org.apache.lucene.search.FieldComparator"/> that works
	  /// off of the <seealso cref="FunctionValues"/> for a ValueSource
	  /// instead of the normal Lucene FieldComparator that works off of a FieldCache.
	  /// </summary>
	  internal class ValueSourceComparator : FieldComparator<double?>
	  {
		  private readonly ValueSource outerInstance;

		internal readonly double[] values;
		internal FunctionValues docVals;
		internal double bottom;
		internal readonly IDictionary fcontext;
		internal double topValue;

		internal ValueSourceComparator(ValueSource outerInstance, IDictionary fcontext, int numHits)
		{
			this.outerInstance = outerInstance;
		  this.fcontext = fcontext;
		  values = new double[numHits];
		}

		public override int compare(int slot1, int slot2)
		{
		  return values[slot1].CompareTo(values[slot2]);
		}

		public override int compareBottom(int doc)
		{
		  return bottom.CompareTo(docVals.doubleVal(doc));
		}

		public override void copy(int slot, int doc)
		{
		  values[slot] = docVals.doubleVal(doc);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.FieldComparator setNextReader(org.apache.lucene.index.AtomicReaderContext context) throws java.io.IOException
		public override FieldComparator setNextReader(AtomicReaderContext context)
		{
		  docVals = outerInstance.getValues(fcontext, context);
		  return this;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public void setBottom(final int bottom)
		public override int Bottom
		{
			set
			{
			  this.bottom = values[value];
			}
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: @Override public void setTopValue(final Double value)
		public override double? TopValue
		{
			set
			{
			  this.topValue = (double)value;
			}
		}

		public override double? value(int slot)
		{
		  return values[slot];
		}

		public override int compareTop(int doc)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final double docValue = docVals.doubleVal(doc);
		  double docValue = docVals.doubleVal(doc);
		  return topValue.CompareTo(docValue);
		}
	  }
	}

}