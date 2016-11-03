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
    /// <p>Expert: policy for deletion of stale <seealso cref="IndexCommit index commits"/>.
    ///
    /// <p>Implement this interface, and pass it to one
    /// of the <seealso cref="IndexWriter"/> or <seealso cref="IndexReader"/>
    /// constructors, to customize when older
    /// <seealso cref="IndexCommit point-in-time commits"/>
    /// are deleted from the index directory.  The default deletion policy
    /// is <seealso cref="KeepOnlyLastCommitDeletionPolicy"/>, which always
    /// removes old commits as soon as a new commit is done (this
    /// matches the behavior before 2.2).</p>
    ///
    /// <p>One expected use case for this (and the reason why it
    /// was first created) is to work around problems with an
    /// index directory accessed via filesystems like NFS because
    /// NFS does not provide the "delete on last close" semantics
    /// that Lucene's "point in time" search normally relies on.
    /// By implementing a custom deletion policy, such as "a
    /// commit is only removed once it has been stale for more
    /// than X minutes", you can give your readers time to
    /// refresh to the new commit before <seealso cref="IndexWriter"/>
    /// removes the old commits.  Note that doing so will
    /// increase the storage requirements of the index.  See <a
    /// target="top"
    /// href="http://issues.apache.org/jira/browse/LUCENE-710">LUCENE-710</a>
    /// for details.</p>
    ///
    /// <p>Implementers of sub-classes should make sure that <seealso cref="#clone()"/>
    /// returns an independent instance able to work with any other <seealso cref="IndexWriter"/>
    /// or <seealso cref="Directory"/> instance.</p>
    /// </summary>

    public abstract class IndexDeletionPolicy
    {
        /// <summary>
        /// Sole constructor, typically called by sub-classes constructors. </summary>
        protected internal IndexDeletionPolicy()
        {
        }

        /// <summary>
        /// <p>this is called once when a writer is first
        /// instantiated to give the policy a chance to remove old
        /// commit points.</p>
        ///
        /// <p>The writer locates all index commits present in the
        /// index directory and calls this method.  The policy may
        /// choose to delete some of the commit points, doing so by
        /// calling method <seealso cref="IndexCommit#delete delete()"/>
        /// of <seealso cref="IndexCommit"/>.</p>
        ///
        /// <p><u>Note:</u> the last CommitPoint is the most recent one,
        /// i.e. the "front index state". Be careful not to delete it,
        /// unless you know for sure what you are doing, and unless
        /// you can afford to lose the index content while doing that.
        /// </summary>
        /// <param name="commits"> List of current
        /// <seealso cref="IndexCommit point-in-time commits"/>,
        ///  sorted by age (the 0th one is the oldest commit).
        ///  Note that for a new index this method is invoked with
        ///  an empty list. </param>
        public abstract void OnInit<T>(IList<T> commits) where T : IndexCommit;

        /// <summary>
        /// <p>this is called each time the writer completed a commit.
        /// this gives the policy a chance to remove old commit points
        /// with each commit.</p>
        ///
        /// <p>The policy may now choose to delete old commit points
        /// by calling method <seealso cref="IndexCommit#delete delete()"/>
        /// of <seealso cref="IndexCommit"/>.</p>
        ///
        /// <p>this method is only called when {@link
        /// IndexWriter#commit} or <seealso cref="IndexWriter#close"/> is
        /// called, or possibly not at all if the {@link
        /// IndexWriter#rollback} is called.
        ///
        /// <p><u>Note:</u> the last CommitPoint is the most recent one,
        /// i.e. the "front index state". Be careful not to delete it,
        /// unless you know for sure what you are doing, and unless
        /// you can afford to lose the index content while doing that.
        /// </summary>
        /// <param name="commits"> List of <seealso cref="IndexCommit"/>,
        ///  sorted by age (the 0th one is the oldest commit). </param>
        public abstract void OnCommit<T>(IList<T> commits) where T : IndexCommit;

        public virtual object Clone()
        {
            return (IndexDeletionPolicy)base.MemberwiseClone();
        }
    }
}