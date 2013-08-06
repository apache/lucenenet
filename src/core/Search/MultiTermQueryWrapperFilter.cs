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

using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search
{

    /// <summary> A wrapper for <see cref="MultiTermQuery" />, that exposes its
    /// functionality as a <see cref="Filter" />.
    /// <p/>
    /// <c>MultiTermQueryWrapperFilter</c> is not designed to
    /// be used by itself. Normally you subclass it to provide a Filter
    /// counterpart for a <see cref="MultiTermQuery" /> subclass.
    /// <p/>
    /// For example, <see cref="TermRangeFilter" /> and <see cref="PrefixFilter" /> extend
    /// <c>MultiTermQueryWrapperFilter</c>.
    /// This class also provides the functionality behind
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE" />;
    /// this is why it is not abstract.
    /// </summary>
    [Serializable]
    public class MultiTermQueryWrapperFilter<T> : Filter
        where T : MultiTermQuery
    {
        protected internal T query;

        /// <summary> Wrap a <see cref="MultiTermQuery" /> as a Filter.</summary>
        protected internal MultiTermQueryWrapperFilter(T query)
        {
            this.query = query;
        }

        public override string ToString()
        {
            // query.toString should be ok for the filter, too, if the query boost is 1.0f
            return query.ToString();
        }

        public override bool Equals(object o)
        {
            if (o == this)
                return true;
            if (o == null)
                return false;
            if (GetType().Equals(o.GetType()))
            {
                return query.Equals(((MultiTermQueryWrapperFilter<T>)o).query);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return query.GetHashCode();
        }

        public string Field
        {
            get { return query.Field; }
        }

        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            var reader = context.AtomicReader;
            var fields = reader.Fields;
            if (fields == null)
            {
                // reader has no fields
                return DocIdSet.EMPTY_DOCIDSET;
            }

            var terms = fields.Terms(query.Field);
            if (terms == null)
            {
                // field does not exist
                return DocIdSet.EMPTY_DOCIDSET;
            }

            var termsEnum = query.GetTermsEnum(terms);
            //assert termsEnum != null;
            if (termsEnum.Next() != null)
            {
                // fill into a FixedBitSet
                var bitSet = new FixedBitSet(context.Reader.MaxDoc);
                DocsEnum docsEnum = null;
                do
                {
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
                return DocIdSet.EMPTY_DOCIDSET;
            }
        }
    }
}