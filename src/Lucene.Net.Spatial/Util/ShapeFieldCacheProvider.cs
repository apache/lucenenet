using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Spatial4n.Core.Shapes;

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
    /// Provides access to a
    /// <see cref="ShapeFieldCache{T}">ShapeFieldCache&lt;T&gt;</see>
    /// for a given
    /// <see cref="Lucene.Net.Index.AtomicReader">Lucene.Net.Index.AtomicReader</see>.
    /// 
    /// If a Cache does not exist for the TextReader, then it is built by iterating over
    /// the all terms for a given field, reconstructing the Shape from them, and adding
    /// them to the Cache.
    /// </summary>
    /// @lucene.internal
    public abstract class ShapeFieldCacheProvider<T>
        where T : IShape
    {
        //private Logger log = Logger.GetLogger(GetType().FullName);

        private readonly WeakDictionary<IndexReader, ShapeFieldCache<T>> sidx =
            new WeakDictionary<IndexReader, ShapeFieldCache<T>>();

        protected internal readonly int defaultSize;
        protected internal readonly string shapeField;

        public ShapeFieldCacheProvider(string shapeField, int defaultSize)
        {
            // it may be a List<T> or T
            this.shapeField = shapeField;
            this.defaultSize = defaultSize;
        }

        protected internal abstract T ReadShape(BytesRef term);

        private readonly object locker = new object();

        public virtual ShapeFieldCache<T> GetCache(AtomicReader reader)
        {
            lock (locker)
            {
                ShapeFieldCache<T> idx;
                if (sidx.TryGetValue(reader, out idx) && idx != null)
                {
                    return idx;
                }
                /*long startTime = Runtime.CurrentTimeMillis();
				log.Fine("Building Cache [" + reader.MaxDoc() + "]");*/
                idx = new ShapeFieldCache<T>(reader.MaxDoc, defaultSize);
                int count = 0;
                DocsEnum docs = null;
                Terms terms = reader.Terms(shapeField);
                TermsEnum te = null;
                if (terms != null)
                {
                    te = terms.Iterator(te);
                    BytesRef term = te.Next();
                    while (term != null)
                    {
                        T shape = ReadShape(term);
                        if (shape != null)
                        {
                            docs = te.Docs(null, docs, DocsEnum.FLAG_NONE);
                            int docid = docs.NextDoc();
                            while (docid != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                idx.Add(docid, shape);
                                docid = docs.NextDoc();
                                count++;
                            }
                        }
                        term = te.Next();
                    }
                }
                sidx[reader] = idx;
                /*long elapsed = Runtime.CurrentTimeMillis() - startTime;
                log.Fine("Cached: [" + count + " in " + elapsed + "ms] " + idx);*/
                return idx;
            }
        }
    }
}
