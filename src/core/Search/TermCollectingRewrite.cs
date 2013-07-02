using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search
{
    public abstract class TermCollectingRewrite<Q> : MultiTermQuery.RewriteMethod
        where Q : Query
    {
        protected abstract Q TopLevelQuery { get; }

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
                Fields fields = context.Reader.Fields;
                if (fields == null)
                {
                    // reader has no fields
                    continue;
                }

                Terms terms = fields.Terms(query.Field);
                if (terms == null)
                {
                    // field does not exist
                    continue;
                }

                TermsEnum termsEnum = GetTermsEnum(query, terms, collector.attributes);
                //assert termsEnum != null;

                if (termsEnum == TermsEnum.EMPTY)
                    continue;

                // Check comparator compatibility:
                IComparer<BytesRef> newTermComp = termsEnum.Comparator;
                if (lastTermComp != null && newTermComp != null && newTermComp != lastTermComp)
                    throw new SystemException("term comparator should not change between segments: " + lastTermComp + " != " + newTermComp);
                lastTermComp = newTermComp;
                collector.SetReaderContext(topReaderContext, context);
                collector.SetNextEnum(termsEnum);
                BytesRef bytes;
                while ((bytes = termsEnum.Next()) != null)
                {
                    if (!collector.Collect(bytes))
                        return; // interrupt whole term collection, so also don't iterate other subReaders
                }
            }
        }

        internal abstract class TermCollector
        {
            protected AtomicReaderContext readerContext;
            protected IndexReaderContext topReaderContext;

            public void SetReaderContext(IndexReaderContext topReaderContext, AtomicReaderContext readerContext)
            {
                this.readerContext = readerContext;
                this.topReaderContext = topReaderContext;
            }
            /** attributes used for communication with the enum */
            public AttributeSource attributes = new AttributeSource();

            /** return false to stop collecting */
            public abstract bool Collect(BytesRef bytes);

            /** the next segment's {@link TermsEnum} that is used to collect terms */
            public abstract void SetNextEnum(TermsEnum termsEnum);
        }

        public abstract Query Rewrite(IndexReader reader, MultiTermQuery query);
    }
}
