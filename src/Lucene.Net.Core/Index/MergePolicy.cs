using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Util;

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

    using Directory = Lucene.Net.Store.Directory;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using MergeInfo = Lucene.Net.Store.MergeInfo;

    //using AlreadySetException = Lucene.Net.Util.SetOnce.AlreadySetException;

    /// <summary>
    /// <p>Expert: a MergePolicy determines the sequence of
    /// primitive merge operations.</p>
    ///
    /// <p>Whenever the segments in an index have been altered by
    /// <seealso cref="IndexWriter"/>, either the addition of a newly
    /// flushed segment, addition of many segments from
    /// addIndexes* calls, or a previous merge that may now need
    /// to cascade, <seealso cref="IndexWriter"/> invokes {@link
    /// #findMerges} to give the MergePolicy a chance to pick
    /// merges that are now required.  this method returns a
    /// <seealso cref="MergeSpecification"/> instance describing the set of
    /// merges that should be done, or null if no merges are
    /// necessary.  When IndexWriter.forceMerge is called, it calls
    /// <seealso cref="#findForcedMerges(SegmentInfos,int,Map)"/> and the MergePolicy should
    /// then return the necessary merges.</p>
    ///
    /// <p>Note that the policy can return more than one merge at
    /// a time.  In this case, if the writer is using {@link
    /// SerialMergeScheduler}, the merges will be run
    /// sequentially but if it is using {@link
    /// ConcurrentMergeScheduler} they will be run concurrently.</p>
    ///
    /// <p>The default MergePolicy is {@link
    /// TieredMergePolicy}.</p>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class MergePolicy : IDisposable
    {
        /// <summary>
        /// A map of doc IDs. </summary>
        public abstract class DocMap
        {
            /// <summary>
            /// Sole constructor, typically invoked from sub-classes constructors. </summary>
            protected internal DocMap()
            {
            }

            /// <summary>
            /// Return the new doc ID according to its old value. </summary>
            public abstract int Map(int old);

            /// <summary>
            /// Useful from an assert. </summary>
            internal virtual bool IsConsistent(int maxDoc)
            {
                FixedBitSet targets = new FixedBitSet(maxDoc);
                for (int i = 0; i < maxDoc; ++i)
                {
                    int target = Map(i);
                    if (target < 0 || target >= maxDoc)
                    {
                        Debug.Assert(false, "out of range: " + target + " not in [0-" + maxDoc + "[");
                        return false;
                    }
                    else if (targets.Get(target))
                    {
                        Debug.Assert(false, target + " is already taken (" + i + ")");
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// OneMerge provides the information necessary to perform
        ///  an individual primitive merge operation, resulting in
        ///  a single new segment.  The merge spec includes the
        ///  subset of segments to be merged as well as whether the
        ///  new segment should use the compound file format.
        /// </summary>

        public class OneMerge
        {
            internal SegmentCommitInfo info; // used by IndexWriter
            internal bool RegisterDone; // used by IndexWriter
            internal long MergeGen; // used by IndexWriter
            internal bool IsExternal; // used by IndexWriter
            public int MaxNumSegments = -1; // used by IndexWriter

            /// <summary>
            /// Estimated size in bytes of the merged segment. </summary>
            public long EstimatedMergeBytes; // used by IndexWriter

            // Sum of sizeInBytes of all SegmentInfos; set by IW.mergeInit
            internal long TotalMergeBytes;

            internal IList<SegmentReader> Readers; // used by IndexWriter

            /// <summary>
            /// Segments to be merged. </summary>
            public readonly IList<SegmentCommitInfo> Segments;

            /// <summary>
            /// Number of documents in the merged segment. </summary>
            public readonly int TotalDocCount;

            internal bool Aborted_Renamed;
            internal Exception Error;
            internal bool Paused;

            /// <summary>
            /// Sole constructor. </summary>
            /// <param name="segments"> List of <seealso cref="SegmentCommitInfo"/>s
            ///        to be merged.  </param>
            public OneMerge(IList<SegmentCommitInfo> segments)
            {
                if (0 == segments.Count)
                {
                    throw new Exception("segments must include at least one segment");
                }
                // clone the list, as the in list may be based off original SegmentInfos and may be modified
                this.Segments = new List<SegmentCommitInfo>(segments);
                int count = 0;
                foreach (SegmentCommitInfo info in segments)
                {
                    count += info.Info.DocCount;
                }
                TotalDocCount = count;
            }

            /// <summary>
            /// Expert: Get the list of readers to merge. Note that this list does not
            ///  necessarily match the list of segments to merge and should only be used
            ///  to feed SegmentMerger to initialize a merge. When a <seealso cref="OneMerge"/>
            ///  reorders doc IDs, it must override <seealso cref="#getDocMap"/> too so that
            ///  deletes that happened during the merge can be applied to the newly
            ///  merged segment.
            /// </summary>
            public virtual IList<AtomicReader> MergeReaders
            {
                get
                {
                    if (Readers == null)
                    {
                        throw new InvalidOperationException("IndexWriter has not initialized readers from the segment infos yet");
                    }
                    IList<AtomicReader> readers = new List<AtomicReader>(this.Readers.Count);
                    foreach (AtomicReader reader in this.Readers)
                    {
                        if (reader.NumDocs > 0)
                        {
                            readers.Add(reader);
                        }
                    }
                    return readers;
                }
            }

            /// <summary>
            /// Expert: Sets the <seealso cref="SegmentCommitInfo"/> of this <seealso cref="OneMerge"/>.
            /// Allows sub-classes to e.g. set diagnostics properties.
            /// </summary>
            public virtual SegmentCommitInfo Info
            {
                set
                {
                    this.info = value;
                }
                get
                {
                    return info;
                }
            }

            /// <summary>
            /// Expert: If <seealso cref="#getMergeReaders()"/> reorders document IDs, this method
            ///  must be overridden to return a mapping from the <i>natural</i> doc ID
            ///  (the doc ID that would result from a natural merge) to the actual doc
            ///  ID. this mapping is used to apply deletions that happened during the
            ///  merge to the new segment.
            /// </summary>
            public virtual DocMap GetDocMap(MergeState mergeState)
            {
                return new DocMapAnonymousInnerClassHelper(this);
            }

            private class DocMapAnonymousInnerClassHelper : DocMap
            {
                private readonly OneMerge OuterInstance;

                public DocMapAnonymousInnerClassHelper(OneMerge outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override int Map(int docID)
                {
                    return docID;
                }
            }

            /// <summary>
            /// Record that an exception occurred while executing
            ///  this merge
            /// </summary>
            internal virtual Exception Exception
            {
                set
                {
                    lock (this)
                    {
                        this.Error = value;
                    }
                }
                get
                {
                    lock (this)
                    {
                        return Error;
                    }
                }
            }

            /// <summary>
            /// Mark this merge as aborted.  If this is called
            ///  before the merge is committed then the merge will
            ///  not be committed.
            /// </summary>
            internal virtual void Abort()
            {
                lock (this)
                {
                    Aborted_Renamed = true;
                    Monitor.PulseAll(this);
                }
            }

            /// <summary>
            /// Returns true if this merge was aborted. </summary>
            internal virtual bool Aborted
            {
                get
                {
                    lock (this)
                    {
                        return Aborted_Renamed;
                    }
                }
            }

            /// <summary>
            /// Called periodically by <seealso cref="IndexWriter"/> while
            ///  merging to see if the merge is aborted.
            /// </summary>
            public virtual void CheckAborted(Directory dir)
            {
                lock (this)
                {
                    if (Aborted_Renamed)
                    {
                        throw new MergeAbortedException("merge is aborted: " + SegString(dir));
                    }

                    while (Paused)
                    {
#if !NETSTANDARD
                        try
                        {
#endif
                            // In theory we could wait() indefinitely, but we
                            // do 1000 msec, defensively
                            Monitor.Wait(this, TimeSpan.FromMilliseconds(1000));
#if !NETSTANDARD
                        }
                        catch (ThreadInterruptedException ie)
                        {
                            throw new Exception(ie.Message, ie);
                        }
#endif
                        if (Aborted_Renamed)
                        {
                            throw new MergeAbortedException("merge is aborted: " + SegString(dir));
                        }
                    }
                }
            }

            /// <summary>
            /// Set or clear whether this merge is paused paused (for example
            ///  <seealso cref="ConcurrentMergeScheduler"/> will pause merges
            ///  if too many are running).
            /// </summary>
            public virtual bool Pause
            {
                set
                {
                    lock (this)
                    {
                        this.Paused = value;
                        if (!value)
                        {
                            // Wakeup merge thread, if it's waiting
                            Monitor.PulseAll(this);
                        }
                    }
                }
                get
                {
                    lock (this)
                    {
                        return Paused;
                    }
                }
            }

            /// <summary>
            /// Returns a readable description of the current merge
            ///  state.
            /// </summary>
            public virtual string SegString(Directory dir)
            {
                StringBuilder b = new StringBuilder();
                int numSegments = Segments.Count;
                for (int i = 0; i < numSegments; i++)
                {
                    if (i > 0)
                    {
                        b.Append(' ');
                    }
                    b.Append(Segments[i].ToString(dir, 0));
                }
                if (info != null)
                {
                    b.Append(" into ").Append(info.Info.Name);
                }
                if (MaxNumSegments != -1)
                {
                    b.Append(" [maxNumSegments=" + MaxNumSegments + "]");
                }
                if (Aborted_Renamed)
                {
                    b.Append(" [ABORTED]");
                }
                return b.ToString();
            }

            /// <summary>
            /// Returns the total size in bytes of this merge. Note that this does not
            /// indicate the size of the merged segment, but the
            /// input total size. this is only set once the merge is
            /// initialized by IndexWriter.
            /// </summary>
            public virtual long TotalBytesSize()
            {
                return TotalMergeBytes;
            }

            /// <summary>
            /// Returns the total number of documents that are included with this merge.
            /// Note that this does not indicate the number of documents after the merge.
            ///
            /// </summary>
            public virtual int TotalNumDocs()
            {
                int total = 0;
                foreach (SegmentCommitInfo info in Segments)
                {
                    total += info.Info.DocCount;
                }
                return total;
            }

            /// <summary>
            /// Return <seealso cref="MergeInfo"/> describing this merge. </summary>
            public virtual MergeInfo MergeInfo
            {
                get
                {
                    return new MergeInfo(TotalDocCount, EstimatedMergeBytes, IsExternal, MaxNumSegments);
                }
            }
        }

        /// <summary>
        /// A MergeSpecification instance provides the information
        /// necessary to perform multiple merges.  It simply
        /// contains a list of <seealso cref="OneMerge"/> instances.
        /// </summary>

        public class MergeSpecification
        {
            /// <summary>
            /// The subset of segments to be included in the primitive merge.
            /// </summary>

            public readonly IList<OneMerge> Merges = new List<OneMerge>();

            /// <summary>
            /// Sole constructor.  Use {@link
            ///  #add(MergePolicy.OneMerge)} to add merges.
            /// </summary>
            public MergeSpecification()
            {
            }

            /// <summary>
            /// Adds the provided <seealso cref="OneMerge"/> to this
            ///  specification.
            /// </summary>
            public virtual void Add(OneMerge merge)
            {
                Merges.Add(merge);
            }

            /// <summary>
            /// Returns a description of the merges in this
            ///  specification.
            /// </summary>
            public virtual string SegString(Directory dir)
            {
                StringBuilder b = new StringBuilder();
                b.Append("MergeSpec:\n");
                int count = Merges.Count;
                for (int i = 0; i < count; i++)
                {
                    b.Append("  ").Append(1 + i).Append(": ").Append(Merges[i].SegString(dir));
                }
                return b.ToString();
            }
        }

        /// <summary>
        /// Exception thrown if there are any problems while
        ///  executing a merge.
        /// </summary>
        // LUCENENET: All exeption classes should be marked serializable
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
        public class MergeException : Exception
        {
            internal Directory Dir;

            /// <summary>
            /// Create a {@code MergeException}. </summary>
            public MergeException(string message, Directory dir)
                : base(message)
            {
                this.Dir = dir;
            }

            /// <summary>
            /// Create a {@code MergeException}. </summary>
            public MergeException(Exception exc, Directory dir)
                : base(exc.Message)
            {
                this.Dir = dir;
            }

            /// <summary>
            /// Returns the <seealso cref="Directory"/> of the index that hit
            ///  the exception.
            /// </summary>
            public virtual Directory Directory
            {
                get
                {
                    return Dir;
                }
            }
        }

        /// <summary>
        /// Thrown when a merge was explicity aborted because
        ///  <seealso cref="IndexWriter#close(boolean)"/> was called with
        ///  <code>false</code>.  Normally this exception is
        ///  privately caught and suppresed by <seealso cref="IndexWriter"/>.
        /// </summary>
        public class MergeAbortedException : System.IO.IOException
        {
            /// <summary>
            /// Create a <seealso cref="MergeAbortedException"/>. </summary>
            public MergeAbortedException()
                : base("merge is aborted")
            {
            }

            /// <summary>
            /// Create a <seealso cref="MergeAbortedException"/> with a
            ///  specified message.
            /// </summary>
            public MergeAbortedException(string message)
                : base(message)
            {
            }
        }

        /// <summary>
        /// Default ratio for compound file system usage. Set to <tt>1.0</tt>, always use
        /// compound file system.
        /// </summary>
        protected internal const double DEFAULT_NO_CFS_RATIO = 1.0;

        /// <summary>
        /// Default max segment size in order to use compound file system. Set to <seealso cref="Long#MAX_VALUE"/>.
        /// </summary>
        protected internal static readonly long DEFAULT_MAX_CFS_SEGMENT_SIZE = long.MaxValue;

        /// <summary>
        /// <seealso cref="IndexWriter"/> that contains this instance. </summary>
        protected internal SetOnce<IndexWriter> Writer;

        /// <summary>
        /// If the size of the merge segment exceeds this ratio of
        ///  the total index size then it will remain in
        ///  non-compound format
        /// </summary>
        protected internal double NoCFSRatio_Renamed = DEFAULT_NO_CFS_RATIO;

        /// <summary>
        /// If the size of the merged segment exceeds
        ///  this value then it will not use compound file format.
        /// </summary>
        protected internal long MaxCFSSegmentSize = DEFAULT_MAX_CFS_SEGMENT_SIZE;

        public virtual object Clone()
        {
            MergePolicy clone;
            clone = (MergePolicy)base.MemberwiseClone();

            clone.Writer = new SetOnce<IndexWriter>();
            return clone;
        }

        /// <summary>
        /// Creates a new merge policy instance. Note that if you intend to use it
        /// without passing it to <seealso cref="IndexWriter"/>, you should call
        /// <seealso cref="#setIndexWriter(IndexWriter)"/>.
        /// </summary>
        public MergePolicy()
            : this(DEFAULT_NO_CFS_RATIO, DEFAULT_MAX_CFS_SEGMENT_SIZE)
        {
        }

        /// <summary>
        /// Creates a new merge policy instance with default settings for noCFSRatio
        /// and maxCFSSegmentSize. this ctor should be used by subclasses using different
        /// defaults than the <seealso cref="MergePolicy"/>
        /// </summary>
        protected internal MergePolicy(double defaultNoCFSRatio, long defaultMaxCFSSegmentSize)
        {
            Writer = new SetOnce<IndexWriter>();
            this.NoCFSRatio_Renamed = defaultNoCFSRatio;
            this.MaxCFSSegmentSize = defaultMaxCFSSegmentSize;
        }

        /// <summary>
        /// Sets the <seealso cref="IndexWriter"/> to use by this merge policy. this method is
        /// allowed to be called only once, and is usually set by IndexWriter. If it is
        /// called more than once, <seealso cref="AlreadySetException"/> is thrown.
        /// </summary>
        /// <seealso cref= SetOnce </seealso>
        public virtual IndexWriter IndexWriter
        {
            set
            {
                this.Writer.Set(value);
            }
        }

        /// <summary>
        /// Determine what set of merge operations are now necessary on the index.
        /// <seealso cref="IndexWriter"/> calls this whenever there is a change to the segments.
        /// this call is always synchronized on the <seealso cref="IndexWriter"/> instance so
        /// only one thread at a time will call this method. </summary>
        /// <param name="mergeTrigger"> the event that triggered the merge </param>
        /// <param name="segmentInfos">
        ///          the total set of segments in the index </param>
        public abstract MergeSpecification FindMerges(MergeTrigger? mergeTrigger, SegmentInfos segmentInfos);

        /// <summary>
        /// Determine what set of merge operations is necessary in
        /// order to merge to <= the specified segment count. <seealso cref="IndexWriter"/> calls this when its
        /// <seealso cref="IndexWriter#forceMerge"/> method is called. this call is always
        /// synchronized on the <seealso cref="IndexWriter"/> instance so only one thread at a
        /// time will call this method.
        /// </summary>
        /// <param name="segmentInfos">
        ///          the total set of segments in the index </param>
        /// <param name="maxSegmentCount">
        ///          requested maximum number of segments in the index (currently this
        ///          is always 1) </param>
        /// <param name="segmentsToMerge">
        ///          contains the specific SegmentInfo instances that must be merged
        ///          away. this may be a subset of all
        ///          SegmentInfos.  If the value is True for a
        ///          given SegmentInfo, that means this segment was
        ///          an original segment present in the
        ///          to-be-merged index; else, it was a segment
        ///          produced by a cascaded merge. </param>
        public abstract MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool?> segmentsToMerge);

        /// <summary>
        /// Determine what set of merge operations is necessary in order to expunge all
        /// deletes from the index.
        /// </summary>
        /// <param name="segmentInfos">
        ///          the total set of segments in the index </param>
        public abstract MergeSpecification FindForcedDeletesMerges(SegmentInfos segmentInfos);

        /// <summary>
        /// Release all resources for the policy.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Returns true if a new segment (regardless of its origin) should use the
        /// compound file format. The default implementation returns <code>true</code>
        /// iff the size of the given mergedInfo is less or equal to
        /// <seealso cref="#getMaxCFSSegmentSizeMB()"/> and the size is less or equal to the
        /// TotalIndexSize * <seealso cref="#getNoCFSRatio()"/> otherwise <code>false</code>.
        /// </summary>
        public virtual bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo)
        {
            if (NoCFSRatio == 0.0)
            {
                return false;
            }
            long mergedInfoSize = Size(mergedInfo);
            if (mergedInfoSize > MaxCFSSegmentSize)
            {
                return false;
            }
            if (NoCFSRatio >= 1.0)
            {
                return true;
            }
            long totalSize = 0;
            foreach (SegmentCommitInfo info in infos.Segments)
            {
                totalSize += Size(info);
            }
            return mergedInfoSize <= NoCFSRatio * totalSize;
        }

        /// <summary>
        /// Return the byte size of the provided {@link
        ///  SegmentCommitInfo}, pro-rated by percentage of
        ///  non-deleted documents is set.
        /// </summary>
        protected internal virtual long Size(SegmentCommitInfo info)
        {
            long byteSize = info.SizeInBytes();
            int delCount = Writer.Get().NumDeletedDocs(info);
            double delRatio = (info.Info.DocCount <= 0 ? 0.0f : ((float)delCount / (float)info.Info.DocCount));
            Debug.Assert(delRatio <= 1.0);
            return (info.Info.DocCount <= 0 ? byteSize : (long)(byteSize * (1.0 - delRatio)));
        }

        /// <summary>
        /// Returns true if this single info is already fully merged (has no
        ///  pending deletes, is in the same dir as the
        ///  writer, and matches the current compound file setting
        /// </summary>
        protected internal bool IsMerged(SegmentInfos infos, SegmentCommitInfo info)
        {
            IndexWriter w = Writer.Get();
            Debug.Assert(w != null);
            bool hasDeletions = w.NumDeletedDocs(info) > 0;
            return !hasDeletions && !info.Info.HasSeparateNorms() && info.Info.Dir == w.Directory && UseCompoundFile(infos, info) == info.Info.UseCompoundFile;
        }

        /// <summary>
        /// Returns current {@code noCFSRatio}.
        /// </summary>
        ///  <seealso cref= #setNoCFSRatio  </seealso>
        public virtual double NoCFSRatio
        {
            get
            {
                return NoCFSRatio_Renamed;
            }
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new System.ArgumentException("noCFSRatio must be 0.0 to 1.0 inclusive; got " + value);
                }
                this.NoCFSRatio_Renamed = value;
            }
        }

        /// <summary>
        /// Returns the largest size allowed for a compound file segment </summary>
        public virtual double MaxCFSSegmentSizeMB
        {
            get
            {
                return MaxCFSSegmentSize / 1024 / 1024.0;
            }
            set
            {
                if (value < 0.0)
                {
                    throw new System.ArgumentException("maxCFSSegmentSizeMB must be >=0 (got " + value + ")");
                }
                value *= 1024 * 1024;
                this.MaxCFSSegmentSize = (value > long.MaxValue) ? long.MaxValue : (long)value;
            }
        }
    }
}