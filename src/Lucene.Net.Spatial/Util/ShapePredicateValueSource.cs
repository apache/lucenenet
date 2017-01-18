using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
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
    /// A boolean <see cref="ValueSource"/> that compares a shape from a provided <see cref="ValueSource"/> with a given <see cref="IShape">Shape</see> and sees
    /// if it matches a given <see cref="SpatialOperation"/> (the predicate).
    /// 
    /// @lucene.experimental
    /// </summary>
    public class ShapePredicateValueSource : ValueSource
    {
        private readonly ValueSource shapeValuesource;//the left hand side
        private readonly SpatialOperation op;
        private readonly IShape queryShape;//the right hand side (constant)

        /// <summary>
        /// 
        /// </summary>
        /// <param name="shapeValuesource">
        /// Must yield <see cref="IShape"/> instances from it's ObjectVal(doc). If null
        /// then the result is false. This is the left-hand (indexed) side.
        /// </param>
        /// <param name="op">the predicate</param>
        /// <param name="queryShape">The shape on the right-hand (query) side.</param>
        public ShapePredicateValueSource(ValueSource shapeValuesource, SpatialOperation op, IShape queryShape)
        {
            this.shapeValuesource = shapeValuesource;
            this.op = op;
            this.queryShape = queryShape;
        }


        public override string GetDescription()
        {
            return shapeValuesource + " " + op + " " + queryShape;
        }


        public override void CreateWeight(IDictionary context, IndexSearcher searcher)
        {
            shapeValuesource.CreateWeight(context, searcher);
        }

        public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
        {
            FunctionValues shapeValues = shapeValuesource.GetValues(context, readerContext);

            return new BoolDocValuesAnonymousHelper(this, shapeValues);
        }

        internal class BoolDocValuesAnonymousHelper : BoolDocValues
        {
            private readonly ShapePredicateValueSource outerInstance;
            private readonly FunctionValues shapeValues;

            public BoolDocValuesAnonymousHelper(ShapePredicateValueSource outerInstance, FunctionValues shapeValues)
                : base(outerInstance)
            {
                this.outerInstance = outerInstance;
                this.shapeValues = shapeValues;
            }

            public override bool BoolVal(int doc)
            {
                IShape indexedShape = (IShape)shapeValues.ObjectVal(doc);
                if (indexedShape == null)
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

        public override bool Equals(object o)
        {
            if (this == o) return true;
            if (o == null || GetType() != o.GetType()) return false;

            ShapePredicateValueSource that = (ShapePredicateValueSource)o;

            if (!shapeValuesource.Equals(that.shapeValuesource)) return false;
            if (!op.Equals(that.op)) return false;
            if (!queryShape.Equals(that.queryShape)) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int result = shapeValuesource.GetHashCode();
            result = 31 * result + op.GetHashCode();
            result = 31 * result + queryShape.GetHashCode();
            return result;
        }
    }
}
