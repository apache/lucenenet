//STATUS: DRAFT - 4.8.0

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Lucene.Experimental
    /// </remarks>
    public class IndexAndTaxonomyRevision : IRevision
    {
        #region Java
        //JAVA: private final IndexWriter indexWriter;
        //JAVA: private final SnapshotDirectoryTaxonomyWriter taxoWriter;
        //JAVA: private final IndexCommit indexCommit, taxoCommit;
        //JAVA: private final SnapshotDeletionPolicy indexSDP, taxoSDP;
        //JAVA: private final String version;
        //JAVA: private final Map<String, List<RevisionFile>> sourceFiles;
        #endregion

        public const string INDEX_SOURCE = "index";
        public const string TAXONOMY_SOURCE = "taxonomy";

        private readonly IndexWriter indexWriter;
        private readonly SnapshotDirectoryTaxonomyWriter taxonomyWriter;
        private readonly IndexCommit indexCommit, taxonomyCommit;
        private readonly SnapshotDeletionPolicy indexSdp, taxonomySdp;

        public string Version { get; private set; }
        public IDictionary<string, IList<RevisionFile>> SourceFiles { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexWriter"></param>
        /// <param name="taxonomyWriter"></param>
        /// <exception cref="IOException"></exception>
        public IndexAndTaxonomyRevision(IndexWriter indexWriter, SnapshotDirectoryTaxonomyWriter taxonomyWriter)
        {
            #region Java
            //JAVA: /**
            //JAVA:  * Constructor over the given {@link IndexWriter}. Uses the last
            //JAVA:  * {@link IndexCommit} found in the {@link Directory} managed by the given
            //JAVA:  * writer.
            //JAVA:  */
            //JAVA: public IndexAndTaxonomyRevision(IndexWriter indexWriter, SnapshotDirectoryTaxonomyWriter taxoWriter)
            //JAVA:     throws IOException {
            //JAVA:   IndexDeletionPolicy delPolicy = indexWriter.getConfig().getIndexDeletionPolicy();
            //JAVA:   if (!(delPolicy instanceof SnapshotDeletionPolicy)) {
            //JAVA:     throw new IllegalArgumentException("IndexWriter must be created with SnapshotDeletionPolicy");
            //JAVA:   }
            //JAVA:   this.indexWriter = indexWriter;
            //JAVA:   this.taxoWriter = taxoWriter;
            //JAVA:   this.indexSDP = (SnapshotDeletionPolicy) delPolicy;
            //JAVA:   this.taxoSDP = taxoWriter.getDeletionPolicy();
            //JAVA:   this.indexCommit = indexSDP.snapshot();
            //JAVA:   this.taxoCommit = taxoSDP.snapshot();
            //JAVA:   this.version = revisionVersion(indexCommit, taxoCommit);
            //JAVA:   this.sourceFiles = revisionFiles(indexCommit, taxoCommit);
            //JAVA: }
            #endregion

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

        public int CompareTo(string version)
        {
            #region Java
            //JAVA: public int compareTo(String version) {
            //JAVA:   final String[] parts = version.split(":");
            //JAVA:   final long indexGen = Long.parseLong(parts[0], RADIX);
            //JAVA:   final long taxoGen = Long.parseLong(parts[1], RADIX);
            //JAVA:   final long indexCommitGen = indexCommit.getGeneration();
            //JAVA:   final long taxoCommitGen = taxoCommit.getGeneration();
            //JAVA:   
            //JAVA:   // if the index generation is not the same as this commit's generation,
            //JAVA:   // compare by it. Otherwise, compare by the taxonomy generation.
            //JAVA:   if (indexCommitGen < indexGen) {
            //JAVA:     return -1;
            //JAVA:   } else if (indexCommitGen > indexGen) {
            //JAVA:     return 1;
            //JAVA:   } else {
            //JAVA:     return taxoCommitGen < taxoGen ? -1 : (taxoCommitGen > taxoGen ? 1 : 0);
            //JAVA:   }
            //JAVA: }
            #endregion

            string[] parts = version.Split(':');
            long indexGen = long.Parse(parts[0], NumberStyles.HexNumber);
            long taxonomyGen = long.Parse(parts[1], NumberStyles.HexNumber);
            long indexCommitGen = indexCommit.Generation;
            long taxonomyCommitGen = taxonomyCommit.Generation;

            //TODO: long.CompareTo(); but which goes where.
            if (indexCommitGen < indexGen)
                return -1;

            if (indexCommitGen > indexGen)
                return 1;

            return taxonomyCommitGen < taxonomyGen ? -1 : (taxonomyCommitGen > taxonomyGen ? 1 : 0);
        }

        public int CompareTo(IRevision other)
        {
            #region Java
            //JAVA: public int compareTo(Revision o) {
            //JAVA:   IndexAndTaxonomyRevision other = (IndexAndTaxonomyRevision) o;
            //JAVA:   int cmp = indexCommit.compareTo(other.indexCommit);
            //JAVA:   return cmp != 0 ? cmp : taxoCommit.compareTo(other.taxoCommit);
            //JAVA: }
            #endregion

            //TODO: This breaks the contract and will fail if called with a different implementation
            //      This is a flaw inherited from the original source...
            //      It should at least provide a better description to the InvalidCastException
            IndexAndTaxonomyRevision or = (IndexAndTaxonomyRevision)other;
            int cmp = indexCommit.CompareTo(or.indexCommit);
            return cmp != 0 ? cmp : taxonomyCommit.CompareTo(or.taxonomyCommit);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public Stream Open(string source, string fileName)
        {
            #region Java
            //JAVA: public InputStream open(String source, String fileName) throws IOException {
            //JAVA:   assert source.equals(INDEX_SOURCE) || source.equals(TAXONOMY_SOURCE) : "invalid source; expected=(" + INDEX_SOURCE
            //JAVA:   + " or " + TAXONOMY_SOURCE + ") got=" + source;
            //JAVA:   IndexCommit ic = source.equals(INDEX_SOURCE) ? indexCommit : taxoCommit;
            //JAVA:   return new IndexInputStream(ic.getDirectory().openInput(fileName, IOContext.READONCE));
            //JAVA: }
            #endregion

            Debug.Assert(source.Equals(INDEX_SOURCE) || source.Equals(TAXONOMY_SOURCE), 
                string.Format("invalid source; expected=({0} or {1}) got={2}", INDEX_SOURCE, TAXONOMY_SOURCE, source));
            IndexCommit commit = source.Equals(INDEX_SOURCE) ? indexCommit : taxonomyCommit;
            return new IndexInputStream(commit.Directory.OpenInput(fileName, IOContext.READ_ONCE));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void Release()
        {
            #region Java
            //JAVA: public void release() throws IOException {
            //JAVA:   try {
            //JAVA:     indexSDP.release(indexCommit);
            //JAVA:   } finally {
            //JAVA:     taxoSDP.release(taxoCommit);
            //JAVA:   }
            //JAVA:   
            //JAVA:   try {
            //JAVA:     indexWriter.deleteUnusedFiles();
            //JAVA:   } finally {
            //JAVA:     taxoWriter.getIndexWriter().deleteUnusedFiles();
            //JAVA:   }
            //JAVA: }
            #endregion

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

        //.NET NOTE: Changed doc comment as the JAVA one seems to be a bit too much copy/paste
        /// <summary>
        /// Returns a map of the revision files from the given <see cref="IndexCommit"/>s of the search and taxonomy indexes.
        /// </summary>
        /// <param name="indexCommit"></param>
        /// <param name="taxonomyCommit"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public static IDictionary<string, IList<RevisionFile>> RevisionFiles(IndexCommit indexCommit, IndexCommit taxonomyCommit)
        {
            #region Java
            //JAVA: /** Returns a singleton map of the revision files from the given {@link IndexCommit}. */
            //JAVA: public static Map<String, List<RevisionFile>> revisionFiles(IndexCommit indexCommit, IndexCommit taxoCommit)
            //JAVA:     throws IOException {
            //JAVA:   HashMap<String,List<RevisionFile>> files = new HashMap<>();
            //JAVA:   files.put(INDEX_SOURCE, IndexRevision.revisionFiles(indexCommit).values().iterator().next());
            //JAVA:   files.put(TAXONOMY_SOURCE, IndexRevision.revisionFiles(taxoCommit).values().iterator().next());
            //JAVA:   return files;
            //JAVA: }
            #endregion

            return new Dictionary<string, IList<RevisionFile>>{
                    { INDEX_SOURCE,  IndexRevision.RevisionFiles(indexCommit).Values.First() },
                    { TAXONOMY_SOURCE,  IndexRevision.RevisionFiles(taxonomyCommit).Values.First() }
                };
        }

        /// <summary>
        /// Returns a String representation of a revision's version from the given
        /// <see cref="IndexCommit"/>s of the search and taxonomy indexes.
        /// </summary>
        /// <param name="commit"></param>
        /// <returns>a String representation of a revision's version from the given <see cref="IndexCommit"/>s of the search and taxonomy indexes.</returns>
        public static string RevisionVersion(IndexCommit indexCommit, IndexCommit taxonomyCommit)
        {
            #region Java
            //JAVA: public static String revisionVersion(IndexCommit indexCommit, IndexCommit taxoCommit) {
            //JAVA:   return Long.toString(indexCommit.getGeneration(), RADIX) + ":" + Long.toString(taxoCommit.getGeneration(), RADIX);
            //JAVA: }   
            #endregion

            return string.Format("{0:X}:{1:X}", indexCommit.Generation, taxonomyCommit.Generation);
        }

        /// <summary>
        /// A <seealso cref="DirectoryTaxonomyWriter"/> which sets the underlying
        /// <seealso cref="IndexWriter"/>'s <seealso cref="IndexDeletionPolicy"/> to
        /// <seealso cref="SnapshotDeletionPolicy"/>.
        /// </summary>
        public class SnapshotDirectoryTaxonomyWriter : DirectoryTaxonomyWriter
        {
            /// <summary>
            /// Gets the <see cref="SnapshotDeletionPolicy"/> used by the underlying <see cref="IndexWriter"/>.
            /// </summary>
            public SnapshotDeletionPolicy DeletionPolicy { get; private set; }
            /// <summary>
            /// Gets the <see cref="IndexWriter"/> used by this <see cref="DirectoryTaxonomyWriter"/>.
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

            /// <summary>
            /// 
            /// </summary>
            /// <param name="openMode"></param>
            /// <returns></returns>
            protected override IndexWriterConfig CreateIndexWriterConfig(OpenMode openMode)
            {
                IndexWriterConfig conf = base.CreateIndexWriterConfig(openMode);
                conf.IndexDeletionPolicy = DeletionPolicy = new SnapshotDeletionPolicy(conf.IndexDeletionPolicy);
                return conf;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="directory"></param>
            /// <param name="config"></param>
            /// <returns></returns>
            protected override IndexWriter OpenIndexWriter(Directory directory, IndexWriterConfig config)
            {
                return IndexWriter = base.OpenIndexWriter(directory, config);
            }
        }

    }
}