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
using System.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Prefix
{
    /// <summary>Base class for Lucene Filters on SpatialPrefixTree fields.</summary>
    /// <remarks>Base class for Lucene Filters on SpatialPrefixTree fields.</remarks>
    /// <lucene.experimental></lucene.experimental>
    public abstract class AbstractPrefixTreeFilter : Filter
    {
        protected internal readonly int detailLevel;
        protected internal readonly string fieldName;

        protected internal readonly SpatialPrefixTree grid;
        protected internal readonly Shape queryShape;

        public AbstractPrefixTreeFilter(Shape queryShape, string
                                                              fieldName, SpatialPrefixTree grid, int detailLevel)
        {
            //not in equals/hashCode since it's implied for a specific field
            this.queryShape = queryShape;
            this.fieldName = fieldName;
            this.grid = grid;
            this.detailLevel = detailLevel;
        }

        public override bool Equals(object o)
        {
            if (this == o)
            {
                return true;
            }
            if (!GetType().Equals(o.GetType()))
            {
                return false;
            }
            var that = (AbstractPrefixTreeFilter)o;
            if (detailLevel != that.detailLevel)
            {
                return false;
            }
            if (!fieldName.Equals(that.fieldName))
            {
                return false;
            }
            if (!queryShape.Equals(that.queryShape))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = queryShape.GetHashCode();
            result = 31 * result + fieldName.GetHashCode();
            result = 31 * result + detailLevel;
            return result;
        }

        #region Nested type: BaseTermsEnumTraverser

        /// <summary>
        /// Holds transient state and docid collecting utility methods as part of
        /// traversing a
        /// <see cref="TermsEnum">Lucene.Net.Index.TermsEnum</see>
        /// .
        /// </summary>
        public abstract class BaseTermsEnumTraverser
        {
            private readonly AbstractPrefixTreeFilter _enclosing;
            protected internal readonly AtomicReaderContext context;

            protected internal readonly int maxDoc;
            protected internal Bits acceptDocs;

            protected internal DocsEnum docsEnum;
            protected internal TermsEnum termsEnum;

            /// <exception cref="System.IO.IOException"></exception>
            public BaseTermsEnumTraverser(AbstractPrefixTreeFilter _enclosing, AtomicReaderContext
                                                                                   context, Bits acceptDocs)
            {
                this._enclosing = _enclosing;
                //remember to check for null in getDocIdSet
                this.context = context;
                AtomicReader reader = context.AtomicReader;
                this.acceptDocs = acceptDocs;
                maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(this._enclosing.fieldName);
                if (terms != null)
                {
                    termsEnum = terms.Iterator(null);
                }
            }

            /// <exception cref="System.IO.IOException"></exception>
            protected internal virtual void CollectDocs(FixedBitSet bitSet)
            {
                //WARN: keep this specialization in sync
                Debug.Assert(termsEnum != null);
                docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE
                    );
                int docid;
                while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    bitSet.Set(docid);
                }
            }
        }

        #endregion
    }
}