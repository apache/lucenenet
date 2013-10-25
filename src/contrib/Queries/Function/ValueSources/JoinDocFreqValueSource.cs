using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class JoinDocFreqValueSource : FieldCacheSource
    {
        public const string NAME = "joindf";

        protected readonly string qfield;

        public JoinDocFreqValueSource(string field, string qfield)
            : base (field)
        {
            this.qfield = qfield;
        }

        public override string Description
        {
            get 
            {
                return NAME + @"(" + field + @":(" + qfield + @"))";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            BinaryDocValues terms = cache.GetTerms(readerContext.AtomicReader, field, PackedInts.FAST);
            IndexReader top = ReaderUtil.GetTopLevelContext(readerContext).Reader;
            Terms t = MultiFields.GetTerms(top, qfield);
            TermsEnum termsEnum = t == null ? TermsEnum.EMPTY : t.Iterator(null);
            return new AnonymousIntDocValues(this, terms, termsEnum);
        }

        private sealed class AnonymousIntDocValues : IntDocValues
        {
            public AnonymousIntDocValues(JoinDocFreqValueSource parent, BinaryDocValues terms, TermsEnum termsEnum)
                : base(parent)
            {
                this.parent = parent;
                this.terms = terms;
                this.termsEnum = termsEnum;
            }

            private readonly JoinDocFreqValueSource parent;
            private readonly BinaryDocValues terms;
            private readonly TermsEnum termsEnum;

            readonly BytesRef bref = new BytesRef();

            public override int IntVal(int doc)
            {
                try
                {
                    terms.Get(doc, bref);
                    if (termsEnum.SeekExact(bref, true))
                    {
                        return termsEnum.DocFreq;
                    }
                    else
                    {
                        return 0;
                    }
                }
                catch (IOException e)
                {
                    throw new Exception(@"caught exception in function " + parent.Description + @" : doc=" + doc, e);
                }
            }
        }

        public override bool Equals(Object o)
        {
            if (o.GetType() != typeof (JoinDocFreqValueSource))
                return false;
            JoinDocFreqValueSource other = (JoinDocFreqValueSource) o;
            if (!qfield.Equals(other.qfield))
                return false;
            return base.Equals(other);
        }

        public override int GetHashCode()
        {
            return qfield.GetHashCode() + base.GetHashCode();
        }
    }
}
