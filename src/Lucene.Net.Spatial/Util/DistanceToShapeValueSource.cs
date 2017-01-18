using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Spatial4n.Core.Context;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;
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
            this.shapeValueSource = shapeValueSource;
            this.queryPoint = queryPoint;
            this.multiplier = multiplier;
            this.distCalc = ctx.DistCalc;
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
            FunctionValues shapeValues = shapeValueSource.GetValues(context, readerContext);

            return new DoubleDocValuesAnonymousHelper(this, shapeValues);
        }

        internal class DoubleDocValuesAnonymousHelper : DoubleDocValues
        {
            private readonly DistanceToShapeValueSource outerInstance;
            private readonly FunctionValues shapeValues;

            public DoubleDocValuesAnonymousHelper(DistanceToShapeValueSource outerInstance, FunctionValues shapeValues)
                : base(outerInstance)
            {
                this.outerInstance = outerInstance;
                this.shapeValues = shapeValues;
            }

            public override double DoubleVal(int doc)
            {
                IShape shape = (IShape)shapeValues.ObjectVal(doc);
                if (shape == null || shape.IsEmpty)
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

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

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
            temp = Number.DoubleToLongBits(multiplier);
            result = 31 * result + (int)(temp ^ ((long)((ulong)temp) >> 32));
            return result;
        }
    }
}
