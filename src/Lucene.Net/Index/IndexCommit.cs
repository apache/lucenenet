using System;
using System.Collections.Generic;

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

    /// <summary>
    /// <para>Expert: represents a single commit into an index as seen by the
    /// <see cref="IndexDeletionPolicy"/> or <see cref="IndexReader"/>.</para>
    ///
    /// <para> Changes to the content of an index are made visible
    /// only after the writer who made that change commits by
    /// writing a new segments file
    /// (<c>segments_N</c>). This point in time, when the
    /// action of writing of a new segments file to the directory
    /// is completed, is an index commit.</para>
    ///
    /// <para>Each index commit point has a unique segments file
    /// associated with it. The segments file associated with a
    /// later index commit point would have a larger N.</para>
    ///
    /// @lucene.experimental
    /// </summary>

    public abstract class IndexCommit : IComparable<IndexCommit>
    {
        /// <summary>
        /// Get the segments file (<c>segments_N</c>) associated
        /// with this commit point.
        /// </summary>
        public abstract string SegmentsFileName { get; }

        /// <summary>
        /// Returns all index files referenced by this commit point.
        /// </summary>
        public abstract ICollection<string> FileNames { get; }

        /// <summary>
        /// Returns the <see cref="Store.Directory"/> for the index.
        /// </summary>
        public abstract Directory Directory { get; }

        /// <summary>
        /// Delete this commit point.  This only applies when using
        /// the commit point in the context of <see cref="IndexWriter"/>'s
        /// <see cref="IndexDeletionPolicy"/>.
        /// <para/>
        /// Upon calling this, the writer is notified that this commit
        /// point should be deleted.
        /// <para/>
        /// Decision that a commit-point should be deleted is taken by the <see cref="IndexDeletionPolicy"/> in effect
        /// and therefore this should only be called by its <see cref="IndexDeletionPolicy.OnInit{T}(IList{T})"/> or
        /// <see cref="IndexDeletionPolicy.OnCommit{T}(IList{T})"/> methods.
        /// </summary>
        public abstract void Delete();

        /// <summary>
        /// Returns <c>true</c> if this commit should be deleted; this is
        /// only used by <see cref="IndexWriter"/> after invoking the
        /// <see cref="IndexDeletionPolicy"/>.
        /// </summary>
        public abstract bool IsDeleted { get; }

        /// <summary>
        /// Returns number of segments referenced by this commit. </summary>
        public abstract int SegmentCount { get; }

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected IndexCommit()
        {
        }

        /// <summary>
        /// Two IndexCommits are equal if both their <see cref="Store.Directory"/> and versions are equal. </summary>
        public override bool Equals(object other)
        {
            if (other is IndexCommit otherCommit)
            {
                return otherCommit.Directory == Directory && otherCommit.Generation == Generation;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Directory.GetHashCode() + Generation.GetHashCode();
        }

        /// <summary>
        /// Returns the generation (the _N in segments_N) for this
        /// <see cref="IndexCommit"/>
        /// </summary>
        public abstract long Generation { get; }

        /// <summary>
        /// Returns userData, previously passed to 
        /// <see cref="IndexWriter.SetCommitData(IDictionary{string, string})"/>} for this commit.  
        /// The dictionary is <see cref="string"/> -> <see cref="string"/>.
        /// </summary>
        public abstract IDictionary<string, string> UserData { get; }

        public virtual int CompareTo(IndexCommit commit)
        {
            if (Directory != commit.Directory)
            {
                throw UnsupportedOperationException.Create("cannot compare IndexCommits from different Directory instances");
            }

            long gen = Generation;
            long comgen = commit.Generation;
            if (gen < comgen)
            {
                return -1;
            }
            else if (gen > comgen)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }
}