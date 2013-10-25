using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    internal class ConstIntDocValues : IntDocValues
    {
        readonly int ival;
        readonly float fval;
        readonly double dval;
        readonly long lval;
        readonly string sval;
        readonly ValueSource parent;

        internal ConstIntDocValues(int val, ValueSource parent)
            : base(parent)
        {
            ival = val;
            fval = val;
            dval = val;
            lval = val;
            sval = val.ToString();
            this.parent = parent;
        }

        public override float FloatVal(int doc)
        {
            return fval;
        }

        public override int IntVal(int doc)
        {
            return ival;
        }

        public override long LongVal(int doc)
        {
            return lval;
        }

        public override double DoubleVal(int doc)
        {
            return dval;
        }

        public override string StrVal(int doc)
        {
            return sval;
        }

        public override string ToString(int doc)
        {
            return parent.Description + '=' + sval;
        }
    }

    internal class ConstDoubleDocValues : DoubleDocValues
    {
        readonly int ival;
        readonly float fval;
        readonly double dval;
        readonly long lval;
        readonly string sval;
        readonly ValueSource parent;
        
        internal ConstDoubleDocValues(double val, ValueSource parent)
            : base(parent)
        {
            ival = (int)val;
            fval = (float)val;
            dval = val;
            lval = (long)val;
            sval = val.ToString();
            this.parent = parent;
        }

        public override float FloatVal(int doc)
        {
            return fval;
        }

        public override int IntVal(int doc)
        {
            return ival;
        }

        public override long LongVal(int doc)
        {
            return lval;
        }

        public override double DoubleVal(int doc)
        {
            return dval;
        }

        public override string StrVal(int doc)
        {
            return sval;
        }

        public override string ToString(int doc)
        {
            return parent.Description + '=' + sval;
        }
    }

    public class DocFreqValueSource : ValueSource
    {
        protected readonly string field;
        protected readonly string indexedField;
        protected readonly string val;
        protected readonly BytesRef indexedBytes;

        public DocFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
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
                return @"docfreq";
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
            IndexSearcher searcher = (IndexSearcher)context["searcher"];
            int docfreq = searcher.IndexReader.DocFreq(new Term(indexedField, indexedBytes));
            return new ConstIntDocValues(docfreq, this);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            context["searcher"] = searcher;
        }

        public override int GetHashCode()
        {
            return GetType().GetHashCode() + indexedField.GetHashCode() * 29 + indexedBytes.GetHashCode();
        }

        public override bool Equals(Object o)
        {
            if (this.GetType() != o.GetType())
                return false;
            DocFreqValueSource other = (DocFreqValueSource)o;
            return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other.indexedBytes);
        }
    }
}
