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
    /// This is a <see cref="LogMergePolicy"/> that measures size of a
    /// segment as the number of documents (not taking deletions
    /// into account).
    /// </summary>

    public class LogDocMergePolicy : LogMergePolicy
    {
        /// <summary> Default minimum segment size. </summary>
        /// <seealso cref="MinMergeDocs"/>
        public const int DEFAULT_MIN_MERGE_DOCS = 1000;

        /// <summary>
        /// Sole constructor, setting all settings to their
        /// defaults.
        /// </summary>
        public LogDocMergePolicy()
        {
            m_minMergeSize = DEFAULT_MIN_MERGE_DOCS;

            // maxMergeSize(ForForcedMerge) are never used by LogDocMergePolicy; set
            // it to Long.MAX_VALUE to disable it
            m_maxMergeSize = long.MaxValue;
            m_maxMergeSizeForForcedMerge = long.MaxValue;
        }

        protected override long Size(SegmentCommitInfo info)
        {
            return SizeDocs(info);
        }

        /// <summary>
        /// Sets the minimum size for the lowest level segments.
        /// Any segments below this size are considered to be on
        /// the same level (even if they vary drastically in size)
        /// and will be merged whenever there are mergeFactor of
        /// them.  This effectively truncates the "long tail" of
        /// small segments that would otherwise be created into a
        /// single level.  If you set this too large, it could
        /// greatly increase the merging cost during indexing (if
        /// you flush many small segments).
        /// </summary>
        public virtual int MinMergeDocs
        {
            get => (int)m_minMergeSize;
            set => m_minMergeSize = value;
        }
    }
}
