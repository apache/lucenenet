using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
using System.Collections;
using System.Diagnostics;

namespace Lucene.Net.Spatial.Vector
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
    /// An implementation of the Lucene <see cref="ValueSource"/> model that returns the distance
    /// for a <see cref="PointVectorStrategy"/>.
    /// 
    /// @lucene.internal
    /// </summary>
    public class DistanceValueSource : ValueSource
    {
        private readonly PointVectorStrategy strategy;
        private readonly IPoint from;
        private readonly double multiplier;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DistanceValueSource(PointVectorStrategy strategy, IPoint from, double multiplier)
        {
            this.strategy = strategy;
            this.from = from;
            this.multiplier = multiplier;
        }

        /// <summary>
        /// Returns the <see cref="ValueSource"/> description.
        /// </summary>
        public override string GetDescription()
        {
            return "DistanceValueSource(" + strategy + ", " + from + ")";
        }

        /// <summary>
        /// Returns the <see cref="FunctionValues"/> used by the function query.
        /// </summary>
        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            return new DistanceFunctionValue(this, readerContext.AtomicReader);
        }

        #region Nested type: DistanceFunctionValues

        internal class DistanceFunctionValue : FunctionValues
        {
            private readonly DistanceValueSource outerInstance;
            private readonly IDistanceCalculator calculator;
            private readonly IPoint from;
            private readonly double nullValue;

            private readonly FieldCache.Doubles ptX, ptY;
            private readonly IBits validX, validY;

            public DistanceFunctionValue(DistanceValueSource outerInstance, AtomicReader reader)
            {
                this.outerInstance = outerInstance;

                ptX = FieldCache.DEFAULT.GetDoubles(reader, outerInstance.strategy.FieldNameX, true);
                ptY = FieldCache.DEFAULT.GetDoubles(reader, outerInstance.strategy.FieldNameY, true);
                validX = FieldCache.DEFAULT.GetDocsWithField(reader, outerInstance.strategy.FieldNameX);
                validY = FieldCache.DEFAULT.GetDocsWithField(reader, outerInstance.strategy.FieldNameY);

                from = outerInstance.from;
                calculator = outerInstance.strategy.SpatialContext.DistCalc;
                nullValue = (outerInstance.strategy.SpatialContext.IsGeo ? 180 * outerInstance.multiplier : double.MaxValue);
            }

            public override float FloatVal(int doc)
            {
                return (float)DoubleVal(doc);
            }

            public override double DoubleVal(int doc)
            {
                // make sure it has minX and area
                if (validX.Get(doc))
                {
                    Debug.Assert(validY.Get(doc));
                    return calculator.Distance(outerInstance.from, ptX.Get(doc), ptY.Get(doc)) * outerInstance.multiplier;
                }
                return nullValue;
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + "=" + FloatVal(doc);
            }
        }

        #endregion

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            var that = o as DistanceValueSource;
            if (that == null) return false;

            if (!from.Equals(that.from)) return false;
            if (!strategy.Equals(that.strategy)) return false;
            if (multiplier != that.multiplier) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return from.GetHashCode();
        }
    }
}