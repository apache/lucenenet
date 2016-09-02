/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections.Generic;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>
    /// A basic implementation of
    /// <see cref="PrefixTreeStrategy">PrefixTreeStrategy</see>
    /// using a large
    /// <see cref="Lucene.Net.Queries.TermsFilter">Lucene.Net.Queries.TermsFilter
    /// 	</see>
    /// of all the cells from
    /// <see cref="Lucene.Net.Spatial.Prefix.Tree.SpatialPrefixTree.GetCells(Shape, int, bool, bool)
    /// 	"SpatialPrefixTree.GetCells(Shape, int, bool, bool)
    /// 	</see>
    /// . It only supports the search of indexed Point shapes.
    /// <p/>
    /// The precision of query shapes (distErrPct) is an important factor in using
    /// this Strategy. If the precision is too precise then it will result in many
    /// terms which will amount to a slower query.
    /// </summary>
    /// <lucene.experimental></lucene.experimental>
    public class TermQueryPrefixTreeStrategy : PrefixTreeStrategy
    {
        public TermQueryPrefixTreeStrategy(SpatialPrefixTree grid, string fieldName)
            : base(grid, fieldName, false)
        {
        }

        //do not simplify indexed cells
        public override Filter MakeFilter(SpatialArgs args)
        {
            SpatialOperation op = args.Operation;
            if (op != SpatialOperation.Intersects)
            {
                throw new UnsupportedSpatialOperation(op);
            }
            Shape shape = args.Shape;
            int detailLevel = grid.GetLevelForDistance(args.ResolveDistErr(ctx, distErrPct));
            IList<Cell> cells = grid.GetCells(shape, detailLevel, false, true);
            //no parents
            //simplify
            var terms = new BytesRef[cells.Count];
            int i = 0;
            foreach (Cell cell in cells)
            {
                terms[i++] = new BytesRef(cell.TokenString);//TODO use cell.getTokenBytes()
            }
            return new TermsFilter(FieldName, terms);
        }
    }
}