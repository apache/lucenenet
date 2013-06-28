using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public class TieredMergePolicy : MergePolicy
    {
        private int maxMergeAtOnce = 10;
        private long maxMergedSegmentBytes = 5 * 1024 * 1024 * 1024L;
        private int maxMergeAtOnceExplicit = 30;

        private long floorSegmentBytes = 2 * 1024 * 1024L;
        private double segsPerTier = 10.0;
        private double forceMergeDeletesPctAllowed = 10.0;
        private bool useCompoundFile = true;
        private double noCFSRatio = 0.1;
        private long maxCFSSegmentSize = long.MaxValue;
        private double reclaimDeletesWeight = 2.0;

        public TieredMergePolicy()
        {
        }

        public virtual TieredMergePolicy SetMaxMergeAtOnce(int v)
        {
            if (v < 2)
            {
                throw new ArgumentException("maxMergeAtOnce must be > 1 (got " + v + ")");
            }
            maxMergeAtOnce = v;
            return this;
        }

        public virtual int MaxMergeAtOnce
        {
            get
            {
                return maxMergeAtOnce;
            }
            set
            {
                SetMaxMergeAtOnce(value);
            }
        }

        public virtual TieredMergePolicy SetMaxMergeAtOnceExplicit(int v)
        {
            if (v < 2)
            {
                throw new ArgumentException("maxMergeAtOnceExplicit must be > 1 (got " + v + ")");
            }
            maxMergeAtOnceExplicit = v;
            return this;
        }

        public virtual int MaxMergeAtOnceExplicit
        {
            get
            {
                return maxMergeAtOnceExplicit;
            }
            set
            {
                SetMaxMergeAtOnceExplicit(value);
            }
        }

        public virtual TieredMergePolicy SetMaxMergedSegmentMB(double v)
        {
            if (v < 0.0)
            {
                throw new ArgumentException("maxMergedSegmentMB must be >=0 (got " + v + ")");
            }
            v *= 1024 * 1024;
            maxMergedSegmentBytes = (v > long.MaxValue) ? long.MaxValue : (long)v;
            return this;
        }

        public virtual double MaxMergedSegmentMB
        {
            get
            {
                return maxMergedSegmentBytes / 1024 / 1024.0;
            }
            set
            {
                SetMaxMergedSegmentMB(value);
            }
        }

        public virtual TieredMergePolicy SetReclaimDeletesWeight(double v)
        {
            if (v < 0.0)
            {
                throw new ArgumentException("reclaimDeletesWeight must be >= 0.0 (got " + v + ")");
            }
            reclaimDeletesWeight = v;
            return this;
        }

        public virtual double ReclaimDeletesWeight
        {
            get
            {
                return reclaimDeletesWeight;
            }
            set
            {
                SetReclaimDeletesWeight(value);
            }
        }

        public virtual TieredMergePolicy SetFloorSegmentMB(double v)
        {
            if (v <= 0.0)
            {
                throw new ArgumentException("floorSegmentMB must be >= 0.0 (got " + v + ")");
            }
            v *= 1024 * 1024;
            floorSegmentBytes = (v > long.MaxValue) ? long.MaxValue : (long)v;
            return this;
        }

        public virtual double FloorSegmentMB
        {
            get
            {
                return floorSegmentBytes / (1024 * 1024.0);
            }
            set
            {
                SetFloorSegmentMB(value);
            }
        }

        public virtual TieredMergePolicy SetForceMergeDeletesPctAllowed(double v)
        {
            if (v < 0.0 || v > 100.0)
            {
                throw new ArgumentException("forceMergeDeletesPctAllowed must be between 0.0 and 100.0 inclusive (got " + v + ")");
            }
            forceMergeDeletesPctAllowed = v;
            return this;
        }

        public virtual double ForceMergeDeletesPctAllowed
        {
            get
            {
                return forceMergeDeletesPctAllowed;
            }
            set
            {
                SetForceMergeDeletesPctAllowed(value);
            }
        }

        public virtual TieredMergePolicy SetSegmentsPerTier(double v)
        {
            if (v < 2.0)
            {
                throw new ArgumentException("segmentsPerTier must be >= 2.0 (got " + v + ")");
            }
            segsPerTier = v;
            return this;
        }

        public virtual double SegmentsPerTier
        {
            get
            {
                return segsPerTier;
            }
            set
            {
                SetSegmentsPerTier(value);
            }
        }

        public virtual TieredMergePolicy SetUseCompoundFile(bool useCompoundFile)
        {
            this.useCompoundFile = useCompoundFile;
            return this;
        }

        public virtual bool UseCompoundFileValue
        {
            get
            {
                return useCompoundFile;
            }
            set
            {
                SetUseCompoundFile(value);
            }
        }

        public virtual TieredMergePolicy SetNoCFSRatio(double noCFSRatio)
        {
            if (noCFSRatio < 0.0 || noCFSRatio > 1.0)
            {
                throw new ArgumentException("noCFSRatio must be 0.0 to 1.0 inclusive; got " + noCFSRatio);
            }
            this.noCFSRatio = noCFSRatio;
            return this;
        }

        public virtual double NoCFSRatio
        {
            get
            {
                return noCFSRatio;
            }
            set
            {
                SetNoCFSRatio(value);
            }
        }

        private class SegmentByteSizeDescending : IComparer<SegmentInfoPerCommit>
        {
            private readonly TieredMergePolicy parent;

            public SegmentByteSizeDescending(TieredMergePolicy parent)
            {
                this.parent = parent;
            }

            public int Compare(SegmentInfoPerCommit o1, SegmentInfoPerCommit o2)
            {
                try
                {
                    long sz1 = parent.Size(o1);
                    long sz2 = parent.Size(o2);
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
                        return o1.info.name.CompareTo(o2.info.name);
                    }
                }
                catch (System.IO.IOException)
                {
                    throw;
                }
            }
        }

        protected abstract class MergeScore
        {
            protected MergeScore()
            {
            }

            internal abstract double Score { get; }

            internal abstract string Explanation { get; }
        }

        public override MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos infos)
        {
            if (Verbose)
            {
                Message("findMerges: " + infos.Count + " segments");
            }
            if (infos.Count == 0)
            {
                return null;
            }
            ICollection<SegmentInfoPerCommit> merging = writer.Get().MergingSegments;
            ICollection<SegmentInfoPerCommit> toBeMerged = new HashSet<SegmentInfoPerCommit>();

            List<SegmentInfoPerCommit> infosSorted = new List<SegmentInfoPerCommit>(infos);
            infosSorted.Sort(new SegmentByteSizeDescending(this));

            // Compute total index bytes & print details about the index
            long totIndexBytes = 0;
            long minSegmentBytes = long.MaxValue;
            foreach (SegmentInfoPerCommit info in infosSorted)
            {
                long segBytes = Size(info);
                if (Verbose)
                {
                    String extra = merging.Contains(info) ? " [merging]" : "";
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
                List<SegmentInfoPerCommit> eligible = new List<SegmentInfoPerCommit>();
                for (int idx = tooBigCount; idx < infosSorted.Count; idx++)
                {
                    SegmentInfoPerCommit info = infosSorted[idx];
                    if (merging.Contains(info))
                    {
                        mergingBytes += info.SizeInBytes;
                    }
                    else if (!toBeMerged.Contains(info))
                    {
                        eligible.Add(info);
                    }
                }

                bool maxMergeIsRunning = mergingBytes >= maxMergedSegmentBytes;

                if (Verbose)
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
                    List<SegmentInfoPerCommit> best = null;
                    bool bestTooLarge = false;
                    long bestMergeBytes = 0;

                    // Consider all merge starts:
                    for (int startIdx = 0; startIdx <= eligible.Count - maxMergeAtOnce; startIdx++)
                    {

                        long totAfterMergeBytes = 0;

                        List<SegmentInfoPerCommit> candidate = new List<SegmentInfoPerCommit>();
                        bool hitTooLarge = false;
                        for (int idx = startIdx; idx < eligible.Count && candidate.Count < maxMergeAtOnce; idx++)
                        {
                            SegmentInfoPerCommit info = eligible[idx];
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
                        if (Verbose)
                        {
                            Message("  maybe=" + writer.Get().SegString(candidate) + " score=" + score.Score + " " + score.Explanation + " tooLarge=" + hitTooLarge + " size=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00} MB", totAfterMergeBytes / 1024.0 / 1024.0));
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
                        foreach (SegmentInfoPerCommit info in merge.segments)
                        {
                            toBeMerged.Add(info);
                        }

                        if (Verbose)
                        {
                            Message("  add merge=" + writer.Get().SegString(merge.segments) + " size=" + String.Format(CultureInfo.InvariantCulture, "%.3f MB", bestMergeBytes / 1024.0 / 1024.0) + " score=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00}", bestScore.Score) + " " + bestScore.Explanation + (bestTooLarge ? " [max merge]" : ""));
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

        protected virtual MergeScore Score(IList<SegmentInfoPerCommit> candidate, bool hitTooLarge, long mergingBytes)
        {
            long totBeforeMergeBytes = 0;
            long totAfterMergeBytes = 0;
            long totAfterMergeBytesFloored = 0;
            foreach (SegmentInfoPerCommit info in candidate)
            {
                long segBytes = Size(info);
                totAfterMergeBytes += segBytes;
                totAfterMergeBytesFloored += FloorSize(segBytes);
                totBeforeMergeBytes += info.SizeInBytes;
            }

            // Measure "skew" of the merge, which can range
            // from 1.0/numSegsBeingMerged (good) to 1.0
            // (poor):
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

            return new AnonymousMergeScore(finalMergeScore, skew, nonDelRatio);

        }

        private sealed class AnonymousMergeScore : MergeScore
        {
            private readonly double finalMergeScore;
            private readonly double skew;
            private readonly double nonDelRatio;

            public AnonymousMergeScore(double finalMergeScore, double skew, double nonDelRatio)
            {
                this.finalMergeScore = finalMergeScore;
                this.skew = skew;
                this.nonDelRatio = nonDelRatio;
            }

            internal override double Score
            {
                get { return finalMergeScore; }
            }

            internal override string Explanation
            {
                get
                {
                    return "skew=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00}", skew) + " nonDelRatio=" + String.Format(CultureInfo.InvariantCulture, "{0:0.00}", nonDelRatio);
                }
            }
        }

        public override MergeSpecification FindForcedMerges(SegmentInfos infos, int maxSegmentCount, IDictionary<SegmentInfoPerCommit, bool> segmentsToMerge)
        {
            if (Verbose)
            {
                Message("findForcedMerges maxSegmentCount=" + maxSegmentCount + " infos=" + writer.Get().SegString(infos) + " segmentsToMerge=" + segmentsToMerge);
            }

            List<SegmentInfoPerCommit> eligible = new List<SegmentInfoPerCommit>();
            bool forceMergeRunning = false;
            ICollection<SegmentInfoPerCommit> merging = writer.Get().MergingSegments;
            bool segmentIsOriginal = false;
            foreach (SegmentInfoPerCommit info in infos)
            {
                Boolean isOriginal = segmentsToMerge[info];
                if (isOriginal != null)
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

            if ((maxSegmentCount > 1 && eligible.Count <= maxSegmentCount) ||
                (maxSegmentCount == 1 && eligible.Count == 1 && (!segmentIsOriginal || IsMerged(eligible[0]))))
            {
                if (Verbose)
                {
                    Message("already merged");
                }
                return null;
            }

            eligible.Sort(new SegmentByteSizeDescending(this));

            if (Verbose)
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
                OneMerge merge = new OneMerge(eligible.GetRange(end - maxMergeAtOnceExplicit, end));
                if (Verbose)
                {
                    Message("add merge=" + writer.Get().SegString(merge.segments));
                }
                spec.Add(merge);
                end -= maxMergeAtOnceExplicit;
            }

            if (spec == null && !forceMergeRunning)
            {
                // Do merge
                int numToMerge = end - maxSegmentCount + 1;
                OneMerge merge = new OneMerge(eligible.GetRange(end - numToMerge, end));
                if (Verbose)
                {
                    Message("add merge=" + merge.SegString(writer.Get().Directory));
                }
                spec = new MergeSpecification();
                spec.Add(merge);
            }

            return spec;
        }

        public override MergeSpecification FindForcedDeletesMerges(SegmentInfos infos)
        {
            if (Verbose)
            {
                Message("findForcedDeletesMerges infos=" + writer.Get().SegString(infos) + " forceMergeDeletesPctAllowed=" + forceMergeDeletesPctAllowed);
            }
            List<SegmentInfoPerCommit> eligible = new List<SegmentInfoPerCommit>();
            ICollection<SegmentInfoPerCommit> merging = writer.Get().MergingSegments;
            foreach (SegmentInfoPerCommit info in infos)
            {
                double pctDeletes = 100.0 * ((double)writer.Get().NumDeletedDocs(info)) / info.info.DocCount;
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

            if (Verbose)
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

                OneMerge merge = new OneMerge(eligible.GetRange(start, end));
                if (Verbose)
                {
                    Message("add merge=" + writer.Get().SegString(merge.segments));
                }
                spec.Add(merge);
                start = end;
            }

            return spec;
        }

        public override bool UseCompoundFile(SegmentInfos infos, SegmentInfoPerCommit mergedInfo)
        {
            if (!UseCompoundFileValue)
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

        protected override void Dispose(bool disposing)
        {
        }

        private bool IsMerged(SegmentInfoPerCommit info)
        {
            IndexWriter w = writer.Get();
            //assert w != null;
            bool hasDeletions = w.NumDeletedDocs(info) > 0;
            return !hasDeletions &&
              !info.info.HasSeparateNorms &&
              info.info.dir == w.Directory &&
              (info.info.UseCompoundFile == useCompoundFile || noCFSRatio < 1.0 || maxCFSSegmentSize < long.MaxValue);
        }

        // Segment size in bytes, pro-rated by % deleted
        private long Size(SegmentInfoPerCommit info)
        {
            long byteSize = info.SizeInBytes;
            int delCount = writer.Get().NumDeletedDocs(info);
            double delRatio = (info.info.DocCount <= 0 ? 0.0f : ((double)delCount / (double)info.info.DocCount));
            //assert delRatio <= 1.0;
            return (long)(byteSize * (1.0 - delRatio));
        }

        private long FloorSize(long bytes)
        {
            return Math.Max(floorSegmentBytes, bytes);
        }

        private bool Verbose
        {
            get
            {
                IndexWriter w = writer.Get();
                return w != null && w.infoStream.IsEnabled("TMP");
            }
        }

        private void Message(String message)
        {
            writer.Get().infoStream.Message("TMP", message);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder("[" + GetType().Name + ": ");
            sb.Append("maxMergeAtOnce=").Append(maxMergeAtOnce).Append(", ");
            sb.Append("maxMergeAtOnceExplicit=").Append(maxMergeAtOnceExplicit).Append(", ");
            sb.Append("maxMergedSegmentMB=").Append(maxMergedSegmentBytes / 1024 / 1024.0).Append(", ");
            sb.Append("floorSegmentMB=").Append(floorSegmentBytes / 1024 / 1024.0).Append(", ");
            sb.Append("forceMergeDeletesPctAllowed=").Append(forceMergeDeletesPctAllowed).Append(", ");
            sb.Append("segmentsPerTier=").Append(segsPerTier).Append(", ");
            sb.Append("useCompoundFile=").Append(useCompoundFile).Append(", ");
            sb.Append("maxCFSSegmentSizeMB=").Append(MaxCFSSegmentSizeMB).Append(", ");
            sb.Append("noCFSRatio=").Append(noCFSRatio);
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
                SetMaxCFSSegmentSizeMB(v);
            }
        }

        public TieredMergePolicy SetMaxCFSSegmentSizeMB(double v)
        {
            if (v < 0.0)
            {
                throw new ArgumentException("maxCFSSegmentSizeMB must be >=0 (got " + v + ")");
            }
            v *= 1024 * 1024;
            this.maxCFSSegmentSize = (v > long.MaxValue) ? long.MaxValue : (long)v;
            return this;
        }
    }
}
