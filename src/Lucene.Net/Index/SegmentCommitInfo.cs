using J2N.Collections.Generic.Extensions;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Embeds a [read-only] <see cref="SegmentInfo"/> and adds per-commit
    /// fields.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class SegmentCommitInfo
    {
        /// <summary>
        /// The <see cref="SegmentInfo"/> that we wrap. </summary>
        public SegmentInfo Info { get; private set; }

        // How many deleted docs in the segment:
        private int delCount;

        // Generation number of the live docs file (-1 if there
        // are no deletes yet):
        private long delGen;

        // Normally 1+delGen, unless an exception was hit on last
        // attempt to write:
        private long nextWriteDelGen;

        // Generation number of the FieldInfos (-1 if there are no updates)
        private long fieldInfosGen;

        // Normally 1 + fieldInfosGen, unless an exception was hit on last attempt to
        // write
        private long nextWriteFieldInfosGen;

        // Track the per-generation updates files
        private readonly IDictionary<long, ISet<string>> genUpdatesFiles = new Dictionary<long, ISet<string>>();

        private long sizeInBytes = -1; // LUCENENET NOTE: This was volatile in the original, but long cannot be volatile in .NET

        /// <summary>
        /// Sole constructor.
        /// </summary>
        /// <param name="info">
        ///          <see cref="SegmentInfo"/> that we wrap </param>
        /// <param name="delCount">
        ///          number of deleted documents in this segment </param>
        /// <param name="delGen">
        ///          deletion generation number (used to name deletion files) </param>
        /// <param name="fieldInfosGen">
        ///          <see cref="FieldInfos"/> generation number (used to name field-infos files)
        ///  </param>
        public SegmentCommitInfo(SegmentInfo info, int delCount, long delGen, long fieldInfosGen)
        {
            this.Info = info;
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

            this.fieldInfosGen = fieldInfosGen;
            if (fieldInfosGen == -1)
            {
                nextWriteFieldInfosGen = 1;
            }
            else
            {
                nextWriteFieldInfosGen = fieldInfosGen + 1;
            }
        }

        /// <summary>
        /// Returns the per generation updates files. </summary>
        public virtual IDictionary<long, ISet<string>> UpdatesFiles => genUpdatesFiles.AsReadOnly();

        /// <summary>
        /// Sets the updates file names per generation. Does not deep clone the map. </summary>
        public virtual void SetGenUpdatesFiles(IDictionary<long, ISet<string>> genUpdatesFiles)
        {
            this.genUpdatesFiles.Clear();
            this.genUpdatesFiles.PutAll(genUpdatesFiles);
        }

        /// <summary>
        /// Called when we succeed in writing deletes </summary>
        internal virtual void AdvanceDelGen()
        {
            delGen = nextWriteDelGen;
            nextWriteDelGen = delGen + 1;
            sizeInBytes = -1;
        }

        /// <summary>
        /// Called if there was an exception while writing
        /// deletes, so that we don't try to write to the same
        /// file more than once.
        /// </summary>
        internal virtual void AdvanceNextWriteDelGen()
        {
            nextWriteDelGen++;
        }

        /// <summary>
        /// Called when we succeed in writing a new <see cref="FieldInfos"/> generation. </summary>
        internal virtual void AdvanceFieldInfosGen()
        {
            fieldInfosGen = nextWriteFieldInfosGen;
            nextWriteFieldInfosGen = fieldInfosGen + 1;
            sizeInBytes = -1;
        }

        /// <summary>
        /// Called if there was an exception while writing a new generation of
        /// <see cref="FieldInfos"/>, so that we don't try to write to the same file more than once.
        /// </summary>
        internal virtual void AdvanceNextWriteFieldInfosGen()
        {
            nextWriteFieldInfosGen++;
        }

        /// <summary>
        /// Returns total size in bytes of all files for this
        /// segment.
        /// <para/><b>NOTE:</b> this value is not correct for 3.0 segments
        /// that have shared docstores. To get the correct value, upgrade!
        /// </summary>
        public virtual long GetSizeInBytes()
        {
            if (sizeInBytes == -1)
            {
                long sum = 0;
                foreach (string fileName in GetFiles())
                {
                    sum += Info.Dir.FileLength(fileName);
                }
                sizeInBytes = sum;
            }

            return sizeInBytes;
        }

        /// <summary>
        /// Returns all files in use by this segment. </summary>
        public virtual ICollection<string> GetFiles()
        {
            // Start from the wrapped info's files:
            ISet<string> files = new JCG.HashSet<string>(Info.GetFiles());

            // TODO we could rely on TrackingDir.getCreatedFiles() (like we do for
            // updates) and then maybe even be able to remove LiveDocsFormat.files().

            // Must separately add any live docs files:
            Info.Codec.LiveDocsFormat.Files(this, files);

            // Must separately add any field updates files
            foreach (ISet<string> updateFiles in genUpdatesFiles.Values)
            {
                files.UnionWith(updateFiles);
            }

            return files;
        }

        // NOTE: only used in-RAM by IW to track buffered deletes;
        // this is never written to/read from the Directory
        private long bufferedDeletesGen;

        internal virtual long BufferedDeletesGen => bufferedDeletesGen;

        internal void SetBufferedDeletesGen(long value)
        {
            bufferedDeletesGen = value;
            sizeInBytes = -1;
        }

        /// <summary>
        /// Returns <c>true</c> if there are any deletions for the
        /// segment at this commit.
        /// </summary>
        public virtual bool HasDeletions => delGen != -1;

        /// <summary>
        /// Returns <c>true</c> if there are any field updates for the segment in this commit. </summary>
        public virtual bool HasFieldUpdates => fieldInfosGen != -1;

        /// <summary>
        /// Returns the next available generation number of the <see cref="FieldInfos"/> files. </summary>
        public virtual long NextFieldInfosGen => nextWriteFieldInfosGen;

        /// <summary>
        /// Returns the generation number of the field infos file or -1 if there are no
        /// field updates yet.
        /// </summary>
        public virtual long FieldInfosGen => fieldInfosGen;

        /// <summary>
        /// Returns the next available generation number
        /// of the live docs file.
        /// </summary>
        public virtual long NextDelGen => nextWriteDelGen;

        /// <summary>
        /// Returns generation number of the live docs file
        /// or -1 if there are no deletes yet.
        /// </summary>
        public virtual long DelGen => delGen;

        /// <summary>
        /// Returns the number of deleted docs in the segment.
        /// </summary>
        public virtual int DelCount
        {
            get => delCount;
            internal set
            {
                if (value < 0 || value > Info.DocCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(DelCount), "invalid delCount=" + value + " (docCount=" + Info.DocCount + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
                }
                this.delCount = value;
            }
        }

        /// <summary>
        /// Returns a description of this segment. </summary>
        public virtual string ToString(Directory dir, int pendingDelCount)
        {
            string s = Info.ToString(dir, delCount + pendingDelCount);
            if (delGen != -1)
            {
                s += ":delGen=" + delGen;
            }
            if (fieldInfosGen != -1)
            {
                s += ":fieldInfosGen=" + fieldInfosGen;
            }
            return s;
        }

        public override string ToString()
        {
            return ToString(Info.Dir, 0);
        }

        public virtual object Clone()
        {
            SegmentCommitInfo other = new SegmentCommitInfo(Info, delCount, delGen, fieldInfosGen);
            // Not clear that we need to carry over nextWriteDelGen
            // (i.e. do we ever clone after a failed write and
            // before the next successful write?), but just do it to
            // be safe:
            other.nextWriteDelGen = nextWriteDelGen;
            other.nextWriteFieldInfosGen = nextWriteFieldInfosGen;

            // deep clone
            foreach (KeyValuePair<long, ISet<string>> e in genUpdatesFiles)
            {
                other.genUpdatesFiles[e.Key] = new JCG.HashSet<string>(e.Value);
            }

            return other;
        }
    }
}