using Lucene.Net.Diagnostics;
using Lucene.Net.Index;

namespace Lucene.Net.Search
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

    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Fields = Lucene.Net.Index.Fields;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IBits = Lucene.Net.Util.IBits;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// A wrapper for <see cref="MultiTermQuery"/>, that exposes its
    /// functionality as a <see cref="Filter"/>.
    /// <para/>
    /// <see cref="MultiTermQueryWrapperFilter{Q}"/> is not designed to
    /// be used by itself. Normally you subclass it to provide a <see cref="Filter"/>
    /// counterpart for a <see cref="MultiTermQuery"/> subclass.
    /// <para/>
    /// For example, <see cref="TermRangeFilter"/> and <see cref="PrefixFilter"/> extend
    /// <see cref="MultiTermQueryWrapperFilter{Q}"/>.
    /// This class also provides the functionality behind
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE"/>;
    /// this is why it is not abstract.
    /// </summary>
    public class MultiTermQueryWrapperFilter<Q> : Filter where Q : MultiTermQuery
    {
        protected readonly Q m_query;

        /// <summary>
        /// Wrap a <see cref="MultiTermQuery"/> as a <see cref="Filter"/>.
        /// </summary>
        protected internal MultiTermQueryWrapperFilter(Q query)
        {
            this.m_query = query;
        }

        public override string ToString()
        {
            // query.toString should be ok for the filter, too, if the query boost is 1.0f
            return m_query.ToString();
        }

        public override sealed bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o is null)
            {
                return false;
            }
            if (this.GetType().Equals(o.GetType()))
            {
                return this.m_query.Equals(((MultiTermQueryWrapperFilter<Q>)o).m_query);
            }
            return false;
        }

        public override sealed int GetHashCode()
        {
            return m_query.GetHashCode();
        }

        /// <summary>
        /// Returns the field name for this query </summary>
        public string Field => m_query.Field;

        /// <summary>
        /// Returns a <see cref="DocIdSet"/> with documents that should be permitted in search
        /// results.
        /// </summary>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            AtomicReader reader = (context.AtomicReader);
            Fields fields = reader.Fields;
            if (fields is null)
            {
                // reader has no fields
                return null;
            }

            Terms terms = fields.GetTerms(m_query.m_field);
            if (terms is null)
            {
                // field does not exist
                return null;
            }

            TermsEnum termsEnum = m_query.GetTermsEnum(terms);
            if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);
            if (termsEnum.MoveNext())
            {
                // fill into a FixedBitSet
                FixedBitSet bitSet = new FixedBitSet(context.AtomicReader.MaxDoc);
                DocsEnum docsEnum = null;
                do
                {
                    // System.out.println("  iter termCount=" + termCount + " term=" +
                    // enumerator.term().toBytesString());
                    docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsFlags.NONE);
                    int docid;
                    while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        bitSet.Set(docid);
                    }
                } while (termsEnum.MoveNext());

                return bitSet;
            }
            else
            {
                return null;
            }
        }
    }
}