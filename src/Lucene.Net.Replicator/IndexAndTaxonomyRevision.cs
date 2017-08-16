using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Facet.Taxonomy.WriterCache;
using Lucene.Net.Index;
using Lucene.Net.Store;
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

    /// <summary>
    /// A <see cref="IRevision"/> of a single index and taxonomy index files which comprises
    /// the list of files from both indexes. This revision should be used whenever a
    /// pair of search and taxonomy indexes need to be replicated together to
    /// guarantee consistency of both on the replicating (client) side.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    /// <seealso cref="IndexRevision"/>
    public class IndexAndTaxonomyRevision : IRevision
    {
        /// <summary>
        /// A <see cref="DirectoryTaxonomyWriter"/> which sets the underlying
        /// <see cref="Index.IndexWriter"/>'s <see cref="IndexDeletionPolicy"/> to
        /// <see cref="SnapshotDeletionPolicy"/>.
        /// </summary>
        public class SnapshotDirectoryTaxonomyWriter : DirectoryTaxonomyWriter
        {
            /// <summary>
            /// Gets the <see cref="SnapshotDeletionPolicy"/> used by the underlying <see cref="Index.IndexWriter"/>.
            /// </summary>
            public SnapshotDeletionPolicy DeletionPolicy { get; private set; }
            /// <summary>
            /// Gets the <see cref="Index.IndexWriter"/> used by this <see cref="DirectoryTaxonomyWriter"/>.
            /// </summary>
            public IndexWriter IndexWriter { get; private set; }

            /// <summary>
            /// <see cref="DirectoryTaxonomyWriter(Directory, OpenMode, ITaxonomyWriterCache)"/>
            /// </summary>
            /// <exception cref="IOException"></exception>
            public SnapshotDirectoryTaxonomyWriter(Directory directory, OpenMode openMode, ITaxonomyWriterCache cache)
                : base(directory, openMode, cache)
            {
            }

            /// <summary>
            /// <see cref="DirectoryTaxonomyWriter(Directory, OpenMode)"/>
            /// </summary>
            /// <exception cref="IOException"></exception>
            public SnapshotDirectoryTaxonomyWriter(Directory directory, OpenMode openMode = OpenMode.CREATE_OR_APPEND)
                : base(directory, openMode)
            {
            }

            protected override IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
            {
                IndexWriterConfig conf = base.CreateIndexWriterConfig(openMode);
                conf.IndexDeletionPolicy = DeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
                return conf;
            }

            protected override IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
            {
                return IndexWriter = base.OpenIndexWriter(directory, config);
            }
        }

        public const string INDEX_SOURCE = "index";
        public const string TAXONOMY_SOURCE = "taxonomy";

        private readonly IndexWriter indexWriter;
        private readonly SnapshotDirectoryTaxonomyWriter taxonomyWriter;
        private readonly IndexCommit indexCommit, taxonomyCommit;
        private readonly SnapshotDeletionPolicy indexSdp, taxonomySdp;

        /// <summary>
        /// Returns a map of the revision files from the given <see cref="IndexCommit"/>s of the search and taxonomy indexes.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public static IDictionary<string, IList<RevisionFile>> RevisionFiles(IndexCommit indexCommit, IndexCommit taxonomyCommit)
        {
            return new Dictionary<string, IList<RevisionFile>>{
                    { INDEX_SOURCE,  IndexRevision.RevisionFiles(indexCommit).Values.First() },
                    { TAXONOMY_SOURCE,  IndexRevision.RevisionFiles(taxonomyCommit).Values.First() }
                };
        }

        /// <summary>
        /// Returns a <see cref="string"/> representation of a revision's version from the given
        /// <see cref="IndexCommit"/>s of the search and taxonomy indexes.
        /// </summary>
        /// <param name="commit"></param>
        /// <returns>a <see cref="string"/> representation of a revision's version from the given <see cref="IndexCommit"/>s of the search and taxonomy indexes.</returns>
        public static string RevisionVersion(IndexCommit indexCommit, IndexCommit taxonomyCommit)
        {
            return string.Format("{0:X}:{1:X}", indexCommit.Generation, taxonomyCommit.Generation);
        }

        /// <summary>
        /// Constructor over the given <see cref="IndexWriter"/>. Uses the last
        /// <see cref="IndexCommit"/> found in the <see cref="Directory"/> managed by the given
        /// writer.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public IndexAndTaxonomyRevision(IndexWriter indexWriter, SnapshotDirectoryTaxonomyWriter taxonomyWriter)
        {
            this.indexSdp = indexWriter.Config.IndexDeletionPolicy as SnapshotDeletionPolicy;
            if (indexSdp == null)
                throw new ArgumentException("IndexWriter must be created with SnapshotDeletionPolicy", "indexWriter");

            this.indexWriter = indexWriter;
            this.taxonomyWriter = taxonomyWriter;
            this.taxonomySdp = taxonomyWriter.DeletionPolicy;
            this.indexCommit = indexSdp.Snapshot();
            this.taxonomyCommit = taxonomySdp.Snapshot();
            this.Version = RevisionVersion(indexCommit, taxonomyCommit);
            this.SourceFiles = RevisionFiles(indexCommit, taxonomyCommit);
        }

        /// <summary>
        /// Compares this <see cref="IndexAndTaxonomyRevision"/> to the given <see cref="version"/>.
        /// </summary>
        public int CompareTo(string version)
        {
            string[] parts = version.Split(':');
            long indexGen = long.Parse(parts[0], NumberStyles.HexNumber);
            long taxonomyGen = long.Parse(parts[1], NumberStyles.HexNumber);
            long indexCommitGen = indexCommit.Generation;
            long taxonomyCommitGen = taxonomyCommit.Generation;

            if (indexCommitGen < indexGen)
                return -1;

            if (indexCommitGen > indexGen)
                return 1;

            return taxonomyCommitGen < taxonomyGen ? -1 : (taxonomyCommitGen > taxonomyGen ? 1 : 0);
        }

        public int CompareTo(IRevision other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            IndexAndTaxonomyRevision itr = other as IndexAndTaxonomyRevision;
            if(itr == null)
                throw new ArgumentException(string.Format("Cannot compare IndexAndTaxonomyRevision to a {0}", other.GetType()), "other");

            int cmp = indexCommit.CompareTo(itr.indexCommit);
            return cmp != 0 ? cmp : taxonomyCommit.CompareTo(itr.taxonomyCommit);
        }

        public string Version { get; private set; }

        public IDictionary<string, IList<RevisionFile>> SourceFiles { get; private set; }

        /// <exception cref="IOException"></exception>
        public Stream Open(string source, string fileName)
        {
            Debug.Assert(source.Equals(INDEX_SOURCE) || source.Equals(TAXONOMY_SOURCE), string.Format("invalid source; expected=({0} or {1}) got={2}", INDEX_SOURCE, TAXONOMY_SOURCE, source));
            IndexCommit commit = source.Equals(INDEX_SOURCE) ? indexCommit : taxonomyCommit;
            return new IndexInputStream(commit.Directory.OpenInput(fileName, IOContext.READ_ONCE));
        }

        /// <exception cref="IOException"></exception>
        public void Release()
        {
            try
            {
                indexSdp.Release(indexCommit);
            }
            finally 
            {
                taxonomySdp.Release(taxonomyCommit);
            }

            try
            {
                indexWriter.DeleteUnusedFiles();
            }
            finally
            {
                taxonomyWriter.IndexWriter.DeleteUnusedFiles();
            }
        }
    }
}