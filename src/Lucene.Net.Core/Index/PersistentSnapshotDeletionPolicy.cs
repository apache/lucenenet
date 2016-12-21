using Lucene.Net.Support;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using System.IO;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements. See the NOTICE file distributed with this
         * work for additional information regarding copyright ownership. The ASF
         * licenses this file to You under the Apache License, Version 2.0 (the
         * "License"); you may not use this file except in compliance with the License.
         * You may obtain a copy of the License at
         *
         * http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
         * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
         * License for the specific language governing permissions and limitations under
         * the License.
         */

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;

    /// <summary>
    /// A <seealso cref="SnapshotDeletionPolicy"/> which adds a persistence layer so that
    /// snapshots can be maintained across the life of an application. The snapshots
    /// are persisted in a <seealso cref="Directory"/> and are committed as soon as
    /// <seealso cref="#snapshot()"/> or <seealso cref="#release(IndexCommit)"/> is called.
    /// <p>
    /// <b>NOTE:</b> Sharing <seealso cref="PersistentSnapshotDeletionPolicy"/>s that write to
    /// the same directory across <seealso cref="IndexWriter"/>s will corrupt snapshots. You
    /// should make sure every <seealso cref="IndexWriter"/> has its own
    /// <seealso cref="PersistentSnapshotDeletionPolicy"/> and that they all write to a
    /// different <seealso cref="Directory"/>.  It is OK to use the same
    /// Directory that holds the index.
    ///
    /// <p> this class adds a <seealso cref="#release(long)"/> method to
    /// release commits from a previous snapshot's <seealso cref="IndexCommit#getGeneration"/>.
    ///
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
        /// <seealso cref="PersistentSnapshotDeletionPolicy"/> wraps another
        /// <seealso cref="IndexDeletionPolicy"/> to enable flexible
        /// snapshotting, passing <seealso cref="OpenMode#CREATE_OR_APPEND"/>
        /// by default.
        /// </summary>
        /// <param name="primary">
        ///          the <seealso cref="IndexDeletionPolicy"/> that is used on non-snapshotted
        ///          commits. Snapshotted commits, by definition, are not deleted until
        ///          explicitly released via <seealso cref="#release"/>. </param>
        /// <param name="dir">
        ///          the <seealso cref="Directory"/> which will be used to persist the snapshots
        ///          information. </param>
        public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir)
            : this(primary, dir, OpenMode.CREATE_OR_APPEND)
        {
        }

        /// <summary>
        /// <seealso cref="PersistentSnapshotDeletionPolicy"/> wraps another
        /// <seealso cref="IndexDeletionPolicy"/> to enable flexible snapshotting.
        /// </summary>
        /// <param name="primary">
        ///          the <seealso cref="IndexDeletionPolicy"/> that is used on non-snapshotted
        ///          commits. Snapshotted commits, by definition, are not deleted until
        ///          explicitly released via <seealso cref="#release"/>. </param>
        /// <param name="dir">
        ///          the <seealso cref="Directory"/> which will be used to persist the snapshots
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
        /// <seealso cref= SnapshotDeletionPolicy#snapshot </seealso>
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
                        catch (Exception e)
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
        /// <seealso cref= SnapshotDeletionPolicy#release </seealso>
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
                        catch (Exception e)
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
        /// <seealso cref= IndexCommit#getGeneration </seealso>
        /// <seealso cref= SnapshotDeletionPolicy#release </seealso>
        public virtual void Release(long gen)
        {
            lock (this)
            {
                base.ReleaseGen(gen);
                Persist();
            }
        }

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
                    @out.WriteVInt(refCounts.Count);
                    foreach (KeyValuePair<long, int> ent in refCounts)
                    {
                        @out.WriteVLong(ent.Key);
                        @out.WriteVInt(ent.Value);
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(@out);
                        try
                        {
                            dir.DeleteFile(fileName);
                        }
                        catch (Exception e)
                        {
                            // Suppress so we keep throwing original exception
                        }
                    }
                    else
                    {
                        IOUtils.Close(@out);
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
                    catch (IOException ioe)
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
                    if (file.StartsWith(SNAPSHOTS_PREFIX))
                    {
                        dir.DeleteFile(file);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the file name the snapshots are currently
        ///  saved to, or null if no snapshots have been saved.
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
        /// Reads the snapshots information from the given <seealso cref="Directory"/>. this
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
                    if (file.StartsWith(SNAPSHOTS_PREFIX))
                    {
                        long gen = Convert.ToInt64(file.Substring(SNAPSHOTS_PREFIX.Length));
                        if (genLoaded == -1 || gen > genLoaded)
                        {
                            snapshotFiles.Add(file);
                            IDictionary<long, int> m = new Dictionary<long, int>();
                            IndexInput @in = dir.OpenInput(file, IOContext.DEFAULT);
                            try
                            {
                                CodecUtil.CheckHeader(@in, CODEC_NAME, VERSION_START, VERSION_START);
                                int count = @in.ReadVInt();
                                for (int i = 0; i < count; i++)
                                {
                                    long commitGen = @in.ReadVLong();
                                    int refCount = @in.ReadVInt();
                                    m[commitGen] = refCount;
                                }
                            }
                            catch (IOException ioe2)
                            {
                                // Save first exception & throw in the end
                                if (ioe == null)
                                {
                                    ioe = ioe2;
                                }
                            }
                            finally
                            {
                                @in.Dispose();
                            }

                            genLoaded = gen;
                            refCounts.Clear();
                            refCounts.PutAll(m);
                        }
                    }
                }

                if (genLoaded == -1)
                {
                    // Nothing was loaded...
                    if (ioe != null)
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
                            if (!curFileName.Equals(file))
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