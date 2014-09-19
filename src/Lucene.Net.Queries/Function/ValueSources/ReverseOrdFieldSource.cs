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
using System.Collections;
using Lucene.Net.Queries.Function.DocValues;
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Obtains the ordinal of the field value from the default Lucene <seealso cref="org.apache.lucene.search.FieldCache"/> using getTermsIndex()
	/// and reverses the order.
	/// <br>
	/// The native lucene index order is used to assign an ordinal value for each field value.
	/// <br>Field values (terms) are lexicographically ordered by unicode value, and numbered starting at 1.
	/// <br>
	/// Example of reverse ordinal (rord):<br>
	///  If there were only three field values: "apple","banana","pear"
	/// <br>then rord("apple")=3, rord("banana")=2, ord("pear")=1
	/// <para>
	///  WARNING: ord() depends on the position in an index and can thus change when other documents are inserted or deleted,
	///  or if a MultiSearcher is used.
	/// <br>
	///  WARNING: as of Solr 1.4, ord() and rord() can cause excess memory use since they must use a FieldCache entry
	/// at the top level reader, while sorting and function queries now use entries at the segment level.  Hence sorting
	/// or using a different function query, in addition to ord()/rord() will double memory use.
	/// 
	/// 
	/// </para>
	/// </summary>

	public class ReverseOrdFieldSource : ValueSource
	{
	  public readonly string field;

	  public ReverseOrdFieldSource(string field)
	  {
		this.field = field;
	  }

	  public override string description()
	  {
		return "rord(" + field + ')';
	  }

	  // TODO: this is trappy? perhaps this query instead should make you pass a slow reader yourself?
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.IndexReader topReader = org.apache.lucene.index.ReaderUtil.getTopLevelContext(readerContext).reader();
		IndexReader topReader = ReaderUtil.getTopLevelContext(readerContext).reader();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.AtomicReader r = org.apache.lucene.index.SlowCompositeReaderWrapper.wrap(topReader);
		AtomicReader r = SlowCompositeReaderWrapper.wrap(topReader);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int off = readerContext.docBase;
		int off = readerContext.docBase;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.SortedDocValues sindex = org.apache.lucene.search.FieldCache.DEFAULT.getTermsIndex(r, field);
		SortedDocValues sindex = FieldCache.DEFAULT.getTermsIndex(r, field);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int end = sindex.getValueCount();
		int end = sindex.ValueCount;

		return new IntDocValuesAnonymousInnerClassHelper(this, this, off, sindex, end);
	  }

	  private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
	  {
		  private readonly ReverseOrdFieldSource outerInstance;

		  private int off;
		  private SortedDocValues sindex;
		  private int end;

		  public IntDocValuesAnonymousInnerClassHelper(ReverseOrdFieldSource outerInstance, ReverseOrdFieldSource this, int off, SortedDocValues sindex, int end) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.off = off;
			  this.sindex = sindex;
			  this.end = end;
		  }

		  public override int intVal(int doc)
		  {
			 return (end - sindex.getOrd(doc + off) - 1);
		  }
	  }

	  public override bool Equals(object o)
	  {
		if (o == null || (o.GetType() != typeof(ReverseOrdFieldSource)))
		{
			return false;
		}
		ReverseOrdFieldSource other = (ReverseOrdFieldSource)o;
		return this.field.Equals(other.field);
	  }

	  private static readonly int hcode = typeof(ReverseOrdFieldSource).GetHashCode();
	  public override int GetHashCode()
	  {
		return hcode + field.GetHashCode();
	  }

	}

}