using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Scales values to be between <c>min</c> and <c>max</c>.
    /// <para/>This implementation currently traverses all of the source values to obtain
    /// their min and max.
    /// <para/>This implementation currently cannot distinguish when documents have been
    /// deleted or documents that have no value, and 0.0 values will be used for
    /// these cases.  This means that if values are normally all greater than 0.0, one can
    /// still end up with 0.0 as the min value to map from.  In these cases, an
    /// appropriate map() function could be used as a workaround to change 0.0
    /// to a value in the real range.
    /// <para/>
    /// NOTE: This was ScaleFloatFunction in Lucene
    /// </summary>
    public class ScaleSingleFunction : ValueSource
    {
        protected readonly ValueSource m_source;
        protected readonly float m_min;
        protected readonly float m_max;

        public ScaleSingleFunction(ValueSource source, float min, float max)
        {
            this.m_source = source;
            this.m_min = min;
            this.m_max = max;
        }

        public override string GetDescription()
        {
            return "scale(" + m_source.GetDescription() + "," + m_min + "," + m_max + ")";
        }

        private class ScaleInfo
        {
            internal float MinVal { get; set; }
            internal float MaxVal { get; set; }
        }

        private ScaleInfo CreateScaleInfo(IDictionary context, AtomicReaderContext readerContext)
        {
            var leaves = ReaderUtil.GetTopLevelContext(readerContext).Leaves;

            float minVal = float.PositiveInfinity;
            float maxVal = float.NegativeInfinity;

            foreach (AtomicReaderContext leaf in leaves)
            {
                int maxDoc = leaf.Reader.MaxDoc;
                FunctionValues vals = m_source.GetValues(context, leaf);
                for (int i = 0; i < maxDoc; i++)
                {

                    float val = vals.SingleVal(i);
                    if ((Number.SingleToRawInt32Bits(val) & (0xff << 23)) == 0xff << 23)
                    {
                        // if the exponent in the float is all ones, then this is +Inf, -Inf or NaN
                        // which don't make sense to factor into the scale function
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
                // must have been an empty index
                minVal = maxVal = 0;
            }

            var scaleInfo = new ScaleInfo { MinVal = minVal, MaxVal = maxVal };
            context[this] = scaleInfo;
            return scaleInfo;
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {

            var scaleInfo = (ScaleInfo)context[this];
            if (scaleInfo == null)
            {
                scaleInfo = CreateScaleInfo(context, readerContext);
            }

            float scale = (scaleInfo.MaxVal - scaleInfo.MinVal == 0) ? 0 : (m_max - m_min) / (scaleInfo.MaxVal - scaleInfo.MinVal);
            float minSource = scaleInfo.MinVal;
            float maxSource = scaleInfo.MaxVal;

            var vals = m_source.GetValues(context, readerContext);
            return new SingleDocValuesAnonymousInnerClassHelper(this, this, scale, minSource, maxSource, vals);
        }

        private class SingleDocValuesAnonymousInnerClassHelper : SingleDocValues
        {
            private readonly ScaleSingleFunction outerInstance;

            private readonly float scale;
            private readonly float minSource;
            private readonly float maxSource;
            private readonly FunctionValues vals;

            public SingleDocValuesAnonymousInnerClassHelper(ScaleSingleFunction outerInstance, ScaleSingleFunction @this, float scale, float minSource, float maxSource, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.scale = scale;
                this.minSource = minSource;
                this.maxSource = maxSource;
                this.vals = vals;
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return (vals.SingleVal(doc) - minSource) * scale + outerInstance.m_min;
            }
            public override string ToString(int doc)
            {
                return "scale(" + vals.ToString(doc) + ",toMin=" + outerInstance.m_min + ",toMax=" + outerInstance.m_max + ",fromMin=" + minSource + ",fromMax=" + maxSource + ")";
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            m_source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = Number.SingleToInt32Bits(m_min);
            h = h * 29;
            h += Number.SingleToInt32Bits(m_max);
            h = h * 29;
            h += m_source.GetHashCode();
            return h;
        }

        public override bool Equals(object o)
        {
            var other = o as ScaleSingleFunction;
            if (other == null)
                return false;
            return this.m_min == other.m_min 
                && this.m_max == other.m_max 
                && this.m_source.Equals(other.m_source);
        }
    }
}