using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
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
    /// A Filter matching documents that have an <see cref="SpatialRelation.Intersects"/>
    /// (i.e. not DISTINCT) relationship with a provided query shape.
    /// 
    /// @lucene.internal
    /// </summary>
    public class IntersectsPrefixTreeFilter : AbstractVisitingPrefixTreeFilter
    {
        private readonly bool hasIndexedLeaves;

        public IntersectsPrefixTreeFilter(IShape queryShape, string fieldName, 
                                          SpatialPrefixTree grid, int detailLevel,
                                          int prefixGridScanLevel, bool hasIndexedLeaves)
            : base(queryShape, fieldName, grid, detailLevel, prefixGridScanLevel)
        {
            this.hasIndexedLeaves = hasIndexedLeaves;
        }

        public override bool Equals(object o)
        {
            return base.Equals(o) && hasIndexedLeaves == ((IntersectsPrefixTreeFilter)o).hasIndexedLeaves;
        }

        /// <summary>
        /// LUCENENET specific: need to override GetHashCode to 
        /// prevent a compiler warning and realistically, the hash code
        /// should work similarly to Equals.
        /// </summary>
        public override int GetHashCode()
        {
            return HashHelpers.CombineHashCodes(base.GetHashCode(), hasIndexedLeaves.GetHashCode());
        }

        /// <exception cref="System.IO.IOException"></exception>
        public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
        {
            return new _VisitorTemplate_55(this, context, acceptDocs, hasIndexedLeaves).GetDocIdSet();
        }

        #region Nested type: _VisitorTemplate_55

        private sealed class _VisitorTemplate_55 : VisitorTemplate
        {
            private FixedBitSet results;

            public _VisitorTemplate_55(IntersectsPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs, bool hasIndexedLeaves)
                : base(outerInstance, context, acceptDocs, hasIndexedLeaves)
            {
            }

            protected internal override void Start()
            {
                results = new FixedBitSet(maxDoc);
            }

            protected internal override DocIdSet Finish()
            {
                return results;
            }

            protected internal override bool Visit(Cell cell)
            {
                if (cell.ShapeRel == SpatialRelation.WITHIN || cell.Level == outerInstance.detailLevel)
                {
                    CollectDocs(results);
                    return false;
                }
                return true;
            }

            protected internal override void VisitLeaf(Cell cell)
            {
                CollectDocs(results);
            }

            protected internal override void VisitScanned(Cell cell)
            {
                if (outerInstance.queryShape.Relate(cell.Shape).Intersects())
                {
                    CollectDocs(results);
                }
            }
        }

        #endregion
    }
}