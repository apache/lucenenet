// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy
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

    using ITwoPhaseCommit = Lucene.Net.Index.ITwoPhaseCommit;

    /// <summary>
    /// <see cref="ITaxonomyWriter"/> is the interface which the faceted-search library uses
    /// to dynamically build the taxonomy at indexing time.
    /// <para>
    /// Notes about concurrent access to the taxonomy:
    /// </para>
    /// <para>
    /// An implementation must allow multiple readers and a single writer to be
    /// active concurrently. Readers follow so-called "point in time" semantics,
    /// i.e., a reader object will only see taxonomy entries which were available
    /// at the time it was created. What the writer writes is only available to
    /// (new) readers after the writer's <see cref="Index.IndexWriter.Commit"/> is called.
    /// </para>
    /// <para>
    /// Faceted search keeps two indices - namely Lucene's main index, and this
    /// taxonomy index. When one or more readers are active concurrently with the
    /// writer, care must be taken to avoid an inconsistency between the state of
    /// these two indices: When writing to the indices, the taxonomy must always
    /// be committed to disk *before* the main index, because the main index
    /// refers to categories listed in the taxonomy.
    /// Such control can best be achieved by turning off the main index's
    /// "autocommit" feature, and explicitly calling <see cref="Index.IndexWriter.Commit"/> for both indices
    /// (first for the taxonomy, then for the main index).
    /// In old versions of Lucene (2.2 or earlier), when autocommit could not be
    /// turned off, a more complicated solution needs to be used. E.g., use
    /// some sort of (possibly inter-process) locking to ensure that a reader
    /// is being opened only right after both indices have been flushed (and
    /// before anything else is written to them).
    /// </para>
    /// 
    /// @lucene.experimental
    /// </summary>
    public interface ITaxonomyWriter : IDisposable, ITwoPhaseCommit
    {
        /// <summary>
        /// <see cref="AddCategory"/> adds a category with a given path name to the taxonomy,
        /// and returns its ordinal. If the category was already present in
        /// the taxonomy, its existing ordinal is returned.
        /// <para/>
        /// Before adding a category, <see cref="AddCategory"/> makes sure that all its
        /// ancestor categories exist in the taxonomy as well. As result, the
        /// ordinal of a category is guaranteed to be smaller then the ordinal of
        /// any of its descendants. 
        /// </summary>
        int AddCategory(FacetLabel categoryPath);

        /// <summary>
        /// <see cref="GetParent"/> returns the ordinal of the parent category of the category
        /// with the given ordinal.
        /// <para>
        /// When a category is specified as a path name, finding the path of its
        /// parent is as trivial as dropping the last component of the path.
        /// <see cref="GetParent"/> is functionally equivalent to calling <see cref="TaxonomyReader.GetPath"/> on the
        /// given ordinal, dropping the last component of the path, and then calling
        /// <see cref="TaxonomyReader.GetOrdinal(FacetLabel)"/> to get an ordinal back.
        /// </para>
        /// <para>
        /// If the given ordinal is the <see cref="TaxonomyReader.ROOT_ORDINAL"/>, an 
        /// <see cref="TaxonomyReader.INVALID_ORDINAL"/> is returned.
        /// If the given ordinal is a top-level category, the 
        /// <see cref="TaxonomyReader.ROOT_ORDINAL"/> is returned.
        /// If an invalid ordinal is given (negative or beyond the last available
        /// ordinal), an <see cref="ArgumentOutOfRangeException"/> is thrown. However, it is
        /// expected that <see cref="GetParent"/> will only be called for ordinals which are
        /// already known to be in the taxonomy.
        /// </para>
        /// <para>
        /// TODO (Facet): instead of a <see cref="GetParent(int)">GetParent(ordinal)</see> method, consider having a
        /// GetCategory(categorypath, prefixlen) which is similar to <see cref="AddCategory"/>
        /// except it doesn't add new categories; This method can be used to get
        /// the ordinals of all prefixes of the given category, and it can use
        /// exactly the same code and cache used by <see cref="AddCategory"/> so it means less code.
        /// </para>
        /// </summary>
        int GetParent(int ordinal);

        /// <summary>
        /// <see cref="Count"/> returns the number of categories in the taxonomy.
        /// <para/>
        /// Because categories are numbered consecutively starting with 0, it
        /// means the taxonomy contains ordinals 0 through <see cref="Count"/>-1.
        /// <para/>
        /// Note that the number returned by <see cref="Count"/> is often slightly higher
        /// than the number of categories inserted into the taxonomy; This is
        /// because when a category is added to the taxonomy, its ancestors
        /// are also added automatically (including the root, which always get
        /// ordinal 0).
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Sets the commit user data map. That method is considered a transaction and
        /// will be committed (<see cref="Index.IndexWriter.Commit"/>) even if no other changes were made to
        /// the writer instance.
        /// <para>
        /// <b>NOTE:</b> the map is cloned internally, therefore altering the map's
        /// contents after calling this method has no effect.
        /// </para>
        /// </summary>
        void SetCommitData(IDictionary<string, string> commitUserData);

        /// <summary>
        /// Returns the commit user data map that was set on
        /// <see cref="SetCommitData(IDictionary{string, string})"/>.
        /// </summary>
        IDictionary<string, string> CommitData { get; }
    }
}