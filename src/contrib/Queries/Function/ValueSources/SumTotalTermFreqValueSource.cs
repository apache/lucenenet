using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class SumTotalTermFreqValueSource : ValueSource
    {
        protected readonly string indexedField;

        public SumTotalTermFreqValueSource(string indexedField)
        {
            this.indexedField = indexedField;
        }

        public virtual string Name
        {
            get
            {
                return "sumtotaltermfreq";
            }
        }

        public override string Description
        {
            get
            {
                return Name + '(' + indexedField + ')';
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return (FunctionValues)context[this];
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            long sumTotalTermFreq = 0;
            foreach (AtomicReaderContext readerContext in searcher.TopReaderContext.Leaves)
            {
                Fields fields = readerContext.AtomicReader.Fields;
                if (fields == null)
                    continue;
                Terms terms = fields.Terms(indexedField);
                if (terms == null)
                    continue;
                long v = terms.SumTotalTermFreq;
                if (v == -1)
                {
                    sumTotalTermFreq = -1;
                    break;
                }
                else
                {
                    sumTotalTermFreq = v;
                }
            }

            long ttf = sumTotalTermFreq;
            context[this] = new AnonymousLongDocValues(this, ttf);
        }

        private sealed class AnonymousLongDocValues : LongDocValues
        {
            public AnonymousLongDocValues(SumTotalTermFreqValueSource parent, long ttf)
                : base(parent)
            {
                this.parent = parent;
                this.ttf = ttf;
            }

            private readonly SumTotalTermFreqValueSource parent;
            private readonly long ttf;

            public override long LongVal(int doc)
            {
                return ttf;
            }
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() + indexedField.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            SumTotalTermFreqValueSource other = (SumTotalTermFreqValueSource)o;
            return this.indexedField.Equals(other.indexedField);
        }
    }
}
