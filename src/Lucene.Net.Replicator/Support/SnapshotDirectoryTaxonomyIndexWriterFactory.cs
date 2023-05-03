using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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

    // LUCENENET specific - refactored SnapshotDirectoryTaxonomyWriter into SnapshotDirectoryTaxonomyIndexWriterFactory and de-nested
    
    /// <summary>
    /// An implementation of <see cref="DirectoryTaxonomyIndexWriterFactory"/>
    /// which sets the underlying <see cref="Index.IndexWriter"/>'s <see cref="IndexDeletionPolicy"/> to
    /// <see cref="SnapshotDeletionPolicy"/>.
    /// </summary>
    public class SnapshotDirectoryTaxonomyIndexWriterFactory : DirectoryTaxonomyIndexWriterFactory
    {
        private SnapshotDeletionPolicy sdp;
        private IndexWriter writer;

        /// <summary>
        /// Creates a new <see cref="IndexWriterConfig"/> using <see cref="DirectoryTaxonomyIndexWriterFactory.CreateIndexWriterConfig"/> and
        /// sets IndexDeletionPolicy to <see cref="SnapshotDeletionPolicy"/>.
        /// </summary>
        public override IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
        {
            IndexWriterConfig conf = base.CreateIndexWriterConfig(openMode);
            conf.IndexDeletionPolicy = sdp = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
            return conf;
        }

        /// <inheritdoc/>
        public override IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
        {
            return writer = base.OpenIndexWriter(directory, config);
        }

        /// <summary>
        /// Gets the <see cref="SnapshotDeletionPolicy"/> used by the underlying <see cref="Index.IndexWriter"/>.
        /// </summary>
        public virtual SnapshotDeletionPolicy DeletionPolicy => sdp;

        /// <summary>
        /// Gets the <see cref="Index.IndexWriter"/> that was opened by <see cref="DirectoryTaxonomyWriter"/>
        /// that is using this factory class.
        /// </summary>
        public virtual IndexWriter IndexWriter => writer;
    }
}