using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
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
    /// A boolean <see cref="ValueSource" /> that compares a shape from a provided <see cref="ValueSource" /> with a given <see cref="IShape" /> and sees
    /// if it matches a given <see cref="SpatialOperation" /> (the predicate).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class ShapePredicateValueSource : ValueSource
    {
        private readonly ValueSource shapeValueSource;//the left hand side
        private readonly SpatialOperation op;
        private readonly IShape queryShape;//the right hand side (constant)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shapeValueSource">
        /// Must yield <see cref="IShape"/> instances from it's ObjectVal(doc). If null
        /// then the result is false. This is the left-hand (indexed) side.
        /// </param>
        /// <param name="op">the predicate</param>
        /// <param name="queryShape">The shape on the right-hand (query) side.</param>
        public ShapePredicateValueSource(ValueSource shapeValueSource, SpatialOperation op, IShape queryShape)
        {
            // LUCENENET specific - added guard clauses
            this.shapeValueSource = shapeValueSource ?? throw new ArgumentNullException(nameof(shapeValueSource));
            this.op = op ?? throw new ArgumentNullException(nameof(op));
            this.queryShape = queryShape ?? throw new ArgumentNullException(nameof(queryShape));
        }


        public override string GetDescription()
        {
            return shapeValueSource + " " + op + " " + queryShape;
        }


        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            shapeValueSource.CreateWeight(context, searcher);
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues shapeValues = shapeValueSource.GetValues(context, readerContext);

            return new BoolDocValuesAnonymousClass(this, shapeValues);
        }

        private sealed class BoolDocValuesAnonymousClass : BoolDocValues
        {
            private readonly ShapePredicateValueSource outerInstance;
            private readonly FunctionValues shapeValues;

            public BoolDocValuesAnonymousClass(ShapePredicateValueSource outerInstance, FunctionValues shapeValues)
                : base(outerInstance)
            {
                // LUCENENET specific - added guard clauses
                this.outerInstance = outerInstance ?? throw new ArgumentNullException(nameof(outerInstance));
                this.shapeValues = shapeValues ?? throw new ArgumentNullException(nameof(shapeValues));
            }

            public override bool BoolVal(int doc)
            {
                IShape indexedShape = (IShape)shapeValues.ObjectVal(doc);
                if (indexedShape is null)
                    return false;
                return outerInstance.op.Evaluate(indexedShape, outerInstance.queryShape);
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

            ShapePredicateValueSource that = (ShapePredicateValueSource)o;

            if (!shapeValueSource.Equals(that.shapeValueSource)) return false;
            if (!op.Equals(that.op)) return false;
            if (!queryShape.Equals(that.queryShape)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = shapeValueSource.GetHashCode();
            result = 31 * result + op.GetHashCode();
            result = 31 * result + queryShape.GetHashCode();
            return result;
        }
    }
}
