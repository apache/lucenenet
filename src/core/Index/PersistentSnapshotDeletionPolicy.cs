using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Index
{
    public class PersistentSnapshotDeletionPolicy : SnapshotDeletionPolicy, IDisposable
    {
        private const string SNAPSHOTS_ID = "$SNAPSHOTS_DOC$";

        private readonly IndexWriter writer;

        public static IDictionary<string, string> ReadSnapshotsInfo(Directory dir)
        {
            IndexReader r = DirectoryReader.Open(dir);
            IDictionary<string, string> snapshots = new HashMap<string, string>();
            try
            {
                int numDocs = r.NumDocs;
                // index is allowed to have exactly one document or 0.
                if (numDocs == 1)
                {
                    Document doc = r.Document(r.MaxDoc - 1);
                    if (doc.GetField(SNAPSHOTS_ID) == null)
                    {
                        throw new InvalidOperationException("directory is not a valid snapshots store!");
                    }
                    doc.RemoveField(SNAPSHOTS_ID);
                    foreach (IIndexableField f in doc)
                    {
                        snapshots[f.Name] = f.StringValue;
                    }
                }
                else if (numDocs != 0)
                {
                    throw new InvalidOperationException(
                        "should be at most 1 document in the snapshots directory: " + numDocs);
                }
            }
            finally
            {
                r.Dispose();
            }
            return snapshots;
        }

        public PersistentSnapshotDeletionPolicy(IndexDeletionPolicy primary, Directory dir, OpenMode mode, Version matchVersion)
            : base(primary, null)
        {


            // Initialize the index writer over the snapshot directory.
            writer = new IndexWriter(dir, new IndexWriterConfig(matchVersion, null).SetOpenMode(mode));
            if (mode != OpenMode.APPEND)
            {
                // IndexWriter no longer creates a first commit on an empty Directory. So
                // if we were asked to CREATE*, call commit() just to be sure. If the
                // index contains information and mode is CREATE_OR_APPEND, it's a no-op.
                writer.Commit();
            }

            try
            {
                // Initializes the snapshots information. This code should basically run
                // only if mode != CREATE, but if it is, it's no harm as we only open the
                // reader once and immediately close it.
                foreach (KeyValuePair<string, string> e in ReadSnapshotsInfo(dir))
                {
                    RegisterSnapshotInfo(e.Key, e.Value, null);
                }
            }
            catch (SystemException)
            {
                writer.Dispose(); // don't leave any open file handles
                throw;
            }
        }

        public override void OnInit<T>(IList<T> commits)
        {
            // super.onInit() needs to be called first to ensure that initialization
            // behaves as expected. The superclass, SnapshotDeletionPolicy, ensures
            // that any snapshot IDs with empty IndexCommits are released. Since this 
            // happens, this class needs to persist these changes.
            base.OnInit(commits);
            PersistSnapshotInfos(null, null);
        }

        public override IndexCommit Snapshot(string id)
        {
            CheckSnapshotted(id);
            if (SNAPSHOTS_ID.Equals(id))
            {
                throw new ArgumentException(id + " is reserved and cannot be used as a snapshot id");
            }
            PersistSnapshotInfos(id, lastCommit.SegmentsFileName);
            return base.Snapshot(id);
        }

        public override void Release(string id)
        {
            base.Release(id);
            PersistSnapshotInfos(null, null);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                writer.Dispose();
            }
        }

        private void PersistSnapshotInfos(string id, string segment)
        {
            writer.DeleteAll();
            Document d = new Document();
            FieldType ft = new FieldType();
            ft.Stored = true;
            d.Add(new Field(SNAPSHOTS_ID, "", ft));
            foreach (KeyValuePair<string, string> e in base.GetSnapshots())
            {
                d.Add(new Field(e.Key, e.Value, ft));
            }
            if (id != null)
            {
                d.Add(new Field(id, segment, ft));
            }
            writer.AddDocument(d);
            writer.Commit();
        }
    }
}
