using System.Collections.Generic;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;

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
    /// Embeds a [read-only] SegmentInfo and adds per-commit
    ///  fields.
    ///
    ///  @lucene.experimental
    /// </summary>
    public class SegmentCommitInfo
    {
        /// <summary>
        /// The <seealso cref="SegmentInfo"/> that we wrap. </summary>
        public readonly SegmentInfo Info;

        // How many deleted docs in the segment:
        private int DelCount_Renamed;

        // Generation number of the live docs file (-1 if there
        // are no deletes yet):
        private long DelGen_Renamed;

        // Normally 1+delGen, unless an exception was hit on last
        // attempt to write:
        private long NextWriteDelGen;

        // Generation number of the FieldInfos (-1 if there are no updates)
        private long FieldInfosGen_Renamed;

        // Normally 1 + fieldInfosGen, unless an exception was hit on last attempt to
        // write
        private long NextWriteFieldInfosGen;

        // Track the per-generation updates files
        private readonly IDictionary<long, ISet<string>> GenUpdatesFiles_Renamed = new Dictionary<long, ISet<string>>();

        private long SizeInBytes_Renamed = -1;

        /// <summary>
        /// Sole constructor.
        /// </summary>
        /// <param name="info">
        ///          <seealso cref="SegmentInfo"/> that we wrap </param>
        /// <param name="delCount">
        ///          number of deleted documents in this segment </param>
        /// <param name="delGen">
        ///          deletion generation number (used to name deletion files) </param>
        /// <param name="fieldInfosGen">
        ///          FieldInfos generation number (used to name field-infos files)
        ///  </param>
        public SegmentCommitInfo(SegmentInfo info, int delCount, long delGen, long fieldInfosGen)
        {
            this.Info = info;
            this.DelCount_Renamed = delCount;
            this.DelGen_Renamed = delGen;
            if (delGen == -1)
            {
                NextWriteDelGen = 1;
            }
            else
            {
                NextWriteDelGen = delGen + 1;
            }

            this.FieldInfosGen_Renamed = fieldInfosGen;
            if (fieldInfosGen == -1)
            {
                NextWriteFieldInfosGen = 1;
            }
            else
            {
                NextWriteFieldInfosGen = fieldInfosGen + 1;
            }
        }

        /// <summary>
        /// Returns the per generation updates files. </summary>
        public virtual IDictionary<long, ISet<string>> UpdatesFiles
        {
            get
            {
                return CollectionsHelper.UnmodifiableMap(GenUpdatesFiles_Renamed);
            }
        }

        /// <summary>
        /// Sets the updates file names per generation. Does not deep clone the map. </summary>
        public virtual IDictionary<long, ISet<string>> GenUpdatesFiles
        {
            set
            {
                this.GenUpdatesFiles_Renamed.Clear();
                this.GenUpdatesFiles_Renamed.PutAll(value);
            }
        }

        /// <summary>
        /// Called when we succeed in writing deletes </summary>
        internal virtual void AdvanceDelGen()
        {
            DelGen_Renamed = NextWriteDelGen;
            NextWriteDelGen = DelGen_Renamed + 1;
            SizeInBytes_Renamed = -1;
        }

        /// <summary>
        /// Called if there was an exception while writing
        ///  deletes, so that we don't try to write to the same
        ///  file more than once.
        /// </summary>
        internal virtual void AdvanceNextWriteDelGen()
        {
            NextWriteDelGen++;
        }

        /// <summary>
        /// Called when we succeed in writing a new FieldInfos generation. </summary>
        internal virtual void AdvanceFieldInfosGen()
        {
            FieldInfosGen_Renamed = NextWriteFieldInfosGen;
            NextWriteFieldInfosGen = FieldInfosGen_Renamed + 1;
            SizeInBytes_Renamed = -1;
        }

        /// <summary>
        /// Called if there was an exception while writing a new generation of
        /// FieldInfos, so that we don't try to write to the same file more than once.
        /// </summary>
        internal virtual void AdvanceNextWriteFieldInfosGen()
        {
            NextWriteFieldInfosGen++;
        }

        /// <summary>
        /// Returns total size in bytes of all files for this
        ///  segment.
        /// <p><b>NOTE:</b> this value is not correct for 3.0 segments
        /// that have shared docstores. To get the correct value, upgrade!
        /// </summary>
        public virtual long SizeInBytes()
        {
            if (SizeInBytes_Renamed == -1)
            {
                long sum = 0;
                foreach (string fileName in Files())
                {
                    sum += Info.Dir.FileLength(fileName);
                }
                SizeInBytes_Renamed = sum;
            }

            return SizeInBytes_Renamed;
        }

        /// <summary>
        /// Returns all files in use by this segment. </summary>
        public virtual ICollection<string> Files()
        {
            // Start from the wrapped info's files:
            ISet<string> files = new HashSet<string>(Info.Files);

            // TODO we could rely on TrackingDir.getCreatedFiles() (like we do for
            // updates) and then maybe even be able to remove LiveDocsFormat.files().

            // Must separately add any live docs files:
            Info.Codec.LiveDocsFormat.Files(this, files);

            // Must separately add any field updates files
            foreach (ISet<string> updateFiles in GenUpdatesFiles_Renamed.Values)
            {
                CollectionsHelper.AddAll(files, updateFiles);
            }

            return files;
        }

        // NOTE: only used in-RAM by IW to track buffered deletes;
        // this is never written to/read from the Directory
        private long BufferedDeletesGen_Renamed;

        internal virtual long BufferedDeletesGen
        {
            get
            {
                return BufferedDeletesGen_Renamed;
            }
            set
            {
                BufferedDeletesGen_Renamed = value;
                SizeInBytes_Renamed = -1;
            }
        }

        /// <summary>
        /// Returns true if there are any deletions for the
        /// segment at this commit.
        /// </summary>
        public virtual bool HasDeletions()
        {
            return DelGen_Renamed != -1;
        }

        /// <summary>
        /// Returns true if there are any field updates for the segment in this commit. </summary>
        public virtual bool HasFieldUpdates()
        {
            return FieldInfosGen_Renamed != -1;
        }

        /// <summary>
        /// Returns the next available generation number of the FieldInfos files. </summary>
        public virtual long NextFieldInfosGen
        {
            get
            {
                return NextWriteFieldInfosGen;
            }
        }

        /// <summary>
        /// Returns the generation number of the field infos file or -1 if there are no
        /// field updates yet.
        /// </summary>
        public virtual long FieldInfosGen
        {
            get
            {
                return FieldInfosGen_Renamed;
            }
        }

        /// <summary>
        /// Returns the next available generation number
        /// of the live docs file.
        /// </summary>
        public virtual long NextDelGen
        {
            get
            {
                return NextWriteDelGen;
            }
        }

        /// <summary>
        /// Returns generation number of the live docs file
        /// or -1 if there are no deletes yet.
        /// </summary>
        public virtual long DelGen
        {
            get
            {
                return DelGen_Renamed;
            }
        }

        /// <summary>
        /// Returns the number of deleted docs in the segment.
        /// </summary>
        public virtual int DelCount
        {
            get
            {
                return DelCount_Renamed;
            }
            set
            {
                if (value < 0 || value > Info.DocCount)
                {
                    throw new System.ArgumentException("invalid delCount=" + value + " (docCount=" + Info.DocCount + ")");
                }
                this.DelCount_Renamed = value;
            }
        }

        /// <summary>
        /// Returns a description of this segment. </summary>
        public virtual string ToString(Directory dir, int pendingDelCount)
        {
            string s = Info.ToString(dir, DelCount_Renamed + pendingDelCount);
            if (DelGen_Renamed != -1)
            {
                s += ":delGen=" + DelGen_Renamed;
            }
            if (FieldInfosGen_Renamed != -1)
            {
                s += ":fieldInfosGen=" + FieldInfosGen_Renamed;
            }
            return s;
        }

        public override string ToString()
        {
            return ToString(Info.Dir, 0);
        }

        public virtual object Clone()
        {
            SegmentCommitInfo other = new SegmentCommitInfo(Info, DelCount_Renamed, DelGen_Renamed, FieldInfosGen_Renamed);
            // Not clear that we need to carry over nextWriteDelGen
            // (i.e. do we ever clone after a failed write and
            // before the next successful write?), but just do it to
            // be safe:
            other.NextWriteDelGen = NextWriteDelGen;
            other.NextWriteFieldInfosGen = NextWriteFieldInfosGen;

            // deep clone
            foreach (KeyValuePair<long, ISet<string>> e in GenUpdatesFiles_Renamed)
            {
                other.GenUpdatesFiles_Renamed[e.Key] = new HashSet<string>(e.Value);
            }

            return other;
        }
    }
}