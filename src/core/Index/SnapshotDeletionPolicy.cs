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
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{

    /// <summary>A <see cref="IndexDeletionPolicy" /> that wraps around any other
    /// <see cref="IndexDeletionPolicy" /> and adds the ability to hold and
    /// later release a single "snapshot" of an index.  While
    /// the snapshot is held, the <see cref="IndexWriter" /> will not
    /// remove any files associated with it even if the index is
    /// otherwise being actively, arbitrarily changed.  Because
    /// we wrap another arbitrary <see cref="IndexDeletionPolicy" />, this
    /// gives you the freedom to continue using whatever <see cref="IndexDeletionPolicy" />
    /// you would normally want to use with your
    /// index.  Note that you can re-use a single instance of
    /// SnapshotDeletionPolicy across multiple writers as long
    /// as they are against the same index Directory.  Any
    /// snapshot held when a writer is closed will "survive"
    /// when the next writer is opened.
    /// 
    /// <p/><b>WARNING</b>: This API is a new and experimental and
    /// may suddenly change.<p/> 
    /// </summary>

    public class SnapshotDeletionPolicy : IndexDeletionPolicy
    {
        private class SnapshotInfo
        {
            internal string id;
            internal string segmentsFileName;
            internal IndexCommit commit;

            public SnapshotInfo(string id, string segmentsFileName, IndexCommit commit)
            {
                this.id = id;
                this.segmentsFileName = segmentsFileName;
                this.commit = commit;
            }

            public override string ToString()
            {
                return id + " : " + segmentsFileName;
            }
        }

        protected class SnapshotCommitPoint : IndexCommit
        {
            protected IndexCommit cp;
            private readonly SnapshotDeletionPolicy parent;

            public SnapshotCommitPoint(SnapshotDeletionPolicy parent, IndexCommit cp)
            {
                this.parent = parent;
                this.cp = cp;
            }

            public override string ToString()
            {
                return "SnapshotDeletionPolicy.SnapshotCommitPoint(" + cp + ")";
            }

            protected bool ShouldDelete(string segmentsFileName)
            {
                return !parent.segmentsFileToIDs.ContainsKey(segmentsFileName);
            }

            public override void Delete()
            {
                lock (parent)
                {
                    // Suppress the delete request if this commit point is
                    // currently snapshotted.
                    if (ShouldDelete(SegmentsFileName))
                    {
                        cp.Delete();
                    }
                }
            }

            public override Directory Directory
            {
                get { return cp.Directory; }
            }

            public override ICollection<string> FileNames
            {
                get { return cp.FileNames; }
            }

            public override long Generation
            {
                get { return cp.Generation; }
            }

            public override string SegmentsFileName
            {
                get { return cp.SegmentsFileName; }
            }

            public override IDictionary<string, string> UserData
            {
                get { return cp.UserData; }
            }

            public override bool IsDeleted
            {
                get { return cp.IsDeleted; }
            }

            public override int SegmentCount
            {
                get { return cp.SegmentCount; }
            }
        }

        private IDictionary<string, SnapshotInfo> idToSnapshot = new HashMap<string, SnapshotInfo>();

        private IDictionary<string, ISet<string>> segmentsFileToIDs = new HashMap<string, ISet<string>>();

        private IndexDeletionPolicy primary;

        protected IndexCommit lastCommit;

        public SnapshotDeletionPolicy(IndexDeletionPolicy primary)
        {
            this.primary = primary;
        }

        public SnapshotDeletionPolicy(IndexDeletionPolicy primary, IDictionary<string, string> snapshotsInfo)
            : this(primary)
        {
            if (snapshotsInfo != null)
            {
                // Add the ID->segmentIDs here - the actual IndexCommits will be
                // reconciled on the call to onInit()
                foreach (KeyValuePair<string, string> e in snapshotsInfo)
                {
                    RegisterSnapshotInfo(e.Key, e.Value, null);
                }
            }
        }

        protected virtual void CheckSnapshotted(string id)
        {
            if (IsSnapshotted(id))
            {
                throw new InvalidOperationException("Snapshot ID " + id
                    + " is already used - must be unique");
            }
        }

        protected virtual void RegisterSnapshotInfo(string id, String segment, IndexCommit commit)
        {
            idToSnapshot[id] = new SnapshotInfo(id, segment, commit);
            ISet<String> ids = segmentsFileToIDs[segment];
            if (ids == null)
            {
                ids = new HashSet<String>();
                segmentsFileToIDs[segment] = ids;
            }
            ids.Add(id);
        }

        protected virtual IList<IndexCommit> WrapCommits<T>(IList<T> commits)
            where T : IndexCommit
        {
            IList<IndexCommit> wrappedCommits = new List<IndexCommit>(commits.Count);
            foreach (IndexCommit ic in commits)
            {
                wrappedCommits.Add(new SnapshotCommitPoint(this, ic));
            }
            return wrappedCommits;
        }

        public virtual IndexCommit GetSnapshot(string id)
        {
            lock (this)
            {
                SnapshotInfo snapshotInfo = idToSnapshot[id];
                if (snapshotInfo == null)
                {
                    throw new InvalidOperationException("No snapshot exists by ID: " + id);
                }
                return snapshotInfo.commit;
            }
        }

        public virtual IDictionary<string, string> GetSnapshots()
        {
            lock (this)
            {
                IDictionary<string, string> snapshots = new HashMap<string, string>();
                foreach (KeyValuePair<string, SnapshotInfo> e in idToSnapshot)
                {
                    snapshots[e.Key] = e.Value.segmentsFileName;
                }
                return snapshots;
            }
        }

        public virtual bool IsSnapshotted(string id)
        {
            return idToSnapshot.ContainsKey(id);
        }

        public override void OnCommit<T>(IList<T> commits)
        {
            lock (this)
            {
                primary.OnCommit(WrapCommits(commits));
                lastCommit = commits[commits.Count - 1];
            }
        }

        public override void OnInit<T>(IList<T> commits)
        {
            lock (this)
            {
                primary.OnInit(WrapCommits(commits));
                lastCommit = commits[commits.Count - 1];

                /*
                 * Assign snapshotted IndexCommits to their correct snapshot IDs as
                 * specified in the constructor.
                 */
                foreach (IndexCommit commit in commits)
                {
                    ISet<String> ids = segmentsFileToIDs[commit.SegmentsFileName];
                    if (ids != null)
                    {
                        foreach (String id in ids)
                        {
                            idToSnapshot[id].commit = commit;
                        }
                    }
                }

                /*
                 * Second, see if there are any instances where a snapshot ID was specified
                 * in the constructor but an IndexCommit doesn't exist. In this case, the ID
                 * should be removed.
                 * 
                 * Note: This code is protective for extreme cases where IDs point to
                 * non-existent segments. As the constructor should have received its
                 * information via a call to getSnapshots(), the data should be well-formed.
                 */
                // Find lost snapshots
                List<String> idsToRemove = null;
                foreach (KeyValuePair<String, SnapshotInfo> e in idToSnapshot)
                {
                    if (e.Value.commit == null)
                    {
                        if (idsToRemove == null)
                        {
                            idsToRemove = new List<String>();
                        }
                        idsToRemove.Add(e.Key);
                    }
                }
                // Finally, remove those 'lost' snapshots.
                if (idsToRemove != null)
                {
                    foreach (String id in idsToRemove)
                    {
                        SnapshotInfo info = idToSnapshot[id];
                        idToSnapshot.Remove(id);
                        segmentsFileToIDs.Remove(info.segmentsFileName);
                    }
                }
            }
        }

        /// <summary>Release the currently held snapshot. </summary>
        public virtual void Release(string id)
        {
            lock (this)
            {
                SnapshotInfo info = idToSnapshot[id];
                idToSnapshot.Remove(id);
                if (info == null)
                {
                    throw new InvalidOperationException("Snapshot doesn't exist: " + id);
                }
                ISet<String> ids = segmentsFileToIDs[info.segmentsFileName];
                if (ids != null)
                {
                    ids.Remove(id);
                    if (ids.Count == 0)
                    {
                        segmentsFileToIDs.Remove(info.segmentsFileName);
                    }
                }
            }
        }

        public virtual IndexCommit Snapshot(string id)
        {
            lock (this)
            {
                if (lastCommit == null)
                {
                    // no commit exists. Really shouldn't happen, but might be if SDP is
                    // accessed before onInit or onCommit were called.
                    throw new InvalidOperationException("No index commit to snapshot");
                }

                // Can't use the same snapshot ID twice...
                CheckSnapshotted(id);

                RegisterSnapshotInfo(id, lastCommit.SegmentsFileName, lastCommit);
                return lastCommit;
            }
        }

        public override object Clone()
        {
            SnapshotDeletionPolicy other = (SnapshotDeletionPolicy)base.Clone();
            other.primary = (IndexDeletionPolicy)this.primary.Clone();
            other.lastCommit = null;
            other.segmentsFileToIDs = new HashMap<String, ISet<String>>(segmentsFileToIDs);
            other.idToSnapshot = new HashMap<String, SnapshotInfo>(idToSnapshot);
            return other;
        }
    }
}