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
    /// segment as the total byte size of the segment's files.
    /// </summary>
    public class LogByteSizeMergePolicy : LogMergePolicy
    {
        /// <summary>Default minimum segment size. </summary>
        /// <seealso cref="MinMergeMB"/>
        public static readonly double DEFAULT_MIN_MERGE_MB = 1.6;

        /// <summary>
        /// Default maximum segment size.  A segment of this size 
        /// or larger will never be merged. </summary> 
        /// <seealso cref="MaxMergeMB"/>
        public static readonly double DEFAULT_MAX_MERGE_MB = 2048;

        /// <summary>
        /// Default maximum segment size.  A segment of this size 
        /// or larger will never be merged during <see cref="IndexWriter.ForceMerge(int)"/>.  </summary>
        /// <seealso cref="MaxMergeMBForForcedMerge"/>
        public static readonly double DEFAULT_MAX_MERGE_MB_FOR_FORCED_MERGE = long.MaxValue;

        /// <summary>
        /// Sole constructor, setting all settings to their
        /// defaults.
        /// </summary>
        public LogByteSizeMergePolicy()
        {
            m_minMergeSize = (long)(DEFAULT_MIN_MERGE_MB * 1024 * 1024);
            m_maxMergeSize = (long)(DEFAULT_MAX_MERGE_MB * 1024 * 1024);
            
            // .Net port, original line is inappropriate, overflows in .NET 
            // and the property gets set to a negative value.
            // In Java however such statements results in long.MaxValue

            //MaxMergeSizeForForcedMerge = (long)(DEFAULT_MAX_MERGE_MB_FOR_FORCED_MERGE * 1024 * 1024);
            m_maxMergeSizeForForcedMerge = long.MaxValue;
        }

        protected override long Size(SegmentCommitInfo info)
        {
            return SizeBytes(info);
        }

        /// <summary>
        /// <para>Determines the largest segment (measured by total
        /// byte size of the segment's files, in MB) that may be
        /// merged with other segments.  Small values (e.g., less
        /// than 50 MB) are best for interactive indexing, as this
        /// limits the length of pauses while indexing to a few
        /// seconds.  Larger values are best for batched indexing
        /// and speedier searches.</para>
        ///
        /// <para>Note that <see cref="LogMergePolicy.MaxMergeDocs"/> is also
        /// used to check whether a segment is too large for
        /// merging (it's either or).</para>
        /// </summary>
        public virtual double MaxMergeMB
        {
            get => ((double)m_maxMergeSize) / 1024 / 1024;
            set
            {
                m_maxMergeSize = (long)(value * 1024 * 1024);
                if (m_maxMergeSize < 0)
                {
                    m_maxMergeSize = long.MaxValue;
                }
            }
        }

        /// <summary>
        /// Determines the largest segment (measured by total
        /// byte size of the segment's files, in MB) that may be
        /// merged with other segments during forceMerge. Setting
        /// it low will leave the index with more than 1 segment,
        /// even if <see cref="IndexWriter.ForceMerge(int)"/> is called.
        /// </summary>
        public virtual double MaxMergeMBForForcedMerge
        {
            get => ((double)m_maxMergeSizeForForcedMerge) / 1024 / 1024;
            set
            {
                m_maxMergeSizeForForcedMerge = (long)(value * 1024 * 1024);
                if (m_maxMergeSizeForForcedMerge < 0)
                {
                    m_maxMergeSizeForForcedMerge = long.MaxValue;
                }
            }
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
        public virtual double MinMergeMB
        {
            get => ((double)m_minMergeSize) / 1024 / 1024;
            set
            {
                m_minMergeSize = (long)(value * 1024 * 1024);
                if (m_minMergeSize < 0)
                {
                    m_minMergeSize = long.MaxValue;
                }
            }
        }
    }
}