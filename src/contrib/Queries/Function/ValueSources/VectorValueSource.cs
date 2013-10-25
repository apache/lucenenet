using Lucene.Net.Index;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class VectorValueSource : MultiValueSource
    {
        protected readonly IList<ValueSource> sources;

        public VectorValueSource(IList<ValueSource> sources)
        {
            this.sources = sources;
        }

        public virtual IList<ValueSource> Sources
        {
            get
            {
                return sources;
            }
        }

        public override int Dimension
        {
            get
            {
                return sources.Count;
            }
        }

        public virtual string Name
        {
            get
            {
                return "vector";
            }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            int size = sources.Count;
            if (size == 2)
            {
                FunctionValues x = sources[0].GetValues(context, readerContext);
                FunctionValues y = sources[1].GetValues(context, readerContext);
                return new AnonymousFunctionValues(this, x, y);
            }

            FunctionValues[] valsArr = new FunctionValues[size];
            for (int i = 0; i < size; i++)
            {
                valsArr[i] = sources[i].GetValues(context, readerContext);
            }

            return new AnonymousFunctionValues1(this, valsArr);
        }

        private sealed class AnonymousFunctionValues : FunctionValues
        {
            public AnonymousFunctionValues(VectorValueSource parent, FunctionValues x, FunctionValues y)
            {
                this.parent = parent;
                this.x = x;
                this.y = y;
            }

            private readonly VectorValueSource parent;
            private readonly FunctionValues x, y;

            public override void ByteVal(int doc, byte[] vals)
            {
                vals[0] = x.ByteVal(doc);
                vals[1] = y.ByteVal(doc);
            }

            public override void ShortVal(int doc, short[] vals)
            {
                vals[0] = x.ShortVal(doc);
                vals[1] = y.ShortVal(doc);
            }

            public override void IntVal(int doc, int[] vals)
            {
                vals[0] = x.IntVal(doc);
                vals[1] = y.IntVal(doc);
            }

            public override void LongVal(int doc, long[] vals)
            {
                vals[0] = x.LongVal(doc);
                vals[1] = y.LongVal(doc);
            }

            public override void FloatVal(int doc, float[] vals)
            {
                vals[0] = x.FloatVal(doc);
                vals[1] = y.FloatVal(doc);
            }

            public override void DoubleVal(int doc, double[] vals)
            {
                vals[0] = x.DoubleVal(doc);
                vals[1] = y.DoubleVal(doc);
            }

            public override void StrVal(int doc, String[] vals)
            {
                vals[0] = x.StrVal(doc);
                vals[1] = y.StrVal(doc);
            }

            public override string ToString(int doc)
            {
                return parent.Name + @"(" + x.ToString(doc) + @"," + y.ToString(doc) + @")";
            }
        }

        private sealed class AnonymousFunctionValues1 : FunctionValues
        {
            public AnonymousFunctionValues1(VectorValueSource parent, FunctionValues[] valsArr)
            {
                this.parent = parent;
                this.valsArr = valsArr;
            }

            private readonly VectorValueSource parent;
            private readonly FunctionValues[] valsArr;

            public override void ByteVal(int doc, byte[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].ByteVal(doc);
                }
            }

            public override void ShortVal(int doc, short[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].ShortVal(doc);
                }
            }

            public override void FloatVal(int doc, float[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].FloatVal(doc);
                }
            }

            public override void IntVal(int doc, int[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].IntVal(doc);
                }
            }

            public override void LongVal(int doc, long[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].LongVal(doc);
                }
            }

            public override void DoubleVal(int doc, double[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].DoubleVal(doc);
                }
            }

            public override void StrVal(int doc, String[] vals)
            {
                for (int i = 0; i < valsArr.Length; i++)
                {
                    vals[i] = valsArr[i].StrVal(doc);
                }
            }

            public override string ToString(int doc)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(parent.Name).Append('(');
                bool firstTime = true;
                foreach (FunctionValues vals in valsArr)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(vals.ToString(doc));
                }

                sb.Append(')');
                return sb.ToString();
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            foreach (ValueSource source in sources)
                source.CreateWeight(context, searcher);
        }

        public override string Description
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(Name).Append('(');
                bool firstTime = true;
                foreach (ValueSource source in sources)
                {
                    if (firstTime)
                    {
                        firstTime = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    sb.Append(source);
                }

                sb.Append(@")");
                return sb.ToString();
            }
        }

        public override bool Equals(Object o)
        {
            if (this == o)
                return true;
            if (!(o is VectorValueSource))
                return false;
            VectorValueSource that = (VectorValueSource)o;
            return sources.Equals(that.sources);
        }

        public override int GetHashCode()
        {
            return sources.GetHashCode();
        }
    }
}
