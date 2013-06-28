/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NumericTokenStream = Lucene.Net.Analysis.NumericTokenStream;
using Term = Lucene.Net.Index.Term;
using NumericUtils = Lucene.Net.Util.NumericUtils;
using StringHelper = Lucene.Net.Util.StringHelper;
using ToStringUtils = Lucene.Net.Util.ToStringUtils;

namespace Lucene.Net.Search
{
	
	/// <summary> <p/>A <see cref="Query" /> that matches numeric values within a
	/// specified range.  To use this, you must first index the
	/// numeric values using <see cref="NumericField" /> (expert: <see cref="NumericTokenStream" />
	///).  If your terms are instead textual,
	/// you should use <see cref="TermRangeQuery" />.  <see cref="NumericRangeFilter{T}" />
	/// is the filter equivalent of this
	/// query.<p/>
	/// 
	/// <p/>You create a new NumericRangeQuery with the static
	/// factory methods, eg:
	/// 
    /// <code>
	/// Query q = NumericRangeQuery.newFloatRange("weight",
	/// new Float(0.3f), new Float(0.10f),
	/// true, true);
    /// </code>
	/// 
	/// matches all documents whose float valued "weight" field
	/// ranges from 0.3 to 0.10, inclusive.
	/// 
	/// <p/>The performance of NumericRangeQuery is much better
	/// than the corresponding <see cref="TermRangeQuery" /> because the
	/// number of terms that must be searched is usually far
	/// fewer, thanks to trie indexing, described below.<p/>
	/// 
	/// <p/>You can optionally specify a <a
	/// href="#precisionStepDesc"><c>precisionStep</c></a>
	/// when creating this query.  This is necessary if you've
	/// changed this configuration from its default (4) during
	/// indexing.  Lower values consume more disk space but speed
	/// up searching.  Suitable values are between <b>1</b> and
	/// <b>8</b>. A good starting point to test is <b>4</b>,
	/// which is the default value for all <c>Numeric*</c>
	/// classes.  See <a href="#precisionStepDesc">below</a> for
	/// details.
	/// 
	/// <p/>This query defaults to
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/> for
	/// 32 bit (int/float) ranges with precisionStep &lt;8 and 64
	/// bit (long/double) ranges with precisionStep &lt;6.
	/// Otherwise it uses 
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE"/> as the
	/// number of terms is likely to be high.  With precision
	/// steps of &lt;4, this query can be run with one of the
	/// BooleanQuery rewrite methods without changing
	/// BooleanQuery's default max clause count.
	/// 
	/// <p/><font color="red"><b>NOTE:</b> This API is experimental and
	/// might change in incompatible ways in the next release.</font>
	/// 
	/// <br/><h3>How it works</h3>
	/// 
	/// <p/>See the publication about <a target="_blank" href="http://www.panfmp.org">panFMP</a>,
	/// where this algorithm was described (referred to as <c>TrieRangeQuery</c>):
	/// 
	/// <blockquote><strong>Schindler, U, Diepenbroek, M</strong>, 2008.
	/// <em>Generic XML-based Framework for Metadata Portals.</em>
	/// Computers &amp; Geosciences 34 (12), 1947-1955.
	/// <a href="http://dx.doi.org/10.1016/j.cageo.2008.02.023"
	/// target="_blank">doi:10.1016/j.cageo.2008.02.023</a></blockquote>
	/// 
	/// <p/><em>A quote from this paper:</em> Because Apache Lucene is a full-text
	/// search engine and not a conventional database, it cannot handle numerical ranges
	/// (e.g., field value is inside user defined bounds, even dates are numerical values).
	/// We have developed an extension to Apache Lucene that stores
	/// the numerical values in a special string-encoded format with variable precision
	/// (all numerical values like doubles, longs, floats, and ints are converted to
	/// lexicographic sortable string representations and stored with different precisions
	/// (for a more detailed description of how the values are stored,
	/// see <see cref="NumericUtils" />). A range is then divided recursively into multiple intervals for searching:
	/// The center of the range is searched only with the lowest possible precision in the <em>trie</em>,
	/// while the boundaries are matched more exactly. This reduces the number of terms dramatically.<p/>
	/// 
	/// <p/>For the variant that stores long values in 8 different precisions (each reduced by 8 bits) that
	/// uses a lowest precision of 1 byte, the index contains only a maximum of 256 distinct values in the
	/// lowest precision. Overall, a range could consist of a theoretical maximum of
	/// <c>7*255*2 + 255 = 3825</c> distinct terms (when there is a term for every distinct value of an
	/// 8-byte-number in the index and the range covers almost all of them; a maximum of 255 distinct values is used
	/// because it would always be possible to reduce the full 256 values to one term with degraded precision).
	/// In practice, we have seen up to 300 terms in most cases (index with 500,000 metadata records
	/// and a uniform value distribution).<p/>
	/// 
	/// <a name="precisionStepDesc"/><h3>Precision Step</h3>
	/// <p/>You can choose any <c>precisionStep</c> when encoding values.
	/// Lower step values mean more precisions and so more terms in index (and index gets larger).
	/// On the other hand, the maximum number of terms to match reduces, which optimized query speed.
	/// The formula to calculate the maximum term count is:
    /// <code>
	/// n = [ (bitsPerValue/precisionStep - 1) * (2^precisionStep - 1 ) * 2 ] + (2^precisionStep - 1 )
    /// </code>
	/// <p/><em>(this formula is only correct, when <c>bitsPerValue/precisionStep</c> is an integer;
	/// in other cases, the value must be rounded up and the last summand must contain the modulo of the division as
	/// precision step)</em>.
	/// For longs stored using a precision step of 4, <c>n = 15*15*2 + 15 = 465</c>, and for a precision
	/// step of 2, <c>n = 31*3*2 + 3 = 189</c>. But the faster search speed is reduced by more seeking
	/// in the term enum of the index. Because of this, the ideal <c>precisionStep</c> value can only
	/// be found out by testing. <b>Important:</b> You can index with a lower precision step value and test search speed
	/// using a multiple of the original step value.<p/>
	/// 
	/// <p/>Good values for <c>precisionStep</c> are depending on usage and data type:
	/// <list type="bullet">
	/// <item>The default for all data types is <b>4</b>, which is used, when no <c>precisionStep</c> is given.</item>
	/// <item>Ideal value in most cases for <em>64 bit</em> data types <em>(long, double)</em> is <b>6</b> or <b>8</b>.</item>
	/// <item>Ideal value in most cases for <em>32 bit</em> data types <em>(int, float)</em> is <b>4</b>.</item>
	/// <item>Steps <b>&gt;64</b> for <em>long/double</em> and <b>&gt;32</b> for <em>int/float</em> produces one token
	/// per value in the index and querying is as slow as a conventional <see cref="TermRangeQuery" />. But it can be used
	/// to produce fields, that are solely used for sorting (in this case simply use <see cref="int.MaxValue" /> as
	/// <c>precisionStep</c>). Using <see cref="NumericField">NumericFields</see> for sorting
	/// is ideal, because building the field cache is much faster than with text-only numbers.
	/// Sorting is also possible with range query optimized fields using one of the above <c>precisionSteps</c>.</item>
	/// </list>
	/// 
	/// <p/>Comparisons of the different types of RangeQueries on an index with about 500,000 docs showed
	/// that <see cref="TermRangeQuery" /> in boolean rewrite mode (with raised <see cref="BooleanQuery" /> clause count)
	/// took about 30-40 secs to complete, <see cref="TermRangeQuery" /> in constant score filter rewrite mode took 5 secs
	/// and executing this class took &lt;100ms to complete (on an Opteron64 machine, Java 1.5, 8 bit
	/// precision step). This query type was developed for a geographic portal, where the performance for
	/// e.g. bounding boxes or exact date/time stamps is important.<p/>
	/// 
	/// </summary>
	/// <since> 2.9
	/// 
	/// </since>
	[Serializable]
	public sealed class NumericRangeQuery<T> : MultiTermQuery
        where T : struct, IComparable<T> // best equiv constraint for java's number class
	{
        internal NumericRangeQuery(string field, int precisionStep, FieldType.NumericType dataType,
                                   T? min, T? max, bool minInclusive, bool maxInclusive)
            : base(field)
        {
            if (precisionStep < 1)
                throw new ArgumentException("precisionStep must be >= 1");
            this.precisionStep = precisionStep;
            this.dataType = dataType;
            this.min = min;
            this.max = max;
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        protected override TermsEnum GetTermsEnum(Terms terms, Util.AttributeSource atts)
        {
            if (min.HasValue && max.HasValue && (min.Value).CompareTo(max.Value) > 0)
            {
                return TermsEnum.EMPTY;
            }
            return new NumericRangeTermsEnum(terms.Iterator(null));
        }

	    /// <summary>Returns the field name for this query </summary>
	    public string Field
	    {
	        get { return field; }
	    }

	    /// <summary>Returns <c>true</c> if the lower endpoint is inclusive </summary>
	    public bool IncludesMin
	    {
	        get { return minInclusive; }
	    }

	    /// <summary>Returns <c>true</c> if the upper endpoint is inclusive </summary>
	    public bool IncludesMax
	    {
	        get { return maxInclusive; }
	    }

	    /// <summary>Returns the lower value of this range query </summary>
	    public T? Min
	    {
	        get { return min; }
	    }

	    /// <summary>Returns the upper value of this range query </summary>
	    public T? Max
	    {
	        get { return max; }
	    }

		public override string ToString(string field)
		{
			var sb = new System.Text.StringBuilder();
			if (!this.field.Equals(field))
				sb.Append(this.field).Append(':');
            return sb.Append(minInclusive ? '[' : '{')
                .Append((min == null) ? "*" : min.ToString())
                .Append(" TO ")
                .Append((max == null) ? "*" : max.ToString())
                .Append(maxInclusive ? ']' : '}')
                .Append(ToStringUtils.Boost(Boost))
                .ToString();
        }
		
		public  override bool Equals(object o)
		{
			if (o == this)
				return true;
			if (!base.Equals(o))
				return false;
			if (o is NumericRangeQuery<T>)
			{
                var q = (NumericRangeQuery<T>)o;
                return (field == q.field 
                    && (q.min == null ? min == null : q.min.Equals(min)) 
                    && (q.max == null ? max == null : q.max.Equals(max)) 
                    && minInclusive == q.minInclusive 
                    && maxInclusive == q.maxInclusive 
                    && precisionStep == q.precisionStep);
            }
			return false;
		}
		
		public override int GetHashCode()
		{
			int hash = base.GetHashCode();
            hash += (field.GetHashCode() ^ 0x4565fd66 + precisionStep ^ 0x64365465);
            if (min != null)
                hash += (min.GetHashCode() ^ 0x14fa55fb);
            if (max != null)
                hash += (max.GetHashCode() ^ 0x733fa5fe);
			return hash + (minInclusive.GetHashCode() ^ 0x14fa55fb) + (maxInclusive.GetHashCode() ^ 0x733fa5fe);
		}


        [System.Runtime.Serialization.OnDeserialized]
        internal void OnDeserialized(System.Runtime.Serialization.StreamingContext context)
        {
            field = StringHelper.Intern(field);
        }
		
		// members (package private, to be also fast accessible by NumericRangeTermsEnum)
		internal string field;
		internal int precisionStep;
	    internal FieldType.NumericType dataType;
		internal T? min;
		internal T? max;
		internal bool minInclusive;
		internal bool maxInclusive;

	    internal static readonly long LONG_NEGATIVE_INFINITY =
	        NumericUtils.DoubleToSortableLong(double.NegativeInfinity);

	    internal static readonly long LONG_POSITIVE_INFINITY =
	        NumericUtils.DoubleToSortableLong(double.PositiveInfinity);

	    internal static readonly int INT_NEGATIVE_INFINITY =
	        NumericUtils.FloatToSortableInt(float.NegativeInfinity);

	    internal static readonly int INT_POSITIVE_INFINITY =
	        NumericUtils.FloatToSortableInt(float.PositiveInfinity);

		/// <summary> Subclass of FilteredTermEnum for enumerating all terms that match the
		/// sub-ranges for trie range queries.
		/// <p/>
		/// WARNING: This term enumeration is not guaranteed to be always ordered by
		/// <see cref="Term.CompareTo(Term)" />.
		/// The ordering depends on how <see cref="NumericUtils.SplitLongRange" /> and
		/// <see cref="NumericUtils.SplitIntRange" /> generates the sub-ranges. For
		/// <see cref="MultiTermQuery" /> ordering is not relevant.
		/// </summary>
		private sealed class NumericRangeTermsEnum : FilteredTermsEnum
		{
			private class AnonymousClassLongRangeBuilder:NumericUtils.LongRangeBuilder
			{
				public AnonymousClassLongRangeBuilder(NumericRangeTermsEnum parent)
				{
				    this.parent = parent;
				}
				private NumericRangeTermsEnum parent;

				//@Override
                public override void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
				{
                    parent.rangeBounds.AddLast(minPrefixCoded);
                    parent.rangeBounds.AddLast(maxPrefixCoded);
				}
			}
			private class AnonymousClassIntRangeBuilder:NumericUtils.IntRangeBuilder
			{
				public AnonymousClassIntRangeBuilder(NumericRangeTermsEnum parent)
				{
				    this.parent = parent;
				}
				private NumericRangeTermsEnum parent;

				//@Override
                public override void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
				{
                    parent.rangeBounds.AddLast(minPrefixCoded);
                    parent.rangeBounds.AddLast(maxPrefixCoded);
				}
			}

            private NumericRangeQuery<T> parent;

		    private BytesRef currentLowerBound, currentUpperBound;
			private readonly LinkedList<BytesRef> rangeBounds = new LinkedList<BytesRef>();
		    private readonly IComparer<BytesRef> termComp;


            internal NumericRangeTermsEnum(NumericRangeQuery<T> parent, TermsEnum termsEnum)
                : base(termsEnum)
            {
                this.parent = parent;

                switch (parent.dataType)
				{
				    case FieldType.NumericType.LONG:
                    case FieldType.NumericType.DOUBLE:
				        {
				            long minBound;
                            if (parent.dataType == FieldType.NumericType.LONG)
                            {
                                minBound = (parent.min == null) ? long.MinValue : Convert.ToInt64(parent.min.Value);
                            }
                            else
                            {
                                minBound = (parent.min == null)
                                               ? LONG_NEGATIVE_INFINITY
                                               : NumericUtils.DoubleToSortableLong(Convert.ToDouble(parent.min.Value));
                            }
                            if (!parent.minInclusive && parent.min != null)
                            {
                                if (minBound == long.MaxValue) break;
                                minBound++;
                            }

				            long maxBound;
                            if (parent.dataType == FieldType.NumericType.LONG)
                            {
                                maxBound = (parent.max == null) ? long.MaxValue : Convert.ToInt64(parent.max);
                            }
                            else
                            {
                                maxBound = (parent.max == null)
                                               ? LONG_POSITIVE_INFINITY
                                               : NumericUtils.DoubleToSortableLong(Convert.ToDouble(parent.max));
                            }
                            if (!parent.maxInclusive && parent.max != null)
                            {
                                if (maxBound == long.MinValue) break;
                                maxBound--;
                            }

                            NumericUtils.SplitLongRange(new AnonymousClassLongRangeBuilder(this), parent.precisionStep, minBound, maxBound);

				            break;
				        }
                    case FieldType.NumericType.INT:
                    case FieldType.NumericType.FLOAT:
				        {
				            int minBound;
                            if (parent.dataType == FieldType.NumericType.INT)
                            {
                                minBound = (parent.min == null) ? int.MinValue : Convert.ToInt32(parent.min);
                            }
                            else
                            {
                                minBound = (parent.min == null)
                                               ? INT_NEGATIVE_INFINITY
                                               : NumericUtils.FloatToSortableInt(Convert.ToSingle(parent.min));
                            }
                            if (!parent.minInclusive && parent.min != null)
                            {
                                if (minBound == int.MaxValue) break;
                                minBound++;
                            }

				            int maxBound;
                            if (parent.dataType == FieldType.NumericType.INT)
                            {
                                maxBound = (parent.max == null) ? int.MaxValue : Convert.ToInt32(parent.max);
                            }
                            else
                            {
                                maxBound = (parent.max == null)
                                               ? INT_POSITIVE_INFINITY
                                               : NumericUtils.FloatToSortableInt(Convert.ToSingle(parent.max));
                            }
                            if (!parent.maxInclusive && maxBound != null)
                            {
                                if (maxBound == int.MaxValue) break;
                                maxBound--;
                            }

				            NumericUtils.SplitIntRange(new AnonymousClassIntRangeBuilder(this), parent.precisionStep, minBound, maxBound);

				            break;
				        }
                    default:
				        throw new ArgumentException("Invalid NumericType");
				}

                termComp = Comparator;
            }
			
            private void NextRange()
            {
                // assert rangeBounds.size() % 2 == 0;
                currentLowerBound = rangeBounds.First.Value;
                rangeBounds.RemoveFirst();

                // assert currentUpperBound == null || termComp.compare(currentUpperBound, currentLowerBound) <= 0 :
                    // "The current upper bound must be <= the new lower bound";
                currentUpperBound = rangeBounds.First.Value;
                rangeBounds.RemoveFirst();
            }

            protected sealed override BytesRef NextSeekTerm(BytesRef currentTerm)
            {
                while (rangeBounds.Count >= 2)
                {
                    NextRange();

                    if (currentTerm != null && termComp.Compare(currentTerm, currentUpperBound) > 0)
                        continue;

                    return (currentTerm != null && termComp.Compare(currentTerm, currentLowerBound) > 0) ? currentTerm : currentLowerBound;
                }

                // assert !rangeBounds.Any();
                currentLowerBound = currentUpperBound = null;
                return null;
            }

            protected sealed override AcceptStatus Accept(BytesRef term)
            {
                while (currentUpperBound == null || termComp.Compare(term, currentUpperBound) > 0)
                {
                    if (!rangeBounds.Any())
                        return AcceptStatus.END;
                    if (termComp.Compare(term, rangeBounds.First.Value) < 0)
                        return AcceptStatus.NO_AND_SEEK;

                    NextRange();
                }
                return AcceptStatus.YES;
            }
		}
	}

    public static class NumericRangeQuery
    {
        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>long</c>
        /// range using the given <a href="#precisionStepDesc"><c>precisionStep</c></a>.
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<long> NewLongRange(string field, int precisionStep, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, precisionStep, FieldType.NumericType.LONG, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>long</c>
        /// range using the default <c>precisionStep</c> <see cref="NumericUtils.PRECISION_STEP_DEFAULT" /> (4).
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<long> NewLongRange(string field, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, NumericUtils.PRECISION_STEP_DEFAULT, FieldType.NumericType.LONG, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>int</c>
        /// range using the given <a href="#precisionStepDesc"><c>precisionStep</c></a>.
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<int> NewIntRange(string field, int precisionStep, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, precisionStep, FieldType.NumericType.INT, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>int</c>
        /// range using the default <c>precisionStep</c> <see cref="NumericUtils.PRECISION_STEP_DEFAULT" /> (4).
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<int> NewIntRange(string field, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, NumericUtils.PRECISION_STEP_DEFAULT, FieldType.NumericType.INT, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>double</c>
        /// range using the given <a href="#precisionStepDesc"><c>precisionStep</c></a>.
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, int precisionStep, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, precisionStep, FieldType.NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>double</c>
        /// range using the default <c>precisionStep</c> <see cref="NumericUtils.PRECISION_STEP_DEFAULT" /> (4).
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, NumericUtils.PRECISION_STEP_DEFAULT, FieldType.NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>float</c>
        /// range using the given <a href="#precisionStepDesc"><c>precisionStep</c></a>.
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<float> NewFloatRange(string field, int precisionStep, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, precisionStep, FieldType.NumericType.FLOAT, min, max, minInclusive, maxInclusive);
        }

        /// <summary> Factory that creates a <c>NumericRangeQuery</c>, that queries a <c>float</c>
        /// range using the default <c>precisionStep</c> <see cref="NumericUtils.PRECISION_STEP_DEFAULT" /> (4).
        /// You can have half-open ranges (which are in fact &lt;/&#8804; or &gt;/&#8805; queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<float> NewFloatRange(string field, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, NumericUtils.PRECISION_STEP_DEFAULT, FieldType.NumericType.FLOAT, min, max, minInclusive, maxInclusive);
        }
    }
}