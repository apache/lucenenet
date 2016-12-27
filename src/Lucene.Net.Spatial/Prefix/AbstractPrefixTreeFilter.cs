using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;
using System.Diagnostics;

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
    /// Base class for Lucene Filters on SpatialPrefixTree fields.
    /// @lucene.experimental
    /// </summary>
    public abstract class AbstractPrefixTreeFilter : Filter
    {
        protected internal readonly IShape queryShape;
        protected internal readonly string fieldName;
        protected internal readonly SpatialPrefixTree grid;//not in equals/hashCode since it's implied for a specific field
        protected internal readonly int detailLevel;
        
        public AbstractPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, int detailLevel)
        {
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
        /// traversing a <see cref="TermsEnum">Lucene.Net.Index.TermsEnum</see>.
        /// </summary>
        public abstract class BaseTermsEnumTraverser
        {
            protected readonly AbstractPrefixTreeFilter outerInstance;
            protected readonly AtomicReaderContext context;
            protected IBits acceptDocs;
            protected readonly int maxDoc;

            protected TermsEnum termsEnum;//remember to check for null in getDocIdSet
            protected DocsEnum docsEnum;
            
            public BaseTermsEnumTraverser(AbstractPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs)
            {
                this.outerInstance = outerInstance;
                
                this.context = context;
                AtomicReader reader = context.AtomicReader;
                this.acceptDocs = acceptDocs;
                maxDoc = reader.MaxDoc;
                Terms terms = reader.Terms(outerInstance.fieldName);
                if (terms != null)
                {
                    termsEnum = terms.Iterator(null);
                }
            }

            protected virtual void CollectDocs(FixedBitSet bitSet)
            {
                //WARN: keep this specialization in sync
                Debug.Assert(termsEnum != null);
                docsEnum = termsEnum.Docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
                int docid;
                while ((docid = docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    bitSet.Set(docid);
                }
            }
        }

        #endregion

        /* Eventually uncomment when needed.

        protected void collectDocs(Collector collector) throws IOException {
          //WARN: keep this specialization in sync
          assert termsEnum != null;
          docsEnum = termsEnum.docs(acceptDocs, docsEnum, DocsEnum.FLAG_NONE);
          int docid;
          while ((docid = docsEnum.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS) {
            collector.collect(docid);
          }
        }

        public abstract class Collector {
          abstract void collect(int docid) throws IOException;
        }
        */
    }
}