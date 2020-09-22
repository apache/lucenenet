using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

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
        public static readonly string SNAPSHOTS_PREFIX = "snapshots_";

        private static readonly int VERSION_START = 0;
        private static readonly int VERSION_CURRENT = VERSION_START;
        private static readonly string CODEC_NAME = "snapshots";

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
                throw new InvalidOperationException("no snapshots stored in this directory");
            }
        }

        /// <summary>
        /// Snapshots the last commit. Once this method returns, the
        /// snapshot information is persisted in the directory.
        /// </summary>
        /// <seealso cref="SnapshotDeletionPolicy.Snapshot()"/>
        public override IndexCommit Snapshot()
        {
            lock (this)
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
#pragma warning disable 168
                        catch (Exception e)
#pragma warning restore 168
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                }
                return ic;
            }
        }

        /// <summary>
        /// Deletes a snapshotted commit. Once this method returns, the snapshot
        /// information is persisted in the directory.
        /// </summary>
        /// <seealso cref="SnapshotDeletionPolicy.Release(IndexCommit)"/>
        public override void Release(IndexCommit commit)
        {
            lock (this)
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
#pragma warning disable 168
                        catch (Exception e)
#pragma warning restore 168
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                }
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
            lock (this)
            {
                base.ReleaseGen(gen);
                Persist();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal void Persist()
        {
            lock (this)
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
#pragma warning disable 168
                        catch (Exception e)
#pragma warning restore 168
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
#pragma warning disable 168
                    catch (IOException ioe)
#pragma warning restore 168
                    {
                        // OK: likely it didn't exist
                    }
                }

                nextWriteGen++;
            }
        }

        private void ClearPriorSnapshots()
        {
            lock (this)
            {
                foreach (string file in dir.ListAll())
                {
                    if (file.StartsWith(SNAPSHOTS_PREFIX, StringComparison.Ordinal))
                    {
                        dir.DeleteFile(file);
                    }
                }
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
            lock (this)
            {
                long genLoaded = -1;
                IOException ioe = null;
                IList<string> snapshotFiles = new List<string>();
                foreach (string file in dir.ListAll())
                {
                    if (file.StartsWith(SNAPSHOTS_PREFIX, StringComparison.Ordinal))
                    {
                        long gen = Convert.ToInt64(file.Substring(SNAPSHOTS_PREFIX.Length), CultureInfo.InvariantCulture);
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
                            catch (IOException ioe2)
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
                    if (ioe is object)
                    {
                        // ... not for lack of trying:
                        throw ioe;
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
        }
    }
}