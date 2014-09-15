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

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using Fields = org.apache.lucene.index.Fields;
	using Terms = org.apache.lucene.index.Terms;
	using LongDocValues = org.apache.lucene.queries.function.docvalues.LongDocValues;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;
	using BytesRef = org.apache.lucene.util.BytesRef;


	/// <summary>
	/// <code>SumTotalTermFreqValueSource</code> returns the number of tokens.
	/// (sum of term freqs across all documents, across all terms).
	/// Returns -1 if frequencies were omitted for the field, or if 
	/// the codec doesn't support this statistic.
	/// @lucene.internal
	/// </summary>
	public class SumTotalTermFreqValueSource : ValueSource
	{
	  protected internal readonly string indexedField;

	  public SumTotalTermFreqValueSource(string indexedField)
	  {
		this.indexedField = indexedField;
	  }

	  public virtual string name()
	  {
		return "sumtotaltermfreq";
	  }

	  public override string description()
	  {
		return name() + '(' + indexedField + ')';
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		return (FunctionValues)context[this];
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		long sumTotalTermFreq = 0;
		foreach (AtomicReaderContext readerContext in searcher.TopReaderContext.leaves())
		{
		  Fields fields = readerContext.reader().fields();
		  if (fields == null)
		  {
			  continue;
		  }
		  Terms terms = fields.terms(indexedField);
		  if (terms == null)
		  {
			  continue;
		  }
		  long v = terms.SumTotalTermFreq;
		  if (v == -1)
		  {
			sumTotalTermFreq = -1;
			break;
		  }
		  else
		  {
			sumTotalTermFreq += v;
		  }
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final long ttf = sumTotalTermFreq;
		long ttf = sumTotalTermFreq;
		context[this] = new LongDocValuesAnonymousInnerClassHelper(this, this, ttf);
	  }

	  private class LongDocValuesAnonymousInnerClassHelper : LongDocValues
	  {
		  private readonly SumTotalTermFreqValueSource outerInstance;

		  private long ttf;

		  public LongDocValuesAnonymousInnerClassHelper(SumTotalTermFreqValueSource outerInstance, org.apache.lucene.queries.function.valuesource.SumTotalTermFreqValueSource this, long ttf) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.ttf = ttf;
		  }

		  public override long longVal(int doc)
		  {
			return ttf;
		  }
	  }

	  public override int GetHashCode()
	  {
		return this.GetType().GetHashCode() + indexedField.GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		SumTotalTermFreqValueSource other = (SumTotalTermFreqValueSource)o;
		return this.indexedField.Equals(other.indexedField);
	  }
	}

}