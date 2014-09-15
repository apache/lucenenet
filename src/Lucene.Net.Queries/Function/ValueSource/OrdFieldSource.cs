using System.Collections;

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

namespace org.apache.lucene.queries.function.valuesource
{


	using AtomicReader = org.apache.lucene.index.AtomicReader;
	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using CompositeReader = org.apache.lucene.index.CompositeReader;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using ReaderUtil = org.apache.lucene.index.ReaderUtil;
	using SlowCompositeReaderWrapper = org.apache.lucene.index.SlowCompositeReaderWrapper;
	using SortedDocValues = org.apache.lucene.index.SortedDocValues;
	using IntDocValues = org.apache.lucene.queries.function.docvalues.IntDocValues;
	using FieldCache = org.apache.lucene.search.FieldCache;
	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueInt = org.apache.lucene.util.mutable.MutableValueInt;

	/// <summary>
	/// Obtains the ordinal of the field value from the default Lucene <seealso cref="org.apache.lucene.search.FieldCache"/> using getStringIndex().
	/// <br>
	/// The native lucene index order is used to assign an ordinal value for each field value.
	/// <br>Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
	/// <br>
	/// Example:<br>
	///  If there were only three field values: "apple","banana","pear"
	/// <br>then ord("apple")=1, ord("banana")=2, ord("pear")=3
	/// <para>
	/// WARNING: ord() depends on the position in an index and can thus change when other documents are inserted or deleted,
	///  or if a MultiSearcher is used.
	/// <br>WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
	/// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
	/// or using a different function query, in addition to ord()/rord() will double memory use.
	/// 
	/// </para>
	/// </summary>

	public class OrdFieldSource : ValueSource
	{
	  protected internal readonly string field;

	  public OrdFieldSource(string field)
	  {
		this.field = field;
	  }

	  public override string description()
	  {
		return "ord(" + field + ')';
	  }


	  // TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int off = readerContext.docBase;
		int off = readerContext.docBase;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.IndexReader topReader = org.apache.lucene.index.ReaderUtil.getTopLevelContext(readerContext).reader();
		IndexReader topReader = ReaderUtil.getTopLevelContext(readerContext).reader();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.AtomicReader r = org.apache.lucene.index.SlowCompositeReaderWrapper.wrap(topReader);
		AtomicReader r = SlowCompositeReaderWrapper.wrap(topReader);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.SortedDocValues sindex = org.apache.lucene.search.FieldCache.DEFAULT.getTermsIndex(r, field);
		SortedDocValues sindex = FieldCache.DEFAULT.getTermsIndex(r, field);
		return new IntDocValuesAnonymousInnerClassHelper(this, this, off, sindex);
	  }

	  private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
	  {
		  private readonly OrdFieldSource outerInstance;

		  private int off;
		  private SortedDocValues sindex;

		  public IntDocValuesAnonymousInnerClassHelper(OrdFieldSource outerInstance, org.apache.lucene.queries.function.valuesource.OrdFieldSource this, int off, SortedDocValues sindex) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.off = off;
			  this.sindex = sindex;
		  }

		  protected internal virtual string toTerm(string readableValue)
		  {
			return readableValue;
		  }
		  public override int intVal(int doc)
		  {
			return sindex.getOrd(doc + off);
		  }
		  public override int ordVal(int doc)
		  {
			return sindex.getOrd(doc + off);
		  }
		  public override int numOrd()
		  {
			return sindex.ValueCount;
		  }

		  public override bool exists(int doc)
		  {
			return sindex.getOrd(doc + off) != 0;
		  }

		  public override ValueFiller ValueFiller
		  {
			  get
			  {
				return new ValueFillerAnonymousInnerClassHelper(this);
			  }
		  }

		  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
		  {
			  private readonly IntDocValuesAnonymousInnerClassHelper outerInstance;

			  public ValueFillerAnonymousInnerClassHelper(IntDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  mval = new MutableValueInt();
			  }

			  private readonly MutableValueInt mval;

			  public override MutableValue Value
			  {
				  get
				  {
					return mval;
				  }
			  }

			  public override void fillValue(int doc)
			  {
				mval.value = outerInstance.sindex.getOrd(doc);
				mval.exists = mval.value != 0;
			  }
		  }
	  }

	  public override bool Equals(object o)
	  {
		return o != null && o.GetType() == typeof(OrdFieldSource) && this.field.Equals(((OrdFieldSource)o).field);
	  }

	  private static readonly int hcode = typeof(OrdFieldSource).GetHashCode();
	  public override int GetHashCode()
	  {
		return hcode + field.GetHashCode();
	  }

	}

}