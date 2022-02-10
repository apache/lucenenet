using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Spatial4n.Shapes;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    /// Provides access to a <see cref="ShapeFieldCache{T}" />
    /// for a given <see cref="AtomicReader" />.
    /// <para/>
    /// If a Cache does not exist for the Reader, then it is built by iterating over
    /// the all terms for a given field, reconstructing the Shape from them, and adding
    /// them to the Cache.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public abstract class ShapeFieldCacheProvider<T>
        where T : IShape
    {
        //private Logger log = Logger.GetLogger(GetType().FullName);

        // LUCENENET specific - use Lazy<T> to ensure only 1 thread can call the createValueCallback at a time,
        // since the default behavior is not atomic. See https://github.com/apache/lucenenet/issues/417.
        private readonly ConditionalWeakTable<IndexReader, Lazy<ShapeFieldCache<T>>> sidx =
            new ConditionalWeakTable<IndexReader, Lazy<ShapeFieldCache<T>>>();

        protected readonly int m_defaultSize;
        protected readonly string m_shapeField;

        protected ShapeFieldCacheProvider(string shapeField, int defaultSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            // it may be a List<T> or T
            this.m_shapeField = shapeField ?? throw new ArgumentNullException(nameof(shapeField)); // LUCENENET specific - added guard clause
            this.m_defaultSize = defaultSize;
        }

        [return: MaybeNull]
        protected abstract T ReadShape(BytesRef term);

        public virtual ShapeFieldCache<T> GetCache(AtomicReader reader)
        {
            // LUCENENET: ConditionalWeakTable allows us to simplify and remove locks on the
            // read operation. For the create case, we use Lazy<T> to ensure atomicity.
            return sidx.GetValue(reader, (key) => new Lazy<ShapeFieldCache<T>>(() =>
            {
                /*long startTime = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                log.Fine("Building Cache [" + reader.MaxDoc() + "]");*/
                ShapeFieldCache<T> idx = new ShapeFieldCache<T>(key.MaxDoc, m_defaultSize);
                int count = 0;
                DocsEnum? docs = null;
                Terms terms = ((AtomicReader)key).GetTerms(m_shapeField);
                TermsEnum? te = null;
                if (terms != null)
                {
                    te = terms.GetEnumerator(te);
                    while (te.MoveNext())
                    {
                        T? shape = ReadShape(te.Term);
                        if (shape != null)
                        {
                            docs = te.Docs(null, docs, DocsFlags.NONE);
                            int docid = docs.NextDoc();
                            while (docid != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                idx.Add(docid, shape);
                                docid = docs.NextDoc();
                                count++;
                            }
                        }
                    }
                }
                /*long elapsed = J2N.Time.NanoTime() / J2N.Time.MillisecondsPerNanosecond - startTime; // LUCENENET: Use NanoTime() rather than CurrentTimeMilliseconds() for more accurate/reliable results
                log.Fine("Cached: [" + count + " in " + elapsed + "ms] " + idx);*/
                return idx;
            })).Value;
        }
    }
}
