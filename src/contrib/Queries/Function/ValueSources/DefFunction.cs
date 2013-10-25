using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class DefFunction : MultiFunction
    {
        public DefFunction(List<ValueSource> sources)
            : base(sources)
        {
        }

        protected override string Name
        {
            get
            {
                return @"def";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> fcontext, AtomicReaderContext readerContext)
        {
            return new AnonymousValues(this, ValsArr(sources, fcontext, readerContext));
        }

        private sealed class AnonymousValues : Values
        {
            public AnonymousValues(DefFunction parent, FunctionValues[] valsArr)
                : base(parent, valsArr)
            {
                this.parent = parent;

                upto = valsArr.Length - 1;
            }

            private readonly DefFunction parent;

            private readonly int upto;

            private FunctionValues Get(int doc)
            {
                for (int i = 0; i < upto; i++)
                {
                    FunctionValues vals = valsArr[i];
                    if (vals.Exists(doc))
                    {
                        return vals;
                    }
                }

                return valsArr[upto];
            }

            public override byte ByteVal(int doc)
            {
                return Get(doc).ByteVal(doc);
            }

            public override short ShortVal(int doc)
            {
                return Get(doc).ShortVal(doc);
            }

            public override float FloatVal(int doc)
            {
                return Get(doc).FloatVal(doc);
            }

            public override int IntVal(int doc)
            {
                return Get(doc).IntVal(doc);
            }

            public override long LongVal(int doc)
            {
                return Get(doc).LongVal(doc);
            }

            public override double DoubleVal(int doc)
            {
                return Get(doc).DoubleVal(doc);
            }

            public override string StrVal(int doc)
            {
                return Get(doc).StrVal(doc);
            }

            public override bool BoolVal(int doc)
            {
                return Get(doc).BoolVal(doc);
            }

            public override bool BytesVal(int doc, BytesRef target)
            {
                return Get(doc).BytesVal(doc, target);
            }

            public override Object ObjectVal(int doc)
            {
                return Get(doc).ObjectVal(doc);
            }

            public override bool Exists(int doc)
            {
                foreach (FunctionValues vals in valsArr)
                {
                    if (vals.Exists(doc))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override FunctionValues.ValueFiller GetValueFiller()
            {
                return base.GetValueFiller();
            }
        }
    }
}
