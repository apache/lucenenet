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
using System;
using System.Collections;
using Lucene.Net.Queries.Function.DocValues;
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Use a field value and find the Document Frequency within another field.
	/// 
	/// @since solr 4.0
	/// </summary>
	public class JoinDocFreqValueSource : FieldCacheSource
	{

	  public const string NAME = "joindf";

	  protected internal readonly string qfield;

	  public JoinDocFreqValueSource(string field, string qfield) : base(field)
	  {
		this.qfield = qfield;
	  }

	  public override string description()
	  {
		return NAME + "(" + field + ":(" + qfield + "))";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.BinaryDocValues terms = cache.getTerms(readerContext.reader(), field, false, org.apache.lucene.util.packed.PackedInts.FAST);
		BinaryDocValues terms = cache.getTerms(readerContext.reader(), field, false, PackedInts.FAST);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.IndexReader top = org.apache.lucene.index.ReaderUtil.getTopLevelContext(readerContext).reader();
		IndexReader top = ReaderUtil.getTopLevelContext(readerContext).reader();
		Terms t = MultiFields.getTerms(top, qfield);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.TermsEnum termsEnum = t == null ? org.apache.lucene.index.TermsEnum.EMPTY : t.iterator(null);
		TermsEnum termsEnum = t == null ? TermsEnum.EMPTY : t.iterator(null);

		return new IntDocValuesAnonymousInnerClassHelper(this, this, terms, termsEnum);
	  }

	  private class IntDocValuesAnonymousInnerClassHelper : IntDocValues
	  {
		  private readonly JoinDocFreqValueSource outerInstance;

		  private BinaryDocValues terms;
		  private TermsEnum termsEnum;

		  public IntDocValuesAnonymousInnerClassHelper(JoinDocFreqValueSource outerInstance, JoinDocFreqValueSource this, BinaryDocValues terms, TermsEnum termsEnum) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.terms = terms;
			  this.termsEnum = termsEnum;
			  @ref = new BytesRef();
		  }

		  internal readonly BytesRef @ref;

		  public override int intVal(int doc)
		  {
			try
			{
			  terms.get(doc, @ref);
			  if (termsEnum.seekExact(@ref))
			  {
				return termsEnum.docFreq();
			  }
			  else
			  {
				return 0;
			  }
			}
			catch (IOException e)
			{
			  throw new Exception("caught exception in function " + outerInstance.description() + " : doc=" + doc, e);
			}
		  }
	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != typeof(JoinDocFreqValueSource))
		{
			return false;
		}
		JoinDocFreqValueSource other = (JoinDocFreqValueSource)o;
		if (!qfield.Equals(other.qfield))
		{
			return false;
		}
		return base.Equals(other);
	  }

	  public override int GetHashCode()
	  {
		return qfield.GetHashCode() + base.GetHashCode();
	  }
	}

}