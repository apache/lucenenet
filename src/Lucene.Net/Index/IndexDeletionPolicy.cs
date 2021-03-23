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

    /// <summary>
    /// <para>Expert: policy for deletion of stale <see cref="IndexCommit"/>s.</para>
    ///
    /// <para>Implement this interface, and pass it to one
    /// of the <see cref="IndexWriter"/> or <see cref="IndexReader"/>
    /// constructors, to customize when older
    /// point-in-time commits (<see cref="IndexCommit"/>)
    /// are deleted from the index directory.  The default deletion policy
    /// is <see cref="KeepOnlyLastCommitDeletionPolicy"/>, which always
    /// removes old commits as soon as a new commit is done (this
    /// matches the behavior before 2.2).</para>
    ///
    /// <para>One expected use case for this (and the reason why it
    /// was first created) is to work around problems with an
    /// index directory accessed via filesystems like NFS because
    /// NFS does not provide the "delete on last close" semantics
    /// that Lucene's "point in time" search normally relies on.
    /// By implementing a custom deletion policy, such as "a
    /// commit is only removed once it has been stale for more
    /// than X minutes", you can give your readers time to
    /// refresh to the new commit before <see cref="IndexWriter"/>
    /// removes the old commits.  Note that doing so will
    /// increase the storage requirements of the index.  See <a
    /// target="top"
    /// href="http://issues.apache.org/jira/browse/LUCENE-710">LUCENE-710</a>
    /// for details.</para>
    ///
    /// <para>Implementers of sub-classes should make sure that <see cref="Clone()"/>
    /// returns an independent instance able to work with any other <see cref="IndexWriter"/>
    /// or <see cref="Store.Directory"/> instance.</para>
    /// </summary>

    public abstract class IndexDeletionPolicy // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// Sole constructor, typically called by sub-classes constructors. </summary>
        protected IndexDeletionPolicy()
        {
        }

        /// <summary>
        /// <para>this is called once when a writer is first
        /// instantiated to give the policy a chance to remove old
        /// commit points.</para>
        ///
        /// <para>The writer locates all index commits present in the
        /// index directory and calls this method.  The policy may
        /// choose to delete some of the commit points, doing so by
        /// calling method <see cref="IndexCommit.Delete()"/>.</para>
        ///
        /// <para><u>Note:</u> the last CommitPoint is the most recent one,
        /// i.e. the "front index state". Be careful not to delete it,
        /// unless you know for sure what you are doing, and unless
        /// you can afford to lose the index content while doing that.</para>
        /// </summary>
        /// <param name="commits"> List of current point-in-time commits
        /// (<see cref="IndexCommit"/>),
        /// sorted by age (the 0th one is the oldest commit).
        /// Note that for a new index this method is invoked with
        /// an empty list. </param>
        public abstract void OnInit<T>(IList<T> commits) where T : IndexCommit;

        /// <summary>
        /// <para>this is called each time the writer completed a commit.
        /// this gives the policy a chance to remove old commit points
        /// with each commit.</para>
        ///
        /// <para>The policy may now choose to delete old commit points
        /// by calling method <see cref="IndexCommit.Delete()"/>
        /// of <see cref="IndexCommit"/>.</para>
        ///
        /// <para>This method is only called when
        /// <see cref="IndexWriter.Commit()"/>} or <see cref="IndexWriter.Dispose()"/> is
        /// called, or possibly not at all if the 
        /// <see cref="IndexWriter.Rollback()"/>} method is called.</para>
        ///
        /// <para><u>Note:</u> the last CommitPoint is the most recent one,
        /// i.e. the "front index state". Be careful not to delete it,
        /// unless you know for sure what you are doing, and unless
        /// you can afford to lose the index content while doing that.</para>
        /// </summary>
        /// <param name="commits"> List of <see cref="IndexCommit"/>s,
        /// sorted by age (the 0th one is the oldest commit). </param>
        public abstract void OnCommit<T>(IList<T> commits) where T : IndexCommit;

        public virtual object Clone()
        {
            return (IndexDeletionPolicy)base.MemberwiseClone();
        }
    }
}