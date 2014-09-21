using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;

namespace Lucene.Net.Queries
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
    /// <summary>
	/// Constructs a filter for docs matching any of the terms added to this class.
	/// Unlike a RangeFilter this can be used for filtering on multiple terms that are not necessarily in
	/// a sequence. An example might be a collection of primary keys from a database query result or perhaps
	/// a choice of "category" labels picked by the end user. As a filter, this is much faster than the
	/// equivalent query (a BooleanQuery with many "should" TermQueries)
	/// </summary>
	public sealed class TermsFilter : Filter
	{

	  /*
	   * this class is often used for large number of terms in a single field.
	   * to optimize for this case and to be filter-cache friendly we 
	   * serialize all terms into a single byte array and store offsets
	   * in a parallel array to keep the # of object constant and speed up
	   * equals / hashcode.
	   * 
	   * This adds quite a bit of complexity but allows large term filters to
	   * be efficient for GC and cache-lookups
	   */
	  private readonly int[] offsets;
	  private readonly sbyte[] termsBytes;
	  private readonly TermsAndField[] termsAndFields;
	  private readonly int hashCode_Renamed; // cached hashcode for fast cache lookups
	  private const int PRIME = 31;

	  /// <summary>
	  /// Creates a new <seealso cref="TermsFilter"/> from the given list. The list
	  /// can contain duplicate terms and multiple fields.
	  /// </summary>
	  public TermsFilter(IList<Term> terms) : this(new FieldAndTermEnumAnonymousInnerClassHelper(this, terms), terms.Count)
	  {
	  }

	  private class FieldAndTermEnumAnonymousInnerClassHelper : FieldAndTermEnum
	  {
		  private readonly TermsFilter outerInstance;

		  private IList<Term> terms;

		  public FieldAndTermEnumAnonymousInnerClassHelper(TermsFilter outerInstance, IList<Term> terms)
		  {
			  this.outerInstance = outerInstance;
			  this.terms = terms;
			  iter = Sort(terms).GetEnumerator();
		  }

			// we need to sort for deduplication and to have a common cache key
		  readonly IEnumerator<Term> iter;
		  public override BytesRef Next()
		  {
			if (iter.HasNext())
			{
			  Term next = iter.next();
			  field = next.field();
			  return next.bytes();
			}
			return null;
		  }
	  }

	  /// <summary>
	  /// Creates a new <seealso cref="TermsFilter"/> from the given <seealso cref="BytesRef"/> list for
	  /// a single field.
	  /// </summary>
	  public TermsFilter(string field, IList<BytesRef> terms) : this(new FieldAndTermEnumAnonymousInnerClassHelper2(this, field, terms), terms.Count)
	  {
	  }

	  private class FieldAndTermEnumAnonymousInnerClassHelper2 : FieldAndTermEnum
	  {
		  private readonly TermsFilter outerInstance;

		  private IList<BytesRef> terms;

		  public FieldAndTermEnumAnonymousInnerClassHelper2(TermsFilter outerInstance, string field, IList<BytesRef> terms) : base(field)
		  {
			  this.outerInstance = outerInstance;
			  this.terms = terms;
			  iter = Sort(terms).GetEnumerator();
		  }

			// we need to sort for deduplication and to have a common cache key
		  readonly IEnumerator<BytesRef> iter;
		  public override BytesRef Next()
		  {
			if (iter.HasNext())
			{
			  return iter.Next();
			}
			return null;
		  }
	  }

	  /// <summary>
	  /// Creates a new <seealso cref="TermsFilter"/> from the given <seealso cref="BytesRef"/> array for
	  /// a single field.
	  /// </summary>
	  public TermsFilter(string field, params BytesRef[] terms) : this(field, Arrays.AsList(terms))
	  {
		// this ctor prevents unnecessary Term creations
	  }

	  /// <summary>
	  /// Creates a new <seealso cref="TermsFilter"/> from the given array. The array can
	  /// contain duplicate terms and multiple fields.
	  /// </summary>
	  public TermsFilter(params Term[] terms) : this(terms.ToList())
	  {
	  }


	  private TermsFilter(FieldAndTermEnum iter, int length)
	  {
		// TODO: maybe use oal.index.PrefixCodedTerms instead?
		// If number of terms is more than a few hundred it
		// should be a win

		// TODO: we also pack terms in FieldCache/DocValues
		// ... maybe we can refactor to share that code

		// TODO: yet another option is to build the union of the terms in
		// an automaton an call intersect on the termsenum if the density is high

		int hash = 9;
		sbyte[] serializedTerms = new sbyte[0];
		this.offsets = new int[length + 1];
		int lastEndOffset = 0;
		int index = 0;
		List<TermsAndField> termsAndFields = new List<TermsAndField>();
		TermsAndField lastTermsAndField = null;
		BytesRef previousTerm = null;
		string previousField = null;
		BytesRef currentTerm;
		string currentField;
		while ((currentTerm = iter.Next()) != null)
		{
		  currentField = iter.Field();
		  if (currentField == null)
		  {
			throw new System.ArgumentException("Field must not be null");
		  }
		  if (previousField != null)
		  {
			// deduplicate
			if (previousField.Equals(currentField))
			{
			  if (previousTerm.BytesEquals(currentTerm))
			  {
				continue;
			  }
			}
			else
			{
			  int start = lastTermsAndField == null ? 0 : lastTermsAndField.end;
			  lastTermsAndField = new TermsAndField(start, index, previousField);
			  termsAndFields.Add(lastTermsAndField);
			}
		  }
		  hash = PRIME * hash + currentField.GetHashCode();
		  hash = PRIME * hash + currentTerm.GetHashCode();
		  if (serializedTerms.Length < lastEndOffset + currentTerm.length)
		  {
			serializedTerms = ArrayUtil.grow(serializedTerms, lastEndOffset + currentTerm.length);
		  }
		  Array.Copy(currentTerm.bytes, currentTerm.offset, serializedTerms, lastEndOffset, currentTerm.length);
		  offsets[index] = lastEndOffset;
		  lastEndOffset += currentTerm.length;
		  index++;
		  previousTerm = currentTerm;
		  previousField = currentField;
		}
		offsets[index] = lastEndOffset;
		int start = lastTermsAndField == null ? 0 : lastTermsAndField.end;
		lastTermsAndField = new TermsAndField(start, index, previousField);
		termsAndFields.Add(lastTermsAndField);
		this.termsBytes = ArrayUtil.Shrink(serializedTerms, lastEndOffset);
		this.termsAndFields = termsAndFields.ToArray();
		this.hashCode_Renamed = hash;

	  }

	  public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
	  {
		AtomicReader reader = context.AtomicReader;
		FixedBitSet result = null; // lazy init if needed - no need to create a big bitset ahead of time
		Fields fields = reader.Fields;
		BytesRef spare = new BytesRef(this.termsBytes);
		if (fields == null)
		{
		  return result;
		}
		Terms terms = null;
		TermsEnum termsEnum = null;
		DocsEnum docs = null;
		foreach (TermsAndField termsAndField in this.termsAndFields)
		{
		  if ((terms = fields.Terms(termsAndField.field)) != null)
		  {
			termsEnum = terms.iterator(termsEnum); // this won't return null
			for (int i = termsAndField.start; i < termsAndField.end; i++)
			{
			  spare.offset = offsets[i];
			  spare.length = offsets[i + 1] - offsets[i];
			  if (termsEnum.seekExact(spare))
			  {
				docs = termsEnum.docs(acceptDocs, docs, DocsEnum.FLAG_NONE); // no freq since we don't need them
				if (result == null)
				{
				  if (docs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				  {
					result = new FixedBitSet(reader.maxDoc());
					// lazy init but don't do it in the hot loop since we could read many docs
					result.set(docs.docID());
				  }
				}
				while (docs.nextDoc() != DocIdSetIterator.NO_MORE_DOCS)
				{
				  result.set(docs.docID());
				}
			  }
			}
		  }
		}
		return result;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
		  return true;
		}
		if ((obj == null) || (obj.GetType() != this.GetType()))
		{
		  return false;
		}

		TermsFilter test = (TermsFilter) obj;
		// first check the fields before even comparing the bytes
		if (test.hashCode_Renamed == hashCode_Renamed && Arrays.Equals(termsAndFields, test.termsAndFields))
		{
		  int lastOffset = termsAndFields[termsAndFields.Length - 1].end;
		  // compare offsets since we sort they must be identical
		  if (ArrayUtil.Equals(offsets, 0, test.offsets, 0, lastOffset + 1))
		  {
			// straight byte comparison since we sort they must be identical
			return ArrayUtil.Equals(termsBytes, 0, test.termsBytes, 0, offsets[lastOffset]);
		  }
		}
		return false;
	  }

	  public override int GetHashCode()
	  {
		return hashCode_Renamed;
	  }

	  public override string ToString()
	  {
		StringBuilder builder = new StringBuilder();
		BytesRef spare = new BytesRef(termsBytes);
		bool first = true;
		for (int i = 0; i < termsAndFields.Length; i++)
		{
		  TermsAndField current = termsAndFields[i];
		  for (int j = current.start; j < current.end; j++)
		  {
			spare.Offset = offsets[j];
			spare.Length = offsets[j + 1] - offsets[j];
			if (!first)
			{
			  builder.Append(' ');
			}
			first = false;
			builder.Append(current.field).Append(':');
			builder.Append(spare.Utf8ToString());
		  }
		}

		return builder.ToString();
	  }

	  private sealed class TermsAndField
	  {
		internal readonly int start;
		internal readonly int end;
		internal readonly string field;


		internal TermsAndField(int start, int end, string field) : base()
		{
		  this.start = start;
		  this.end = end;
		  this.field = field;
		}

		public override int GetHashCode()
		{
		  const int prime = 31;
		  int result = 1;
		  result = prime * result + ((field == null) ? 0 : field.GetHashCode());
		  result = prime * result + end;
		  result = prime * result + start;
		  return result;
		}

		public override bool Equals(object obj)
		{
		  if (this == obj)
		  {
			  return true;
		  }
		  if (obj == null)
		  {
			  return false;
		  }
		  if (this.GetType() != obj.GetType())
		  {
			  return false;
		  }
		  TermsAndField other = (TermsAndField) obj;
		  if (field == null)
		  {
			if (other.field != null)
			{
				return false;
			}
		  }
		  else if (!field.Equals(other.field))
		  {
			  return false;
		  }
		  if (end != other.end)
		  {
			  return false;
		  }
		  if (start != other.start)
		  {
			  return false;
		  }
		  return true;
		}

	  }

	  private abstract class FieldAndTermEnum
	  {
		protected internal string field;

		public abstract BytesRef Next();

		public FieldAndTermEnum()
		{
		}

		public FieldAndTermEnum(string field)
		{
			this.field = field;
		}

	      public virtual string Field
	      {
	          get { return field; }
	      }
	  }

	  /*
	   * simple utility that returns the in-place sorted list
	   */
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private static <T extends Comparable<? base T>> java.util.List<T> sort(java.util.List<T> toSort)
	  private static IList<T> Sort<T>(IList<T> toSort) where T : Comparable<? base T>
	  {
		if (toSort.Count == 0)
		{
		  throw new System.ArgumentException("no terms provided");
		}
		toSort.Sort();
		return toSort;
	  }
	}

}