using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Facet.Taxonomy.WriterCache;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
        // LUCENENET specific - de-nested SnapshotDirectoryTaxonomyWriter and rewrote it as
        // SnapshotDirectoryTaxonomyIndexWriterFactory

        public const string INDEX_SOURCE = "index";
        public const string TAXONOMY_SOURCE = "taxonomy";

        private readonly IndexWriter indexWriter;
        // LUCENENET specific, storing the taxonomyWriterFactory that creates writer and policies
        private readonly SnapshotDirectoryTaxonomyIndexWriterFactory taxonomyWriterFactory;
        private readonly IndexCommit indexCommit, taxonomyCommit;
        private readonly SnapshotDeletionPolicy indexSdp, taxonomySdp;
        private readonly string version;
        private readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

        /// <summary>
        /// Returns a map of the revision files from the given <see cref="IndexCommit"/>s of the search and taxonomy indexes.
        /// </summary>
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
        /// <exception cref="InvalidOperationException">If this index does not have any commits yet.</exception>
        /// <exception cref="ArgumentException">If the <see cref="IndexWriterConfig.IndexDeletionPolicy"/> is not a <see cref="SnapshotDeletionPolicy"/>.</exception>
        public IndexAndTaxonomyRevision(IndexWriter indexWriter, SnapshotDirectoryTaxonomyIndexWriterFactory taxonomyWriterFactory)
        {
            this.indexSdp = indexWriter.Config.IndexDeletionPolicy as SnapshotDeletionPolicy;
            if (indexSdp is null)
                throw new ArgumentException("IndexWriter must be created with SnapshotDeletionPolicy", nameof(indexWriter));

            if (taxonomyWriterFactory.IndexWriter is null)
                throw new ArgumentException("TaxonomyWriter must be created with SnapshotDirectoryTaxonomyIndexWriterFactory", nameof(taxonomyWriterFactory));

            this.indexWriter = indexWriter;
            this.taxonomyWriterFactory = taxonomyWriterFactory;
            this.taxonomySdp = taxonomyWriterFactory.DeletionPolicy;
            this.indexCommit = indexSdp.Snapshot();
            this.taxonomyCommit = taxonomySdp.Snapshot();
            this.version = RevisionVersion(indexCommit, taxonomyCommit);
            this.sourceFiles = RevisionFiles(indexCommit, taxonomyCommit);
        }

        /// <summary>
        /// Compares this <see cref="IndexAndTaxonomyRevision"/> to the given <see cref="version"/>.
        /// </summary>
        public virtual int CompareTo(string version)
        {
            string[] parts = version.Split(':').TrimEnd();
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

        public virtual int CompareTo(IRevision other)
        {
            if (other is null)
                throw new ArgumentNullException(nameof(other));

            if (!(other is IndexAndTaxonomyRevision itr))
                throw new ArgumentException($"Cannot compare IndexAndTaxonomyRevision to a {other.GetType()}", nameof(other));

            int cmp = indexCommit.CompareTo(itr.indexCommit);
            return cmp != 0 ? cmp : taxonomyCommit.CompareTo(itr.taxonomyCommit);
        }

        public virtual string Version => version;

        public virtual IDictionary<string, IList<RevisionFile>> SourceFiles => sourceFiles;

        /// <exception cref="IOException">An IO exception occurred.</exception>
        public virtual Stream Open(string source, string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(source.Equals(INDEX_SOURCE, StringComparison.Ordinal) || source.Equals(TAXONOMY_SOURCE, StringComparison.Ordinal), "invalid source; expected=({0} or {1}) got={2}", INDEX_SOURCE, TAXONOMY_SOURCE, source);
            IndexCommit commit = source.Equals(INDEX_SOURCE, StringComparison.Ordinal) ? indexCommit : taxonomyCommit;
            return new IndexInputStream(commit.Directory.OpenInput(fileName, IOContext.READ_ONCE));
        }

        /// <exception cref="IOException">An IO exception occurred.</exception>
        public virtual void Release()
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
                taxonomyWriterFactory.IndexWriter.DeleteUnusedFiles();
            }
        }
    }
}