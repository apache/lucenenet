using Lucene.Net.Support;
using System;
using System.Collections.Generic;
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
    ///  Merges segments of approximately equal size, subject to
    ///  an allowed number of segments per tier.  this is similar
    ///  to <seealso cref="LogByteSizeMergePolicy"/>, except this merge
    ///  policy is able to merge non-adjacent segment, and
    ///  separates how many segments are merged at once ({@link
    ///  #setMaxMergeAtOnce}) from how many segments are allowed
    ///  per tier (<seealso cref="#setSegmentsPerTier"/>).  this merge
    ///  policy also does not over-merge (i.e. cascade merges).
    ///
    ///  <p>For normal merging, this policy first computes a
    ///  "budget" of how many segments are allowed to be in the
    ///  index.  If the index is over-budget, then the policy
    ///  sorts segments by decreasing size (pro-rating by percent
    ///  deletes), and then finds the least-cost merge.  Merge
    ///  cost is measured by a combination of the "skew" of the
    ///  merge (size of largest segment divided by smallest segment),
    ///  total merge size and percent deletes reclaimed,
    ///  so that merges with lower skew, smaller size
    ///  and those reclaiming more deletes, are
    ///  favored.
    ///
    ///  <p>If a merge will produce a segment that's larger than
    ///  <seealso cref="#setMaxMergedSegmentMB"/>, then the policy will
    ///  merge fewer segments (down to 1 at once, if that one has
    ///  deletions) to keep the segment size under budget.
    ///
    ///  <p><b>NOTE</b>: this policy freely merges non-adjacent
    ///  segments; if this is a problem, use {@link
    ///  LogMergePolicy}.
    ///
    ///  <p><b>NOTE</b>: this policy always merges by byte size
    ///  of the segments, always pro-rates by percent deletes,
    ///  and does not apply any maximum segment size during
    ///  forceMerge (unlike <seealso cref="LogByteSizeMergePolicy"/>).
    ///
    ///  @lucene.experimental
    /// </summary>

    // TODO
    //   - we could try to take into account whether a large
    //     merge is already running (under CMS) and then bias
    //     ourselves towards picking smaller merges if so (or,
    //     maybe CMS should do so)

    public class TieredMergePolicy : MergePolicy
    {
        /// <summary>
        /// Default noCFSRatio.  If a merge's size is >= 10% of
        ///  the index, then we disable compound file for it. </summary>
        ///  <seealso cref= MergePolicy#setNoCFSRatio  </seealso>
        public new static readonly double DEFAULT_NO_CFS_RATIO = 0.1;

        private int maxMergeAtOnce = 10;
        private long maxMergedSegmentBytes = 5 * 1024 * 1024 * 1024L;
        private int maxMergeAtOnceExplicit = 30;

        private long floorSegmentBytes = 2 * 1024 * 1024L;
        private double segsPerTier = 10.0;
        private double forceMergeDeletesPctAllowed = 10.0;
        private double reclaimDeletesWeight = 2.0;

        /// <summary>
        /// Sole constructor, setting all settings to their
        ///  defaults.
        /// </summary>
        public TieredMergePolicy()
            : base(DEFAULT_NO_CFS_RATIO, MergePolicy.DEFAULT_MAX_CFS_SEGMENT_SIZE)
        {
        }

        /// <summary>
        /// Maximum number of segments to be merged at a time
        ///  during "normal" merging.  For explicit merging (eg,
        ///  forceMerge or forceMergeDeletes was called), see {@link
        ///  #setMaxMergeAtOnceExplicit}.  Default is 10.
        /// </summary>
        public virtual TieredMergePolicy SetMaxMergeAtOnce(int v)
        {
            if (v < 2)
            {
                throw new System.ArgumentException("maxMergeAtOnce must be > 1 (got " + v + ")");
            }
            maxMergeAtOnce = v;
            return this;
        }

        /// <summary>
        /// Returns the current maxMergeAtOnce setting.
        /// </summary>
        /// <seealso cref= #setMaxMergeAtOnce  </seealso>
        public virtual int MaxMergeAtOnce
        {
            get
            {
                return maxMergeAtOnce;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetMaxMergeAtOnce(value);
            }
        }

        // TODO: should addIndexes do explicit merging, too?  And,
        // if user calls IW.maybeMerge "explicitly"

        /// <summary>
        /// Maximum number of segments to be merged at a time,
        ///  during forceMerge or forceMergeDeletes. Default is 30.
        /// </summary>
        public virtual TieredMergePolicy SetMaxMergeAtOnceExplicit(int v)
        {
            if (v < 2)
            {
                throw new System.ArgumentException("maxMergeAtOnceExplicit must be > 1 (got " + v + ")");
            }
            maxMergeAtOnceExplicit = v;
            return this;
        }

        /// <summary>
        /// Returns the current maxMergeAtOnceExplicit setting.
        /// </summary>
        /// <seealso cref= #setMaxMergeAtOnceExplicit  </seealso>
        public virtual int MaxMergeAtOnceExplicit
        {
            get
            {
                return maxMergeAtOnceExplicit;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetMaxMergeAtOnceExplicit(value);
            }
        }

        /// <summary>
        /// Maximum sized segment to produce during
        ///  normal merging.  this setting is approximate: the
        ///  estimate of the merged segment size is made by summing
        ///  sizes of to-be-merged segments (compensating for
        ///  percent deleted docs).  Default is 5 GB.
        /// </summary>
        public virtual TieredMergePolicy SetMaxMergedSegmentMB(double v)
        {
            if (v < 0.0)
            {
                throw new System.ArgumentException("maxMergedSegmentMB must be >=0 (got " + v + ")");
            }
            v *= 1024 * 1024;
            maxMergedSegmentBytes = (v > long.MaxValue) ? long.MaxValue : (long)v;
            return this;
        }

        /// <summary>
        /// Returns the current maxMergedSegmentMB setting.
        /// </summary>
        /// <seealso cref= #getMaxMergedSegmentMB  </seealso>
        public virtual double MaxMergedSegmentMB
        {
            get
            {
                return maxMergedSegmentBytes / 1024 / 1024.0;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetMaxMergedSegmentMB(value);
            }
        }

        /// <summary>
        /// Controls how aggressively merges that reclaim more
        ///  deletions are favored.  Higher values will more
        ///  aggressively target merges that reclaim deletions, but
        ///  be careful not to go so high that way too much merging
        ///  takes place; a value of 3.0 is probably nearly too
        ///  high.  A value of 0.0 means deletions don't impact
        ///  merge selection.
        /// </summary>
        public virtual TieredMergePolicy SetReclaimDeletesWeight(double v)
        {
            if (v < 0.0)
            {
                throw new System.ArgumentException("reclaimDeletesWeight must be >= 0.0 (got " + v + ")");
            }
            reclaimDeletesWeight = v;
            return this;
        }

        /// <summary>
        /// See <seealso cref="#setReclaimDeletesWeight"/>. </summary>
        public virtual double ReclaimDeletesWeight
        {
            get
            {
                return reclaimDeletesWeight;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetReclaimDeletesWeight(value);
            }
        }

        /// <summary>
        /// Segments smaller than this are "rounded up" to this
        ///  size, ie treated as equal (floor) size for merge
        ///  selection.  this is to prevent frequent flushing of
        ///  tiny segments from allowing a long tail in the index.
        ///  Default is 2 MB.
        /// </summary>
        public virtual TieredMergePolicy SetFloorSegmentMB(double v)
        {
            if (v <= 0.0)
            {
                throw new System.ArgumentException("floorSegmentMB must be >= 0.0 (got " + v + ")");
            }
            v *= 1024 * 1024;
            floorSegmentBytes = (v > long.MaxValue) ? long.MaxValue : (long)v;
            return this;
        }

        /// <summary>
        /// Returns the current floorSegmentMB.
        /// </summary>
        ///  <seealso cref= #setFloorSegmentMB  </seealso>
        public virtual double FloorSegmentMB
        {
            get
            {
                return floorSegmentBytes / (1024 * 1024.0);
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetFloorSegmentMB(value);
            }
        }

        /// <summary>
        /// When forceMergeDeletes is called, we only merge away a
        ///  segment if its delete percentage is over this
        ///  threshold.  Default is 10%.
        /// </summary>
        public virtual TieredMergePolicy SetForceMergeDeletesPctAllowed(double v)
        {
            if (v < 0.0 || v > 100.0)
            {
                throw new System.ArgumentException("forceMergeDeletesPctAllowed must be between 0.0 and 100.0 inclusive (got " + v + ")");
            }
            forceMergeDeletesPctAllowed = v;
            return this;
        }

        /// <summary>
        /// Returns the current forceMergeDeletesPctAllowed setting.
        /// </summary>
        /// <seealso cref= #setForceMergeDeletesPctAllowed  </seealso>
        public virtual double ForceMergeDeletesPctAllowed
        {
            get
            {
                return forceMergeDeletesPctAllowed;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetForceMergeDeletesPctAllowed(value);
            }
        }

        /// <summary>
        /// Sets the allowed number of segments per tier.  Smaller
        ///  values mean more merging but fewer segments.
        ///
        ///  <p><b>NOTE</b>: this value should be >= the {@link
        ///  #setMaxMergeAtOnce} otherwise you'll force too much
        ///  merging to occur.</p>
        ///
        ///  <p>Default is 10.0.</p>
        /// </summary>
        public virtual TieredMergePolicy SetSegmentsPerTier(double v)
        {
            if (v < 2.0)
            {
                throw new System.ArgumentException("segmentsPerTier must be >= 2.0 (got " + v + ")");
            }
            segsPerTier = v;
            return this;
        }

        /// <summary>
        /// Returns the current segmentsPerTier setting.
        /// </summary>
        /// <seealso cref= #setSegmentsPerTier  </seealso>
        public virtual double SegmentsPerTier
        {
            get
            {
                return segsPerTier;
            }

            set // LUCENENET TODO: Double setter functionality could be confusing
            {
                SetSegmentsPerTier(value);
            }
        }

        private class SegmentByteSizeDescending : IComparer<SegmentCommitInfo>
        {
            private readonly TieredMergePolicy OuterInstance;

            public SegmentByteSizeDescending(TieredMergePolicy outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual int Compare(SegmentCommitInfo o1, SegmentCommitInfo o2)
            {
                try
                {
                    long sz1 = OuterInstance.Size(o1);
                    long sz2 = OuterInstance.Size(o2);
                    if (sz1 > sz2)
                    {
                        return -1;
                    }
                    else if (sz2 > sz1)
                    {
                        return 1;
                    }
                    else
                    {
                        return o1.Info.Name.CompareTo(o2.Info.Name);
                    }
                }
                catch (System.IO.IOException ioe)
                {
                    throw new Exception(ioe.Message, ioe);
                }
            }
        }

        /// <summary>
        /// Holds score and explanation for a single candidate
        ///  merge.
        /// </summary>
        protected abstract class MergeScore
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass
            ///  constructors, typically implicit.)
            /// </summary>
            protected MergeScore()
            {
            }

            /// <summary>
            /// Returns the score for this merge candidate; lower
            ///  scores are better.
            /// </summary>
            internal abstract double Score { get; } // LUCENENET TODO: Should this be public? Or should the class be internal? It doesn't make any sense to have protected class with no protected members.

            /// <summary>
            /// Human readable explanation of how the merge got this
            ///  score.
            /// </summary>
            internal abstract string Explanation { get; }
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos infos)
        {
            if (Verbose())
            {
                Message("findMerges: " + infos.Size() + " segments");
            }
            if (infos.Size() == 0)
            {
                return null;
            }
            ICollection<SegmentCommitInfo> merging = writer.Get().MergingSegments;
            ICollection<SegmentCommitInfo> toBeMerged = new HashSet<SegmentCommitInfo>();

            List<SegmentCommitInfo> infosSorted = new List<SegmentCommitInfo>(infos.AsList());
            infosSorted.Sort(new SegmentByteSizeDescending(this));

            // Compute total index bytes & print details about the index
            long totIndexBytes = 0;
            long minSegmentBytes = long.MaxValue;
            foreach (SegmentCommitInfo info in infosSorted)
            {
                long segBytes = Size(info);
                if (Verbose())
                {
                    string extra = merging.Contains(info) ? " [merging]" : "";
                    if (segBytes >= maxMergedSegmentBytes / 2.0)
                    {
                        extra += " [skip: too large]";
                    }
                    else if (segBytes < floorSegmentBytes)
                    {
                        extra += " [floored]";
                    }
                    Message("  seg=" + writer.Get().SegString(info) + " size=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00}", segBytes / 1024 / 1024.0) + " MB" + extra);
                }

                minSegmentBytes = Math.Min(segBytes, minSegmentBytes);
                // Accum total byte size
                totIndexBytes += segBytes;
            }

            // If we have too-large segments, grace them out
            // of the maxSegmentCount:
            int tooBigCount = 0;
            while (tooBigCount < infosSorted.Count && Size(infosSorted[tooBigCount]) >= maxMergedSegmentBytes / 2.0)
            {
                totIndexBytes -= Size(infosSorted[tooBigCount]);
                tooBigCount++;
            }

            minSegmentBytes = FloorSize(minSegmentBytes);

            // Compute max allowed segs in the index
            long levelSize = minSegmentBytes;
            long bytesLeft = totIndexBytes;
            double allowedSegCount = 0;
            while (true)
            {
                double segCountLevel = bytesLeft / (double)levelSize;
                if (segCountLevel < segsPerTier)
                {
                    allowedSegCount += Math.Ceiling(segCountLevel);
                    break;
                }
                allowedSegCount += segsPerTier;
                bytesLeft -= (long)(segsPerTier * levelSize);
                levelSize *= maxMergeAtOnce;
            }
            int allowedSegCountInt = (int)allowedSegCount;

            MergeSpecification spec = null;

            // Cycle to possibly select more than one merge:
            while (true)
            {
                long mergingBytes = 0;

                // Gather eligible segments for merging, ie segments
                // not already being merged and not already picked (by
                // prior iteration of this loop) for merging:
                IList<SegmentCommitInfo> eligible = new List<SegmentCommitInfo>();
                for (int idx = tooBigCount; idx < infosSorted.Count; idx++)
                {
                    SegmentCommitInfo info = infosSorted[idx];
                    if (merging.Contains(info))
                    {
                        mergingBytes += info.SizeInBytes();
                    }
                    else if (!toBeMerged.Contains(info))
                    {
                        eligible.Add(info);
                    }
                }

                bool maxMergeIsRunning = mergingBytes >= maxMergedSegmentBytes;

                if (Verbose())
                {
                    Message("  allowedSegmentCount=" + allowedSegCountInt + " vs count=" + infosSorted.Count + " (eligible count=" + eligible.Count + ") tooBigCount=" + tooBigCount);
                }

                if (eligible.Count == 0)
                {
                    return spec;
                }

                if (eligible.Count >= allowedSegCountInt)
                {
                    // OK we are over budget -- find best merge!
                    MergeScore bestScore = null;
                    IList<SegmentCommitInfo> best = null;
                    bool bestTooLarge = false;
                    long bestMergeBytes = 0;

                    // Consider all merge starts:
                    for (int startIdx = 0; startIdx <= eligible.Count - maxMergeAtOnce; startIdx++)
                    {
                        long totAfterMergeBytes = 0;

                        IList<SegmentCommitInfo> candidate = new List<SegmentCommitInfo>();
                        bool hitTooLarge = false;
                        for (int idx = startIdx; idx < eligible.Count && candidate.Count < maxMergeAtOnce; idx++)
                        {
                            SegmentCommitInfo info = eligible[idx];
                            long segBytes = Size(info);

                            if (totAfterMergeBytes + segBytes > maxMergedSegmentBytes)
                            {
                                hitTooLarge = true;
                                // NOTE: we continue, so that we can try
                                // "packing" smaller segments into this merge
                                // to see if we can get closer to the max
                                // size; this in general is not perfect since
                                // this is really "bin packing" and we'd have
                                // to try different permutations.
                                continue;
                            }
                            candidate.Add(info);
                            totAfterMergeBytes += segBytes;
                        }

                        MergeScore score = Score(candidate, hitTooLarge, mergingBytes);
                        if (Verbose())
                        {
                            Message("  maybe=" + writer.Get().SegString(candidate) + " score=" + score.Score + " " + score.Explanation + " tooLarge=" + hitTooLarge + " size=" + string.Format(CultureInfo.InvariantCulture, "%.3f MB", totAfterMergeBytes / 1024.0 / 1024.0));
                        }

                        // If we are already running a max sized merge
                        // (maxMergeIsRunning), don't allow another max
                        // sized merge to kick off:
                        if ((bestScore == null || score.Score < bestScore.Score) && (!hitTooLarge || !maxMergeIsRunning))
                        {
                            best = candidate;
                            bestScore = score;
                            bestTooLarge = hitTooLarge;
                            bestMergeBytes = totAfterMergeBytes;
                        }
                    }

                    if (best != null)
                    {
                        if (spec == null)
                        {
                            spec = new MergeSpecification();
                        }
                        OneMerge merge = new OneMerge(best);
                        spec.Add(merge);
                        foreach (SegmentCommitInfo info in merge.Segments)
                        {
                            toBeMerged.Add(info);
                        }

                        if (Verbose())
                        {
                            Message("  add merge=" + writer.Get().SegString(merge.Segments) + " size=" + string.Format(CultureInfo.InvariantCulture, "%.3f MB", bestMergeBytes / 1024.0 / 1024.0) + " score=" + string.Format(CultureInfo.InvariantCulture, "%.3f", bestScore.Score) + " " + bestScore.Explanation + (bestTooLarge ? " [max merge]" : ""));
                        }
                    }
                    else
                    {
                        return spec;
                    }
                }
                else
                {
                    return spec;
                }
            }
        }

        /// <summary>
        /// Expert: scores one merge; subclasses can override. </summary>
        protected virtual MergeScore Score(IList<SegmentCommitInfo> candidate, bool hitTooLarge, long mergingBytes)
        {
            long totBeforeMergeBytes = 0;
            long totAfterMergeBytes = 0;
            long totAfterMergeBytesFloored = 0;
            foreach (SegmentCommitInfo info in candidate)
            {
                long segBytes = Size(info);
                totAfterMergeBytes += segBytes;
                totAfterMergeBytesFloored += FloorSize(segBytes);
                totBeforeMergeBytes += info.SizeInBytes();
            }

            // Roughly measure "skew" of the merge, i.e. how
            // "balanced" the merge is (whether the segments are
            // about the same size), which can range from
            // 1.0/numSegsBeingMerged (good) to 1.0 (poor). Heavily
            // lopsided merges (skew near 1.0) is no good; it means
            // O(N^2) merge cost over time:
            double skew;
            if (hitTooLarge)
            {
                // Pretend the merge has perfect skew; skew doesn't
                // matter in this case because this merge will not
                // "cascade" and so it cannot lead to N^2 merge cost
                // over time:
                skew = 1.0 / maxMergeAtOnce;
            }
            else
            {
                skew = ((double)FloorSize(Size(candidate[0]))) / totAfterMergeBytesFloored;
            }

            // Strongly favor merges with less skew (smaller
            // mergeScore is better):
            double mergeScore = skew;

            // Gently favor smaller merges over bigger ones.  We
            // don't want to make this exponent too large else we
            // can end up doing poor merges of small segments in
            // order to avoid the large merges:
            mergeScore *= Math.Pow(totAfterMergeBytes, 0.05);

            // Strongly favor merges that reclaim deletes:
            double nonDelRatio = ((double)totAfterMergeBytes) / totBeforeMergeBytes;
            mergeScore *= Math.Pow(nonDelRatio, reclaimDeletesWeight);

            double finalMergeScore = mergeScore;

            return new MergeScoreAnonymousInnerClassHelper(this, skew, nonDelRatio, finalMergeScore);
        }

        private class MergeScoreAnonymousInnerClassHelper : MergeScore
        {
            private readonly TieredMergePolicy OuterInstance;

            private double Skew;
            private double NonDelRatio;
            private double FinalMergeScore;

            public MergeScoreAnonymousInnerClassHelper(TieredMergePolicy outerInstance, double skew, double nonDelRatio, double finalMergeScore)
            {
                this.OuterInstance = outerInstance;
                this.Skew = skew;
                this.NonDelRatio = nonDelRatio;
                this.FinalMergeScore = finalMergeScore;
            }

            internal override double Score
            {
                get
                {
                    return FinalMergeScore;
                }
            }

            internal override string Explanation
            {
                get
                {
                    return "skew=" + string.Format(CultureInfo.InvariantCulture, "%.3f", Skew) + " nonDelRatio=" + string.Format(CultureInfo.InvariantCulture, "%.3f", NonDelRatio);
                }
            }
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos infos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge)
        {
            if (Verbose())
            {
                Message("findForcedMerges maxSegmentCount=" + maxSegmentCount + " infos=" + writer.Get().SegString(infos.Segments) + " segmentsToMerge=" + segmentsToMerge);
            }

            List<SegmentCommitInfo> eligible = new List<SegmentCommitInfo>();
            bool forceMergeRunning = false;
            ICollection<SegmentCommitInfo> merging = writer.Get().MergingSegments;
            bool? segmentIsOriginal = false;
            foreach (SegmentCommitInfo info in infos.Segments)
            {
                bool? isOriginal;
                if (segmentsToMerge.TryGetValue(info, out isOriginal))
                {
                    segmentIsOriginal = isOriginal;
                    if (!merging.Contains(info))
                    {
                        eligible.Add(info);
                    }
                    else
                    {
                        forceMergeRunning = true;
                    }
                }
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            if ((maxSegmentCount > 1 && eligible.Count <= maxSegmentCount) || (maxSegmentCount == 1 && eligible.Count == 1 && (segmentIsOriginal == false || IsMerged(infos, eligible[0]))))
            {
                if (Verbose())
                {
                    Message("already merged");
                }
                return null;
            }

            eligible.Sort(new SegmentByteSizeDescending(this));

            if (Verbose())
            {
                Message("eligible=" + eligible);
                Message("forceMergeRunning=" + forceMergeRunning);
            }

            int end = eligible.Count;

            MergeSpecification spec = null;

            // Do full merges, first, backwards:
            while (end >= maxMergeAtOnceExplicit + maxSegmentCount - 1)
            {
                if (spec == null)
                {
                    spec = new MergeSpecification();
                }
                OneMerge merge = new OneMerge(eligible.SubList(end - maxMergeAtOnceExplicit, end));
                if (Verbose())
                {
                    Message("add merge=" + writer.Get().SegString(merge.Segments));
                }
                spec.Add(merge);
                end -= maxMergeAtOnceExplicit;
            }

            if (spec == null && !forceMergeRunning)
            {
                // Do final merge
                int numToMerge = end - maxSegmentCount + 1;
                OneMerge merge = new OneMerge(eligible.SubList(end - numToMerge, end));
                if (Verbose())
                {
                    Message("add final merge=" + merge.SegString(writer.Get().Directory));
                }
                spec = new MergeSpecification();
                spec.Add(merge);
            }

            return spec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos infos)
        {
            if (Verbose())
            {
                Message("findForcedDeletesMerges infos=" + writer.Get().SegString(infos.Segments) + " forceMergeDeletesPctAllowed=" + forceMergeDeletesPctAllowed);
            }
            List<SegmentCommitInfo> eligible = new List<SegmentCommitInfo>();
            ICollection<SegmentCommitInfo> merging = writer.Get().MergingSegments;
            foreach (SegmentCommitInfo info in infos.Segments)
            {
                double pctDeletes = 100.0 * ((double)writer.Get().NumDeletedDocs(info)) / info.Info.DocCount;
                if (pctDeletes > forceMergeDeletesPctAllowed && !merging.Contains(info))
                {
                    eligible.Add(info);
                }
            }

            if (eligible.Count == 0)
            {
                return null;
            }

            eligible.Sort(new SegmentByteSizeDescending(this));

            if (Verbose())
            {
                Message("eligible=" + eligible);
            }

            int start = 0;
            MergeSpecification spec = null;

            while (start < eligible.Count)
            {
                // Don't enforce max merged size here: app is explicitly
                // calling forceMergeDeletes, and knows this may take a
                // long time / produce big segments (like forceMerge):
                int end = Math.Min(start + maxMergeAtOnceExplicit, eligible.Count);
                if (spec == null)
                {
                    spec = new MergeSpecification();
                }

                OneMerge merge = new OneMerge(eligible.SubList(start, end));
                if (Verbose())
                {
                    Message("add merge=" + writer.Get().SegString(merge.Segments));
                }
                spec.Add(merge);
                start = end;
            }

            return spec;
        }

        public override void Dispose()
        {
        }

        private long FloorSize(long bytes)
        {
            return Math.Max(floorSegmentBytes, bytes);
        }

        private bool Verbose()
        {
            IndexWriter w = writer.Get();
            return w != null && w.infoStream.IsEnabled("TMP");
        }

        private void Message(string message)
        {
            writer.Get().infoStream.Message("TMP", message);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[" + this.GetType().Name + ": ");
            sb.Append("maxMergeAtOnce=").Append(maxMergeAtOnce).Append(", ");
            sb.Append("maxMergeAtOnceExplicit=").Append(maxMergeAtOnceExplicit).Append(", ");
            sb.Append("maxMergedSegmentMB=").Append(maxMergedSegmentBytes / 1024 / 1024.0).Append(", ");
            sb.Append("floorSegmentMB=").Append(floorSegmentBytes / 1024 / 1024.0).Append(", ");
            sb.Append("forceMergeDeletesPctAllowed=").Append(forceMergeDeletesPctAllowed).Append(", ");
            sb.Append("segmentsPerTier=").Append(segsPerTier).Append(", ");
            sb.Append("maxCFSSegmentSizeMB=").Append(MaxCFSSegmentSizeMB).Append(", ");
            sb.Append("noCFSRatio=").Append(noCFSRatio_);
            return sb.ToString();
        }
    }
}