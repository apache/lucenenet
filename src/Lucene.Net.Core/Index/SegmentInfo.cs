using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Index
{
    using System.Text.RegularExpressions;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    /// Information about a segment such as it's name, directory, and files related
    /// to the segment.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class SegmentInfo
    {
        // TODO: remove these from this class, for now this is the representation
        /// <summary>
        /// Used by some member fields to mean not present (e.g.,
        ///  norms, deletions).
        /// </summary>
        public static readonly int NO = -1; // e.g. no norms; no deletes;

        /// <summary>
        /// Used by some member fields to mean present (e.g.,
        ///  norms, deletions).
        /// </summary>
        public static readonly int YES = 1; // e.g. have norms; have deletes;

        /// <summary>
        /// Unique segment name in the directory. </summary>
        public readonly string Name;

        private int DocCount_Renamed; // number of docs in seg

        /// <summary>
        /// Where this segment resides. </summary>
        public readonly Directory Dir; // LUCENENET TODO: Make property

        private bool IsCompoundFile;

        private Codec Codec_Renamed;

        private IDictionary<string, string> Diagnostics_Renamed;

        /// @deprecated not used anymore
        [Obsolete("not used anymore")]
        private IDictionary<string, string> Attributes_Renamed;

        // Tracks the Lucene version this segment was created with, since 3.1. Null
        // indicates an older than 3.0 index, and it's used to detect a too old index.
        // The format expected is "x.y" - "2.x" for pre-3.0 indexes (or null), and
        // specific versions afterwards ("3.0", "3.1" etc.).
        // see Constants.LUCENE_MAIN_VERSION.
        private string Version_Renamed;

        public IDictionary<string, string> Diagnostics
        {
            set
            {
                this.Diagnostics_Renamed = value;
            }
            get
            {
                return Diagnostics_Renamed;
            }
        }

        /// <summary>
        /// Construct a new complete SegmentInfo instance from input.
        /// <p>Note: this is public only to allow access from
        /// the codecs package.</p>
        /// </summary>
        public SegmentInfo(Directory dir, string version, string name, int docCount, bool isCompoundFile, Codec codec, IDictionary<string, string> diagnostics)
            : this(dir, version, name, docCount, isCompoundFile, codec, diagnostics, null)
        {
        }

        /// <summary>
        /// Construct a new complete SegmentInfo instance from input.
        /// <p>Note: this is public only to allow access from
        /// the codecs package.</p>
        /// </summary>
        public SegmentInfo(Directory dir, string version, string name, int docCount, bool isCompoundFile, Codec codec, IDictionary<string, string> diagnostics, IDictionary<string, string> attributes)
        {
            Debug.Assert(!(dir is TrackingDirectoryWrapper));
            this.Dir = dir;
            this.Version_Renamed = version;
            this.Name = name;
            this.DocCount_Renamed = docCount;
            this.IsCompoundFile = isCompoundFile;
            this.Codec_Renamed = codec;
            this.Diagnostics_Renamed = diagnostics;
            this.Attributes_Renamed = attributes;
        }

        /// @deprecated separate norms are not supported in >= 4.0
        [Obsolete("separate norms are not supported in >= 4.0")]
        internal bool HasSeparateNorms() // LUCENENET TODO: Make property
        {
            return GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_KEY) != null;
        }

        /// <summary>
        /// Mark whether this segment is stored as a compound file.
        /// </summary>
        /// <param name="isCompoundFile"> true if this is a compound file;
        /// else, false </param>
        public bool UseCompoundFile
        {
            set
            {
                this.IsCompoundFile = value;
            }
            get
            {
                return IsCompoundFile;
            }
        }

        /// <summary>
        /// Can only be called once. </summary>
        public Codec Codec
        {
            set
            {
                Debug.Assert(this.Codec_Renamed == null);
                if (value == null)
                {
                    throw new System.ArgumentException("codec must be non-null");
                }
                this.Codec_Renamed = value;
            }
            get
            {
                return Codec_Renamed;
            }
        }

        /// <summary>
        /// Returns number of documents in this segment (deletions
        ///  are not taken into account).
        /// </summary>
        public int DocCount // LUCENENET TODO: Make method ? throws exception
        {
            get
            {
                if (this.DocCount_Renamed == -1)
                {
                    throw new InvalidOperationException("docCount isn't set yet");
                }
                return DocCount_Renamed;
            }
            set
            {
                if (this.DocCount_Renamed != -1)
                {
                    throw new InvalidOperationException("docCount was already set");
                }
                this.DocCount_Renamed = value;
            }
        }

        // NOTE: leave package private

        /// <summary>
        /// Return all files referenced by this SegmentInfo. </summary>

        public override string ToString()
        {
            return ToString(Dir, 0);
        }

        /// <summary>
        /// Used for debugging.  Format may suddenly change.
        ///
        ///  <p>Current format looks like
        ///  <code>_a(3.1):c45/4</code>, which means the segment's
        ///  name is <code>_a</code>; it was created with Lucene 3.1 (or
        ///  '?' if it's unknown); it's using compound file
        ///  format (would be <code>C</code> if not compound); it
        ///  has 45 documents; it has 4 deletions (this part is
        ///  left off when there are no deletions).</p>
        /// </summary>
        public string ToString(Directory dir, int delCount)
        {
            StringBuilder s = new StringBuilder();
            s.Append(Name).Append('(').Append(Version_Renamed == null ? "?" : Version_Renamed).Append(')').Append(':');
            char cfs = UseCompoundFile ? 'c' : 'C';
            s.Append(cfs);

            if (this.Dir != dir)
            {
                s.Append('x');
            }
            s.Append(DocCount_Renamed);

            if (delCount != 0)
            {
                s.Append('/').Append(delCount);
            }

            // TODO: we could append toString of attributes() here?

            return s.ToString();
        }

        /// <summary>
        /// We consider another SegmentInfo instance equal if it
        ///  has the same dir and same name.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is SegmentInfo)
            {
                SegmentInfo other = (SegmentInfo)obj;
                return other.Dir == Dir && other.Name.Equals(Name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return Dir.GetHashCode() + Name.GetHashCode();
        }

        /// <summary>
        /// Used by DefaultSegmentInfosReader to upgrade a 3.0 segment to record its
        /// version is "3.0". this method can be removed when we're not required to
        /// support 3x indexes anymore, e.g. in 5.0.
        /// <p>
        /// <b>NOTE:</b> this method is used for internal purposes only - you should
        /// not modify the version of a SegmentInfo, or it may result in unexpected
        /// exceptions thrown when you attempt to open the index.
        ///
        /// @lucene.internal
        /// </summary>
        public string Version
        {
            set
            {
                this.Version_Renamed = value;
            }
            get
            {
                return Version_Renamed;
            }
        }

        private ISet<string> SetFiles;

        /// <summary>
        /// Sets the files written for this segment. </summary>
        public ISet<string> Files // LUCENENET TODO: Make method ? throws exception
        {
            get
            {
                if (SetFiles == null)
                {
                    throw new InvalidOperationException("files were not computed yet");
                }
                return Collections.UnmodifiableSet(SetFiles);
            }

            set
            {
                CheckFileNames(value);
                SetFiles = value;
            }
        }

        /// <summary>
        /// Add these files to the set of files written for this
        ///  segment.
        /// </summary>
        public void AddFiles(ICollection<string> files)
        {
            CheckFileNames(files);
            //SetFiles.AddAll(files);
            SetFiles.UnionWith(files);
        }

        /// <summary>
        /// Add this file to the set of files written for this
        ///  segment.
        /// </summary>
        public void AddFile(string file)
        {
            //CheckFileNames(Collections.Singleton(file));
            CheckFileNames(new[] { file });
            SetFiles.Add(file);
        }

        private void CheckFileNames(ICollection<string> files)
        {
            Regex r = IndexFileNames.CODEC_FILE_PATTERN;
            foreach (string file in files)
            {
                if (!r.IsMatch(file))
                {
                    throw new System.ArgumentException("invalid codec filename '" + file + "', must match: " + IndexFileNames.CODEC_FILE_PATTERN.ToString());
                }
            }
        }

        /// <summary>
        /// Get a codec attribute value, or null if it does not exist
        /// </summary>
        /// @deprecated no longer supported
        [Obsolete("no longer supported")]
        public string GetAttribute(string key)
        {
            if (Attributes_Renamed == null)
            {
                return null;
            }
            else
            {
                return Attributes_Renamed[key];
            }
        }

        /// <summary>
        /// Puts a codec attribute value.
        /// <p>
        /// this is a key-value mapping for the field that the codec can use to store
        /// additional metadata, and will be available to the codec when reading the
        /// segment via <seealso cref="#getAttribute(String)"/>
        /// <p>
        /// If a value already exists for the field, it will be replaced with the new
        /// value.
        /// </summary>
        /// @deprecated no longer supported
        [Obsolete("no longer supported")]
        public string PutAttribute(string key, string value)
        {
            if (Attributes_Renamed == null)
            {
                Attributes_Renamed = new Dictionary<string, string>();
            }
            return Attributes_Renamed[key] = value;
        }

        /// <summary>
        /// Returns the internal codec attributes map.
        /// </summary>
        /// <returns> internal codec attributes map. May be null if no mappings exist.
        /// </returns>
        /// @deprecated no longer supported
        [Obsolete("no longer supported")]
        public IDictionary<string, string> Attributes() // LUCENENET TODO: Make property
        {
            return Attributes_Renamed;
        }
    }
}