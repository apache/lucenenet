using J2N.Numerics;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
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
    /// The distance from a provided Point to a Point retrieved from a ValueSource via
    /// <see cref="FunctionValues.ObjectVal(int)"/>. The distance
    /// is calculated via a <see cref="IDistanceCalculator"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class DistanceToShapeValueSource : ValueSource
    {
        private readonly ValueSource shapeValueSource;
        private readonly IPoint queryPoint;
        private readonly double multiplier;
        private readonly IDistanceCalculator distCalc;

        //TODO if FunctionValues returns NaN; will things be ok?
        private readonly double nullValue;//computed

        public DistanceToShapeValueSource(ValueSource shapeValueSource, IPoint queryPoint,
                                          double multiplier, SpatialContext ctx)
        {
            // LUCENENET specific - added guard clauses
            this.shapeValueSource = shapeValueSource ?? throw new ArgumentNullException(nameof(shapeValueSource));
            this.queryPoint = queryPoint ?? throw new ArgumentNullException(nameof(shapeValueSource));
            if (ctx is null)
                throw new ArgumentNullException(nameof(ctx));

            this.multiplier = multiplier;
            this.distCalc = ctx.DistanceCalculator;
            this.nullValue =
                (ctx.IsGeo ? 180 * multiplier : double.MaxValue);
        }

        public override string GetDescription()
        {
            return "distance(" + queryPoint + " to " + shapeValueSource.GetDescription() + ")*" + multiplier + ")";
        }

        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            shapeValueSource.CreateWeight(context, searcher);
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            // LUCENENET specific - added guard clause
            if (readerContext is null)
                throw new ArgumentNullException(nameof(readerContext));

            FunctionValues shapeValues = shapeValueSource.GetValues(context, readerContext);

            return new DoubleDocValuesAnonymousClass(this, shapeValues);
        }

        private sealed class DoubleDocValuesAnonymousClass : DoubleDocValues
        {
            private readonly DistanceToShapeValueSource outerInstance;
            private readonly FunctionValues shapeValues;

            public DoubleDocValuesAnonymousClass(DistanceToShapeValueSource outerInstance, FunctionValues shapeValues)
                : base(outerInstance)
            {
                // LUCENENET specific - added guard clauses
                this.outerInstance = outerInstance ?? throw new ArgumentNullException(nameof(outerInstance));
                this.shapeValues = shapeValues ?? throw new ArgumentNullException(nameof(shapeValues));
            }

            public override double DoubleVal(int doc)
            {
                IShape shape = (IShape)shapeValues.ObjectVal(doc);
                if (shape is null || shape.IsEmpty)
                    return outerInstance.nullValue;
                IPoint pt = shape.Center;
                return outerInstance.distCalc.Distance(outerInstance.queryPoint, pt) * outerInstance.multiplier;
            }

            public override Explanation Explain(int doc)
            {
                Explanation exp = base.Explain(doc);
                exp.AddDetail(shapeValues.Explain(doc));
                return exp;
            }
        }

        public override bool Equals(object? o)
        {
            if (this == o) return true;
            if (o is null || GetType() != o.GetType()) return false;

            DistanceToShapeValueSource that = (DistanceToShapeValueSource)o;

            if (!queryPoint.Equals(that.queryPoint)) return false;
            if (that.multiplier.CompareTo(multiplier) != 0) return false;
            if (!shapeValueSource.Equals(that.shapeValueSource)) return false;
            if (!distCalc.Equals(that.distCalc)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result;
            long temp;
            result = shapeValueSource.GetHashCode();
            result = 31 * result + queryPoint.GetHashCode();
            temp = J2N.BitConversion.DoubleToInt64Bits(multiplier);
            result = 31 * result + (int)(temp ^ (temp.TripleShift(32)));
            return result;
        }
    }
}
