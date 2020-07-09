using Lucene.Net.Search;
using System.IO;

namespace Lucene.Net.Index
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

    using Directory = Lucene.Net.Store.Directory;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;

    /// <summary>
    /// Utility class to safely share <see cref="DirectoryReader"/> instances across
    /// multiple threads, while periodically reopening. This class ensures each
    /// reader is disposed only once all threads have finished using it.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="SearcherManager"/>
    public sealed class ReaderManager : ReferenceManager<DirectoryReader>
    {
        /// <summary>
        /// Creates and returns a new <see cref="ReaderManager"/> from the given
        /// <see cref="IndexWriter"/>.
        /// </summary>
        /// <param name="writer">
        ///          the <see cref="IndexWriter"/> to open the <see cref="IndexReader"/> from. </param>
        /// <param name="applyAllDeletes">
        ///          If <c>true</c>, all buffered deletes will be applied (made
        ///          visible) in the <see cref="IndexSearcher"/> / <see cref="DirectoryReader"/>.
        ///          If <c>false</c>, the deletes may or may not be applied, but
        ///          remain buffered (in <see cref="IndexWriter"/>) so that they will be applied in
        ///          the future. Applying deletes can be costly, so if your app can
        ///          tolerate deleted documents being returned you might gain some
        ///          performance by passing <c>false</c>. See
        ///          <see cref="DirectoryReader.OpenIfChanged(DirectoryReader, IndexWriter, bool)"/>.
        /// </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public ReaderManager(IndexWriter writer, bool applyAllDeletes)
        {
            Current = DirectoryReader.Open(writer, applyAllDeletes);
        }

        /// <summary>
        /// Creates and returns a new <see cref="ReaderManager"/> from the given <see cref="Directory"/>. </summary>
        /// <param name="dir"> the directory to open the <see cref="DirectoryReader"/> on.
        /// </param>
        /// <exception cref="IOException"> If there is a low-level I/O error </exception>
        public ReaderManager(Directory dir)
        {
            Current = DirectoryReader.Open(dir);
        }

        protected override void DecRef(DirectoryReader reference)
        {
            reference.DecRef();
        }

        protected override DirectoryReader RefreshIfNeeded(DirectoryReader referenceToRefresh)
        {
            return DirectoryReader.OpenIfChanged(referenceToRefresh);
        }

        protected override bool TryIncRef(DirectoryReader reference)
        {
            return reference.TryIncRef();
        }

        protected override int GetRefCount(DirectoryReader reference)
        {
            return reference.RefCount;
        }
    }
}