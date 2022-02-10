using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Fields = Lucene.Net.Index.Fields;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
    using Term = Lucene.Net.Index.Term;
    using TermContext = Lucene.Net.Index.TermContext;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;

    public abstract class TermCollectingRewrite<Q> : MultiTermQuery.RewriteMethod where Q : Query // LUCENENET NOTE: Class was made public instaed of internal because it has public derived types
    {
        /// <summary>
        /// Return a suitable top-level <see cref="Query"/> for holding all expanded terms. </summary>
        protected abstract Q GetTopLevelQuery();

        /// <summary>
        /// Add a <see cref="MultiTermQuery"/> term to the top-level query </summary>
        protected void AddClause(Q topLevel, Term term, int docCount, float boost)
        {
            AddClause(topLevel, term, docCount, boost, null);
        }

        protected abstract void AddClause(Q topLevel, Term term, int docCount, float boost, TermContext states);

        internal void CollectTerms(IndexReader reader, MultiTermQuery query, TermCollector collector)
        {
            IndexReaderContext topReaderContext = reader.Context;
            IComparer<BytesRef> lastTermComp = null;
            foreach (AtomicReaderContext context in topReaderContext.Leaves)
            {
                Fields fields = context.AtomicReader.Fields;
                if (fields is null)
                {
                    // reader has no fields
                    continue;
                }

                Terms terms = fields.GetTerms(query.m_field);
                if (terms is null)
                {
                    // field does not exist
                    continue;
                }

                TermsEnum termsEnum = GetTermsEnum(query, terms, collector.Attributes);
                if (Debugging.AssertsEnabled) Debugging.Assert(termsEnum != null);

                if (termsEnum == TermsEnum.EMPTY)
                {
                    continue;
                }

                // Check comparer compatibility:
                IComparer<BytesRef> newTermComp = termsEnum.Comparer;
                if (lastTermComp != null && newTermComp != null && newTermComp != lastTermComp)
                {
                    throw RuntimeException.Create("term comparer should not change between segments: " + lastTermComp + " != " + newTermComp);
                }
                lastTermComp = newTermComp;
                collector.SetReaderContext(topReaderContext, context);
                collector.SetNextEnum(termsEnum);
                while (termsEnum.MoveNext())
                {
                    if (!collector.Collect(termsEnum.Term))
                    {
                        return; // interrupt whole term collection, so also don't iterate other subReaders
                    }
                }
            }
        }

        internal abstract class TermCollector
        {
            protected internal AtomicReaderContext m_readerContext;
            protected internal IndexReaderContext m_topReaderContext;

            public virtual void SetReaderContext(IndexReaderContext topReaderContext, AtomicReaderContext readerContext)
            {
                this.m_readerContext = readerContext;
                this.m_topReaderContext = topReaderContext;
            }

            /// <summary>
            /// attributes used for communication with the enum </summary>
            public AttributeSource Attributes => attributes;

            private readonly AttributeSource attributes = new AttributeSource();

            /// <summary>
            /// return false to stop collecting </summary>
            public abstract bool Collect(BytesRef bytes);

            /// <summary>
            /// the next segment's <seealso cref="TermsEnum"/> that is used to collect terms </summary>
            public abstract void SetNextEnum(TermsEnum termsEnum);
        }
    }
}