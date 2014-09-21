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

using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Util;
using Spatial4n.Core.Distance;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Vector
{
    /// <summary>
    /// An implementation of the Lucene ValueSource model that returns the distance.
    /// </summary>
    public class DistanceValueSource : ValueSource
    {
        private readonly Point from;
        private readonly PointVectorStrategy strategy;

        public DistanceValueSource(PointVectorStrategy strategy, Point from)
        {
            this.strategy = strategy;
            this.from = from;
        }

        public override string Description
        {
            get { return "DistanceValueSource(" + strategy + ", " + from + ")"; }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return new DistanceFunctionValue(this, readerContext.AtomicReader);
        }

        public override bool Equals(object o)
        {
            if (this == o) return true;

            var that = o as DistanceValueSource;
            if (that == null) return false;

            if (!from.Equals(that.from)) return false;
            if (!strategy.Equals(that.strategy)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return from.GetHashCode();
        }

        #region Nested type: DistanceFunctionValues

        public class DistanceFunctionValue : FunctionValues
        {
            private readonly DistanceCalculator calculator;
            private readonly DistanceValueSource enclosingInstance;
            private readonly Point from;
            private readonly double nullValue;

            private readonly FieldCache.Doubles ptX, ptY;
            private readonly IBits validX, validY;

            public DistanceFunctionValue(DistanceValueSource enclosingInstance, AtomicReader reader)
            {
                this.enclosingInstance = enclosingInstance;

                ptX = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.FieldNameX, true);
                ptY = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.FieldNameY, true);
                validX = FieldCache.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.FieldNameX);
                validY = FieldCache.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.FieldNameY);

                from = enclosingInstance.from;
                calculator = enclosingInstance.strategy.SpatialContext.GetDistCalc();
                nullValue = (enclosingInstance.strategy.SpatialContext.IsGeo() ? 180 : double.MaxValue);
            }

            public override float FloatVal(int doc)
            {
                return (float) DoubleVal(doc);
            }

            public override double DoubleVal(int doc)
            {
                // make sure it has minX and area
                if (validX[doc])
                {
                    Debug.Assert(validY[doc]);
                    return calculator.Distance(from, ptX.Get(doc), ptY.Get(doc));
                }
                return nullValue;
            }

            public override string ToString(int doc)
            {
                return enclosingInstance.Description + "=" + FloatVal(doc);
            }
        }

        #endregion
    }
}