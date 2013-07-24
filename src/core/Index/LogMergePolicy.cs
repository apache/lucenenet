/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Index
{

    /// <summary><p/>This class implements a <see cref="MergePolicy" /> that tries
    /// to merge segments into levels of exponentially
    /// increasing size, where each level has fewer segments than
    /// the value of the merge factor. Whenever extra segments
    /// (beyond the merge factor upper bound) are encountered,
    /// all segments within the level are merged. You can get or
    /// set the merge factor using <see cref="MergeFactor" /> and
    /// <see cref="MergeFactor" /> respectively.<p/>
    /// 
    /// <p/>This class is abstract and requires a subclass to
    /// define the <see cref="Size" /> method which specifies how a
    /// segment's size is determined.  <see cref="LogDocMergePolicy" />
    /// is one subclass that measures size by document count in
    /// the segment.  <see cref="LogByteSizeMergePolicy" /> is another
    /// subclass that measures size as the total byte size of the
    /// file(s) for the segment.<p/>
    /// </summary>

    public abstract class LogMergePolicy : MergePolicy
    {

        /// <summary>Defines the allowed range of log(size) for each
        /// level.  A level is computed by taking the max segment
        /// log size, minus LEVEL_LOG_SPAN, and finding all
        /// segments falling within that range. 
        /// </summary>
        public const double LEVEL_LOG_SPAN = 0.75;

        /// <summary>Default merge factor, which is how many segments are
        /// merged at a time 
        /// </summary>
        public const int DEFAULT_MERGE_FACTOR = 10;

        /// <summary>Default maximum segment size.  A segment of this size</summary>
        /// <seealso cref="MaxMergeDocs">
        /// </seealso>
        public static readonly int DEFAULT_MAX_MERGE_DOCS = Int32.MaxValue;

        /// <summary> Default noCFSRatio.  If a merge's size is >= 10% of
        ///  the index, then we disable compound file for it.
        ///  See <see cref="NoCFSRatio"/>
        ///  </summary>
        public static double DEFAULT_NO_CFS_RATIO = 0.1;

        public static readonly long DEFAULT_MAX_CFS_SEGMENT_SIZE = long.MaxValue;

        private int mergeFactor = DEFAULT_MERGE_FACTOR;

        internal long minMergeSize;
        internal long maxMergeSize;
        protected long maxMergeSizeForForcedMerge = long.MaxValue;
        internal int maxMergeDocs = DEFAULT_MAX_MERGE_DOCS;

        protected double noCFSRatio = DEFAULT_NO_CFS_RATIO;

        protected long maxCFSSegmentSize = DEFAULT_MAX_CFS_SEGMENT_SIZE;

        protected internal bool calibrateSizeByDeletes = true;

        private bool useCompoundFile = true;

        protected LogMergePolicy()
            : base()
        {
        }

        protected internal virtual bool Verbose
        {
            get
            {
                IndexWriter w = writer.Get();
                return w != null && w.infoStream.IsEnabled("LMP");
            }
        }

        public double NoCFSRatio
        {
            get { return noCFSRatio; }
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentException("noCFSRatio must be 0.0 to 1.0 inclusive; got " + value);
                }
                this.noCFSRatio = value;
            }
        }

        /* If a merged segment will be more than this percentage
         *  of the total size of the index, leave the segment as
         *  non-compound file even if compound file is enabled.
         *  Set to 1.0 to always use CFS regardless of merge
         *  size. */
        private void Message(string message)
        {
            if (Verbose)
                writer.Get().infoStream.Message("LMP", message);
        }


        /// <summary>Gets or sets how often segment indices are merged by
        /// addDocument().  With smaller values, less RAM is used
        /// while indexing, and searches on unoptimized indices are
        /// faster, but indexing speed is slower.  With larger
        /// values, more RAM is used during indexing, and while
        /// searches on unoptimized indices are slower, indexing is
        /// faster.  Thus larger values (&gt; 10) are best for batch
        /// index creation, and smaller values (&lt; 10) for indices
        /// that are interactively maintained. 
        /// </summary>
        public virtual int MergeFactor
        {
            get { return mergeFactor; }
            set
            {
                if (value < 2)
                    throw new ArgumentException("mergeFactor cannot be less than 2");
                this.mergeFactor = value;
            }
        }

        public override bool UseCompoundFile(SegmentInfos infos, SegmentInfoPerCommit mergedInfo)
        {
            if (!GetUseCompoundFile())
            {
                return false;
            }
            long mergedInfoSize = Size(mergedInfo);
            if (mergedInfoSize > maxCFSSegmentSize)
            {
                return false;
            }
            if (NoCFSRatio >= 1.0)
            {
                return true;
            }
            long totalSize = 0;
            foreach (SegmentInfoPerCommit info in infos)
            {
                totalSize += Size(info);
            }
            return mergedInfoSize <= NoCFSRatio * totalSize;
        }

        // .NET Port: having to revert from property to Get/Set methods due to overloaded UseCompoundFile method
        public virtual bool GetUseCompoundFile()
        {
            return useCompoundFile;
        }

        public virtual void SetUseCompoundFile(bool value)
        {
            useCompoundFile = value;
        }

        /// <summary>Gets or sets whether the segment size should be calibrated by
        /// the number of deletes when choosing segments for merge. 
        /// </summary>
        public virtual bool CalibrateSizeByDeletes
        {
            set { this.calibrateSizeByDeletes = value; }
            get { return calibrateSizeByDeletes; }
        }

        protected override void Dispose(bool disposing)
        {
        }

        protected internal abstract long Size(SegmentInfoPerCommit info);

        protected internal virtual long SizeDocs(SegmentInfoPerCommit info)
        {
            if (calibrateSizeByDeletes)
            {
                int delCount = writer.Get().NumDeletedDocs(info);
                //assert delCount <= info.info.getDocCount();
                return (info.info.DocCount - (long)delCount);
            }
            else
            {
                return info.info.DocCount;
            }
        }

        protected internal virtual long SizeBytes(SegmentInfoPerCommit info)
        {
            long byteSize = info.SizeInBytes;
            if (calibrateSizeByDeletes)
            {
                int delCount = writer.Get().NumDeletedDocs(info);
                double delRatio = (info.info.DocCount <= 0 ? 0.0f : ((float)delCount / (float)info.info.DocCount));
                return (info.info.DocCount <= 0 ? byteSize : (long)(byteSize * (1.0f - delRatio)));
            }
            else
            {
                return byteSize;
            }
        }

        protected bool IsMerged(SegmentInfos infos, int maxNumSegments, IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge)
        {
            int numSegments = infos.Count;
            int numToMerge = 0;
            SegmentInfoPerCommit mergeInfo = null;
            bool segmentIsOriginal = false;
            for (int i = 0; i < numSegments && numToMerge <= maxNumSegments; i++)
            {
                SegmentInfoPerCommit info = infos.Info(i);
                bool isOriginal = segmentsToMerge[info];
                if (isOriginal != null)
                {
                    segmentIsOriginal = isOriginal;
                    numToMerge++;
                    mergeInfo = info;
                }
            }

            return numToMerge <= maxNumSegments &&
              (numToMerge != 1 || !segmentIsOriginal || IsMerged(mergeInfo));
        }

        /// <summary>Returns true if this single info is optimized (has no
        /// pending norms or deletes, is in the same dir as the
        /// writer, and matches the current compound file setting 
        /// </summary>
        protected bool IsMerged(SegmentInfoPerCommit info)
        {
            IndexWriter w = writer.Get();
            //assert w != null;
            bool hasDeletions = w.NumDeletedDocs(info) > 0;
            return !hasDeletions &&
              !info.info.HasSeparateNorms &&
              info.info.dir == w.Directory &&
              (info.info.UseCompoundFile == useCompoundFile || noCFSRatio < 1.0);
        }

        private MergeSpecification FindForcedMergesSizeLimit(SegmentInfos infos, int maxNumSegments, int last)
        {
            MergeSpecification spec = new MergeSpecification();
            List<SegmentInfoPerCommit> segments = infos;

            int start = last - 1;
            while (start >= 0)
            {
                SegmentInfoPerCommit info = infos.Info(start);
                if (Size(info) > maxMergeSizeForForcedMerge || SizeDocs(info) > maxMergeDocs)
                {
                    if (Verbose)
                    {
                        Message("findForcedMergesSizeLimit: skip segment=" + info + ": size is > maxMergeSize (" + maxMergeSizeForForcedMerge + ") or sizeDocs is > maxMergeDocs (" + maxMergeDocs + ")");
                    }
                    // need to skip that segment + add a merge for the 'right' segments,
                    // unless there is only 1 which is merged.
                    if (last - start - 1 > 1 || (start != last - 1 && !IsMerged(infos.Info(start + 1))))
                    {
                        // there is more than 1 segment to the right of
                        // this one, or a mergeable single segment.
                        spec.Add(new OneMerge(segments.SubList(start + 1, last)));
                    }
                    last = start;
                }
                else if (last - start == mergeFactor)
                {
                    // mergeFactor eligible segments were found, add them as a merge.
                    spec.Add(new OneMerge(segments.SubList(start, last)));
                    last = start;
                }
                --start;
            }

            // Add any left-over segments, unless there is just 1
            // already fully merged
            if (last > 0 && (++start + 1 < last || !IsMerged(infos.Info(start))))
            {
                spec.Add(new OneMerge(segments.SubList(start, last)));
            }

            return spec.merges.Count == 0 ? null : spec;
        }

        private MergeSpecification FindForcedMergesMaxNumSegments(SegmentInfos infos, int maxNumSegments, int last)
        {
            MergeSpecification spec = new MergeSpecification();
            List<SegmentInfoPerCommit> segments = infos;

            // First, enroll all "full" merges (size
            // mergeFactor) to potentially be run concurrently:
            while (last - maxNumSegments + 1 >= mergeFactor)
            {
                spec.Add(new OneMerge(segments.SubList(last - mergeFactor, last)));
                last -= mergeFactor;
            }

            // Only if there are no full merges pending do we
            // add a final partial (< mergeFactor segments) merge:
            if (0 == spec.merges.Count)
            {
                if (maxNumSegments == 1)
                {

                    // Since we must merge down to 1 segment, the
                    // choice is simple:
                    if (last > 1 || !IsMerged(infos.Info(0)))
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
            return spec.merges.Count == 0 ? null : spec;
        }

        /// <summary>Returns the merges necessary to optimize the index.
        /// This merge policy defines "optimized" to mean only one
        /// segment in the index, where that segment has no
        /// deletions pending nor separate norms, and it is in
        /// compound file format if the current useCompoundFile
        /// setting is true.  This method returns multiple merges
        /// (mergeFactor at a time) so the <see cref="MergeScheduler" />
        /// in use may make use of concurrency. 
        /// </summary>
        public override MergeSpecification FindForcedMerges(SegmentInfos infos, int maxNumSegments, IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge)
        {
            //assert maxNumSegments > 0;
            if (Verbose)
            {
                Message("findForcedMerges: maxNumSegs=" + maxNumSegments + " segsToMerge=" + segmentsToMerge);
            }

            // If the segments are already merged (e.g. there's only 1 segment), or
            // there are <maxNumSegments:.
            if (IsMerged(infos, maxNumSegments, segmentsToMerge))
            {
                if (Verbose)
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
                SegmentInfoPerCommit info = infos.Info(--last);
                if (segmentsToMerge[info] != null)
                {
                    last++;
                    break;
                }
            }

            if (last == 0)
            {
                if (Verbose)
                {
                    Message("last == 0; skip");
                }
                return null;
            }

            // There is only one segment already, and it is merged
            if (maxNumSegments == 1 && last == 1 && IsMerged(infos.Info(0)))
            {
                if (Verbose)
                {
                    Message("already 1 seg; skip");
                }
                return null;
            }

            // Check if there are any segments above the threshold
            bool anyTooLarge = false;
            for (int i = 0; i < last; i++)
            {
                SegmentInfoPerCommit info = infos.Info(i);
                if (Size(info) > maxMergeSizeForForcedMerge || SizeDocs(info) > maxMergeDocs)
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

        /// <summary> Finds merges necessary to expunge all deletes from the
        /// index.  We simply merge adjacent segments that have
        /// deletes, up to mergeFactor at a time.
        /// </summary>
        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos)
        {
            List<SegmentInfoPerCommit> segments = segmentInfos;
            int numSegments = segments.Count;

            if (Verbose)
            {
                Message("findForcedDeleteMerges: " + numSegments + " segments");
            }

            MergeSpecification spec = new MergeSpecification();
            int firstSegmentWithDeletions = -1;
            IndexWriter w = writer.Get();
            //assert w != null;
            for (int i = 0; i < numSegments; i++)
            {
                SegmentInfoPerCommit info = segmentInfos.Info(i);
                int delCount = w.NumDeletedDocs(info);
                if (delCount > 0)
                {
                    if (Verbose)
                    {
                        Message("  segment " + info.info.name + " has deletions");
                    }
                    if (firstSegmentWithDeletions == -1)
                        firstSegmentWithDeletions = i;
                    else if (i - firstSegmentWithDeletions == mergeFactor)
                    {
                        // We've seen mergeFactor segments in a row with
                        // deletions, so force a merge now:
                        if (Verbose)
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
                    if (Verbose)
                    {
                        Message("  add merge " + firstSegmentWithDeletions + " to " + (i - 1) + " inclusive");
                    }
                    spec.Add(new OneMerge(segments.SubList(firstSegmentWithDeletions, i)));
                    firstSegmentWithDeletions = -1;
                }
            }

            if (firstSegmentWithDeletions != -1)
            {
                if (Verbose)
                {
                    Message("  add merge " + firstSegmentWithDeletions + " to " + (numSegments - 1) + " inclusive");
                }
                spec.Add(new OneMerge(segments.SubList(firstSegmentWithDeletions, numSegments)));
            }

            return spec;
        }

        private class SegmentInfoAndLevel : IComparable<SegmentInfoAndLevel>
        {
            internal SegmentInfoPerCommit info;
            internal float level;
            internal int index;

            public SegmentInfoAndLevel(SegmentInfoPerCommit info, float level, int index)
            {
                this.info = info;
                this.level = level;
                this.index = index;
            }

            public int CompareTo(SegmentInfoAndLevel other)
            {
                return other.level.CompareTo(level);
            }
        }

        /// <summary>Checks if any merges are now necessary and returns a
        /// <see cref="MergePolicy.MergeSpecification" /> if so.  A merge
        /// is necessary when there are more than <see cref="MergeFactor" />
        /// segments at a given level.  When
        /// multiple levels have too many segments, this method
        /// will return multiple merges, allowing the <see cref="MergeScheduler" />
        /// to use concurrency. 
        /// </summary>
        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos infos)
        {
            int numSegments = infos.Count;
            if (Verbose)
            {
                Message("findMerges: " + numSegments + " segments");
            }

            // Compute levels, which is just log (base mergeFactor)
            // of the size of each segment
            List<SegmentInfoAndLevel> levels = new List<SegmentInfoAndLevel>();
            float norm = (float)Math.Log(mergeFactor);

            ICollection<SegmentInfoPerCommit> mergingSegments = writer.Get().MergingSegments;

            for (int i = 0; i < numSegments; i++)
            {
                SegmentInfoPerCommit info = infos.Info(i);
                long size = Size(info);

                // Floor tiny segments
                if (size < 1)
                {
                    size = 1;
                }

                SegmentInfoAndLevel infoLevel = new SegmentInfoAndLevel(info, (float)Math.Log(size) / norm, i);
                levels.Add(infoLevel);

                if (Verbose)
                {
                    long segBytes = SizeBytes(info);
                    String extra = mergingSegments.Contains(info) ? " [merging]" : "";
                    if (size >= maxMergeSize)
                    {
                        extra += " [skip: too large]";
                    }
                    Message("seg=" + writer.Get().SegString(info) + " level=" + infoLevel.level + " size=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00} MB", segBytes / 1024 / 1024.0) + extra);
                }
            }

            float levelFloor;
            if (minMergeSize <= 0)
                levelFloor = (float)0.0;
            else
                levelFloor = (float)(Math.Log(minMergeSize) / norm);

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
                if (Verbose)
                {
                    Message("  level " + levelBottom + " to " + maxLevel + ": " + (1 + upto - start) + " segments");
                }

                // Finally, record all merges that are viable at this level:
                int end = start + mergeFactor;
                while (end <= 1 + upto)
                {
                    bool anyTooLarge = false;
                    bool anyMerging = false;
                    for (int i = start; i < end; i++)
                    {
                        SegmentInfoPerCommit info = levels[i].info;
                        anyTooLarge |= (Size(info) >= maxMergeSize || SizeDocs(info) >= maxMergeDocs);
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
                            spec = new MergeSpecification();
                        List<SegmentInfoPerCommit> mergeInfos = new List<SegmentInfoPerCommit>();
                        for (int i = start; i < end; i++)
                        {
                            mergeInfos.Add(levels[i].info);
                            //assert infos.contains(levels.get(i).info);
                        }
                        if (Verbose)
                        {
                            Message("  add merge=" + writer.Get().SegString(mergeInfos) + " start=" + start + " end=" + end);
                        }
                        spec.Add(new OneMerge(mergeInfos));
                    }
                    else if (Verbose)
                    {
                        Message("    " + start + " to " + end + ": contains segment over maxMergeSize or maxMergeDocs; skipping");
                    }

                    start = end;
                    end = start + mergeFactor;
                }

                start = 1 + upto;
            }

            return spec;
        }

        /// <summary>
        /// Gets or sets the largest segment (measured by document
        /// count) that may be merged with other segments.
        /// <p/>Determines the largest segment (measured by
        /// document count) that may be merged with other segments.
        /// Small values (e.g., less than 10,000) are best for
        /// interactive indexing, as this limits the length of
        /// pauses while indexing to a few seconds.  Larger values
        /// are best for batched indexing and speedier
        /// searches.<p/>
        /// 
        /// <p/>The default value is <see cref="int.MaxValue" />.<p/>
        /// 
        /// <p/>The default merge policy (<see cref="LogByteSizeMergePolicy" />)
        /// also allows you to set this
        /// limit by net size (in MB) of the segment, using 
        /// <see cref="LogByteSizeMergePolicy.MaxMergeMB" />.<p/>
        /// </summary>
        public virtual int MaxMergeDocs
        {
            set { this.maxMergeDocs = value; }
            get { return maxMergeDocs; }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[" + GetType().Name + ": ");
            sb.Append("minMergeSize=").Append(minMergeSize).Append(", ");
            sb.Append("mergeFactor=").Append(mergeFactor).Append(", ");
            sb.Append("maxMergeSize=").Append(maxMergeSize).Append(", ");
            sb.Append("maxMergeSizeForForcedMerge=").Append(maxMergeSizeForForcedMerge).Append(", ");
            sb.Append("calibrateSizeByDeletes=").Append(calibrateSizeByDeletes).Append(", ");
            sb.Append("maxMergeDocs=").Append(maxMergeDocs).Append(", ");
            sb.Append("useCompoundFile=").Append(useCompoundFile).Append(", ");
            sb.Append("maxCFSSegmentSizeMB=").Append(MaxCFSSegmentSizeMB).Append(", ");
            sb.Append("noCFSRatio=").Append(noCFSRatio);
            sb.Append("]");
            return sb.ToString();
        }

        public double MaxCFSSegmentSizeMB
        {
            get
            {
                return maxCFSSegmentSize / 1024 / 1024.0;
            }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentException("maxCFSSegmentSizeMB must be >=0 (got " + value + ")");
                }
                value *= 1024 * 1024;
                this.maxCFSSegmentSize = (value > long.MaxValue) ? long.MaxValue : (long)value;
            }
        }
    }
}