using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Search.Spans
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

	using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
	using Fields = Lucene.Net.Index.Fields;
	using Term = Lucene.Net.Index.Term;
	using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
	using TermContext = Lucene.Net.Index.TermContext;
	using TermState = Lucene.Net.Index.TermState;
	using Terms = Lucene.Net.Index.Terms;
	using TermsEnum = Lucene.Net.Index.TermsEnum;
	using Bits = Lucene.Net.Util.Bits;
	using ToStringUtils = Lucene.Net.Util.ToStringUtils;


	/// <summary>
	/// Matches spans containing a term. </summary>
	public class SpanTermQuery : SpanQuery
	{
	  protected internal Term Term_Renamed;

	  /// <summary>
	  /// Construct a SpanTermQuery matching the named term's spans. </summary>
	  public SpanTermQuery(Term term)
	  {
		  this.Term_Renamed = term;
	  }

	  /// <summary>
	  /// Return the term whose spans are matched. </summary>
	  public virtual Term Term
	  {
		  get
		  {
			  return Term_Renamed;
		  }
	  }

	  public override string Field
	  {
		  get
		  {
			  return Term_Renamed.Field();
		  }
	  }
	  public override void ExtractTerms(Set<Term> terms)
	  {
		terms.add(Term_Renamed);
	  }

	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		if (Term_Renamed.Field().Equals(field))
		{
		  buffer.Append(Term_Renamed.Text());
		}
		else
		{
		  buffer.Append(Term_Renamed.ToString());
		}
		buffer.Append(ToStringUtils.Boost(Boost));
		return buffer.ToString();
	  }

	  public override int HashCode()
	  {
		const int prime = 31;
		int result = base.HashCode();
		result = prime * result + ((Term_Renamed == null) ? 0 : Term_Renamed.HashCode());
		return result;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
		  return true;
		}
		if (!base.Equals(obj))
		{
		  return false;
		}
		if (this.GetType() != obj.GetType())
		{
		  return false;
		}
		SpanTermQuery other = (SpanTermQuery) obj;
		if (Term_Renamed == null)
		{
		  if (other.Term_Renamed != null)
		  {
			return false;
		  }
		}
		else if (!Term_Renamed.Equals(other.Term_Renamed))
		{
		  return false;
		}
		return true;
	  }

	  public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
	  {
		TermContext termContext = termContexts[Term_Renamed];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermState state;
		TermState state;
		if (termContext == null)
		{
		  // this happens with span-not query, as it doesn't include the NOT side in extractTerms()
		  // so we seek to the term now in this segment..., this sucks because its ugly mostly!
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Fields fields = context.reader().fields();
		  Fields fields = context.Reader().fields();
		  if (fields != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.Terms terms = fields.terms(term.field());
			Terms terms = fields.Terms(Term_Renamed.Field());
			if (terms != null)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermsEnum termsEnum = terms.iterator(null);
			  TermsEnum termsEnum = terms.Iterator(null);
			  if (termsEnum.SeekExact(Term_Renamed.Bytes()))
			  {
				state = termsEnum.TermState();
			  }
			  else
			  {
				state = null;
			  }
			}
			else
			{
			  state = null;
			}
		  }
		  else
		  {
			state = null;
		  }
		}
		else
		{
		  state = termContext.Get(context.Ord);
		}

		if (state == null) // term is not present in that reader
		{
		  return TermSpans.EMPTY_TERM_SPANS;
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.TermsEnum termsEnum = context.reader().terms(term.field()).iterator(null);
		TermsEnum termsEnum = context.Reader().terms(Term_Renamed.Field()).iterator(null);
		termsEnum.SeekExact(Term_Renamed.Bytes(), state);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Index.DocsAndPositionsEnum postings = termsEnum.docsAndPositions(acceptDocs, null, Lucene.Net.Index.DocsAndPositionsEnum.FLAG_PAYLOADS);
		DocsAndPositionsEnum postings = termsEnum.DocsAndPositions(acceptDocs, null, DocsAndPositionsEnum.FLAG_PAYLOADS);

		if (postings != null)
		{
		  return new TermSpans(postings, Term_Renamed);
		}
		else
		{
		  // term does exist, but has no positions
		  throw new IllegalStateException("field \"" + Term_Renamed.Field() + "\" was indexed without position data; cannot run SpanTermQuery (term=" + Term_Renamed.Text() + ")");
		}
	  }
	}

}