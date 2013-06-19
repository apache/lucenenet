using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lucene.Net.Store;

namespace Lucene.Net.Index
{
    public class SegmentInfoPerCommit : ICloneable
    {
        public readonly SegmentInfo info;

        private int delCount;

        private long delGen;

        private long nextWriteDelGen;

        private long sizeInBytes = -1;

        public SegmentInfoPerCommit(SegmentInfo info, int delCount, long delGen)
        {
            this.info = info;
            this.delCount = delCount;
            this.delGen = delGen;
            if (delGen == -1)
            {
                nextWriteDelGen = 1;
            }
            else
            {
                nextWriteDelGen = delGen + 1;
            }
        }

        internal void AdvanceDelGen()
        {
            delGen = nextWriteDelGen;
            nextWriteDelGen = delGen + 1;
            Interlocked.Exchange(ref sizeInBytes, -1);
        }

        internal void AdvanceNextWriteDelGen()
        {
            nextWriteDelGen++;
        }

        public long SizeInBytes
        {
            get
            {
                if (Interlocked.Read(ref sizeInBytes) == -1)
                {
                    long sum = 0;
                    foreach (String fileName in Files)
                    {
                        sum += info.dir.FileLength(fileName);
                    }
                    Interlocked.Exchange(ref sizeInBytes, sum); // using Interlocked instead of volatile since it's long
                }

                return Interlocked.Read(ref sizeInBytes);
            }
        }

        public ICollection<String> Files
        {
            get
            {
                // Start from the wrapped info's files:
                ICollection<String> files = new HashSet<String>(info.Files);

                // Must separately add any live docs files:
                info.Codec.LiveDocsFormat().Files(this, files);

                return files;
            }
        }

        private long bufferedDeletesGen;

        internal long BufferedDeletesGen
        {
            get { return bufferedDeletesGen; }
            set
            {
                bufferedDeletesGen = value;
                Interlocked.Exchange(ref sizeInBytes, -1);
            }
        }

        internal void ClearDelGen()
        {
            delGen = -1;
            Interlocked.Exchange(ref sizeInBytes, -1);
        }

        public long DelGen
        {
            get { return this.delGen; }
            set
            {
                this.delGen = value;
                Interlocked.Exchange(ref sizeInBytes, value);
            }
        }

        public bool HasDeletions
        {
            get { return delGen != -1; }
        }

        public long NextDelGen
        {
            get { return nextWriteDelGen; }
        }

        public int DelCount
        {
            get { return delCount; }
            set
            {
                this.delCount = value;
            }
        }

        public String ToString(Directory dir, int pendingDelCount)
        {
            return info.ToString(dir, delCount + pendingDelCount);
        }

        public override string ToString()
        {
            String s = info.ToString(info.dir, delCount);
            if (delGen != -1)
            {
                s += ":delGen=" + delGen;
            }
            return s;
        }
        
        public object Clone()
        {
            SegmentInfoPerCommit other = new SegmentInfoPerCommit(info, delCount, delGen);
            // Not clear that we need to carry over nextWriteDelGen
            // (i.e. do we ever clone after a failed write and
            // before the next successful write?), but just do it to
            // be safe:
            other.nextWriteDelGen = nextWriteDelGen;
            return other;
        }
    }
}
