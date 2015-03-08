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
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Function;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.BBox
{
    public class BBoxSimilarityValueSource : ValueSource
    {
        private readonly BBoxSimilarity similarity;
        private readonly BBoxStrategy strategy;

        public BBoxSimilarityValueSource(BBoxStrategy strategy, BBoxSimilarity similarity)
        {
            this.strategy = strategy;
            this.similarity = similarity;
        }

        public override string Description
        {
            get { return "BBoxSimilarityValueSource(" + similarity + ")"; }
        }

        public override FunctionValues GetValues(IDictionary<object, object> context, AtomicReaderContext readerContext)
        {
            return new BBoxSimilarityValueSourceFunctionValue(readerContext.AtomicReader, this);
        }

        public override bool Equals(object o)
        {
            var other = o as BBoxSimilarityValueSource;
            if (other == null) return false;
            return similarity.Equals(other.similarity);
        }

        public override int GetHashCode()
        {
            return typeof(BBoxSimilarityValueSource).GetHashCode() + similarity.GetHashCode();
        }

        #region Nested type: BBoxSimilarityValueSourceFunctionValue

        private class BBoxSimilarityValueSourceFunctionValue : FunctionValues
        {
            private readonly BBoxSimilarityValueSource _enclosingInstance;
            private readonly FieldCache.Doubles maxX, maxY;
            private readonly FieldCache.Doubles minX, minY;
            private readonly Rectangle rect;

            private readonly IBits validMaxX;
            private readonly IBits validMinX;

            public BBoxSimilarityValueSourceFunctionValue(AtomicReader reader,
                                                          BBoxSimilarityValueSource enclosingInstance)
            {
                _enclosingInstance = enclosingInstance;
                rect = _enclosingInstance.strategy.SpatialContext.MakeRectangle(0, 0, 0, 0); //reused

                minX = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.field_minX, true);
                minY = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.field_minY, true);
                maxX = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.field_maxX, true);
                maxY = FieldCache.DEFAULT.GetDoubles(reader, enclosingInstance.strategy.field_maxY, true);

                validMinX = FieldCache.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.field_minX);
                validMaxX = FieldCache.DEFAULT.GetDocsWithField(reader, enclosingInstance.strategy.field_maxX);
            }

            public override float FloatVal(int doc)
            {
                // make sure it has minX and area
                if (validMinX[doc] && validMaxX[doc])
                {
                    rect.Reset(
                        minX.Get(doc), maxX.Get(doc),
                        minY.Get(doc), maxY.Get(doc));
                    return (float)_enclosingInstance.similarity.Score(rect, null);
                }
                else
                {
                    return (float)_enclosingInstance.similarity.Score(null, null);
                }
            }

            public override Explanation Explain(int doc)
            {
                // make sure it has minX and area
                if (validMinX[doc] && validMaxX[doc])
                {
                    rect.Reset(
                        minX.Get(doc), maxX.Get(doc),
                        minY.Get(doc), maxY.Get(doc));
                    var exp = new Explanation();
                    _enclosingInstance.similarity.Score(rect, exp);
                    return exp;
                }
                return new Explanation(0, "No BBox");
            }

            public override string ToString(int doc)
            {
                return _enclosingInstance.Description + "=" + FloatVal(doc);
            }
        }

        #endregion
    }
}