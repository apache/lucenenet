using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Spatial
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
    /// A Spatial Filter implementing <see cref="SpatialOperation.IsDisjointTo"/> in terms
    /// of a <see cref="SpatialStrategy">SpatialStrategy</see>'s support for
    /// <see cref="SpatialOperation.Intersects"/>.
    /// A document is considered disjoint if it has spatial data that does not
    /// intersect with the query shape.  Another way of looking at this is that it's
    /// a way to invert a query shape.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class DisjointSpatialFilter : Filter
    {
        private readonly string? field;//maybe null
        private readonly Filter intersectsFilter;

        /// <param name="strategy">Needed to compute intersects</param>
        /// <param name="args">Used in spatial intersection</param>
        /// <param name="field">
        /// This field is used to determine which docs have spatial data via
        /// <see cref="IFieldCache.GetDocsWithField(AtomicReader, string)"/>.
        /// Passing <c>null</c> will assume all docs have spatial data.
        /// </param>
        public DisjointSpatialFilter(SpatialStrategy strategy, SpatialArgs args, string? field)
        {
            // LUCENENET specific - added guard clauses
            if (strategy is null)
                throw new ArgumentNullException(nameof(strategy));
            if (args is null)
                throw new ArgumentNullException(nameof(args));

            this.field = field;

            // TODO consider making SpatialArgs cloneable
            SpatialOperation origOp = args.Operation; //copy so we can restore
            args.Operation = SpatialOperation.Intersects; //temporarily set to intersects
            intersectsFilter = strategy.MakeFilter(args);
            args.Operation = origOp;
        }

        //restore so it looks like it was
        public override bool Equals(object? o)
        {
            if (this == o)
            {
                return true;
            }
            if (o is null || GetType() != o.GetType())
            {
                return false;
            }
            var that = (DisjointSpatialFilter)o;
            if (field != null ? !field.Equals(that.field, StringComparison.Ordinal) : that.field != null)
            {
                return false;
            }
            if (!intersectsFilter.Equals(that.intersectsFilter))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = field is null ? 0 : field.GetHashCode();
            result = 31 * result + intersectsFilter.GetHashCode();
            return result;
        }

        /// <exception cref="IOException"></exception>
        public override DocIdSet? GetDocIdSet(AtomicReaderContext context, IBits? acceptDocs)
        {
            // LUCENENET specific - added guard clause
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            IBits? docsWithField;
            if (field is null)
            {
                docsWithField = null;
            }
            else
            {
                //NOTE By using the FieldCache we re-use a cache
                // which is nice but loading it in this way might be slower than say using an
                // intersects filter against the world bounds. So do we add a method to the
                // strategy, perhaps?  But the strategy can't cache it.
                docsWithField = FieldCache.DEFAULT.GetDocsWithField(context.AtomicReader, field);

                int maxDoc = context.AtomicReader.MaxDoc;
                if (docsWithField.Length != maxDoc)
                {
                    throw IllegalStateException.Create("Bits length should be maxDoc (" + maxDoc + ") but wasn't: " + docsWithField);
                }

                if (docsWithField is Bits.MatchNoBits)
                {
                    return null;//match nothing
                }
                else if (docsWithField is Bits.MatchAllBits)
                {
                    docsWithField = null;//all docs
                }
            }
            
            //not so much a chain but a way to conveniently invert the Filter
            DocIdSet docIdSet = new ChainedFilter(new Filter[] { intersectsFilter }, ChainedFilter.ANDNOT).GetDocIdSet(context, acceptDocs);
            return BitsFilteredDocIdSet.Wrap(docIdSet, docsWithField);
        }
    }
}