using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class TotalTermFreqValueSource : ValueSource
    {
        protected readonly string field;
        protected readonly string indexedField;
        protected readonly string val;
        protected readonly BytesRef indexedBytes;

        public TotalTermFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
        {
            this.field = field;
            this.val = val;
            this.indexedField = indexedField;
            this.indexedBytes = indexedBytes;
        }

        public virtual string Name
        {
            get
            {
                return "totaltermfreq";
            }
        }

        public override string Description
        {
            get
            {
                return Name + '(' + field + ',' + val + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return (FunctionValues)context[this];
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            long totalTermFreq = 0;
            foreach (AtomicReaderContext readerContext in searcher.TopReaderContext.Leaves)
            {
                long val = readerContext.AtomicReader.TotalTermFreq(new Term(indexedField, indexedBytes));
                if (val == -1)
                {
                    totalTermFreq = -1;
                    break;
                }
                else
                {
                    totalTermFreq = val;
                }
            }

            long ttf = totalTermFreq;
            context[this] = new AnonymousLongDocValues(this, ttf);
        }

        private sealed class AnonymousLongDocValues : LongDocValues
        {
            public AnonymousLongDocValues(TotalTermFreqValueSource parent, long ttf)
                : base(parent)
            {
                this.parent = parent;
                this.ttf = ttf;
            }

            private readonly TotalTermFreqValueSource parent;
            private readonly long ttf;

            public override long LongVal(int doc)
            {
                return ttf;
            }
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() + indexedField.GetHashCode() * 29 + indexedBytes.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            TotalTermFreqValueSource other = (TotalTermFreqValueSource)o;
            return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other.indexedBytes);
        }
    }
}
