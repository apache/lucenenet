/**
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

namespace Lucene.Net.Index
{
    /// <summary>
    /// Deprecated.  Please subclass Indexcommit class instead.
    /// </summary>
    public abstract class IndexCommit : IndexCommitPoint
    {

        /**
         * Get the segments file (<code>segments_N</code>) associated 
         * with this commit point.
         */
        public abstract string GetSegmentsFileName();

        /**
         * Returns all index files referenced by this commit point.
         */
        public abstract System.Collections.Generic.ICollection<string> GetFileNames();

        /**
         * Returns the {@link Directory} for the index.
         */
        public abstract Directory GetDirectory();

        /**
         * Delete this commit point.  This only applies when using
         * the commit point in the context of IndexWriter's
         * IndexDeletionPolicy.
         * <p>
         * Upon calling this, the writer is notified that this commit 
         * point should be deleted. 
         * <p>
         * Decision that a commit-point should be deleted is taken by the {@link IndexDeletionPolicy} in effect
         * and therefore this should only be called by its {@link IndexDeletionPolicy#onInit onInit()} or 
         * {@link IndexDeletionPolicy#onCommit onCommit()} methods.
        */
        public virtual void Delete()
        {
            throw new System.Exception("This IndexCommit does not support this method.");
        }

        public virtual bool IsDeleted()
        {
            throw new System.Exception("This IndexCommit does not support this method.");
        }

        /**
         * Returns true if this commit is an optimized index.
         */
        public virtual bool IsOptimized()
        {
            throw new System.Exception("This IndexCommit does not support this method.");
        }

        /**
         * Two IndexCommits are equal if both their Directory and versions are equal.
         */
        public override bool Equals(object other)
        {
            if (other is IndexCommit)
            {
                IndexCommit otherCommit = (IndexCommit)other;
                return otherCommit.GetDirectory().Equals(GetDirectory()) && otherCommit.GetVersion() == GetVersion();
            }
            else
                return false;
        }

        public override int GetHashCode()
        {
            return GetDirectory().GetHashCode() + GetSegmentsFileName().GetHashCode();
        }

        /** Returns the version for this IndexCommit.  This is the
            same value that {@link IndexReader#getVersion} would
            return if it were opened on this commit. */
        public virtual long GetVersion()
        {
            throw new System.Exception("This IndexCommit does not support this method.");
        }

        /** Returns the generation (the _N in segments_N) for this
            IndexCommit */
        public virtual long GetGeneration()
        {
            throw new System.Exception("This IndexCommit does not support this method.");
        }

        /** Convenience method that returns the last modified time
         *  of the segments_N file corresponding to this index
         *  commit, equivalent to
         *  getDirectory().fileModified(getSegmentsFileName()). */
        public long GetTimestamp()
        {
            return GetDirectory().FileModified(GetSegmentsFileName());
        }
    }
}
