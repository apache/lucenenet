using J2N.Collections.Generic.Extensions;
using System;
using System.Collections.Generic;
using Lucene.Net.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using TrackingDirectoryWrapper = Lucene.Net.Store.TrackingDirectoryWrapper;

    /// <summary>
    /// Information about a segment such as it's name, directory, and files related
    /// to the segment.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class SegmentInfo
    {
        // TODO: remove these from this class, for now this is the representation
        /// <summary>
        /// Used by some member fields to mean not present (e.g.,
        /// norms, deletions).
        /// </summary>
        public static readonly int NO = -1; // e.g. no norms; no deletes;

        /// <summary>
        /// Used by some member fields to mean present (e.g.,
        /// norms, deletions).
        /// </summary>
        public static readonly int YES = 1; // e.g. have norms; have deletes;

        /// <summary>
        /// Unique segment name in the directory. </summary>
        public string Name { get; private set; }

        private int docCount; // number of docs in seg

        /// <summary>
        /// Where this segment resides. </summary>
        public Directory Dir { get; private set; }

        private bool isCompoundFile;

        private Codec codec;

        private IDictionary<string, string> diagnostics;

        [Obsolete("not used anymore")]
        private IDictionary<string, string> attributes;

        // Tracks the Lucene version this segment was created with, since 3.1. Null
        // indicates an older than 3.0 index, and it's used to detect a too old index.
        // The format expected is "x.y" - "2.x" for pre-3.0 indexes (or null), and
        // specific versions afterwards ("3.0", "3.1" etc.).
        // see Constants.LUCENE_MAIN_VERSION.
        private string version;

        /// <summary>
        /// Gets or Sets diagnostics saved into the segment when it was written.
        /// </summary>
        public IDictionary<string, string> Diagnostics
        {
            get => diagnostics;
            set => this.diagnostics = value;
        }

        /// <summary>
        /// Construct a new complete <see cref="SegmentInfo"/> instance from input.
        /// <para>Note: this is public only to allow access from
        /// the codecs package.</para>
        /// </summary>
        public SegmentInfo(Directory dir, string version, string name, int docCount, bool isCompoundFile, Codec codec, IDictionary<string, string> diagnostics)
            : this(dir, version, name, docCount, isCompoundFile, codec, diagnostics, null)
        {
        }

        /// <summary>
        /// Construct a new complete <see cref="SegmentInfo"/> instance from input.
        /// <para>Note: this is public only to allow access from
        /// the codecs package.</para>
        /// </summary>
        public SegmentInfo(Directory dir, string version, string name, int docCount, bool isCompoundFile, Codec codec, IDictionary<string, string> diagnostics, IDictionary<string, string> attributes)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!(dir is TrackingDirectoryWrapper));
            this.Dir = dir;
            this.version = version;
            this.Name = name;
            this.docCount = docCount;
            this.isCompoundFile = isCompoundFile;
            this.codec = codec;
            this.diagnostics = diagnostics;
#pragma warning disable 612, 618
            this.attributes = attributes;
#pragma warning restore 612, 618
        }

        [Obsolete("separate norms are not supported in >= 4.0")]
        internal bool HasSeparateNorms => GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_KEY) != null;

        /// <summary>
        /// Gets or Sets whether this segment is stored as a compound file.
        /// <c>true</c> if this is a compound file;
        /// else, <c>false</c>
        /// </summary>
        public bool UseCompoundFile
        {
            get => isCompoundFile;
            set => this.isCompoundFile = value;
        }

        /// <summary>
        /// Gets or Sets <see cref="Codecs.Codec"/> that wrote this segment.
        /// Setter can only be called once. </summary>
        public Codec Codec
        {
            get => codec;
            set
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(this.codec is null);
                this.codec = value ?? throw new ArgumentNullException(nameof(Codec), "Codec must be non-null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
        }

        /// <summary>
        /// Returns number of documents in this segment (deletions
        /// are not taken into account).
        /// </summary>
        public int DocCount
        {
            get
            {
                if (this.docCount == -1)
                {
                    throw IllegalStateException.Create("docCount isn't set yet");
                }
                return docCount;
            }
            internal set // NOTE: leave package private
            {
                if (this.docCount != -1)
                {
                    throw IllegalStateException.Create("docCount was already set");
                }
                this.docCount = value;
            }
        }

        /// <summary>
        /// Return all files referenced by this <see cref="SegmentInfo"/>. </summary>
        public ISet<string> GetFiles()
        {
            if (setFiles is null)
            {
                throw IllegalStateException.Create("files were not computed yet");
            }
            return setFiles.AsReadOnly();
        }

        public override string ToString()
        {
            return ToString(Dir, 0);
        }

        /// <summary>
        /// Used for debugging.  Format may suddenly change.
        ///
        /// <para>Current format looks like
        /// <c>_a(3.1):c45/4</c>, which means the segment's
        /// name is <c>_a</c>; it was created with Lucene 3.1 (or
        /// '?' if it's unknown); it's using compound file
        /// format (would be <c>C</c> if not compound); it
        /// has 45 documents; it has 4 deletions (this part is
        /// left off when there are no deletions).</para>
        /// </summary>
        public string ToString(Directory dir, int delCount)
        {
            StringBuilder s = new StringBuilder();
            s.Append(Name).Append('(').Append(version ?? "?").Append(')').Append(':');
            char cfs = UseCompoundFile ? 'c' : 'C';
            s.Append(cfs);

            if (this.Dir != dir)
            {
                s.Append('x');
            }
            s.Append(docCount);

            if (delCount != 0)
            {
                s.Append('/').Append(delCount);
            }

            // TODO: we could append toString of attributes() here?

            return s.ToString();
        }

        /// <summary>
        /// We consider another <see cref="SegmentInfo"/> instance equal if it
        /// has the same dir and same name.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is SegmentInfo other)
            {
                return other.Dir == Dir && other.Name.Equals(Name, StringComparison.Ordinal);
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
        /// <para/>
        /// <b>NOTE:</b> this method is used for internal purposes only - you should
        /// not modify the version of a <see cref="SegmentInfo"/>, or it may result in unexpected
        /// exceptions thrown when you attempt to open the index.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public string Version
        {
            get => version;
            set => this.version = value;
        }

        private ISet<string> setFiles;

        /// <summary>
        /// Sets the files written for this segment. </summary>
        public void SetFiles(ISet<string> files)
        {
            CheckFileNames(files);
            setFiles = files;
        }

        /// <summary>
        /// Add these files to the set of files written for this
        /// segment.
        /// </summary>
        public void AddFiles(ICollection<string> files)
        {
            CheckFileNames(files);
            setFiles.UnionWith(files);
        }

        /// <summary>
        /// Add this file to the set of files written for this
        /// segment.
        /// </summary>
        public void AddFile(string file)
        {
            CheckFileNames(new[] { file });
            setFiles.Add(file);
        }

        private static void CheckFileNames(ICollection<string> files) // LUCENENET: CA1822: Mark members as static
        {
            Regex r = IndexFileNames.CODEC_FILE_PATTERN;
            foreach (string file in files)
            {
                if (!r.IsMatch(file))
                {
                    throw new ArgumentException("invalid codec filename '" + file + "', must match: " + IndexFileNames.CODEC_FILE_PATTERN.ToString());
                }
            }
        }

        /// <summary>
        /// Get a codec attribute value, or null if it does not exist
        /// </summary>
        [Obsolete("no longer supported")]
        public string GetAttribute(string key)
        {
            if (attributes is null)
            {
                return null;
            }
            else
            {
                attributes.TryGetValue(key, out string attribute);
                return attribute;
            }
        }

        /// <summary>
        /// Puts a codec attribute value.
        /// <para/>
        /// This is a key-value mapping for the field that the codec can use to store
        /// additional metadata, and will be available to the codec when reading the
        /// segment via <see cref="GetAttribute(string)"/>
        /// <para/>
        /// If a value already exists for the field, it will be replaced with the new
        /// value.
        /// </summary>
        [Obsolete("no longer supported")]
        public string PutAttribute(string key, string value)
        {
            if (attributes is null)
            {
                attributes = new Dictionary<string, string>();
            }
            return attributes[key] = value;
        }

        /// <summary>
        /// Returns the internal codec attributes map. May be null if no mappings exist.
        /// </summary>
        [Obsolete("no longer supported")]
        public IDictionary<string, string> Attributes => attributes;
    }
}