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
    /// this is a <seealso cref="LogMergePolicy"/> that measures size of a
    ///  segment as the total byte size of the segment's files.
    /// </summary>
    public class LogByteSizeMergePolicy : LogMergePolicy
    {
        /// Default minimum segment size.  <seealso cref= setMinMergeMB </seealso>
        public static readonly double DEFAULT_MIN_MERGE_MB = 1.6;

        /// <summary>
        /// Default maximum segment size.  A segment of this size </summary>
        ///  or larger will never be merged.  <seealso cref= setMaxMergeMB  </seealso>
        public static readonly double DEFAULT_MAX_MERGE_MB = 2048;

        /// <summary>
        /// Default maximum segment size.  A segment of this size </summary>
        ///  or larger will never be merged during forceMerge.  <seealso cref= setMaxMergeMBForForceMerge  </seealso>
        public static readonly double DEFAULT_MAX_MERGE_MB_FOR_FORCED_MERGE = long.MaxValue;

        /// <summary>
        /// Sole constructor, setting all settings to their
        ///  defaults.
        /// </summary>
        public LogByteSizeMergePolicy()
        {
            minMergeSize = (long)(DEFAULT_MIN_MERGE_MB * 1024 * 1024);
            maxMergeSize = (long)(DEFAULT_MAX_MERGE_MB * 1024 * 1024);
            
            // .Net port, original line is inappropriate, overflows in .NET 
            // and the property gets set to a negative value.
            // In Java however such statements results in long.MaxValue

            //MaxMergeSizeForForcedMerge = (long)(DEFAULT_MAX_MERGE_MB_FOR_FORCED_MERGE * 1024 * 1024);
            maxMergeSizeForForcedMerge = long.MaxValue;
        }

        protected override long Size(SegmentCommitInfo info)
        {
            return SizeBytes(info);
        }

        /// <summary>
        /// <p>Determines the largest segment (measured by total
        ///  byte size of the segment's files, in MB) that may be
        ///  merged with other segments.  Small values (e.g., less
        ///  than 50 MB) are best for interactive indexing, as this
        ///  limits the length of pauses while indexing to a few
        ///  seconds.  Larger values are best for batched indexing
        ///  and speedier searches.</p>
        ///
        ///  <p>Note that <seealso cref="#setMaxMergeDocs"/> is also
        ///  used to check whether a segment is too large for
        ///  merging (it's either or).</p>
        /// </summary>
        public virtual double MaxMergeMB
        {
            set
            {
                maxMergeSize = (long)(value * 1024 * 1024);
                if (maxMergeSize < 0)
                {
                    maxMergeSize = long.MaxValue;
                }
            }
            get
            {
                return ((double)maxMergeSize) / 1024 / 1024;
            }
        }

        /// <summary>
        /// <p>Determines the largest segment (measured by total
        ///  byte size of the segment's files, in MB) that may be
        ///  merged with other segments during forceMerge. Setting
        ///  it low will leave the index with more than 1 segment,
        ///  even if <seealso cref="IndexWriter#forceMerge"/> is called.
        /// </summary>
        public virtual double MaxMergeMBForForcedMerge
        {
            set
            {
                maxMergeSizeForForcedMerge = (long)(value * 1024 * 1024);
                if (maxMergeSizeForForcedMerge < 0)
                {
                    maxMergeSizeForForcedMerge = long.MaxValue;
                }
            }
            get
            {
                return ((double)maxMergeSizeForForcedMerge) / 1024 / 1024;
            }
        }

        /// <summary>
        /// Sets the minimum size for the lowest level segments.
        /// Any segments below this size are considered to be on
        /// the same level (even if they vary drastically in size)
        /// and will be merged whenever there are mergeFactor of
        /// them.  this effectively truncates the "long tail" of
        /// small segments that would otherwise be created into a
        /// single level.  If you set this too large, it could
        /// greatly increase the merging cost during indexing (if
        /// you flush many small segments).
        /// </summary>
        public virtual double MinMergeMB
        {
            set
            {
                minMergeSize = (long)(value * 1024 * 1024);
                if (minMergeSize < 0)
                {
                    minMergeSize = long.MaxValue;
                }
            }
            get
            {
                return ((double)minMergeSize) / 1024 / 1024;
            }
        }
    }
}