using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Spatial4n.Context;
using Spatial4n.Distance;
using Spatial4n.Shapes;
using System;
using System.Collections;

namespace Lucene.Net.Spatial.Util
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
    /// An implementation of the Lucene ValueSource that returns the spatial distance
    /// between an input point and a document's points in
    /// <see cref="ShapeFieldCacheProvider{T}"/>. The shortest distance is returned if a
    /// document has more than one point.
    /// 
    /// @lucene.internal
    /// </summary>
    public class ShapeFieldCacheDistanceValueSource : ValueSource
    {
        private readonly SpatialContext ctx;
        private readonly IPoint from;
        private readonly ShapeFieldCacheProvider<IPoint> provider;
        private readonly double multiplier;

        public ShapeFieldCacheDistanceValueSource(SpatialContext ctx, 
            ShapeFieldCacheProvider<IPoint> provider, IPoint from, double multiplier)
        {
            // LUCENENET specific - added guard clauses
            this.ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            this.from = from ?? throw new ArgumentNullException(nameof(from));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.multiplier = multiplier;
        }

        public override string GetDescription()
        {
            return GetType().Name + "(" + provider + ", " + from + ")";
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            // LUCENENET specific - added guard clause
            if (readerContext is null)
                throw new ArgumentNullException(nameof(readerContext));

            return new CachedDistanceFunctionValue(this, readerContext.AtomicReader);
        }

        internal class CachedDistanceFunctionValue : FunctionValues
        {
            private readonly ShapeFieldCacheDistanceValueSource outerInstance;
            private readonly ShapeFieldCache<IPoint> cache;
            private readonly IPoint from;
            private readonly IDistanceCalculator calculator;
            private readonly double nullValue;

            public CachedDistanceFunctionValue(ShapeFieldCacheDistanceValueSource outerInstance, AtomicReader reader)
            {
                // LUCENENET specific - added guard clauses
                this.outerInstance = outerInstance ?? throw new ArgumentNullException(nameof(outerInstance));
                if (reader is null)
                    throw new ArgumentNullException(nameof(reader));
                cache = outerInstance.provider.GetCache(reader);
                from = outerInstance.from;
                calculator = outerInstance.ctx.DistanceCalculator;
                nullValue = (outerInstance.ctx.IsGeo ? 180 * outerInstance.multiplier : double.MaxValue);
            }

            /// <summary>
            /// NOTE: This was floatVal() in Lucene
            /// </summary>
            public override float SingleVal(int doc)
            {
                return (float)DoubleVal(doc);
            }

            public override double DoubleVal(int doc)
            {
                var vals = cache.GetShapes(doc);
                if (vals != null)
                {
                    double v = calculator.Distance(from, vals[0]);
                    for (int i = 1; i < vals.Count; i++)
                    {
                        v = Math.Min(v, calculator.Distance(from, vals[i]));
                    }
                    return v * outerInstance.multiplier;
                }
                return nullValue;
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + "=" + SingleVal(doc);
            }
        }

        public override bool Equals(object? o)
        {
            if (this == o) return true;
            if (o is null || GetType() != o.GetType()) return false;

            if (!(o is ShapeFieldCacheDistanceValueSource that)) return false;
            if (!ctx.Equals(that.ctx)) return false;
            if (!from.Equals(that.from)) return false;
            if (!provider.Equals(that.provider)) return false;
            if (multiplier != that.multiplier) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return from.GetHashCode();
        }
    }
}
