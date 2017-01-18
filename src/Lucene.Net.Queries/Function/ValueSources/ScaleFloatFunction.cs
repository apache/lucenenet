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
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// Scales values to be between min and max.
    /// <para>This implementation currently traverses all of the source values to obtain
    /// their min and max.
    /// </para>
    /// <para>This implementation currently cannot distinguish when documents have been
    /// deleted or documents that have no value, and 0.0 values will be used for
    /// these cases.  This means that if values are normally all greater than 0.0, one can
    /// still end up with 0.0 as the min value to map from.  In these cases, an
    /// appropriate map() function could be used as a workaround to change 0.0
    /// to a value in the real range.
    /// </para>
    /// </summary>
    public class ScaleFloatFunction : ValueSource
    {
        protected internal readonly ValueSource source;
        protected internal readonly float min;
        protected internal readonly float max;

        public ScaleFloatFunction(ValueSource source, float min, float max)
        {
            this.source = source;
            this.min = min;
            this.max = max;
        }

        public override string GetDescription()
        {
            return "scale(" + source.GetDescription() + "," + min + "," + max + ")";
        }

        private class ScaleInfo
        {
            internal float minVal;
            internal float maxVal;
        }

        private ScaleInfo CreateScaleInfo(IDictionary context, AtomicReaderContext readerContext)
        {
            var leaves = ReaderUtil.GetTopLevelContext(readerContext).Leaves;

            float minVal = float.PositiveInfinity;
            float maxVal = float.NegativeInfinity;

            foreach (AtomicReaderContext leaf in leaves)
            {
                int maxDoc = leaf.Reader.MaxDoc;
                FunctionValues vals = source.GetValues(context, leaf);
                for (int i = 0; i < maxDoc; i++)
                {

                    float val = vals.FloatVal(i);
                    if ((Number.FloatToRawIntBits(val) & (0xff << 23)) == 0xff << 23)
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

            var scaleInfo = new ScaleInfo { minVal = minVal, maxVal = maxVal };
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

            float scale = (scaleInfo.maxVal - scaleInfo.minVal == 0) ? 0 : (max - min) / (scaleInfo.maxVal - scaleInfo.minVal);
            float minSource = scaleInfo.minVal;
            float maxSource = scaleInfo.maxVal;

            var vals = source.GetValues(context, readerContext);
            return new FloatDocValuesAnonymousInnerClassHelper(this, this, scale, minSource, maxSource, vals);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly ScaleFloatFunction outerInstance;

            private readonly float scale;
            private readonly float minSource;
            private readonly float maxSource;
            private readonly FunctionValues vals;

            public FloatDocValuesAnonymousInnerClassHelper(ScaleFloatFunction outerInstance, ScaleFloatFunction @this, float scale, float minSource, float maxSource, FunctionValues vals)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.scale = scale;
                this.minSource = minSource;
                this.maxSource = maxSource;
                this.vals = vals;
            }

            public override float FloatVal(int doc)
            {
                return (vals.FloatVal(doc) - minSource) * scale + outerInstance.min;
            }
            public override string ToString(int doc)
            {
                return "scale(" + vals.ToString(doc) + ",toMin=" + outerInstance.min + ",toMax=" + outerInstance.max + ",fromMin=" + minSource + ",fromMax=" + maxSource + ")";
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = Number.FloatToIntBits(min);
            h = h * 29;
            h += Number.FloatToIntBits(max);
            h = h * 29;
            h += source.GetHashCode();
            return h;
        }

        public override bool Equals(object o)
        {
            var other = o as ScaleFloatFunction;
            if (other == null)
                return false;
            return this.min == other.min && this.max == other.max && this.source.Equals(other.source);
        }
    }
}