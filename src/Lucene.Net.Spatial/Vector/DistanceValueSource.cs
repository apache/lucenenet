using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Spatial4n.Distance;
using Spatial4n.Shapes;
using System;
using System.Collections;

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
    /// <para/>
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
        /// <exception cref="ArgumentNullException"><paramref name="strategy"/> or <paramref name="from"/> is <c>null</c>.</exception>
        public DistanceValueSource(PointVectorStrategy strategy, IPoint from, double multiplier)
        {
            // LUCENENET specific - added guard clauses
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
            this.from = from ?? throw new ArgumentNullException(nameof(from));
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
            // LUCENENET specific - added guard clause
            if (readerContext is null)
                throw new ArgumentNullException(nameof(readerContext));

            return new DistanceFunctionValue(this, readerContext.AtomicReader);
        }

        #region Nested type: DistanceFunctionValues

        internal class DistanceFunctionValue : FunctionValues
        {
            private readonly DistanceValueSource outerInstance;
            private readonly IDistanceCalculator calculator;
            //private readonly IPoint from; // LUCENENET: Never read
            private readonly double nullValue;

            private readonly FieldCache.Doubles ptX, ptY;
            private readonly IBits validX, validY;

            public DistanceFunctionValue(DistanceValueSource outerInstance, AtomicReader reader)
            {
                // LUCENENET specific - added guard clauses
                this.outerInstance = outerInstance ?? throw new ArgumentNullException(nameof(outerInstance));
                if (reader is null)
                    throw new ArgumentNullException(nameof(reader));

                ptX = FieldCache.DEFAULT.GetDoubles(reader, outerInstance.strategy.FieldNameX, true);
                ptY = FieldCache.DEFAULT.GetDoubles(reader, outerInstance.strategy.FieldNameY, true);
                validX = FieldCache.DEFAULT.GetDocsWithField(reader, outerInstance.strategy.FieldNameX);
                validY = FieldCache.DEFAULT.GetDocsWithField(reader, outerInstance.strategy.FieldNameY);

                //from = outerInstance.from; // LUCENENET: Never read
                calculator = outerInstance.strategy.SpatialContext.DistanceCalculator;
                nullValue = (outerInstance.strategy.SpatialContext.IsGeo ? 180 * outerInstance.multiplier : double.MaxValue);
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
                // make sure it has minX and area
                if (validX.Get(doc))
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(validY.Get(doc));
                    return calculator.Distance(outerInstance.from, ptX.Get(doc), ptY.Get(doc)) * outerInstance.multiplier;
                }
                return nullValue;
            }

            public override string ToString(int doc)
            {
                return outerInstance.GetDescription() + "=" + SingleVal(doc);
            }
        }

        #endregion

        public override bool Equals(object? o)
        {
            if (this == o) return true;
            if (o is null || GetType() != o.GetType()) return false;

            if (!(o is DistanceValueSource that)) return false;

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