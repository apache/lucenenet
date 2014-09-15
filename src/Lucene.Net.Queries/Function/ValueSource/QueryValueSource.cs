using System;
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
	using ReaderUtil = org.apache.lucene.index.ReaderUtil;
	using FloatDocValues = org.apache.lucene.queries.function.docvalues.FloatDocValues;
	using org.apache.lucene.search;
	using Bits = org.apache.lucene.util.Bits;
	using MutableValue = org.apache.lucene.util.mutable.MutableValue;
	using MutableValueFloat = org.apache.lucene.util.mutable.MutableValueFloat;


	/// <summary>
	/// <code>QueryValueSource</code> returns the relevance score of the query
	/// </summary>
	public class QueryValueSource : ValueSource
	{
	  internal readonly Query q;
	  internal readonly float defVal;

	  public QueryValueSource(Query q, float defVal)
	  {
		this.q = q;
		this.defVal = defVal;
	  }

	  public virtual Query Query
	  {
		  get
		  {
			  return q;
		  }
	  }
	  public virtual float DefaultValue
	  {
		  get
		  {
			  return defVal;
		  }
	  }

	  public override string description()
	  {
		return "query(" + q + ",def=" + defVal + ")";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map fcontext, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary fcontext, AtomicReaderContext readerContext)
	  {
		return new QueryDocValues(this, readerContext, fcontext);
	  }

	  public override int GetHashCode()
	  {
		return q.GetHashCode() * 29;
	  }

	  public override bool Equals(object o)
	  {
		if (typeof(QueryValueSource) != o.GetType())
		{
			return false;
		}
		QueryValueSource other = (QueryValueSource)o;
		return this.q.Equals(other.q) && this.defVal == other.defVal;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		Weight w = searcher.createNormalizedWeight(q);
		context[this] = w;
	  }
	}


	internal class QueryDocValues : FloatDocValues
	{
	  internal readonly AtomicReaderContext readerContext;
	  internal readonly Bits acceptDocs;
	  internal readonly Weight weight;
	  internal readonly float defVal;
	  internal readonly IDictionary fcontext;
	  internal readonly Query q;

	  internal Scorer scorer;
	  internal int scorerDoc; // the document the scorer is on
	  internal bool noMatches = false;

	  // the last document requested... start off with high value
	  // to trigger a scorer reset on first access.
	  internal int lastDocRequested = int.MaxValue;


//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public QueryDocValues(QueryValueSource vs, org.apache.lucene.index.AtomicReaderContext readerContext, java.util.Map fcontext) throws java.io.IOException
	  public QueryDocValues(QueryValueSource vs, AtomicReaderContext readerContext, IDictionary fcontext) : base(vs)
	  {

		this.readerContext = readerContext;
		this.acceptDocs = readerContext.reader().LiveDocs;
		this.defVal = vs.defVal;
		this.q = vs.q;
		this.fcontext = fcontext;

		Weight w = fcontext == null ? null : (Weight)fcontext[vs];
		if (w == null)
		{
		  IndexSearcher weightSearcher;
		  if (fcontext == null)
		  {
			weightSearcher = new IndexSearcher(ReaderUtil.getTopLevelContext(readerContext));
		  }
		  else
		  {
			weightSearcher = (IndexSearcher)fcontext["searcher"];
			if (weightSearcher == null)
			{
			  weightSearcher = new IndexSearcher(ReaderUtil.getTopLevelContext(readerContext));
			}
		  }
		  vs.createWeight(fcontext, weightSearcher);
		  w = (Weight)fcontext[vs];
		}
		weight = w;
	  }

	  public override float floatVal(int doc)
	  {
		try
		{
		  if (doc < lastDocRequested)
		  {
			if (noMatches)
			{
				return defVal;
			}
			scorer = weight.scorer(readerContext, acceptDocs);
			if (scorer == null)
			{
			  noMatches = true;
			  return defVal;
			}
			scorerDoc = -1;
		  }
		  lastDocRequested = doc;

		  if (scorerDoc < doc)
		  {
			scorerDoc = scorer.advance(doc);
		  }

		  if (scorerDoc > doc)
		  {
			// query doesn't match this document... either because we hit the
			// end, or because the next doc is after this doc.
			return defVal;
		  }

		  // a match!
		  return scorer.score();
		}
		catch (IOException e)
		{
		  throw new Exception("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
		}
	  }

	  public override bool exists(int doc)
	  {
		try
		{
		  if (doc < lastDocRequested)
		  {
			if (noMatches)
			{
				return false;
			}
			scorer = weight.scorer(readerContext, acceptDocs);
			scorerDoc = -1;
			if (scorer == null)
			{
			  noMatches = true;
			  return false;
			}
		  }
		  lastDocRequested = doc;

		  if (scorerDoc < doc)
		  {
			scorerDoc = scorer.advance(doc);
		  }

		  if (scorerDoc > doc)
		  {
			// query doesn't match this document... either because we hit the
			// end, or because the next doc is after this doc.
			return false;
		  }

		  // a match!
		  return true;
		}
		catch (IOException e)
		{
		  throw new Exception("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
		}
	  }

	   public override object objectVal(int doc)
	   {
		 try
		 {
		   return exists(doc) ? scorer.score() : null;
		 }
		 catch (IOException e)
		 {
		   throw new Exception("caught exception in QueryDocVals(" + q + ") doc=" + doc, e);
		 }
	   }

	  public override ValueFiller ValueFiller
	  {
		  get
		  {
			//
			// TODO: if we want to support more than one value-filler or a value-filler in conjunction with
			// the FunctionValues, then members like "scorer" should be per ValueFiller instance.
			// Or we can say that the user should just instantiate multiple FunctionValues.
			//
			return new ValueFillerAnonymousInnerClassHelper(this);
		  }
	  }

	  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
	  {
		  private readonly QueryDocValues outerInstance;

		  public ValueFillerAnonymousInnerClassHelper(QueryDocValues outerInstance)
		  {
			  this.outerInstance = outerInstance;
			  mval = new MutableValueFloat();
		  }

		  private readonly MutableValueFloat mval;

		  public override MutableValue Value
		  {
			  get
			  {
				return mval;
			  }
		  }

		  public override void fillValue(int doc)
		  {
			try
			{
			  if (outerInstance.noMatches)
			  {
				mval.value = outerInstance.defVal;
				mval.exists = false;
				return;
			  }
			  outerInstance.scorer = outerInstance.weight.scorer(outerInstance.readerContext, outerInstance.acceptDocs);
			  outerInstance.scorerDoc = -1;
			  if (outerInstance.scorer == null)
			  {
				outerInstance.noMatches = true;
				mval.value = outerInstance.defVal;
				mval.exists = false;
				return;
			  }
			  outerInstance.lastDocRequested = doc;

			  if (outerInstance.scorerDoc < doc)
			  {
				outerInstance.scorerDoc = outerInstance.scorer.advance(doc);
			  }

			  if (outerInstance.scorerDoc > doc)
			  {
				// query doesn't match this document... either because we hit the
				// end, or because the next doc is after this doc.
				mval.value = outerInstance.defVal;
				mval.exists = false;
				return;
			  }

			  // a match!
			  mval.value = outerInstance.scorer.score();
			  mval.exists = true;
			}
			catch (IOException e)
			{
			  throw new Exception("caught exception in QueryDocVals(" + outerInstance.q + ") doc=" + doc, e);
			}
		  }
	  }

	  public override string ToString(int doc)
	  {
		return "query(" + q + ",def=" + defVal + ")=" + floatVal(doc);
	  }
	}
}