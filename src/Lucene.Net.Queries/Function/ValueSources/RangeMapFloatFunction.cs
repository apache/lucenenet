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
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
    /// <code>RangeMapFloatFunction</code> implements a map function over
    /// another <seealso cref="ValueSource"/> whose values fall within min and max inclusive to target.
    /// <br>
    /// Normally Used as an argument to a <seealso cref="FunctionQuery"/>
    /// 
    /// 
    /// </summary>
    public class RangeMapFloatFunction : ValueSource
    {
        protected readonly ValueSource source;
        protected readonly float min;
        protected readonly float max;
        protected readonly ValueSource target;
        protected readonly ValueSource defaultVal;

        public RangeMapFloatFunction(ValueSource source, float min, float max, float target, float? def)
            : this(source, min, max, new ConstValueSource(target), def == null ? null : new ConstValueSource(def.Value))
        {
        }

        public RangeMapFloatFunction(ValueSource source, float min, float max, ValueSource target, ValueSource def)
        {
            this.source = source;
            this.min = min;
            this.max = max;
            this.target = target;
            this.defaultVal = def;
        }

        public override string GetDescription()
        {
            return "map(" + source.GetDescription() + "," + min + "," + max + "," + target.GetDescription() + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues vals = source.GetValues(context, readerContext);
            FunctionValues targets = target.GetValues(context, readerContext);
            FunctionValues defaults = (this.defaultVal == null) ? null : defaultVal.GetValues(context, readerContext);
            return new FloatDocValuesAnonymousInnerClassHelper(this, this, vals, targets, defaults);
        }

        private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
        {
            private readonly RangeMapFloatFunction outerInstance;

            private readonly FunctionValues vals;
            private readonly FunctionValues targets;
            private readonly FunctionValues defaults;

            public FloatDocValuesAnonymousInnerClassHelper(RangeMapFloatFunction outerInstance, RangeMapFloatFunction @this, FunctionValues vals, FunctionValues targets, FunctionValues defaults)
                : base(@this)
            {
                this.outerInstance = outerInstance;
                this.vals = vals;
                this.targets = targets;
                this.defaults = defaults;
            }

            public override float FloatVal(int doc)
            {
                float val = vals.FloatVal(doc);
                return (val >= outerInstance.min && val <= outerInstance.max) ? targets.FloatVal(doc) : (outerInstance.defaultVal == null ? val : defaults.FloatVal(doc));
            }
            public override string ToString(int doc)
            {
                return "map(" + vals.ToString(doc) + ",min=" + outerInstance.min + ",max=" + outerInstance.max + ",target=" + targets.ToString(doc) + ")";
            }
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            source.CreateWeight(context, searcher);
        }

        public override int GetHashCode()
        {
            int h = source.GetHashCode();
            h ^= (h << 10) | ((int)((uint)h >> 23));
            h += Number.FloatToIntBits(min);
            h ^= (h << 14) | ((int)((uint)h >> 19));
            h += Number.FloatToIntBits(max);
            h += target.GetHashCode();
            if (defaultVal != null)
            {
                h += defaultVal.GetHashCode();
            }
            return h;
        }

        public override bool Equals(object o)
        {
            if (typeof(RangeMapFloatFunction) != o.GetType())
            {
                return false;
            }
            var other = o as RangeMapFloatFunction;
            if (other == null)
                return false;
            return this.min == other.min && this.max == other.max && this.target.Equals(other.target) && this.source.Equals(other.source) && (this.defaultVal == other.defaultVal || (this.defaultVal != null && this.defaultVal.Equals(other.defaultVal)));
        }
    }
}