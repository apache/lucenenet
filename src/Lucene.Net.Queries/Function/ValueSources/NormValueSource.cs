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
	/// Function that returns <seealso cref="TFIDFSimilarity#decodeNormValue(long)"/>
	/// for every document.
	/// <para>
	/// Note that the configured Similarity for the field must be
	/// a subclass of <seealso cref="TFIDFSimilarity"/>
	/// @lucene.internal 
	/// </para>
	/// </summary>
	public class NormValueSource : ValueSource
	{
	  protected internal readonly string field;
	  public NormValueSource(string field)
	  {
		this.field = field;
	  }

	  public virtual string name()
	  {
		return "norm";
	  }

	  public override string description()
	  {
		return name() + '(' + field + ')';
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void CreateWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void CreateWeight(IDictionary context, IndexSearcher searcher)
	  {
		context["searcher"] = searcher;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		IndexSearcher searcher = (IndexSearcher)context["searcher"];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.similarities.TFIDFSimilarity similarity = IDFValueSource.asTFIDF(searcher.getSimilarity(), field);
		TFIDFSimilarity similarity = IDFValueSource.asTFIDF(searcher.Similarity, field);
		if (similarity == null)
		{
		  throw new System.NotSupportedException("requires a TFIDFSimilarity (such as DefaultSimilarity)");
		}
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues norms = readerContext.reader().getNormValues(field);
		NumericDocValues norms = readerContext.reader().getNormValues(field);

		if (norms == null)
		{
		  return new ConstDoubleDocValues(0.0, this);
		}

		return new FloatDocValuesAnonymousInnerClassHelper(this, this, similarity, norms);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly NormValueSource outerInstance;

		  private TFIDFSimilarity similarity;
		  private NumericDocValues norms;

		  public FloatDocValuesAnonymousInnerClassHelper(NormValueSource outerInstance, NormValueSource this, TFIDFSimilarity similarity, NumericDocValues norms) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.similarity = similarity;
			  this.norms = norms;
		  }

		  public override float FloatVal(int doc)
		  {
			return similarity.decodeNormValue(norms.get(doc));
		  }
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
		  return false;
		}
		return this.field.Equals(((NormValueSource)o).field);
	  }

	  public override int GetHashCode()
	  {
		return this.GetType().GetHashCode() + field.GetHashCode();
	  }
	}



}