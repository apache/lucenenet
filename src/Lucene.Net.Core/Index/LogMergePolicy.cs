using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

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
    /// <p>this class implements a <seealso cref="MergePolicy"/> that tries
    /// to merge segments into levels of exponentially
    /// increasing size, where each level has fewer segments than
    /// the value of the merge factor. Whenever extra segments
    /// (beyond the merge factor upper bound) are encountered,
    /// all segments within the level are merged. You can get or
    /// set the merge factor using <seealso cref="#getMergeFactor()"/> and
    /// <seealso cref="#setMergeFactor(int)"/> respectively.</p>
    ///
    /// <p>this class is abstract and requires a subclass to
    /// define the <seealso cref="#size"/> method which specifies how a
    /// segment's size is determined.  <seealso cref="LogDocMergePolicy"/>
    /// is one subclass that measures size by document count in
    /// the segment.  <seealso cref="LogByteSizeMergePolicy"/> is another
    /// subclass that measures size as the total byte size of the
    /// file(s) for the segment.</p>
    /// </summary>

    public abstract class LogMergePolicy : MergePolicy
    {
        /// <summary>
        /// Defines the allowed range of log(size) for each
        ///  level.  A level is computed by taking the max segment
        ///  log size, minus LEVEL_LOG_SPAN, and finding all
        ///  segments falling within that range.
        /// </summary>
        public static readonly double LEVEL_LOG_SPAN = 0.75;

        /// <summary>
        /// Default merge factor, which is how many segments are
        ///  merged at a time
        /// </summary>
        public static readonly int DEFAULT_MERGE_FACTOR = 10;

        /// <summary>
        /// Default maximum segment size.  A segment of this size </summary>
        ///  or larger will never be merged.  <seealso cref= setMaxMergeDocs  </seealso>
        public static readonly int DEFAULT_MAX_MERGE_DOCS = int.MaxValue;

        /// <summary>
        /// Default noCFSRatio.  If a merge's size is >= 10% of
        ///  the index, then we disable compound file for it. </summary>
        ///  <seealso cref= MergePolicy#setNoCFSRatio  </seealso>
        public new static readonly double DEFAULT_NO_CFS_RATIO = 0.1;

        /// <summary>
        /// How many segments to merge at a time. </summary>
        protected int m_mergeFactor = DEFAULT_MERGE_FACTOR;

        /// <summary>
        /// Any segments whose size is smaller than this value
        ///  will be rounded up to this value.  this ensures that
        ///  tiny segments are aggressively merged.
        /// </summary>
        protected long m_minMergeSize;

        /// <summary>
        /// If the size of a segment exceeds this value then it
        ///  will never be merged.
        /// </summary>
        protected long m_maxMergeSize;

        // Although the core MPs set it explicitly, we must default in case someone
        // out there wrote his own LMP ...
        /// <summary>
        /// If the size of a segment exceeds this value then it
        /// will never be merged during <seealso cref="IndexWriter#forceMerge"/>.
        /// </summary>
        protected long m_maxMergeSizeForForcedMerge = long.MaxValue;

        /// <summary>
        /// If a segment has more than this many documents then it
        ///  will never be merged.
        /// </summary>
        protected int m_maxMergeDocs = DEFAULT_MAX_MERGE_DOCS;

        /// <summary>
        /// If true, we pro-rate a segment's size by the
        ///  percentage of non-deleted documents.
        /// </summary>
        protected bool m_calibrateSizeByDeletes = true;

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        public LogMergePolicy()
            : base(DEFAULT_NO_CFS_RATIO, MergePolicy.DEFAULT_MAX_CFS_SEGMENT_SIZE)
        {
        }

        /// <summary>
        /// Returns true if {@code LMP} is enabled in {@link
        ///  IndexWriter}'s {@code infoStream}.
        /// </summary>
        protected virtual bool Verbose() // LUCENENET TODO: Make property
        {
            IndexWriter w = m_writer.Get();
            return w != null && w.infoStream.IsEnabled("LMP");
        }

        /// <summary>
        /// Print a debug message to <seealso cref="IndexWriter"/>'s {@code
        ///  infoStream}.
        /// </summary>
        protected virtual void Message(string message)
        {
            if (Verbose())
            {
                m_writer.Get().infoStream.Message("LMP", message);
            }
        }

        /// <summary>
        /// Gets or Sets the number of segments that are merged at
        /// once and also controls the total number of segments
        /// allowed to accumulate in the index.
        /// <para/>
        /// This determines how often segment indices are merged by
        /// AddDocument().  With smaller values, less RAM is used
        /// while indexing, and searches are
        /// faster, but indexing speed is slower.  With larger
        /// values, more RAM is used during indexing, and while
        /// searches is slower, indexing is
        /// faster.  Thus larger values (> 10) are best for batch
        /// index creation, and smaller values (&lt; 10) for indices
        /// that are interactively maintained.
        /// </summary>
        public virtual int MergeFactor
        {
            get
            {
                return m_mergeFactor;
            }
            set
            {
                if (value < 2)
                {
                    throw new System.ArgumentException("mergeFactor cannot be less than 2");
                }
                this.m_mergeFactor = value;
            }
        }

        /// <summary>
        /// Gets or Sets whether the segment size should be calibrated by
        ///  the number of deletes when choosing segments for merge.
        /// </summary>
        public virtual bool CalibrateSizeByDeletes
        {
            set
            {
                this.m_calibrateSizeByDeletes = value;
            }
            get
            {
                return m_calibrateSizeByDeletes;
            }
        }

        public override void Dispose()
        {
        }

        /// <summary>
        /// Return the number of documents in the provided {@link
        ///  SegmentCommitInfo}, pro-rated by percentage of
        ///  non-deleted documents if {@link
        ///  #setCalibrateSizeByDeletes} is set.
        /// </summary>
        protected virtual long SizeDocs(SegmentCommitInfo info)
        {
            if (m_calibrateSizeByDeletes)
            {
                int delCount = m_writer.Get().NumDeletedDocs(info);
                Debug.Assert(delCount <= info.Info.DocCount);
                return (info.Info.DocCount - (long)delCount);
            }
            else
            {
                return info.Info.DocCount;
            }
        }

        /// <summary>
        /// Return the byte size of the provided {@link
        ///  SegmentCommitInfo}, pro-rated by percentage of
        ///  non-deleted documents if {@link
        ///  #setCalibrateSizeByDeletes} is set.
        /// </summary>
        protected virtual long SizeBytes(SegmentCommitInfo info)
        {
            if (m_calibrateSizeByDeletes)
            {
                return base.Size(info);
            }
            return info.SizeInBytes();
        }

        /// <summary>
        /// Returns true if the number of segments eligible for
        ///  merging is less than or equal to the specified {@code
        ///  maxNumSegments}.
        /// </summary>
        protected virtual bool IsMerged(SegmentInfos infos, int maxNumSegments, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
        {
            int numSegments = infos.Count;
            int numToMerge = 0;
            SegmentCommitInfo mergeInfo = null;
            bool segmentIsOriginal = false;
            for (int i = 0; i < numSegments && numToMerge <= maxNumSegments; i++)
            {
                SegmentCommitInfo info = infos.Info(i);
                bool? isOriginal;
                segmentsToMerge.TryGetValue(info, out isOriginal);
                if (isOriginal != null)
                {
                    segmentIsOriginal = isOriginal.Value;
                    numToMerge++;
                    mergeInfo = info;
                }
            }

            return numToMerge <= maxNumSegments && (numToMerge != 1 || !segmentIsOriginal || IsMerged(infos, mergeInfo));
        }

        /// <summary>
        /// Returns the merges necessary to merge the index, taking the max merge
        /// size or max merge docs into consideration. this method attempts to respect
        /// the {@code maxNumSegments} parameter, however it might be, due to size
        /// constraints, that more than that number of segments will remain in the
        /// index. Also, this method does not guarantee that exactly {@code
        /// maxNumSegments} will remain, but &lt;= that number.
        /// </summary>
        private MergeSpecification FindForcedMergesSizeLimit(SegmentInfos infos, int maxNumSegments, int last)
        {
            MergeSpecification spec = new MergeSpecification();
            IList<SegmentCommitInfo> segments = infos.AsList();

            int start = last - 1;
            while (start >= 0)
            {
                SegmentCommitInfo info = infos.Info(start);
                if (Size(info) > m_maxMergeSizeForForcedMerge || SizeDocs(info) > m_maxMergeDocs)
                {
                    if (Verbose())
                    {
                        Message("findForcedMergesSizeLimit: skip segment=" + info + ": size is > maxMergeSize (" + m_maxMergeSizeForForcedMerge + ") or sizeDocs is > maxMergeDocs (" + m_maxMergeDocs + ")");
                    }
                    // need to skip that segment + add a merge for the 'right' segments,
                    // unless there is only 1 which is merged.
                    if (last - start - 1 > 1 || (start != last - 1 && !IsMerged(infos, infos.Info(start + 1))))
                    {
                        // there is more than 1 segment to the right of
                        // this one, or a mergeable single segment.
                        spec.Add(new OneMerge(segments.SubList(start + 1, last)));
                    }
                    last = start;
                }
                else if (last - start == m_mergeFactor)
                {
                    // mergeFactor eligible segments were found, add them as a merge.
                    spec.Add(new OneMerge(segments.SubList(start, last)));
                    last = start;
                }
                --start;
            }

            // Add any left-over segments, unless there is just 1
            // already fully merged
            if (last > 0 && (++start + 1 < last || !IsMerged(infos, infos.Info(start))))
            {
                spec.Add(new OneMerge(segments.SubList(start, last)));
            }

            return spec.Merges.Count == 0 ? null : spec;
        }

        /// <summary>
        /// Returns the merges necessary to forceMerge the index. this method constraints
        /// the returned merges only by the {@code maxNumSegments} parameter, and
        /// guaranteed that exactly that number of segments will remain in the index.
        /// </summary>
        private MergeSpecification FindForcedMergesMaxNumSegments(SegmentInfos infos, int maxNumSegments, int last)
        {
            var spec = new MergeSpecification();
            var segments = infos.AsList();

            // First, enroll all "full" merges (size
            // mergeFactor) to potentially be run concurrently:
            while (last - maxNumSegments + 1 >= m_mergeFactor)
            {
                spec.Add(new OneMerge(segments.SubList(last - m_mergeFactor, last)));
                last -= m_mergeFactor;
            }

            // Only if there are no full merges pending do we
            // add a final partial (< mergeFactor segments) merge:
            if (0 == spec.Merges.Count)
            {
                if (maxNumSegments == 1)
                {
                    // Since we must merge down to 1 segment, the
                    // choice is simple:
                    if (last > 1 || !IsMerged(infos, infos.Info(0)))
                    {
                        spec.Add(new OneMerge(segments.SubList(0, last)));
                    }
                }
                else if (last > maxNumSegments)
                {
                    // Take care to pick a partial merge that is
                    // least cost, but does not make the index too
                    // lopsided.  If we always just picked the
                    // partial tail then we could produce a highly
                    // lopsided index over time:

                    // We must merge this many segments to leave
                    // maxNumSegments in the index (from when
                    // forceMerge was first kicked off):
                    int finalMergeSize = last - maxNumSegments + 1;

                    // Consider all possible starting points:
                    long bestSize = 0;
                    int bestStart = 0;

                    for (int i = 0; i < last - finalMergeSize + 1; i++)
                    {
                        long sumSize = 0;
                        for (int j = 0; j < finalMergeSize; j++)
                        {
                            sumSize += Size(infos.Info(j + i));
                        }
                        if (i == 0 || (sumSize < 2 * Size(infos.Info(i - 1)) && sumSize < bestSize))
                        {
                            bestStart = i;
                            bestSize = sumSize;
                        }
                    }

                    spec.Add(new OneMerge(segments.SubList(bestStart, bestStart + finalMergeSize)));
                }
            }
            return spec.Merges.Count == 0 ? null : spec;
        }

        /// <summary>
        /// Returns the merges necessary to merge the index down
        ///  to a specified number of segments.
        ///  this respects the <seealso cref="#maxMergeSizeForForcedMerge"/> setting.
        ///  By default, and assuming {@code maxNumSegments=1}, only
        ///  one segment will be left in the index, where that segment
        ///  has no deletions pending nor separate norms, and it is in
        ///  compound file format if the current useCompoundFile
        ///  setting is true.  this method returns multiple merges
        ///  (mergeFactor at a time) so the <seealso cref="MergeScheduler"/>
        ///  in use may make use of concurrency.
        /// </summary>
        public override MergeSpecification FindForcedMerges(SegmentInfos infos, int maxNumSegments, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
        {
            Debug.Assert(maxNumSegments > 0);
            if (Verbose())
            {
                Message("findForcedMerges: maxNumSegs=" + maxNumSegments + " segsToMerge=" + segmentsToMerge);
            }

            // If the segments are already merged (e.g. there's only 1 segment), or
            // there are <maxNumSegments:.
            if (IsMerged(infos, maxNumSegments, segmentsToMerge))
            {
                if (Verbose())
                {
                    Message("already merged; skip");
                }
                return null;
            }

            // Find the newest (rightmost) segment that needs to
            // be merged (other segments may have been flushed
            // since merging started):
            int last = infos.Count;
            while (last > 0)
            {
                SegmentCommitInfo info = infos.Info(--last);
                if (segmentsToMerge.ContainsKey(info))
                {
                    last++;
                    break;
                }
            }

            if (last == 0)
            {
                if (Verbose())
                {
                    Message("last == 0; skip");
                }
                return null;
            }

            // There is only one segment already, and it is merged
            if (maxNumSegments == 1 && last == 1 && IsMerged(infos, infos.Info(0)))
            {
                if (Verbose())
                {
                    Message("already 1 seg; skip");
                }
                return null;
            }

            // Check if there are any segments above the threshold
            bool anyTooLarge = false;
            for (int i = 0; i < last; i++)
            {
                SegmentCommitInfo info = infos.Info(i);
                if (Size(info) > m_maxMergeSizeForForcedMerge || SizeDocs(info) > m_maxMergeDocs)
                {
                    anyTooLarge = true;
                    break;
                }
            }

            if (anyTooLarge)
            {
                return FindForcedMergesSizeLimit(infos, maxNumSegments, last);
            }
            else
            {
                return FindForcedMergesMaxNumSegments(infos, maxNumSegments, last);
            }
        }

        /// <summary>
        /// Finds merges necessary to force-merge all deletes from the
        /// index.  We simply merge adjacent segments that have
        /// deletes, up to mergeFactor at a time.
        /// </summary>
        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            var segments = segmentInfos.AsList();
            int numSegments = segments.Count;

            if (Verbose())
            {
                Message("findForcedDeleteMerges: " + numSegments + " segments");
            }

            var spec = new MergeSpecification();
            int firstSegmentWithDeletions = -1;
            IndexWriter w = m_writer.Get();
            Debug.Assert(w != null);
            for (int i = 0; i < numSegments; i++)
            {
                SegmentCommitInfo info = segmentInfos.Info(i);
                int delCount = w.NumDeletedDocs(info);
                if (delCount > 0)
                {
                    if (Verbose())
                    {
                        Message("  segment " + info.Info.Name + " has deletions");
                    }
                    if (firstSegmentWithDeletions == -1)
                    {
                        firstSegmentWithDeletions = i;
                    }
                    else if (i - firstSegmentWithDeletions == m_mergeFactor)
                    {
                        // We've seen mergeFactor segments in a row with
                        // deletions, so force a merge now:
                        if (Verbose())
                        {
                            Message("  add merge " + firstSegmentWithDeletions + " to " + (i - 1) + " inclusive");
                        }
                        spec.Add(new OneMerge(segments.SubList(firstSegmentWithDeletions, i)));
                        firstSegmentWithDeletions = i;
                    }
                }
                else if (firstSegmentWithDeletions != -1)
                {
                    // End of a sequence of segments with deletions, so,
                    // merge those past segments even if it's fewer than
                    // mergeFactor segments
                    if (Verbose())
                    {
                        Message("  add merge " + firstSegmentWithDeletions + " to " + (i - 1) + " inclusive");
                    }
                    spec.Add(new OneMerge(segments.SubList(firstSegmentWithDeletions, i)));
                    firstSegmentWithDeletions = -1;
                }
            }

            if (firstSegmentWithDeletions != -1)
            {
                if (Verbose())
                {
                    Message("  add merge " + firstSegmentWithDeletions + " to " + (numSegments - 1) + " inclusive");
                }
                spec.Add(new OneMerge(segments.SubList(firstSegmentWithDeletions, numSegments)));
            }

            return spec;
        }

        private class SegmentInfoAndLevel : IComparable<SegmentInfoAndLevel>
        {
            internal readonly SegmentCommitInfo info;
            internal readonly float level;
            private int index;

            public SegmentInfoAndLevel(SegmentCommitInfo info, float level, int index)
            {
                this.info = info;
                this.level = level;
                this.index = index;
            }

            // Sorts largest to smallest
            public virtual int CompareTo(SegmentInfoAndLevel other)
            {
                return other.level.CompareTo(level);
            }
        }

        /// <summary>
        /// Checks if any merges are now necessary and returns a
        ///  <seealso cref="MergePolicy.MergeSpecification"/> if so.  A merge
        ///  is necessary when there are more than {@link
        ///  #setMergeFactor} segments at a given level.  When
        ///  multiple levels have too many segments, this method
        ///  will return multiple merges, allowing the {@link
        ///  MergeScheduler} to use concurrency.
        /// </summary>
        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos infos)
        {
            int numSegments = infos.Count;
            if (Verbose())
            {
                Message("findMerges: " + numSegments + " segments");
            }

            // Compute levels, which is just log (base mergeFactor)
            // of the size of each segment
            IList<SegmentInfoAndLevel> levels = new List<SegmentInfoAndLevel>();
            var norm = (float)Math.Log(m_mergeFactor);

            ICollection<SegmentCommitInfo> mergingSegments = m_writer.Get().MergingSegments;

            for (int i = 0; i < numSegments; i++)
            {
                SegmentCommitInfo info = infos.Info(i);
                long size = Size(info);

                // Floor tiny segments
                if (size < 1)
                {
                    size = 1;
                }

                SegmentInfoAndLevel infoLevel = new SegmentInfoAndLevel(info, (float)Math.Log(size) / norm, i);
                levels.Add(infoLevel);

                if (Verbose())
                {
                    long segBytes = SizeBytes(info);
                    string extra = mergingSegments.Contains(info) ? " [merging]" : "";
                    if (size >= m_maxMergeSize)
                    {
                        extra += " [skip: too large]";
                    }
                    Message("seg=" + m_writer.Get().SegString(info) + " level=" + infoLevel.level + " size=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00} MB", segBytes / 1024 / 1024.0) + extra);
                }
            }

            float levelFloor;
            if (m_minMergeSize <= 0)
            {
                levelFloor = (float)0.0;
            }
            else
            {
                levelFloor = (float)(Math.Log(m_minMergeSize) / norm);
            }

            // Now, we quantize the log values into levels.  The
            // first level is any segment whose log size is within
            // LEVEL_LOG_SPAN of the max size, or, who has such as
            // segment "to the right".  Then, we find the max of all
            // other segments and use that to define the next level
            // segment, etc.

            MergeSpecification spec = null;

            int numMergeableSegments = levels.Count;

            int start = 0;
            while (start < numMergeableSegments)
            {
                // Find max level of all segments not already
                // quantized.
                float maxLevel = levels[start].level;
                for (int i = 1 + start; i < numMergeableSegments; i++)
                {
                    float level = levels[i].level;
                    if (level > maxLevel)
                    {
                        maxLevel = level;
                    }
                }

                // Now search backwards for the rightmost segment that
                // falls into this level:
                float levelBottom;
                if (maxLevel <= levelFloor)
                {
                    // All remaining segments fall into the min level
                    levelBottom = -1.0F;
                }
                else
                {
                    levelBottom = (float)(maxLevel - LEVEL_LOG_SPAN);

                    // Force a boundary at the level floor
                    if (levelBottom < levelFloor && maxLevel >= levelFloor)
                    {
                        levelBottom = levelFloor;
                    }
                }

                int upto = numMergeableSegments - 1;
                while (upto >= start)
                {
                    if (levels[upto].level >= levelBottom)
                    {
                        break;
                    }
                    upto--;
                }
                if (Verbose())
                {
                    Message("  level " + levelBottom + " to " + maxLevel + ": " + (1 + upto - start) + " segments");
                }

                // Finally, record all merges that are viable at this level:
                int end = start + m_mergeFactor;
                while (end <= 1 + upto)
                {
                    bool anyTooLarge = false;
                    bool anyMerging = false;
                    for (int i = start; i < end; i++)
                    {
                        SegmentCommitInfo info = levels[i].info;
                        anyTooLarge |= (Size(info) >= m_maxMergeSize || SizeDocs(info) >= m_maxMergeDocs);
                        if (mergingSegments.Contains(info))
                        {
                            anyMerging = true;
                            break;
                        }
                    }

                    if (anyMerging)
                    {
                        // skip
                    }
                    else if (!anyTooLarge)
                    {
                        if (spec == null)
                        {
                            spec = new MergeSpecification();
                        }
                        IList<SegmentCommitInfo> mergeInfos = new List<SegmentCommitInfo>();
                        for (int i = start; i < end; i++)
                        {
                            mergeInfos.Add(levels[i].info);
                            Debug.Assert(infos.Contains(levels[i].info));
                        }
                        if (Verbose())
                        {
                            Message("  add merge=" + m_writer.Get().SegString(mergeInfos) + " start=" + start + " end=" + end);
                        }
                        spec.Add(new OneMerge(mergeInfos));
                    }
                    else if (Verbose())
                    {
                        Message("    " + start + " to " + end + ": contains segment over maxMergeSize or maxMergeDocs; skipping");
                    }

                    start = end;
                    end = start + m_mergeFactor;
                }

                start = 1 + upto;
            }

            return spec;
        }

        /// <summary>
        /// <p>Determines the largest segment (measured by
        /// document count) that may be merged with other segments.
        /// Small values (e.g., less than 10,000) are best for
        /// interactive indexing, as this limits the length of
        /// pauses while indexing to a few seconds.  Larger values
        /// are best for batched indexing and speedier
        /// searches.</p>
        ///
        /// <p>The default value is <seealso cref="Integer#MAX_VALUE"/>.</p>
        ///
        /// <p>The default merge policy ({@link
        /// LogByteSizeMergePolicy}) also allows you to set this
        /// limit by net size (in MB) of the segment, using {@link
        /// LogByteSizeMergePolicy#setMaxMergeMB}.</p>
        /// </summary>
        public virtual int MaxMergeDocs
        {
            set
            {
                this.m_maxMergeDocs = value;
            }
            get
            {
                return m_maxMergeDocs;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[" + this.GetType().Name + ": ");
            sb.Append("minMergeSize=").Append(m_minMergeSize).Append(", ");
            sb.Append("mergeFactor=").Append(m_mergeFactor).Append(", ");
            sb.Append("maxMergeSize=").Append(m_maxMergeSize).Append(", ");
            sb.Append("maxMergeSizeForForcedMerge=").Append(m_maxMergeSizeForForcedMerge).Append(", ");
            sb.Append("calibrateSizeByDeletes=").Append(m_calibrateSizeByDeletes).Append(", ");
            sb.Append("maxMergeDocs=").Append(m_maxMergeDocs).Append(", ");
            sb.Append("maxCFSSegmentSizeMB=").Append(MaxCFSSegmentSizeMB).Append(", ");
            sb.Append("noCFSRatio=").Append(m_noCFSRatio);
            sb.Append("]");
            return sb.ToString();
        }
    }
}