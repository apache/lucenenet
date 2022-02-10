using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using JCG = J2N.Collections.Generic;
using Long = J2N.Numerics.Int64;

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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;

    /// <summary>
    /// A <see cref="SnapshotDeletionPolicy"/> which adds a persistence layer so that
    /// snapshots can be maintained across the life of an application. The snapshots
    /// are persisted in a <see cref="Directory"/> and are committed as soon as
    /// <see cref="Snapshot()"/> or <see cref="Release(IndexCommit)"/> is called.
    /// <para/>
    /// <b>NOTE:</b> Sharing <see cref="PersistentSnapshotDeletionPolicy"/>s that write to
    /// the same directory across <see cref="IndexWriter"/>s will corrupt snapshots. You
    /// should make sure every <see cref="IndexWriter"/> has its own
    /// <see cref="PersistentSnapshotDeletionPolicy"/> and that they all write to a
    /// different <see cref="Directory"/>.  It is OK to use the same
    /// <see cref="Directory"/> that holds the index.
    ///
    /// <para/> This class adds a <see cref="Release(long)"/> method to
    /// release commits from a previous snapshot's <see cref="IndexCommit.Generation"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class PersistentSnapshotDeletionPolicy : SnapshotDeletionPolicy
    {
        /// <summary>
        /// Prefix used for the save file. </summary>
        public const string SNAPSHOTS_PREFIX = "snapshots_";

        private const int VERSION_START = 0;
        private const int VERSION_CURRENT = VERSION_START;
        private const string CODEC_NAME = "snapshots";

        // The index writer which maintains the snapshots metadata
        private long nextWriteGen;

        private readonly Directory dir;

        /// <summary>
        /// <see cref="PersistentSnapshotDeletionPolicy"/> wraps another
        /// <see cref="IndexDeletionPolicy"/> to enable flexible
        /// snapshotting, passing <see cref="OpenMode.CREATE_OR_APPEND"/>
        /// by default.
        /// </summary>
        /// <param name="primary">
        ///          the <see cref="IndexDeletionPolicy"/> that is used on non-snapshotted
        ///          commits. Snapshotted commits, by definition, are not deleted until
        ///          explicitly released via <see cref="Release(IndexCommit)"/>. </param>
        /// <param name="dir">
        ///          the <see cref="Directory"/> which will be used to persist the snapshots
        ///          information. </param>
        public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir)
            : this(primary, dir, OpenMode.CREATE_OR_APPEND)
        {
        }

        /// <summary>
        /// <see cref="PersistentSnapshotDeletionPolicy"/> wraps another
        /// <see cref="IndexDeletionPolicy"/> to enable flexible snapshotting.
        /// </summary>
        /// <param name="primary">
        ///          the <see cref="IndexDeletionPolicy"/> that is used on non-snapshotted
        ///          commits. Snapshotted commits, by definition, are not deleted until
        ///          explicitly released via <see cref="Release(IndexCommit)"/>. </param>
        /// <param name="dir">
        ///          the <see cref="Directory"/> which will be used to persist the snapshots
        ///          information. </param>
        /// <param name="mode">
        ///          specifies whether a new index should be created, deleting all
        ///          existing snapshots information (immediately), or open an existing
        ///          index, initializing the class with the snapshots information. </param>
        public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir, OpenMode mode)
            : base(primary)
        {
            this.dir = dir;

            if (mode == OpenMode.CREATE)
            {
                ClearPriorSnapshots();
            }

            LoadPriorSnapshots();

            if (mode == OpenMode.APPEND && nextWriteGen == 0)
            {
                throw IllegalStateException.Create("no snapshots stored in this directory");
            }
        }

        /// <summary>
        /// Snapshots the last commit. Once this method returns, the
        /// snapshot information is persisted in the directory.
        /// </summary>
        /// <seealso cref="SnapshotDeletionPolicy.Snapshot()"/>
        public override IndexCommit Snapshot()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                IndexCommit ic = base.Snapshot();
                bool success = false;
                try
                {
                    Persist();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            base.Release(ic);
                        }
                        catch (Exception e) when (e.IsException())
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                }
                return ic;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Deletes a snapshotted commit. Once this method returns, the snapshot
        /// information is persisted in the directory.
        /// </summary>
        /// <seealso cref="SnapshotDeletionPolicy.Release(IndexCommit)"/>
        public override void Release(IndexCommit commit)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                base.Release(commit);
                bool success = false;
                try
                {
                    Persist();
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            IncRef(commit);
                        }
                        catch (Exception e) when (e.IsException())
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Deletes a snapshotted commit by generation. Once this method returns, the snapshot
        /// information is persisted in the directory.
        /// </summary>
        /// <seealso cref="IndexCommit.Generation"/>
        /// <seealso cref="SnapshotDeletionPolicy.Release(IndexCommit)"/>
        public virtual void Release(long gen)
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                base.ReleaseGen(gen);
                Persist();
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Persist()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                string fileName = SNAPSHOTS_PREFIX + nextWriteGen;
                IndexOutput @out = dir.CreateOutput(fileName, IOContext.DEFAULT);
                bool success = false;
                try
                {
                    CodecUtil.WriteHeader(@out, CODEC_NAME, VERSION_CURRENT);
                    @out.WriteVInt32(m_refCounts.Count);
                    foreach (KeyValuePair<long, int> ent in m_refCounts)
                    {
                        @out.WriteVInt64(ent.Key);
                        @out.WriteVInt32(ent.Value);
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(@out);
                        try
                        {
                            dir.DeleteFile(fileName);
                        }
                        catch (Exception e) when (e.IsException())
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                    else
                    {
                        IOUtils.Dispose(@out);
                    }
                }

                dir.Sync(/*Collections.singletonList(*/new[] { fileName }/*)*/);

                if (nextWriteGen > 0)
                {
                    string lastSaveFile = SNAPSHOTS_PREFIX + (nextWriteGen - 1);
                    try
                    {
                        dir.DeleteFile(lastSaveFile);
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        // OK: likely it didn't exist
                    }
                }

                nextWriteGen++;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        private void ClearPriorSnapshots()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                foreach (string file in dir.ListAll())
                {
                    if (file.StartsWith(SNAPSHOTS_PREFIX, StringComparison.Ordinal))
                    {
                        dir.DeleteFile(file);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }

        /// <summary>
        /// Returns the file name the snapshots are currently
        /// saved to, or <c>null</c> if no snapshots have been saved.
        /// </summary>
        public virtual string LastSaveFile
        {
            get
            {
                if (nextWriteGen == 0)
                {
                    return null;
                }
                else
                {
                    return SNAPSHOTS_PREFIX + (nextWriteGen - 1);
                }
            }
        }

        /// <summary>
        /// Reads the snapshots information from the given <see cref="Directory"/>. This
        /// method can be used if the snapshots information is needed, however you
        /// cannot instantiate the deletion policy (because e.g., some other process
        /// keeps a lock on the snapshots directory).
        /// </summary>
        private void LoadPriorSnapshots()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                long genLoaded = -1;
                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                IList<string> snapshotFiles = new JCG.List<string>();
                foreach (string file in dir.ListAll())
                {
                    if (file.StartsWith(SNAPSHOTS_PREFIX, StringComparison.Ordinal))
                    {
                        // LUCENENET: Optimized to not allocate a substring during the parse
                        long gen = Long.Parse(file, SNAPSHOTS_PREFIX.Length, file.Length - SNAPSHOTS_PREFIX.Length, radix: 10);
                        if (genLoaded == -1 || gen > genLoaded)
                        {
                            snapshotFiles.Add(file);
                            IDictionary<long, int> m = new Dictionary<long, int>();
                            IndexInput @in = dir.OpenInput(file, IOContext.DEFAULT);
                            try
                            {
                                CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_START);
                                int count = @in.ReadVInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    long commitGen = @in.ReadVInt64();
                                    int refCount = @in.ReadVInt32();
                                    m[commitGen] = refCount;
                                }
                            }
                            catch (Exception ioe2) when (ioe2.IsIOException())
                            {
                                // Save first exception & throw in the end
                                if (ioe is null)
                                {
                                    ioe = ioe2;
                                }
                            }
                            finally
                            {
                                @in.Dispose();
                            }

                            genLoaded = gen;
                            m_refCounts.Clear();
                            m_refCounts.PutAll(m);
                        }
                    }
                }

                if (genLoaded == -1)
                {
                    // Nothing was loaded...
                    if (ioe != null)
                    {
                        // ... not for lack of trying:
                        ExceptionDispatchInfo.Capture(ioe).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                    }
                }
                else
                {
                    if (snapshotFiles.Count > 1)
                    {
                        // Remove any broken / old snapshot files:
                        string curFileName = SNAPSHOTS_PREFIX + genLoaded;
                        foreach (string file in snapshotFiles)
                        {
                            if (!curFileName.Equals(file, StringComparison.Ordinal))
                            {
                                dir.DeleteFile(file);
                            }
                        }
                    }
                    nextWriteGen = 1 + genLoaded;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}