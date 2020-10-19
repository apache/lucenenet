using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Prefix.Tree;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;
using System;
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
        protected internal readonly IShape m_queryShape;
        protected internal readonly string m_fieldName;
        protected internal readonly SpatialPrefixTree m_grid;//not in equals/hashCode since it's implied for a specific field
        protected internal readonly int m_detailLevel;
        
        public AbstractPrefixTreeFilter(IShape queryShape, string fieldName, SpatialPrefixTree grid, int detailLevel)
        {
            this.m_queryShape = queryShape;
            this.m_fieldName = fieldName;
            this.m_grid = grid;
            this.m_detailLevel = detailLevel;
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
            if (m_detailLevel != that.m_detailLevel)
            {
                return false;
            }
            if (!m_fieldName.Equals(that.m_fieldName, StringComparison.Ordinal))
            {
                return false;
            }
            if (!m_queryShape.Equals(that.m_queryShape))
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int result = m_queryShape.GetHashCode();
            result = 31 * result + m_fieldName.GetHashCode();
            result = 31 * result + m_detailLevel;
            return result;
        }

        #region Nested type: BaseTermsEnumTraverser

        /// <summary>
        /// Holds transient state and docid collecting utility methods as part of
        /// traversing a <see cref="TermsEnum">Lucene.Net.Index.TermsEnum</see>.
        /// </summary>
        public abstract class BaseTermsEnumTraverser
        {
            protected readonly AbstractPrefixTreeFilter m_outerInstance;
            protected readonly AtomicReaderContext m_context;
            protected IBits m_acceptDocs;
            protected readonly int m_maxDoc;

            protected TermsEnum m_termsEnum;//remember to check for null in getDocIdSet
            protected DocsEnum m_docsEnum;
            
            public BaseTermsEnumTraverser(AbstractPrefixTreeFilter outerInstance, AtomicReaderContext context, IBits acceptDocs)
            {
                this.m_outerInstance = outerInstance;
                
                this.m_context = context;
                AtomicReader reader = context.AtomicReader;
                this.m_acceptDocs = acceptDocs;
                m_maxDoc = reader.MaxDoc;
                Terms terms = reader.GetTerms(outerInstance.m_fieldName);
                if (terms != null)
                {
                    m_termsEnum = terms.GetEnumerator();
                }
            }

            protected virtual void CollectDocs(FixedBitSet bitSet)
            {
                //WARN: keep this specialization in sync
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(m_termsEnum != null);
                m_docsEnum = m_termsEnum.Docs(m_acceptDocs, m_docsEnum, DocsFlags.NONE);
                int docid;
                while ((docid = m_docsEnum.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
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
          docsEnum = termsEnum.docs(acceptDocs, docsEnum, DocsFlags.NONE);
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