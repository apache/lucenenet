namespace org.apache.lucene.queries
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
	using DocsEnum = org.apache.lucene.index.DocsEnum;
	using Term = org.apache.lucene.index.Term;
	using Terms = org.apache.lucene.index.Terms;
	using TermsEnum = org.apache.lucene.index.TermsEnum;
	using DocIdSet = org.apache.lucene.search.DocIdSet;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using Filter = org.apache.lucene.search.Filter;
	using Bits = org.apache.lucene.util.Bits;

	/// <summary>
	/// A filter that includes documents that match with a specific term.
	/// </summary>
	public sealed class TermFilter : Filter
	{

	  private readonly Term term;

	  /// <param name="term"> The term documents need to have in order to be a match for this filter. </param>
	  public TermFilter(Term term)
	  {
		if (term == null)
		{
		  throw new System.ArgumentException("Term must not be null");
		}
		else if (term.field() == null)
		{
		  throw new System.ArgumentException("Field must not be null");
		}
		this.term = term;
	  }

	  /// <returns> The term this filter includes documents with. </returns>
	  public Term Term
	  {
		  get
		  {
			return term;
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.DocIdSet getDocIdSet(org.apache.lucene.index.AtomicReaderContext context, final org.apache.lucene.util.Bits acceptDocs) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override DocIdSet getDocIdSet(AtomicReaderContext context, Bits acceptDocs)
	  {
		Terms terms = context.reader().terms(term.field());
		if (terms == null)
		{
		  return null;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.TermsEnum termsEnum = terms.iterator(null);
		TermsEnum termsEnum = terms.iterator(null);
		if (!termsEnum.seekExact(term.bytes()))
		{
		  return null;
		}
		return new DocIdSetAnonymousInnerClassHelper(this, acceptDocs, termsEnum);
	  }

	  private class DocIdSetAnonymousInnerClassHelper : DocIdSet
	  {
		  private readonly TermFilter outerInstance;

		  private Bits acceptDocs;
		  private TermsEnum termsEnum;

		  public DocIdSetAnonymousInnerClassHelper(TermFilter outerInstance, Bits acceptDocs, TermsEnum termsEnum)
		  {
			  this.outerInstance = outerInstance;
			  this.acceptDocs = acceptDocs;
			  this.termsEnum = termsEnum;
		  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.search.DocIdSetIterator iterator() throws java.io.IOException
		  public override DocIdSetIterator iterator()
		  {
			return termsEnum.docs(acceptDocs, null, DocsEnum.FLAG_NONE);
		  }

	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (o == null || this.GetType() != o.GetType())
		{
			return false;
		}

		TermFilter that = (TermFilter) o;

		if (term != null ?!term.Equals(that.term) : that.term != null)
		{
			return false;
		}

		return true;
	  }

	  public override int GetHashCode()
	  {
		return term != null ? term.GetHashCode() : 0;
	  }

	  public override string ToString()
	  {
		return term.field() + ":" + term.text();
	  }

	}

}