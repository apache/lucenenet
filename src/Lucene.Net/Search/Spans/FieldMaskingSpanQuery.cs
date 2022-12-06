using Lucene.Net.Util;
using System;
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
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// <para>Wrapper to allow <see cref="SpanQuery"/> objects participate in composite
    /// single-field SpanQueries by 'lying' about their search field. That is,
    /// the masked <see cref="SpanQuery"/> will function as normal,
    /// but <see cref="SpanQuery.Field"/> simply hands back the value supplied
    /// in this class's constructor.</para>
    ///
    /// <para>This can be used to support Queries like <see cref="SpanNearQuery"/> or
    /// <see cref="SpanOrQuery"/> across different fields, which is not ordinarily
    /// permitted.</para>
    ///
    /// <para>This can be useful for denormalized relational data: for example, when
    /// indexing a document with conceptually many 'children': </para>
    ///
    /// <code>
    ///  teacherid: 1
    ///  studentfirstname: james
    ///  studentsurname: jones
    ///
    ///  teacherid: 2
    ///  studenfirstname: james
    ///  studentsurname: smith
    ///  studentfirstname: sally
    ///  studentsurname: jones
    /// </code>
    ///
    /// <para>A <see cref="SpanNearQuery"/> with a slop of 0 can be applied across two
    /// <see cref="SpanTermQuery"/> objects as follows:
    /// <code>
    ///     SpanQuery q1  = new SpanTermQuery(new Term("studentfirstname", "james"));
    ///     SpanQuery q2  = new SpanTermQuery(new Term("studentsurname", "jones"));
    ///     SpanQuery q2m = new FieldMaskingSpanQuery(q2, "studentfirstname");
    ///     Query q = new SpanNearQuery(new SpanQuery[] { q1, q2m }, -1, false);
    /// </code>
    /// to search for 'studentfirstname:james studentsurname:jones' and find
    /// teacherid 1 without matching teacherid 2 (which has a 'james' in position 0
    /// and 'jones' in position 1). </para>
    ///
    /// <para>Note: as <see cref="Field"/> returns the masked field, scoring will be
    /// done using the <see cref="Similarities.Similarity"/> and collection statistics of the field name supplied,
    /// but with the term statistics of the real field. This may lead to exceptions,
    /// poor performance, and unexpected scoring behavior.</para>
    /// </summary>
    public class FieldMaskingSpanQuery : SpanQuery
    {
        private SpanQuery maskedQuery;
        private readonly string field; // LUCENENET: marked readonly

        public FieldMaskingSpanQuery(SpanQuery maskedQuery, string maskedField)
        {
            this.maskedQuery = maskedQuery;
            this.field = maskedField;
        }

        public override string Field => field;

        public virtual SpanQuery MaskedQuery => maskedQuery;

        // :NOTE: getBoost and setBoost are not proxied to the maskedQuery
        // ...this is done to be more consistent with things like SpanFirstQuery

        public override Spans GetSpans(AtomicReaderContext context, IBits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return maskedQuery.GetSpans(context, acceptDocs, termContexts);
        }

        public override void ExtractTerms(ISet<Term> terms)
        {
            maskedQuery.ExtractTerms(terms);
        }

        public override Weight CreateWeight(IndexSearcher searcher)
        {
            return maskedQuery.CreateWeight(searcher);
        }

        public override Query Rewrite(IndexReader reader)
        {
            FieldMaskingSpanQuery clone = null;

            SpanQuery rewritten = (SpanQuery)maskedQuery.Rewrite(reader);
            if (rewritten != maskedQuery)
            {
                clone = (FieldMaskingSpanQuery)this.Clone();
                clone.maskedQuery = rewritten;
            }

            if (clone != null)
            {
                return clone;
            }
            else
            {
                return this;
            }
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("mask(");
            buffer.Append(maskedQuery.ToString(field));
            buffer.Append(')');
            buffer.Append(ToStringUtils.Boost(Boost));
            buffer.Append(" as ");
            buffer.Append(this.field);
            return buffer.ToString();
        }

        public override bool Equals(object o)
        {
            if (!(o is FieldMaskingSpanQuery))
            {
                return false;
            }
            FieldMaskingSpanQuery other = (FieldMaskingSpanQuery)o;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return (this.Field.Equals(other.Field, StringComparison.Ordinal)
                && (NumericUtils.SingleToSortableInt32(this.Boost) == NumericUtils.SingleToSortableInt32(other.Boost))
                && this.MaskedQuery.Equals(other.MaskedQuery));
        }

        public override int GetHashCode()
        {
            return MaskedQuery.GetHashCode() ^ Field.GetHashCode() ^ J2N.BitConversion.SingleToRawInt32Bits(Boost);
        }
    }
}