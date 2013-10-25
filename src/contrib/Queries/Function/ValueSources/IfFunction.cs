using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class IfFunction : BoolFunction
    {
        private readonly ValueSource ifSource;
        private readonly ValueSource trueSource;
        private readonly ValueSource falseSource;

        public IfFunction(ValueSource ifSource, ValueSource trueSource, ValueSource falseSource)
        {
            this.ifSource = ifSource;
            this.trueSource = trueSource;
            this.falseSource = falseSource;
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            FunctionValues ifVals = ifSource.GetValues(context, readerContext);
            FunctionValues trueVals = trueSource.GetValues(context, readerContext);
            FunctionValues falseVals = falseSource.GetValues(context, readerContext);
            return new AnonymousFunctionValues(this, ifVals, trueVals, falseVals);
        }

        private sealed class AnonymousFunctionValues : FunctionValues
        {
            public AnonymousFunctionValues(IfFunction parent, FunctionValues ifVals, FunctionValues trueVals, FunctionValues falseVals)
            {
                this.parent = parent;
                this.ifVals = ifVals;
                this.trueVals = trueVals;
                this.falseVals = falseVals;
            }

            private readonly IfFunction parent;
            private readonly FunctionValues ifVals;
            private readonly FunctionValues trueVals;
            private readonly FunctionValues falseVals;
                
            public override byte ByteVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.ByteVal(doc) : falseVals.ByteVal(doc);
            }

            public override short ShortVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.ShortVal(doc) : falseVals.ShortVal(doc);
            }

            public override float FloatVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.FloatVal(doc) : falseVals.FloatVal(doc);
            }

            public override int IntVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.IntVal(doc) : falseVals.IntVal(doc);
            }

            public override long LongVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.LongVal(doc) : falseVals.LongVal(doc);
            }

            public override double DoubleVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.DoubleVal(doc) : falseVals.DoubleVal(doc);
            }

            public override string StrVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.StrVal(doc) : falseVals.StrVal(doc);
            }

            public override bool BoolVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.BoolVal(doc) : falseVals.BoolVal(doc);
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                return ifVals.BoolVal(doc) ? trueVals.BytesVal(doc, target) : falseVals.BytesVal(doc, target);
            }

            public override Object ObjectVal(int doc)
            {
                return ifVals.BoolVal(doc) ? trueVals.ObjectVal(doc) : falseVals.ObjectVal(doc);
            }

            public override bool Exists(int doc)
            {
                return true;
            }

            public override ValueFiller GetValueFiller()
            {
                return base.GetValueFiller();
            }

            public override string ToString(int doc)
            {
                return @"if(" + ifVals.ToString(doc) + ',' + trueVals.ToString(doc) + ',' + falseVals.ToString(doc) + ')';
            }
        }

        public override string Description
        {
            get
            {
                return @"if(" + ifSource.Description + ',' + trueSource.Description + ',' + falseSource + ')';
            }
        }

        public override int GetHashCode()
        {
            int h = ifSource.GetHashCode();
            h = h * 31 + trueSource.GetHashCode();
            h = h * 31 + falseSource.GetHashCode();
            return h;
        }

        public override bool Equals(Object o)
        {
            if (!(o is IfFunction))
                return false;
            IfFunction other = (IfFunction)o;
            return ifSource.Equals(other.ifSource) && trueSource.Equals(other.trueSource) && falseSource.Equals(other.falseSource);
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            ifSource.CreateWeight(context, searcher);
            trueSource.CreateWeight(context, searcher);
            falseSource.CreateWeight(context, searcher);
        }
    }
}
