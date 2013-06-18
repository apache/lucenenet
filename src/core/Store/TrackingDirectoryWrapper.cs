using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public sealed class TrackingDirectoryWrapper : Directory, IDisposable
    {
        private readonly Directory other;
        private readonly ISet<String> createdFileNames = new HashSet<String>(); // TODO: make this concurrent

        public TrackingDirectoryWrapper(Directory other)
        {
            this.other = other;
        }

        public override string[] ListAll()
        {
            return other.ListAll();
        }

        public override bool FileExists(string name)
        {
            return other.FileExists(name);
        }

        public override void DeleteFile(string name)
        {
            createdFileNames.Remove(name);
            other.DeleteFile(name);
        }

        public override long FileLength(string name)
        {
            return other.FileLength(name);
        }

        public override IndexOutput CreateOutput(string name, IOContext context)
        {
            createdFileNames.Add(name);
            return other.CreateOutput(name, context);
        }

        public override void Sync(ICollection<string> names)
        {
            other.Sync(names);
        }

        public override IndexInput OpenInput(string name, IOContext context)
        {
            return other.OpenInput(name, context);
        }

        public override Lock MakeLock(string name)
        {
            return other.MakeLock(name);
        }

        public override void ClearLock(string name)
        {
            other.ClearLock(name);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                other.Dispose();
            }
        }

        public override LockFactory LockFactory
        {
            get
            {
                return other.LockFactory;
            }
            set
            {
                other.LockFactory = value;
            }
        }

        public override string LockId
        {
            get
            {
                return other.LockId;
            }
        }

        public override string ToString()
        {
            return "TrackingDirectoryWrapper(" + other.ToString() + ")";
        }

        public override void Copy(Directory to, string src, string dest, IOContext context)
        {
            createdFileNames.Add(dest);
            other.Copy(to, src, dest, context);
        }

        public override IndexInputSlicer CreateSlicer(string name, IOContext context)
        {
            return other.CreateSlicer(name, context);
        }

        public ISet<string> CreatedFiles
        {
            get { return createdFileNames; }
        }

        public Directory Delegate
        {
            get { return other; }
        }
    }
}
