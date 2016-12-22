using Lucene.Net.Support;
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
    using Bits = Lucene.Net.Util.Bits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// <p>Wrapper to allow <seealso cref="SpanQuery"/> objects participate in composite
    /// single-field SpanQueries by 'lying' about their search field. That is,
    /// the masked SpanQuery will function as normal,
    /// but <seealso cref="SpanQuery#getField()"/> simply hands back the value supplied
    /// in this class's constructor.</p>
    ///
    /// <p>this can be used to support Queries like <seealso cref="SpanNearQuery"/> or
    /// <seealso cref="SpanOrQuery"/> across different fields, which is not ordinarily
    /// permitted.</p>
    ///
    /// <p>this can be useful for denormalized relational data: for example, when
    /// indexing a document with conceptually many 'children': </p>
    ///
    /// <pre>
    ///  teacherid: 1
    ///  studentfirstname: james
    ///  studentsurname: jones
    ///
    ///  teacherid: 2
    ///  studenfirstname: james
    ///  studentsurname: smith
    ///  studentfirstname: sally
    ///  studentsurname: jones
    /// </pre>
    ///
    /// <p>a SpanNearQuery with a slop of 0 can be applied across two
    /// <seealso cref="SpanTermQuery"/> objects as follows:
    /// <pre class="prettyprint">
    ///    SpanQuery q1  = new SpanTermQuery(new Term("studentfirstname", "james"));
    ///    SpanQuery q2  = new SpanTermQuery(new Term("studentsurname", "jones"));
    ///    SpanQuery q2m = new FieldMaskingSpanQuery(q2, "studentfirstname");
    ///    Query q = new SpanNearQuery(new SpanQuery[]{q1, q2m}, -1, false);
    /// </pre>
    /// to search for 'studentfirstname:james studentsurname:jones' and find
    /// teacherid 1 without matching teacherid 2 (which has a 'james' in position 0
    /// and 'jones' in position 1). </p>
    ///
    /// <p>Note: as <seealso cref="#getField()"/> returns the masked field, scoring will be
    /// done using the Similarity and collection statistics of the field name supplied,
    /// but with the term statistics of the real field. this may lead to exceptions,
    /// poor performance, and unexpected scoring behaviour.</p>
    /// </summary>
    public class FieldMaskingSpanQuery : SpanQuery
    {
        private SpanQuery maskedQuery;
        private string field;

        public FieldMaskingSpanQuery(SpanQuery maskedQuery, string maskedField)
        {
            this.maskedQuery = maskedQuery;
            this.field = maskedField;
        }

        public override string Field
        {
            get
            {
                return field;
            }
        }

        public virtual SpanQuery MaskedQuery
        {
            get
            {
                return maskedQuery;
            }
        }

        // :NOTE: getBoost and setBoost are not proxied to the maskedQuery
        // ...this is done to be more consistent with things like SpanFirstQuery

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
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
            buffer.Append(")");
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
            return (this.Field.Equals(other.Field) && (this.Boost == other.Boost) && this.MaskedQuery.Equals(other.MaskedQuery));
        }

        public override int GetHashCode()
        {
            return MaskedQuery.GetHashCode() ^ Field.GetHashCode() ^ Number.FloatToIntBits(Boost);
        }
    }
}