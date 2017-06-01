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
    /// <see cref="MergeTrigger"/> is passed to
    /// <see cref="MergePolicy.FindMerges(MergeTrigger, SegmentInfos)"/> to indicate the
    /// event that triggered the merge.
    /// </summary>
    public enum MergeTrigger
    {
        /// <summary>
        /// Merge was triggered by a segment flush.
        /// </summary>
        SEGMENT_FLUSH,

        /// <summary>
        /// Merge was triggered by a full flush. Full flushes
        /// can be caused by a commit, NRT reader reopen or a <see cref="IndexWriter.Dispose()"/> call on the index writer.
        /// </summary>
        FULL_FLUSH,

        /// <summary>
        /// Merge has been triggered explicitly by the user.
        /// </summary>
        EXPLICIT,

        /// <summary>
        /// Merge was triggered by a successfully finished merge.
        /// </summary>
        MERGE_FINISHED,

        /// <summary>
        /// Merge was triggered by a disposing <see cref="IndexWriter"/>.
        /// </summary>
        CLOSING
    }
}