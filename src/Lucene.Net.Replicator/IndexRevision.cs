using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;

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
    /// A <see cref="IRevision"/> of a single index files which comprises the list of files
    /// that are part of the current <see cref="IndexCommit"/>. To ensure the files are not
    /// deleted by <see cref="IndexWriter"/> for as long as this revision stays alive (i.e.
    /// until <see cref="Release"/>, the current commit point is snapshotted, using
    /// <see cref="SnapshotDeletionPolicy"/> (this means that the given writer's
    /// <see cref="IndexWriterConfig.IndexDeletionPolicy"/> should return
    /// <see cref="SnapshotDeletionPolicy"/>).
    /// <para/>
    /// When this revision is <see cref="Release()"/>d, it releases the obtained
    /// snapshot as well as calls <see cref="IndexWriter.DeleteUnusedFiles"/> so that the
    /// snapshotted files are deleted (if they are no longer needed).
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class IndexRevision : IRevision
    {
        private const string SOURCE = "index";

        private readonly IndexWriter writer;
        private readonly IndexCommit commit;
        private readonly SnapshotDeletionPolicy sdp;
        private readonly string version;
        private readonly IDictionary<string, IList<RevisionFile>> sourceFiles;

        // returns a RevisionFile with some metadata
        private static RevisionFile CreateRevisionFile(string fileName, Directory directory)
        {
            return new RevisionFile(fileName, directory.FileLength(fileName));
        }

        /// <summary>
        /// Returns a singleton map of the revision files from the given <see cref="IndexCommit"/>.
        /// </summary>
        public static IDictionary<string, IList<RevisionFile>> RevisionFiles(IndexCommit commit)
        {
            ICollection<string> commitFiles = commit.FileNames;
            IList<RevisionFile> revisionFiles = new JCG.List<RevisionFile>(commitFiles.Count);
            string segmentsFile = commit.SegmentsFileName;
            Directory dir = commit.Directory;

            foreach (string file in commitFiles)
            {
                if (!file.Equals(segmentsFile, StringComparison.Ordinal))
                {
                    revisionFiles.Add(CreateRevisionFile(file, dir));
                }
            }
            revisionFiles.Add(CreateRevisionFile(segmentsFile, dir)); // segments_N must be last

            return new Dictionary<string, IList<RevisionFile>>
            {
                { SOURCE, revisionFiles }
            }.AsReadOnly();
        }
   
        /// <summary>
        /// Returns a string representation of a revision's version from the given 
        /// <see cref="IndexCommit"/>
        /// </summary>
        public static string RevisionVersion(IndexCommit commit)
        {
            return commit.Generation.ToString("X");
        }

        /// <summary>
        /// Constructor over the given <see cref="IndexWriter"/>. Uses the last
        /// <see cref="IndexCommit"/> found in the <see cref="Directory"/> managed by the given
        /// writer.
        /// </summary>
        public IndexRevision(IndexWriter writer)
        {
            sdp = writer.Config.IndexDeletionPolicy as SnapshotDeletionPolicy;
            if (sdp is null)
                throw new ArgumentException("IndexWriter must be created with SnapshotDeletionPolicy", nameof(writer));

            this.writer = writer;
            this.commit = sdp.Snapshot();
            this.version = RevisionVersion(commit);
            this.sourceFiles = RevisionFiles(commit);
        }

        public virtual int CompareTo(string version)
        {
            long gen = long.Parse(version, NumberStyles.HexNumber);
            long commitGen = commit.Generation;
            //TODO: long.CompareTo(); but which goes where.
            return commitGen < gen ? -1 : (commitGen > gen ? 1 : 0);
        }

        public virtual int CompareTo(IRevision other)
        {
            //TODO: This breaks the contract and will fail if called with a different implementation
            //      This is a flaw inherited from the original source...
            //      It should at least provide a better description to the InvalidCastException
            IndexRevision or = (IndexRevision)other;
            return commit.CompareTo(or.commit);
        }

        public virtual string Version => version;

        public virtual IDictionary<string, IList<RevisionFile>> SourceFiles => sourceFiles;

        public virtual Stream Open(string source, string fileName)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(source.Equals(SOURCE, StringComparison.Ordinal), "invalid source; expected={0} got={1}", SOURCE, source);
            return new IndexInputStream(commit.Directory.OpenInput(fileName, IOContext.READ_ONCE));
        }

        public virtual void Release()
        {
            sdp.Release(commit);
            writer.DeleteUnusedFiles();
        }

        public override string ToString()
        {
            return "IndexRevision version=" + Version + " files=" + SourceFiles;
        }
    }
}