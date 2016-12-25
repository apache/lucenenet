using System.Diagnostics;

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
    using Bits = Lucene.Net.Util.Bits;
    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Fields = Lucene.Net.Index.Fields;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    /// <summary>
    /// A wrapper for <seealso cref="MultiTermQuery"/>, that exposes its
    /// functionality as a <seealso cref="Filter"/>.
    /// <P>
    /// <code>MultiTermQueryWrapperFilter</code> is not designed to
    /// be used by itself. Normally you subclass it to provide a Filter
    /// counterpart for a <seealso cref="MultiTermQuery"/> subclass.
    /// <P>
    /// For example, <seealso cref="TermRangeFilter"/> and <seealso cref="PrefixFilter"/> extend
    /// <code>MultiTermQueryWrapperFilter</code>.
    /// this class also provides the functionality behind
    /// <seealso cref="MultiTermQuery#CONSTANT_SCORE_FILTER_REWRITE"/>;
    /// this is why it is not abstract.
    /// </summary>
    public class MultiTermQueryWrapperFilter<Q> : Filter where Q : MultiTermQuery
    {
        protected readonly Q Query; // LUCENENET TODO: Rename 

        /// <summary>
        /// Wrap a <seealso cref="MultiTermQuery"/> as a Filter.
        /// </summary>
        protected internal MultiTermQueryWrapperFilter(Q query)
        {
            this.Query = query;
        }

        public override string ToString()
        {
            // query.toString should be ok for the filter, too, if the query boost is 1.0f
            return Query.ToString();
        }

        public override sealed bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (o == null)
            {
                return false;
            }
            if (this.GetType().Equals(o.GetType()))
            {
                return this.Query.Equals(((MultiTermQueryWrapperFilter<Q>)o).Query);
            }
            return false;
        }

        public override sealed int GetHashCode()
        {
            return Query.GetHashCode();
        }

        /// <summary>
        /// Returns the field name for this query </summary>
        public string Field
        {
            get
            {
                return Query.Field;
            }
        }

        /// <summary>
        /// Returns a DocIdSet with documents that should be permitted in search
        /// results.
        /// </summary>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
        {
            AtomicReader reader = (context.AtomicReader);
            Fields fields = reader.Fields;
            if (fields == null)
            {
                // reader has no fields
                return null;
            }

            Terms terms = fields.Terms(Query.m_field);
            if (terms == null)
            {
                // field does not exist
                return null;
            }

            TermsEnum termsEnum = Query.GetTermsEnum(terms);
            Debug.Assert(termsEnum != null);
            if (termsEnum.Next() != null)
            {
                // fill into a FixedBitSet
                FixedBitSet bitSet = new FixedBitSet(context.AtomicReader.MaxDoc);
                DocsEnum docsEnum = null;
                do
                {
                    // System.out.println("  iter termCount=" + termCount + " term=" +
                    // enumerator.term().toBytesString());
                    docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
                    int docid;
                    while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                    {
                        bitSet.Set(docid);
                    }
                } while (termsEnum.Next() != null);

                return bitSet;
            }
            else
            {
                return null;
            }
        }
    }
}