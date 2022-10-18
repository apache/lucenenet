using J2N.Collections.Generic.Extensions;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;

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

    using Directory = Lucene.Net.Store.Directory;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using MergeInfo = Lucene.Net.Store.MergeInfo;

    //using AlreadySetException = Lucene.Net.Util.SetOnce.AlreadySetException;

    /// <summary>
    /// <para>Expert: a <see cref="MergePolicy"/> determines the sequence of
    /// primitive merge operations.</para>
    ///
    /// <para>Whenever the segments in an index have been altered by
    /// <see cref="IndexWriter"/>, either the addition of a newly
    /// flushed segment, addition of many segments from
    /// AddIndexes* calls, or a previous merge that may now need
    /// to cascade, <see cref="IndexWriter"/> invokes <see cref="FindMerges(MergeTrigger, SegmentInfos)"/>
    /// to give the <see cref="MergePolicy"/> a chance to pick
    /// merges that are now required.  This method returns a
    /// <see cref="MergeSpecification"/> instance describing the set of
    /// merges that should be done, or null if no merges are
    /// necessary.  When <see cref="IndexWriter.ForceMerge(int)"/> is called, it calls
    /// <see cref="FindForcedMerges(SegmentInfos, int, IDictionary{SegmentCommitInfo, bool})"/> and the <see cref="MergePolicy"/> should
    /// then return the necessary merges.</para>
    ///
    /// <para>Note that the policy can return more than one merge at
    /// a time.  In this case, if the writer is using 
    /// <see cref="SerialMergeScheduler"/>, the merges will be run
    /// sequentially but if it is using
    /// <see cref="ConcurrentMergeScheduler"/> they will be run concurrently.</para>
    ///
    /// <para>The default MergePolicy is
    /// <see cref="TieredMergePolicy"/>.</para>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class MergePolicy : IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /// <summary>
        /// A map of doc IDs. </summary>
        public abstract class DocMap
        {
            /// <summary>
            /// Sole constructor, typically invoked from sub-classes constructors. </summary>
            protected DocMap()
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
                        if (Debugging.AssertsEnabled) Debugging.Assert(false, "out of range: {0} not in [0-{1}[", target, maxDoc);
                        return false;
                    }
                    else if (targets.Get(target))
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(false,  "{0} is already taken ({1})", target, i);
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
            internal bool registerDone; // used by IndexWriter
            internal long mergeGen; // used by IndexWriter
            internal bool isExternal; // used by IndexWriter

            public int MaxNumSegments // used by IndexWriter
            {
                get => maxNumSegments;
                set => maxNumSegments = value;
            }
            private int maxNumSegments = -1;

            /// <summary>
            /// Estimated size in bytes of the merged segment. </summary>
            public long EstimatedMergeBytes { get; internal set; } // used by IndexWriter // LUCENENET NOTE: original was volatile, but long cannot be in .NET

            // Sum of sizeInBytes of all SegmentInfos; set by IW.mergeInit
            internal long totalMergeBytes; // LUCENENET NOTE: original was volatile, but long cannot be in .NET

            internal IList<SegmentReader> readers; // used by IndexWriter

            /// <summary>
            /// Segments to be merged. </summary>
            public IList<SegmentCommitInfo> Segments { get; private set; }

            /// <summary>
            /// Number of documents in the merged segment. </summary>
            public int TotalDocCount { get; private set; }

            internal bool aborted;
            internal Exception error;
            internal bool paused;

            /// <summary>
            /// Sole constructor. </summary>
            /// <param name="segments"> List of <seealso cref="SegmentCommitInfo"/>s
            ///        to be merged.  </param>
            public OneMerge(IList<SegmentCommitInfo> segments)
            {
                if (0 == segments.Count)
                {
                    throw RuntimeException.Create("segments must include at least one segment");
                }
                // clone the list, as the in list may be based off original SegmentInfos and may be modified
                this.Segments = new JCG.List<SegmentCommitInfo>(segments);
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
            ///  to feed SegmentMerger to initialize a merge. When a <see cref="OneMerge"/>
            ///  reorders doc IDs, it must override <see cref="GetDocMap"/> too so that
            ///  deletes that happened during the merge can be applied to the newly
            ///  merged segment.
            /// </summary>
            public virtual IList<AtomicReader> GetMergeReaders()
            {
                if (this.readers is null)
                {
                    throw IllegalStateException.Create("IndexWriter has not initialized readers from the segment infos yet");
                }
                IList<AtomicReader> readers = new JCG.List<AtomicReader>(this.readers.Count);
                foreach (AtomicReader reader in this.readers)
                {
                    if (reader.NumDocs > 0)
                    {
                        readers.Add(reader);
                    }
                }
                return readers.AsReadOnly();
            }

            /// <summary>
            /// Expert: Sets the <see cref="SegmentCommitInfo"/> of this <see cref="OneMerge"/>.
            /// Allows sub-classes to e.g. set diagnostics properties.
            /// </summary>
            public virtual SegmentCommitInfo Info
            {
                get => info;
                set => this.info = value;
            }

            /// <summary>
            /// Expert: If <see cref="GetMergeReaders()"/> reorders document IDs, this method
            /// must be overridden to return a mapping from the <i>natural</i> doc ID
            /// (the doc ID that would result from a natural merge) to the actual doc
            /// ID. This mapping is used to apply deletions that happened during the
            /// merge to the new segment.
            /// </summary>
            public virtual DocMap GetDocMap(MergeState mergeState)
            {
                return new DocMapAnonymousClass();
            }

            private sealed class DocMapAnonymousClass : DocMap
            {
                public override int Map(int docID)
                {
                    return docID;
                }
            }

            /// <summary>
            /// Record that an exception occurred while executing
            /// this merge
            /// </summary>
            internal virtual Exception Exception
            {
                set
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        this.error = value;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        return error;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            /// <summary>
            /// Mark this merge as aborted.  If this is called
            /// before the merge is committed then the merge will
            /// not be committed.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal virtual void Abort()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    aborted = true;
                    UninterruptableMonitor.PulseAll(this);
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Returns <c>true</c> if this merge was aborted. </summary>
            internal virtual bool IsAborted
            {
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        return aborted;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            /// <summary>
            /// Called periodically by <see cref="IndexWriter"/> while
            /// merging to see if the merge is aborted.
            /// </summary>
            public virtual void CheckAborted(Directory dir)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    if (aborted)
                    {
                        throw new MergeAbortedException("merge is aborted: " + SegString(dir));
                    }

                    while (paused)
                    {
                        try
                        {
                            //In theory we could wait() indefinitely, but we
                            // do 1000 msec, defensively
                            UninterruptableMonitor.Wait(this, TimeSpan.FromMilliseconds(1000));
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            throw RuntimeException.Create(ie);
                        }

                        if (aborted)
                        {
                            throw new MergeAbortedException("merge is aborted: " + SegString(dir));
                        }
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Set or clear whether this merge is paused paused (for example
            /// <see cref="ConcurrentMergeScheduler"/> will pause merges
            /// if too many are running).
            /// </summary>
            internal virtual void SetPause(bool paused)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    this.paused = paused;
                    if (!paused)
                    {
                        // Wakeup merge thread, if it's waiting
                        UninterruptableMonitor.PulseAll(this);
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            /// <summary>
            /// Returns <c>true</c> if this merge is paused.
            /// </summary>
            /// <seealso cref="SetPause(bool)"/>
            internal virtual bool IsPaused
            {
                get
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        return paused;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            /// <summary>
            /// Returns a readable description of the current merge
            /// state.
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
                if (maxNumSegments != -1)
                {
                    b.Append(" [maxNumSegments=" + maxNumSegments + "]");
                }
                if (aborted)
                {
                    b.Append(" [ABORTED]");
                }
                return b.ToString();
            }

            /// <summary>
            /// Returns the total size in bytes of this merge. Note that this does not
            /// indicate the size of the merged segment, but the
            /// input total size. This is only set once the merge is
            /// initialized by <see cref="IndexWriter"/>.
            /// </summary>
            public virtual long TotalBytesSize => totalMergeBytes;

            /// <summary>
            /// Returns the total number of documents that are included with this merge.
            /// Note that this does not indicate the number of documents after the merge.
            /// </summary>
            public virtual int TotalNumDocs
            {
                get
                {
                    int total = 0;
                    foreach (SegmentCommitInfo info in Segments)
                    {
                        total += info.Info.DocCount;
                    }
                    return total;
                }
            }

            /// <summary>
            /// Return <see cref="Store.MergeInfo"/> describing this merge. </summary>
            public virtual MergeInfo MergeInfo => new MergeInfo(TotalDocCount, EstimatedMergeBytes, isExternal, maxNumSegments);
        }

        /// <summary>
        /// A <see cref="MergeSpecification"/> instance provides the information
        /// necessary to perform multiple merges.  It simply
        /// contains a list of <see cref="OneMerge"/> instances.
        /// </summary>

        public class MergeSpecification
        {
            /// <summary>
            /// The subset of segments to be included in the primitive merge.
            /// </summary>
            public IList<OneMerge> Merges { get; private set; }

            /// <summary>
            /// Sole constructor.  Use 
            /// <see cref="Add(OneMerge)"/> to add merges.
            /// </summary>
            public MergeSpecification()
            {
                Merges = new JCG.List<OneMerge>();
            }

            /// <summary>
            /// Adds the provided <see cref="OneMerge"/> to this
            /// specification.
            /// </summary>
            public virtual void Add(OneMerge merge)
            {
                Merges.Add(merge);
            }

            /// <summary>
            /// Returns a description of the merges in this
            /// specification.
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
        /// executing a merge.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        public class MergeException : Exception, IRuntimeException // LUCENENET specific: Added IRuntimeException for identification of the Java superclass in .NET
        {
            private readonly Directory dir; // LUCENENET: marked readonly

            /// <summary>
            /// Create a <see cref="MergeException"/>. </summary>
            public MergeException(string message, Directory dir)
                : base(message)
            {
                this.dir = dir;
            }

            /// <summary>
            /// Create a <see cref="MergeException"/>. </summary>
            public MergeException(Exception exc, Directory dir)
                : base(exc.ToString(), exc)
            {
                this.dir = dir;
            }

            // LUCENENET: For testing purposes
            internal MergeException(string message)
                : base(message)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected MergeException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif

            /// <summary>
            /// Returns the <see cref="Store.Directory"/> of the index that hit
            /// the exception.
            /// </summary>
            public virtual Directory Directory => dir;
        }

        /// <summary>
        /// Thrown when a merge was explicity aborted because
        /// <see cref="IndexWriter.Dispose(bool)"/> was called with
        /// <c>false</c>.  Normally this exception is
        /// privately caught and suppresed by <see cref="IndexWriter"/>.
        /// </summary>
        // LUCENENET: It is no longer good practice to use binary serialization. 
        // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
        [Serializable]
#endif
        public class MergeAbortedException : IOException
        {
            /// <summary>
            /// Create a <see cref="MergeAbortedException"/>. </summary>
            public MergeAbortedException()
                : base("merge is aborted")
            {
            }

            /// <summary>
            /// Create a <see cref="MergeAbortedException"/> with a
            /// specified message.
            /// </summary>
            public MergeAbortedException(string message)
                : base(message)
            {
            }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
            /// <summary>
            /// Initializes a new instance of this class with serialized data.
            /// </summary>
            /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
            /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
            protected MergeAbortedException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
#endif
        }

        /// <summary>
        /// Default ratio for compound file system usage. Set to <c>1.0</c>, always use
        /// compound file system.
        /// </summary>
        protected static readonly double DEFAULT_NO_CFS_RATIO = 1.0;

        /// <summary>
        /// Default max segment size in order to use compound file system. Set to <see cref="long.MaxValue"/>.
        /// </summary>
        protected static readonly long DEFAULT_MAX_CFS_SEGMENT_SIZE = long.MaxValue;

        /// <summary>
        /// <see cref="IndexWriter"/> that contains this instance. </summary>
        protected SetOnce<IndexWriter> m_writer;

        /// <summary>
        /// If the size of the merge segment exceeds this ratio of
        /// the total index size then it will remain in
        /// non-compound format
        /// </summary>
        protected double m_noCFSRatio = DEFAULT_NO_CFS_RATIO;

        /// <summary>
        /// If the size of the merged segment exceeds
        /// this value then it will not use compound file format.
        /// </summary>
        protected long m_maxCFSSegmentSize = DEFAULT_MAX_CFS_SEGMENT_SIZE;

        public virtual object Clone()
        {
            MergePolicy clone;
            clone = (MergePolicy)base.MemberwiseClone();

            clone.m_writer = new SetOnce<IndexWriter>();
            return clone;
        }

        /// <summary>
        /// Creates a new merge policy instance. Note that if you intend to use it
        /// without passing it to <see cref="IndexWriter"/>, you should call
        /// <see cref="SetIndexWriter(IndexWriter)"/>.
        /// </summary>
        protected MergePolicy() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(DEFAULT_NO_CFS_RATIO, DEFAULT_MAX_CFS_SEGMENT_SIZE)
        {
        }

        /// <summary>
        /// Creates a new merge policy instance with default settings for <see cref="m_noCFSRatio"/>
        /// and <see cref="m_maxCFSSegmentSize"/>. This ctor should be used by subclasses using different
        /// defaults than the <see cref="MergePolicy"/>
        /// </summary>
        protected MergePolicy(double defaultNoCFSRatio, long defaultMaxCFSSegmentSize)
        {
            m_writer = new SetOnce<IndexWriter>();
            this.m_noCFSRatio = defaultNoCFSRatio;
            this.m_maxCFSSegmentSize = defaultMaxCFSSegmentSize;
        }

        /// <summary>
        /// Sets the <see cref="IndexWriter"/> to use by this merge policy. This method is
        /// allowed to be called only once, and is usually set by <see cref="IndexWriter"/>. If it is
        /// called more than once, <see cref="AlreadySetException"/> is thrown.
        /// </summary>
        /// <seealso cref="SetOnce{T}"/>
        public virtual void SetIndexWriter(IndexWriter writer)
        {
            this.m_writer.Set(writer);
        }

        /// <summary>
        /// Determine what set of merge operations are now necessary on the index.
        /// <see cref="IndexWriter"/> calls this whenever there is a change to the segments.
        /// This call is always synchronized on the <see cref="IndexWriter"/> instance so
        /// only one thread at a time will call this method. </summary>
        /// <param name="mergeTrigger"> the event that triggered the merge </param>
        /// <param name="segmentInfos">
        ///          the total set of segments in the index </param>
        public abstract MergeSpecification FindMerges(MergeTrigger mergeTrigger, SegmentInfos segmentInfos);

        /// <summary>
        /// Determine what set of merge operations is necessary in
        /// order to merge to &lt;= the specified segment count. <see cref="IndexWriter"/> calls this when its
        /// <see cref="IndexWriter.ForceMerge(int, bool)"/> method is called. This call is always
        /// synchronized on the <see cref="IndexWriter"/> instance so only one thread at a
        /// time will call this method.
        /// </summary>
        /// <param name="segmentInfos">
        ///          The total set of segments in the index </param>
        /// <param name="maxSegmentCount">
        ///          Requested maximum number of segments in the index (currently this
        ///          is always 1) </param>
        /// <param name="segmentsToMerge">
        ///          Contains the specific <see cref="SegmentInfo"/> instances that must be merged
        ///          away. This may be a subset of all
        ///          SegmentInfos.  If the value is <c>true</c> for a
        ///          given <see cref="SegmentInfo"/>, that means this segment was
        ///          an original segment present in the
        ///          to-be-merged index; else, it was a segment
        ///          produced by a cascaded merge. </param>
        public abstract MergeSpecification FindForcedMerges(SegmentInfos segmentInfos, int maxSegmentCount, IDictionary<SegmentCommitInfo, bool> segmentsToMerge);

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
        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Release all resources for the policy.
        /// </summary>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Returns <c>true</c> if a new segment (regardless of its origin) should use the
        /// compound file format. The default implementation returns <c>true</c>
        /// iff the size of the given mergedInfo is less or equal to
        /// <see cref="MaxCFSSegmentSizeMB"/> and the size is less or equal to the
        /// TotalIndexSize * <see cref="NoCFSRatio"/> otherwise <code>false</code>.
        /// </summary>
        public virtual bool UseCompoundFile(SegmentInfos infos, SegmentCommitInfo mergedInfo)
        {
            if (NoCFSRatio == 0.0)
            {
                return false;
            }
            long mergedInfoSize = Size(mergedInfo);
            if (mergedInfoSize > m_maxCFSSegmentSize)
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
        /// Return the byte size of the provided 
        /// <see cref="SegmentCommitInfo"/>, pro-rated by percentage of
        /// non-deleted documents is set.
        /// </summary>
        protected virtual long Size(SegmentCommitInfo info)
        {
            long byteSize = info.GetSizeInBytes();
            int delCount = m_writer.Get().NumDeletedDocs(info);
            double delRatio = (info.Info.DocCount <= 0 ? 0.0f : ((float)delCount / (float)info.Info.DocCount));
            if (Debugging.AssertsEnabled) Debugging.Assert(delRatio <= 1.0);
            return (info.Info.DocCount <= 0 ? byteSize : (long)(byteSize * (1.0 - delRatio)));
        }

        /// <summary>
        /// Returns <c>true</c> if this single info is already fully merged (has no
        /// pending deletes, is in the same dir as the
        /// writer, and matches the current compound file setting
        /// </summary>
        protected bool IsMerged(SegmentInfos infos, SegmentCommitInfo info)
        {
            IndexWriter w = m_writer.Get();
            if (Debugging.AssertsEnabled) Debugging.Assert(w != null);
            bool hasDeletions = w.NumDeletedDocs(info) > 0;
            return !hasDeletions
#pragma warning disable 612, 618
                && !info.Info.HasSeparateNorms
#pragma warning restore 612, 618
                && info.Info.Dir == w.Directory 
                && UseCompoundFile(infos, info) == info.Info.UseCompoundFile;
        }

        /// <summary>
        /// Gets or Sets current <see cref="m_noCFSRatio"/>.
        /// <para/>
        /// If a merged segment will be more than this percentage
        /// of the total size of the index, leave the segment as
        /// non-compound file even if compound file is enabled.
        /// Set to 1.0 to always use CFS regardless of merge
        /// size.
        /// </summary>
        public double NoCFSRatio
        {
            get => m_noCFSRatio;
            set
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(NoCFSRatio), "noCFSRatio must be 0.0 to 1.0 inclusive; got " + value); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.m_noCFSRatio = value;
            }
        }

        /// <summary>
        /// Gets or Sets the largest size allowed for a compound file segment.
        /// <para/>
        /// If a merged segment will be more than this value,
        /// leave the segment as
        /// non-compound file even if compound file is enabled.
        /// Set this to <see cref="double.PositiveInfinity"/> (default) and <see cref="NoCFSRatio"/> to 1.0
        /// to always use CFS regardless of merge size.
        /// </summary>
        public double MaxCFSSegmentSizeMB
        {
            get => m_maxCFSSegmentSize / 1024 / 1024.0;
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxCFSSegmentSizeMB), "maxCFSSegmentSizeMB must be >=0 (got " + value + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                value *= 1024 * 1024;
                this.m_maxCFSSegmentSize = (value > long.MaxValue) ? long.MaxValue : (long)value;
            }
        }
    }
}