// Lucene version compatibility level 4.8.1
using System;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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

    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;

    /// <summary>
    /// <see cref="ITaxonomyWriterCache"/> is a relatively simple interface for a cache of
    /// category->ordinal mappings, used in ITaxonomyWriter implementations (such as
    /// <see cref="DirectoryTaxonomyWriter"/>).
    /// <para>
    /// It basically has <see cref="Put"/> methods for adding a mapping, and <see cref="Get"/> for looking a
    /// mapping up the cache. The cache does <b>not</b> guarantee to hold everything
    /// that has been put into it, and might in fact selectively delete some of the
    /// mappings (e.g., the ones least recently used). This means that if <see cref="Get"/>
    /// returns a negative response, it does not necessarily mean that the category
    /// doesn't exist - just that it is not in the cache. The caller can only infer
    /// that the category doesn't exist if it knows the cache to be complete (because
    /// all the categories were loaded into the cache, and since then no <see cref="Put"/>
    /// returned true).
    /// </para>
    /// <para>
    /// However, if it does so, it should clear out large parts of the cache at once,
    /// because the user will typically need to work hard to recover from every cache
    /// cleanup (see <see cref="Put(FacetLabel, int)"/>'s return value).
    /// </para>
    /// <para>
    /// <b>NOTE:</b> the cache may be accessed concurrently by multiple threads,
    /// therefore cache implementations should take this into consideration.
    /// 
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public interface ITaxonomyWriterCache : IDisposable
    {
        /// <summary>
        /// Lookup a category in the cache, returning its ordinal, or a negative
        /// number if the category is not in the cache.
        /// <para>
        /// It is up to the caller to remember what a negative response means:
        /// If the caller knows the cache is <i>complete</i> (it was initially
        /// fed with all the categories, and since then <see cref="Put"/> never returned true)
        /// it means the category does not exist. Otherwise, the category might
        /// still exist, but just be missing from the cache.
        /// </para>
        /// </summary>
        int Get(FacetLabel categoryPath);

        /// <summary>
        /// Add a category to the cache, with the given ordinal as the value.
        /// <para>
        /// If the implementation keeps only a partial cache (e.g., an LRU cache)
        /// and finds that its cache is full, it should clear up part of the cache
        /// and return <c>true</c>. Otherwise, it should return
        /// <c>false</c>.
        /// </para>
        /// <para>
        /// The reason why the caller needs to know if part of the cache was
        /// cleared is that in that case it will have to commit its on-disk index
        /// (so that all the latest category additions can be searched on disk, if
        /// we can't rely on the cache to contain them).
        /// </para>
        /// <para>
        /// Ordinals should be non-negative. Currently there is no defined way to
        /// specify that a cache should remember a category does NOT exist.
        /// It doesn't really matter, because normally the next thing we do after
        /// finding that a category does not exist is to add it.
        /// </para>
        /// </summary>
        bool Put(FacetLabel categoryPath, int ordinal);

        /// <summary>
        /// Returns true if the cache is full, such that the next <see cref="Put"/> will
        /// evict entries from it, false otherwise.
        /// </summary>
        bool IsFull { get; }

        /// <summary>
        /// Clears the content of the cache. Unlike <see cref="IDisposable.Dispose()"/>, the caller can
        /// assume that the cache is still operable after this method returns.
        /// </summary>
        void Clear();
    }
}