using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Search.Similarities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class NormValueSource : ValueSource
    {
        protected readonly string field;

        public NormValueSource(string field)
        {
            this.field = field;
        }

        public virtual string Name
        {
            get
            {
                return "norm";
            }
        }

        public override string Description
        {
            get
            {
                return Name + '(' + field + ')';
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            IndexSearcher searcher = (IndexSearcher)context["searcher"];
            TFIDFSimilarity similarity = IDFValueSource.AsTFIDF(searcher.Similarity, field);
            if (similarity == null)
            {
                throw new NotSupportedException(@"requires a TFIDFSimilarity (such as DefaultSimilarity)");
            }

            NumericDocValues norms = readerContext.AtomicReader.GetNormValues(field);
            if (norms == null)
            {
                return new ConstDoubleDocValues(0.0, this);
            }

            return new AnonymousFloatDocValues(this, similarity, norms);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(NormValueSource parent, TFIDFSimilarity similarity, NumericDocValues norms)
                : base(parent)
            {
                this.parent = parent;
                this.similarity = similarity;
                this.norms = norms;
            }

            private readonly NormValueSource parent;
            private readonly TFIDFSimilarity similarity;
            private readonly NumericDocValues norms;

            public override float FloatVal(int doc)
            {
                return similarity.DecodeNormValue((sbyte)norms.Get(doc));
            }
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            return this.field.Equals(((NormValueSource)o).field);
        }

        public override int GetHashCode()
        {
            return this.GetType().GetHashCode() + field.GetHashCode();
        }
    }
}
