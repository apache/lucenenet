using Lucene.Net.Index;
using Lucene.Net.Search.Function.DocValues;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.Function.ValueSources
{
    public class ScaleFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float min;
        protected readonly float max;

        public ScaleFloatFunction(ValueSource source, float min, float max)
        {
            this.source = source;
            this.min = min;
            this.max = max;
        }

        public override string Description
        {
            get
            {
                return @"scale(" + source.Description + @"," + min + @"," + max + @")";
            }
        }

        private class ScaleInfo
        {
            internal float minVal;
            internal float maxVal;
        }

        private ScaleInfo CreateScaleInfo(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            IList<AtomicReaderContext> leaves = ReaderUtil.GetTopLevelContext(readerContext).Leaves;
            float minVal = float.PositiveInfinity;
            float maxVal = float.NegativeInfinity;

            foreach (AtomicReaderContext leaf in leaves)
            {
                int maxDoc = leaf.AtomicReader.MaxDoc;
                FunctionValues vals = source.GetValues(context, leaf);
                for (int i = 0; i < maxDoc; i++)
                {
                    float val = vals.FloatVal(i);
                    if ((Number.FloatToIntBits(val) & (0xff << 23)) == 0xff << 23)
                    {
                        continue;
                    }

                    if (val < minVal)
                    {
                        minVal = val;
                    }

                    if (val > maxVal)
                    {
                        maxVal = val;
                    }
                }
            }

            if (minVal == float.PositiveInfinity)
            {
                minVal = maxVal = 0;
            }

            ScaleInfo scaleInfo = new ScaleInfo();
            scaleInfo.minVal = minVal;
            scaleInfo.maxVal = maxVal;
            context[this.source] = scaleInfo;
            return scaleInfo;
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            ScaleInfo scaleInfo = (ScaleInfo)context[source];
            if (scaleInfo == null)
            {
                scaleInfo = CreateScaleInfo(context, readerContext);
            }

            float scale = (scaleInfo.maxVal - scaleInfo.minVal == 0) ? 0 : (max - min) / (scaleInfo.maxVal - scaleInfo.minVal);
            float minSource = scaleInfo.minVal;
            float maxSource = scaleInfo.maxVal;
            FunctionValues vals = source.GetValues(context, readerContext);
            return new AnonymousFloatDocValues(this, vals, minSource, maxSource, scale);
        }

        private sealed class AnonymousFloatDocValues : FloatDocValues
        {
            public AnonymousFloatDocValues(ScaleFloatFunction parent, FunctionValues vals, float minSource, float maxSource, float scale)
                : base(parent)
            {
                this.parent = parent;
                this.vals = vals;
                this.minSource = minSource;
                this.maxSource = maxSource;
                this.scale = scale;
            }

            private readonly ScaleFloatFunction parent;
            private readonly FunctionValues vals;
            private readonly float minSource, maxSource, scale;

            public override float FloatVal(int doc)
            {
                return (vals.FloatVal(doc) - minSource) * scale + parent.min;
            }

            public override string ToString(int doc)
            {
                return @"scale(" + vals.ToString(doc) + @",toMin=" + parent.min + @",toMax=" + parent.max + @",fromMin=" + minSource + @",fromMax=" + maxSource + @")";
            }
        }

        public override void CreateWeight(IDictionary<object, object> context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = Number.FloatToIntBits(min);
            h = h * 29;
            h = Number.FloatToIntBits(max);
            h = h * 29;
            h = source.GetHashCode();
            return h;
        }

        public override bool Equals(Object o)
        {
            if (typeof(ScaleFloatFunction) != o.GetType())
                return false;
            ScaleFloatFunction other = (ScaleFloatFunction)o;
            return this.min == other.min && this.max == other.max && this.source.Equals(other.source);
        }
    }
}
