using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support.IO;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;
using Integer = J2N.Numerics.Int32;
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

    using BlockTreeTermsReader = Lucene.Net.Codecs.BlockTreeTermsReader<object>;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using DocValuesStatus = Lucene.Net.Index.CheckIndex.Status.DocValuesStatus;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IBits = Lucene.Net.Util.IBits;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using Int64BitSet = Lucene.Net.Util.Int64BitSet;
    using IOContext = Lucene.Net.Store.IOContext;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// Basic tool and API to check the health of an index and
    /// write a new segments file that removes reference to
    /// problematic segments.
    ///
    /// <para/>As this tool checks every byte in the index, on a large
    /// index it can take quite a long time to run.
    ///
    /// <para/>
    /// Please make a complete backup of your
    /// index before using this to fix your index!
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class CheckIndex
    {
        private TextWriter infoStream;
        private readonly Directory dir; // LUCENENET: marked readonly

        /// <summary>
        /// Returned from <see cref="CheckIndex.DoCheckIndex()"/> detailing the health and status of the index.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public class Status
        {
            internal Status()
            {
                // Set property defaults
                SegmentsChecked = new JCG.List<string>();
                SegmentInfos = new JCG.List<SegmentInfoStatus>();
            }

            /// <summary>
            /// True if no problems were found with the index. </summary>
            public bool Clean { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// True if we were unable to locate and load the segments_N file. </summary>
            public bool MissingSegments { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// True if we were unable to open the segments_N file. </summary>
            public bool CantOpenSegments { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// True if we were unable to read the version number from segments_N file. </summary>
            public bool MissingSegmentVersion { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Name of latest segments_N file in the index. </summary>
            public string SegmentsFileName { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Number of segments in the index. </summary>
            public int NumSegments { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Empty unless you passed specific segments list to check as optional 3rd argument. </summary>
            /// <seealso cref="CheckIndex.DoCheckIndex(IList{string})"/>
            public IList<string> SegmentsChecked { get; internal set; } // LUCENENET specific - made setter internal 

            /// <summary>
            /// True if the index was created with a newer version of Lucene than the <see cref="CheckIndex"/> tool. </summary>
            public bool ToolOutOfDate { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// List of <see cref="SegmentInfoStatus"/> instances, detailing status of each segment. </summary>
            public IList<SegmentInfoStatus> SegmentInfos { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// <see cref="Directory"/> index is in. </summary>
            public Directory Dir { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// <see cref="Index.SegmentInfos"/> instance containing only segments that
            /// had no problems (this is used with the <see cref="CheckIndex.FixIndex(Status)"/>
            /// method to repair the index.
            /// </summary>
            internal SegmentInfos NewSegments { get; set; }

            /// <summary>
            /// How many documents will be lost to bad segments. </summary>
            public int TotLoseDocCount { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// How many bad segments were found. </summary>
            public int NumBadSegments { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// True if we checked only specific segments 
            /// (<see cref="DoCheckIndex(IList{string})"/> was called with non-null
            /// argument).
            /// </summary>
            public bool Partial { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// The greatest segment name. </summary>
            public int MaxSegmentName { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Whether the <see cref="SegmentInfos.Counter"/> is greater than any of the segments' names. </summary>
            public bool ValidCounter { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Holds the userData of the last commit in the index </summary>
            public IDictionary<string, string> UserData { get; internal set; } // LUCENENET specific - made setter internal

            /// <summary>
            /// Holds the status of each segment in the index.
            /// See <see cref="SegmentInfos"/>.
            /// <para/>
            /// @lucene.experimental
            /// </summary>
            public class SegmentInfoStatus
            {
                internal SegmentInfoStatus()
                {
                    // Set property defaults
                    DocStoreOffset = -1;
                }

                /// <summary>
                /// Name of the segment. </summary>
                public string Name { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Codec used to read this segment. </summary>
                public Codec Codec { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Document count (does not take deletions into account). </summary>
                public int DocCount { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// True if segment is compound file format. </summary>
                public bool Compound { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Number of files referenced by this segment. </summary>
                public int NumFiles { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Net size (MB) of the files referenced by this
                /// segment.
                /// </summary>
                public double SizeMB { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Doc store offset, if this segment shares the doc
                /// store files (stored fields and term vectors) with
                /// other segments.  This is -1 if it does not share.
                /// </summary>
                public int DocStoreOffset { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// String of the shared doc store segment, or <c>null</c> if
                /// this segment does not share the doc store files.
                /// </summary>
                public string DocStoreSegment { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// True if the shared doc store files are compound file
                /// format.
                /// </summary>
                public bool DocStoreCompoundFile { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// True if this segment has pending deletions. </summary>
                public bool HasDeletions { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Current deletions generation. </summary>
                public long DeletionsGen { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Number of deleted documents. </summary>
                public int NumDeleted { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// True if we were able to open an <see cref="AtomicReader"/> on this
                /// segment.
                /// </summary>
                public bool OpenReaderPassed { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Number of fields in this segment. </summary>
                internal int NumFields { get; set; }

                /// <summary>
                /// Map that includes certain
                /// debugging details that <see cref="IndexWriter"/> records into
                /// each segment it creates
                /// </summary>
                public IDictionary<string, string> Diagnostics { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Status for testing of field norms (<c>null</c> if field norms could not be tested). </summary>
                public FieldNormStatus FieldNormStatus { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Status for testing of indexed terms (<c>null</c> if indexed terms could not be tested). </summary>
                public TermIndexStatus TermIndexStatus { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Status for testing of stored fields (<c>null</c> if stored fields could not be tested). </summary>
                public StoredFieldStatus StoredFieldStatus { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Status for testing of term vectors (<c>null</c> if term vectors could not be tested). </summary>
                public TermVectorStatus TermVectorStatus { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Status for testing of <see cref="DocValues"/> (<c>null</c> if <see cref="DocValues"/> could not be tested). </summary>
                public DocValuesStatus DocValuesStatus { get; internal set; } // LUCENENET specific - made setter internal
            }

            /// <summary>
            /// Status from testing field norms.
            /// </summary>
            public sealed class FieldNormStatus
            {
                internal FieldNormStatus()
                {
                    // Set property defaults
                    TotFields = 0L;
                    Error = null;
                }

                /// <summary>
                /// Number of fields successfully tested </summary>
                public long TotFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Exception thrown during term index test (<c>null</c> on success) </summary>
                public Exception Error { get; internal set; } // LUCENENET specific - made setter internal
            }

            /// <summary>
            /// Status from testing term index.
            /// </summary>
            public sealed class TermIndexStatus
            {
                internal TermIndexStatus()
                {
                    // Set property defaults
                    TermCount = 0L;
                    DelTermCount = 0L;
                    TotFreq = 0L;
                    TotPos = 0L;
                    Error = null;
                    BlockTreeStats = null;
                }

                /// <summary>
                /// Number of terms with at least one live doc. </summary>
                public long TermCount { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Number of terms with zero live docs docs. </summary>
                public long DelTermCount { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total frequency across all terms. </summary>
                public long TotFreq { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of positions. </summary>
                public long TotPos { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Exception thrown during term index test (<c>null</c> on success) </summary>
                public Exception Error { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Holds details of block allocations in the block
                /// tree terms dictionary (this is only set if the
                /// <see cref="PostingsFormat"/> for this segment uses block
                /// tree.
                /// </summary>
                public IDictionary<string, BlockTreeTermsReader.Stats> BlockTreeStats { get; internal set; } // LUCENENET specific - made setter internal
            }

            /// <summary>
            /// Status from testing stored fields.
            /// </summary>
            public sealed class StoredFieldStatus
            {
                internal StoredFieldStatus()
                {
                    // Set property defaults
                    DocCount = 0;
                    TotFields = 0;
                    Error = null;
                }

                /// <summary>
                /// Number of documents tested. </summary>
                public int DocCount { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of stored fields tested. </summary>
                public long TotFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Exception thrown during stored fields test (<c>null</c> on success) </summary>
                public Exception Error { get; internal set; } // LUCENENET specific - made setter internal
            }

            /// <summary>
            /// Status from testing stored fields.
            /// </summary>
            public sealed class TermVectorStatus
            {
                internal TermVectorStatus()
                {
                    // Set property defaults
                    DocCount = 0;
                    TotVectors = 0;
                    Error = null;
                }

                /// <summary>
                /// Number of documents tested. </summary>
                public int DocCount { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of term vectors tested. </summary>
                public long TotVectors { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Exception thrown during term vector test (<c>null</c> on success) </summary>
                public Exception Error { get; internal set; } // LUCENENET specific - made setter internal
            }

            /// <summary>
            /// Status from testing <see cref="DocValues"/>
            /// </summary>
            public sealed class DocValuesStatus
            {
                internal DocValuesStatus()
                {
                    // Set property defaults
                    Error = null;
                }

                /// <summary>
                /// Total number of docValues tested. </summary>
                public long TotalValueFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of numeric fields </summary>
                public long TotalNumericFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of binary fields </summary>
                public long TotalBinaryFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of sorted fields </summary>
                public long TotalSortedFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Total number of sortedset fields </summary>
                public long TotalSortedSetFields { get; internal set; } // LUCENENET specific - made setter internal

                /// <summary>
                /// Exception thrown during doc values test (<c>null</c> on success) </summary>
                public Exception Error { get; internal set; } // LUCENENET specific - made setter internal
            }
        }

        /// <summary>
        /// Create a new <see cref="CheckIndex"/> on the directory. </summary>
        public CheckIndex(Directory dir)
        {
            this.dir = dir ?? throw new ArgumentNullException(nameof(dir)); // LUCENENET: Added guard clause
            infoStream = null;
        }

        private bool crossCheckTermVectors;

        /// <summary>
        /// If <c>true</c>, term vectors are compared against postings to
        /// make sure they are the same.  This will likely
        /// drastically increase time it takes to run <see cref="CheckIndex"/>!
        /// </summary>
        public virtual bool CrossCheckTermVectors
        {
            get => crossCheckTermVectors;
            set => crossCheckTermVectors = value;
        }

        private bool verbose;

        // LUCENENET specific - added getter so we don't need to keep a reference outside of this class to dispose
        /// <summary>
        /// Gets or Sets infoStream where messages should go.  If null, no
        /// messages are printed.  If <see cref="InfoStreamIsVerbose"/> is <c>true</c> then more
        /// details are printed.
        /// </summary>
        public virtual TextWriter InfoStream
        {
            get => infoStream;
            set =>
                infoStream = value is null
                    ? null
                    : (value is SafeTextWriterWrapper ? value : new SafeTextWriterWrapper(value));
        }

        /// <summary>
        /// If <c>true</c>, prints more details to the <see cref="InfoStream"/>, if set.
        /// </summary>
        public virtual bool InfoStreamIsVerbose // LUCENENET specific (replaced overload of SetInfoStream with property)
        {
            get => this.verbose;
            set => this.verbose = value;
        }

        public virtual void FlushInfoStream() // LUCENENET specific
        {
            infoStream.Flush();
        }

        private static void Msg(TextWriter @out, string msg)
        {
            @out?.WriteLine(msg);
        }

        /// <summary>
        /// Returns a <see cref="Status"/> instance detailing
        /// the state of the index.
        ///
        /// <para/>As this method checks every byte in the index, on a large
        /// index it can take quite a long time to run.
        ///
        /// <para/><b>WARNING</b>: make sure
        /// you only call this when the index is not opened by any
        /// writer.
        /// </summary>
        public virtual Status DoCheckIndex()
        {
            return DoCheckIndex(null);
        }

        /// <summary>
        /// Returns a <see cref="Status"/> instance detailing
        /// the state of the index.
        /// </summary>
        /// <param name="onlySegments"> list of specific segment names to check
        ///
        /// <para/>As this method checks every byte in the specified
        /// segments, on a large index it can take quite a long
        /// time to run.
        ///
        /// <para/><b>WARNING</b>: make sure
        /// you only call this when the index is not opened by any
        /// writer.  </param>
        public virtual Status DoCheckIndex(IList<string> onlySegments)
        {
            NumberFormatInfo nf = CultureInfo.CurrentCulture.NumberFormat;
            SegmentInfos sis = new SegmentInfos();
            Status result = new Status();
            result.Dir = dir;
            try
            {
                sis.Read(dir);
            }
            catch (Exception t) when (t.IsThrowable())
            {
                Msg(infoStream, "ERROR: could not read any segments file in directory");
                result.MissingSegments = true;
                
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(t.ToString());
                //infoStream.WriteLine(t.StackTrace);
                
                return result;
            }

            // find the oldest and newest segment versions
            string oldest = Convert.ToString(int.MaxValue, CultureInfo.InvariantCulture), newest = Convert.ToString(int.MinValue, CultureInfo.InvariantCulture);
            string oldSegs = null;
            bool foundNonNullVersion = false;
            IComparer<string> versionComparer = StringHelper.VersionComparer;
            foreach (SegmentCommitInfo si in sis.Segments)
            {
                string version = si.Info.Version;
                if (version is null)
                {
                    // pre-3.1 segment
                    oldSegs = "pre-3.1";
                }
                else
                {
                    foundNonNullVersion = true;
                    if (versionComparer.Compare(version, oldest) < 0)
                    {
                        oldest = version;
                    }
                    if (versionComparer.Compare(version, newest) > 0)
                    {
                        newest = version;
                    }
                }
            }

            int numSegments = sis.Count;
            string segmentsFileName = sis.GetSegmentsFileName();
            // note: we only read the format byte (required preamble) here!
            IndexInput input/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            try
            {
                input = dir.OpenInput(segmentsFileName, IOContext.READ_ONCE);
            }
            catch (Exception t) when (t.IsThrowable())
            {
                Msg(infoStream, "ERROR: could not open segments file in directory");
                
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(t.ToString());
                //infoStream.WriteLine(t.StackTrace);
                
                result.CantOpenSegments = true;
                return result;
            }
            int format/* = 0*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            try
            {
                format = input.ReadInt32();
            }
            catch (Exception t) when (t.IsThrowable())
            {
                Msg(infoStream, "ERROR: could not read segment file version in directory");
                
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(t.ToString());
                //infoStream.WriteLine(t.StackTrace);
                
                result.MissingSegmentVersion = true;
                return result;
            }
            finally
            {
                input?.Dispose();
            }

            string sFormat = "";
            bool skip = false;

            result.SegmentsFileName = segmentsFileName;
            result.NumSegments = numSegments;
            result.UserData = sis.UserData;
            string userDataString;
            if (sis.UserData.Count > 0)
            {
                userDataString = " userData=" + sis.UserData;
            }
            else
            {
                userDataString = "";
            }

            string versionString/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (oldSegs != null)
            {
                if (foundNonNullVersion)
                {
                    versionString = "versions=[" + oldSegs + " .. " + newest + "]";
                }
                else
                {
                    versionString = "version=" + oldSegs;
                }
            }
            else
            {
                versionString = oldest.Equals(newest, StringComparison.Ordinal) ? ("version=" + oldest) : ("versions=[" + oldest + " .. " + newest + "]");
            }

            Msg(infoStream, "Segments file=" + segmentsFileName + " numSegments=" + numSegments + " " + versionString + " format=" + sFormat + userDataString);

            if (onlySegments != null)
            {
                result.Partial = true;
                if (infoStream != null)
                {
                    infoStream.Write("\nChecking only these segments:");
                    foreach (string s in onlySegments)
                    {
                        infoStream.Write(" " + s);
                    }
                }
                result.SegmentsChecked.AddRange(onlySegments);
                Msg(infoStream, ":");
            }

            if (skip)
            {
                Msg(infoStream, "\nERROR: this index appears to be created by a newer version of Lucene than this tool was compiled on; please re-compile this tool on the matching version of Lucene; exiting");
                result.ToolOutOfDate = true;
                return result;
            }

            result.NewSegments = (SegmentInfos)sis.Clone();
            result.NewSegments.Clear();
            result.MaxSegmentName = -1;

            // LUCENENET: We created the segments names wrong in 4.8.0-beta00001 - 4.8.0-beta00015,
            // so we added a switch to be able to read these indexes in later versions.
            int segmentRadix = SegmentInfos.useLegacySegmentNames ? 10 : J2N.Character.MaxRadix;

            for (int i = 0; i < numSegments; i++)
            {
                SegmentCommitInfo info = sis[i];
                int segmentName = 0;
                try
                {
                    // LUCENENET: Optimized to not allocate a substring during the parse
                    segmentName = Integer.Parse(info.Info.Name, 1, info.Info.Name.Length - 1, radix: segmentRadix);
                }
                catch
                {
                }
                if (segmentName > result.MaxSegmentName)
                {
                    result.MaxSegmentName = segmentName;
                }
                if (onlySegments != null && !onlySegments.Contains(info.Info.Name))
                {
                    continue;
                }
                Status.SegmentInfoStatus segInfoStat = new Status.SegmentInfoStatus();
                result.SegmentInfos.Add(segInfoStat);
                Msg(infoStream, "  " + (1 + i) + " of " + numSegments + ": name=" + info.Info.Name + " docCount=" + info.Info.DocCount);
                segInfoStat.Name = info.Info.Name;
                segInfoStat.DocCount = info.Info.DocCount;

                string version = info.Info.Version;
                if (info.Info.DocCount <= 0 && version != null && versionComparer.Compare(version, "4.5") >= 0)
                {
                    throw RuntimeException.Create("illegal number of documents: maxDoc=" + info.Info.DocCount);
                }

                int toLoseDocCount = info.Info.DocCount;

                AtomicReader reader = null;

                try
                {
                    Codec codec = info.Info.Codec;
                    Msg(infoStream, "    codec=" + codec);
                    segInfoStat.Codec = codec;
                    Msg(infoStream, "    compound=" + info.Info.UseCompoundFile);
                    segInfoStat.Compound = info.Info.UseCompoundFile;
                    Msg(infoStream, "    numFiles=" + info.GetFiles().Count);
                    segInfoStat.NumFiles = info.GetFiles().Count;
                    segInfoStat.SizeMB = info.GetSizeInBytes() / (1024.0 * 1024.0);
#pragma warning disable 612, 618
                    if (info.Info.GetAttribute(Lucene3xSegmentInfoFormat.DS_OFFSET_KEY) is null)
#pragma warning restore 612, 618
                    {
                        // don't print size in bytes if its a 3.0 segment with shared docstores
                        Msg(infoStream, "    size (MB)=" + segInfoStat.SizeMB.ToString(nf));
                    }
                    IDictionary<string, string> diagnostics = info.Info.Diagnostics;
                    segInfoStat.Diagnostics = diagnostics;
                    if (diagnostics.Count > 0)
                    {
                        Msg(infoStream, "    diagnostics = " + string.Format(J2N.Text.StringFormatter.InvariantCulture, "{0}", diagnostics));
                    }

                    if (!info.HasDeletions)
                    {
                        Msg(infoStream, "    no deletions");
                        segInfoStat.HasDeletions = false;
                    }
                    else
                    {
                        Msg(infoStream, "    has deletions [delGen=" + info.DelGen + "]");
                        segInfoStat.HasDeletions = true;
                        segInfoStat.DeletionsGen = info.DelGen;
                    }

                    infoStream?.Write("    test: open reader.........");

                    reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.DEFAULT);
                    Msg(infoStream, "OK");

                    segInfoStat.OpenReaderPassed = true;

                    infoStream?.Write("    test: check integrity.....");
                    
                    reader.CheckIntegrity();
                    Msg(infoStream, "OK");

                    infoStream?.Write("    test: check live docs.....");
                    
                    int numDocs = reader.NumDocs;
                    toLoseDocCount = numDocs;
                    if (reader.HasDeletions)
                    {
                        if (reader.NumDocs != info.Info.DocCount - info.DelCount)
                        {
                            throw RuntimeException.Create("delete count mismatch: info=" + (info.Info.DocCount - info.DelCount) + " vs reader=" + reader.NumDocs);
                        }
                        if ((info.Info.DocCount - reader.NumDocs) > reader.MaxDoc)
                        {
                            throw RuntimeException.Create("too many deleted docs: maxDoc()=" + reader.MaxDoc + " vs del count=" + (info.Info.DocCount - reader.NumDocs));
                        }
                        if (info.Info.DocCount - numDocs != info.DelCount)
                        {
                            throw RuntimeException.Create("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.Info.DocCount - numDocs));
                        }
                        IBits liveDocs = reader.LiveDocs;
                        if (liveDocs is null)
                        {
                            throw RuntimeException.Create("segment should have deletions, but liveDocs is null");
                        }
                        else
                        {
                            int numLive = 0;
                            for (int j = 0; j < liveDocs.Length; j++)
                            {
                                if (liveDocs.Get(j))
                                {
                                    numLive++;
                                }
                            }
                            if (numLive != numDocs)
                            {
                                throw RuntimeException.Create("liveDocs count mismatch: info=" + numDocs + ", vs bits=" + numLive);
                            }
                        }

                        segInfoStat.NumDeleted = info.Info.DocCount - numDocs;
                        Msg(infoStream, "OK [" + (segInfoStat.NumDeleted) + " deleted docs]");
                    }
                    else
                    {
                        if (info.DelCount != 0)
                        {
                            throw RuntimeException.Create("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.Info.DocCount - numDocs));
                        }
                        IBits liveDocs = reader.LiveDocs;
                        if (liveDocs != null)
                        {
                            // its ok for it to be non-null here, as long as none are set right?
                            for (int j = 0; j < liveDocs.Length; j++)
                            {
                                if (!liveDocs.Get(j))
                                {
                                    throw RuntimeException.Create("liveDocs mismatch: info says no deletions but doc " + j + " is deleted.");
                                }
                            }
                        }
                        Msg(infoStream, "OK");
                    }
                    if (reader.MaxDoc != info.Info.DocCount)
                    {
                        throw RuntimeException.Create("SegmentReader.MaxDoc " + reader.MaxDoc + " != SegmentInfos.docCount " + info.Info.DocCount);
                    }

                    // Test getFieldInfos()
                    infoStream?.Write("    test: fields..............");
                    
                    FieldInfos fieldInfos = reader.FieldInfos;
                    Msg(infoStream, "OK [" + fieldInfos.Count + " fields]");
                    segInfoStat.NumFields = fieldInfos.Count;

                    // Test Field Norms
                    segInfoStat.FieldNormStatus = TestFieldNorms(reader, infoStream);

                    // Test the Term Index
                    segInfoStat.TermIndexStatus = TestPostings(reader, infoStream, verbose);

                    // Test Stored Fields
                    segInfoStat.StoredFieldStatus = TestStoredFields(reader, infoStream);

                    // Test Term Vectors
                    segInfoStat.TermVectorStatus = TestTermVectors(reader, infoStream, verbose, crossCheckTermVectors);

                    segInfoStat.DocValuesStatus = TestDocValues(reader, infoStream);

                    // Rethrow the first exception we encountered
                    //  this will cause stats for failed segments to be incremented properly
                    if (segInfoStat.FieldNormStatus.Error != null)
                    {
                        throw RuntimeException.Create("Field Norm test failed");
                    }
                    else if (segInfoStat.TermIndexStatus.Error != null)
                    {
                        throw RuntimeException.Create("Term Index test failed");
                    }
                    else if (segInfoStat.StoredFieldStatus.Error != null)
                    {
                        throw RuntimeException.Create("Stored Field test failed");
                    }
                    else if (segInfoStat.TermVectorStatus.Error != null)
                    {
                        throw RuntimeException.Create("Term Vector test failed");
                    }
                    else if (segInfoStat.DocValuesStatus.Error != null)
                    {
                        throw RuntimeException.Create("DocValues test failed");
                    }

                    Msg(infoStream, "");
                }
                catch (Exception t) when (t.IsThrowable())
                {
                    Msg(infoStream, "FAILED");
                    string comment;
                    comment = "fixIndex() would remove reference to this segment";
                    Msg(infoStream, "    WARNING: " + comment + "; full exception:");

                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream?.WriteLine(t.ToString());
                    
                    Msg(infoStream, "");
                    result.TotLoseDocCount += toLoseDocCount;
                    result.NumBadSegments++;
                    continue;
                }
                finally
                {
                    reader?.Dispose();
                }

                // Keeper
                result.NewSegments.Add((SegmentCommitInfo)info.Clone());
            }

            if (0 == result.NumBadSegments)
            {
                result.Clean = true;
            }
            else
            {
                Msg(infoStream, "WARNING: " + result.NumBadSegments + " broken segments (containing " + result.TotLoseDocCount + " documents) detected");
            }

            if (!(result.ValidCounter = (result.MaxSegmentName < sis.Counter)))
            {
                result.Clean = false;
                result.NewSegments.Counter = result.MaxSegmentName + 1;
                Msg(infoStream, "ERROR: Next segment name counter " + sis.Counter + " is not greater than max segment name " + result.MaxSegmentName);
            }

            if (result.Clean)
            {
                Msg(infoStream, "No problems were detected with this index.\n");
            }

            return result;
        }

        /// <summary>
        /// Test field norms.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.FieldNormStatus TestFieldNorms(AtomicReader reader, TextWriter infoStream)
        {
            Status.FieldNormStatus status = new Status.FieldNormStatus();

            try
            {
                // Test Field Norms
                infoStream?.Write("    test: field norms.........");

                foreach (FieldInfo info in reader.FieldInfos)
                {
                    if (info.HasNorms)
                    {
#pragma warning disable 612, 618
                        if (Debugging.AssertsEnabled) Debugging.Assert(reader.HasNorms(info.Name)); // deprecated path
#pragma warning restore 612, 618
                        CheckNorms(info, reader /*, infoStream // LUCENENET: Not used */);
                        ++status.TotFields;
                    }
                    else
                    {
#pragma warning disable 612, 618
                        if (Debugging.AssertsEnabled) Debugging.Assert(!reader.HasNorms(info.Name)); // deprecated path
#pragma warning restore 612, 618
                        if (reader.GetNormValues(info.Name) != null)
                        {
                            throw RuntimeException.Create("field: " + info.Name + " should omit norms but has them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.TotFields + " fields]");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.Error = e;
             
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(e.ToString());
                //infoStream.WriteLine(e.StackTrace);      
            }

            return status;
        }

        /// <summary>
        /// Checks <see cref="Fields"/> api is consistent with itself.
        /// Searcher is optional, to verify with queries. Can be <c>null</c>.
        /// </summary>
        private static Status.TermIndexStatus CheckFields(Fields fields, IBits liveDocs, int maxDoc, FieldInfos fieldInfos, bool doPrint, bool isVectors, TextWriter infoStream, bool verbose)
        {
            // TODO: we should probably return our own stats thing...?!

            Status.TermIndexStatus status = new Status.TermIndexStatus();
            int computedFieldCount = 0;

            if (fields is null)
            {
                Msg(infoStream, "OK [no fields/terms]");
                return status;
            }

            DocsEnum docs = null;
            DocsEnum docsAndFreqs = null;
            DocsAndPositionsEnum postings = null;

            string lastField = null;
            foreach (string field in fields)
            {
                // MultiFieldsEnum relies upon this order...
                if (lastField != null && field.CompareToOrdinal(lastField) <= 0)
                {
                    throw RuntimeException.Create("fields out of order: lastField=" + lastField + " field=" + field);
                }
                lastField = field;

                // check that the field is in fieldinfos, and is indexed.
                // TODO: add a separate test to check this for different reader impls
                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                if (fieldInfo is null)
                {
                    throw RuntimeException.Create("fieldsEnum inconsistent with fieldInfos, no fieldInfos for: " + field);
                }
                if (!fieldInfo.IsIndexed)
                {
                    throw RuntimeException.Create("fieldsEnum inconsistent with fieldInfos, isIndexed == false for: " + field);
                }

                // TODO: really the codec should not return a field
                // from FieldsEnum if it has no Terms... but we do
                // this today:
                // assert fields.terms(field) != null;
                computedFieldCount++;

                Terms terms = fields.GetTerms(field);
                if (terms is null)
                {
                    continue;
                }

                bool hasFreqs = terms.HasFreqs;
                bool hasPositions = terms.HasPositions;
                bool hasPayloads = terms.HasPayloads;
                bool hasOffsets = terms.HasOffsets;

                // term vectors cannot omit TF:
                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                bool expectedHasFreqs = (isVectors || IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0);

                if (hasFreqs != expectedHasFreqs)
                {
                    throw RuntimeException.Create("field \"" + field + "\" should have hasFreqs=" + expectedHasFreqs + " but got " + hasFreqs);
                }

                if (hasFreqs == false)
                {
                    if (terms.SumTotalTermFreq != -1)
                    {
                        throw RuntimeException.Create("field \"" + field + "\" hasFreqs is false, but Terms.getSumTotalTermFreq()=" + terms.SumTotalTermFreq + " (should be -1)");
                    }
                }

                if (!isVectors)
                {
                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    bool expectedHasPositions = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS) >= 0;
                    if (hasPositions != expectedHasPositions)
                    {
                        throw RuntimeException.Create("field \"" + field + "\" should have hasPositions=" + expectedHasPositions + " but got " + hasPositions);
                    }

                    bool expectedHasPayloads = fieldInfo.HasPayloads;
                    if (hasPayloads != expectedHasPayloads)
                    {
                        throw RuntimeException.Create("field \"" + field + "\" should have hasPayloads=" + expectedHasPayloads + " but got " + hasPayloads);
                    }

                    // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                    bool expectedHasOffsets = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
                    if (hasOffsets != expectedHasOffsets)
                    {
                        throw RuntimeException.Create("field \"" + field + "\" should have hasOffsets=" + expectedHasOffsets + " but got " + hasOffsets);
                    }
                }

                TermsEnum termsEnum = terms.GetEnumerator();

                bool hasOrd = true;
                long termCountStart = status.DelTermCount + status.TermCount;

                BytesRef lastTerm = null;

                IComparer<BytesRef> termComp = terms.Comparer;

                long sumTotalTermFreq = 0;
                long sumDocFreq = 0;
                FixedBitSet visitedDocs = new FixedBitSet(maxDoc);
                while (termsEnum.MoveNext())
                {
                    BytesRef term = termsEnum.Term;

                    if (Debugging.AssertsEnabled) Debugging.Assert(term.IsValid());

                    // make sure terms arrive in order according to
                    // the comp
                    if (lastTerm is null)
                    {
                        lastTerm = BytesRef.DeepCopyOf(term);
                    }
                    else
                    {
                        if (termComp.Compare(lastTerm, term) >= 0)
                        {
                            throw RuntimeException.Create("terms out of order: lastTerm=" + lastTerm + " term=" + term);
                        }
                        lastTerm.CopyBytes(term);
                    }

                    int docFreq = termsEnum.DocFreq;
                    if (docFreq <= 0)
                    {
                        throw RuntimeException.Create("docfreq: " + docFreq + " is out of bounds");
                    }
                    sumDocFreq += docFreq;

                    docs = termsEnum.Docs(liveDocs, docs);
                    postings = termsEnum.DocsAndPositions(liveDocs, postings);

                    if (hasFreqs == false)
                    {
                        if (termsEnum.TotalTermFreq != -1)
                        {
                            throw RuntimeException.Create("field \"" + field + "\" hasFreqs is false, but TermsEnum.totalTermFreq()=" + termsEnum.TotalTermFreq + " (should be -1)");
                        }
                    }

                    if (hasOrd)
                    {
                        long ord = -1;
                        try
                        {
                            ord = termsEnum.Ord;
                        }
                        catch (Exception uoe) when (uoe.IsUnsupportedOperationException())
                        {
                            hasOrd = false;
                        }

                        if (hasOrd)
                        {
                            long ordExpected = status.DelTermCount + status.TermCount - termCountStart;
                            if (ord != ordExpected)
                            {
                                throw RuntimeException.Create("ord mismatch: TermsEnum has ord=" + ord + " vs actual=" + ordExpected);
                            }
                        }
                    }

                    DocsEnum docs2;
                    if (postings != null)
                    {
                        docs2 = postings;
                    }
                    else
                    {
                        docs2 = docs;
                    }

                    int lastDoc = -1;
                    int docCount = 0;
                    long totalTermFreq = 0;
                    while (true)
                    {
                        int doc = docs2.NextDoc();
                        if (doc == DocIdSetIterator.NO_MORE_DOCS)
                        {
                            break;
                        }
                        status.TotFreq++;
                        visitedDocs.Set(doc);
                        int freq = -1;
                        if (hasFreqs)
                        {
                            freq = docs2.Freq;
                            if (freq <= 0)
                            {
                                throw RuntimeException.Create("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");
                            }
                            status.TotPos += freq;
                            totalTermFreq += freq;
                        }
                        else
                        {
                            // When a field didn't index freq, it must
                            // consistently "lie" and pretend that freq was
                            // 1:
                            if (docs2.Freq != 1)
                            {
                                throw RuntimeException.Create("term " + term + ": doc " + doc + ": freq " + freq + " != 1 when Terms.hasFreqs() is false");
                            }
                        }
                        docCount++;

                        if (doc <= lastDoc)
                        {
                            throw RuntimeException.Create("term " + term + ": doc " + doc + " <= lastDoc " + lastDoc);
                        }
                        if (doc >= maxDoc)
                        {
                            throw RuntimeException.Create("term " + term + ": doc " + doc + " >= maxDoc " + maxDoc);
                        }

                        lastDoc = doc;

                        int lastPos = -1;
                        int lastOffset = 0;
                        if (hasPositions)
                        {
                            for (int j = 0; j < freq; j++)
                            {
                                int pos = postings.NextPosition();

                                if (pos < 0)
                                {
                                    throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
                                }
                                if (pos < lastPos)
                                {
                                    throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
                                }
                                lastPos = pos;
                                BytesRef payload = postings.GetPayload();
                                // LUCENENET specific - restructured to reduce number of checks in production
                                if (!(payload is null))
                                {
                                    if (Debugging.AssertsEnabled) Debugging.Assert(payload.IsValid());
                                    if (payload.Length < 1)
                                    {
                                        throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + " payload length is out of bounds " + payload.Length);
                                    }
                                }
                                if (hasOffsets)
                                {
                                    int startOffset = postings.StartOffset;
                                    int endOffset = postings.EndOffset;
                                    // NOTE: we cannot enforce any bounds whatsoever on vectors... they were a free-for-all before?
                                    // but for offsets in the postings lists these checks are fine: they were always enforced by IndexWriter
                                    if (!isVectors)
                                    {
                                        if (startOffset < 0)
                                        {
                                            throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                        }
                                        if (startOffset < lastOffset)
                                        {
                                            throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                        }
                                        if (endOffset < 0)
                                        {
                                            throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                        }
                                        if (endOffset < startOffset)
                                        {
                                            throw RuntimeException.Create("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
                                        }
                                    }
                                    lastOffset = startOffset;
                                }
                            }
                        }
                    }

                    if (docCount != 0)
                    {
                        status.TermCount++;
                    }
                    else
                    {
                        status.DelTermCount++;
                    }

                    long totalTermFreq2 = termsEnum.TotalTermFreq;
                    bool hasTotalTermFreq = hasFreqs && totalTermFreq2 != -1;

                    // Re-count if there are deleted docs:
                    if (liveDocs != null)
                    {
                        if (hasFreqs)
                        {
                            DocsEnum docsNoDel = termsEnum.Docs(null, docsAndFreqs);
                            docCount = 0;
                            totalTermFreq = 0;
                            while (docsNoDel.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                visitedDocs.Set(docsNoDel.DocID);
                                docCount++;
                                totalTermFreq += docsNoDel.Freq;
                            }
                        }
                        else
                        {
                            DocsEnum docsNoDel = termsEnum.Docs(null, docs, DocsFlags.NONE);
                            docCount = 0;
                            totalTermFreq = -1;
                            while (docsNoDel.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                visitedDocs.Set(docsNoDel.DocID);
                                docCount++;
                            }
                        }
                    }

                    if (docCount != docFreq)
                    {
                        throw RuntimeException.Create("term " + term + " docFreq=" + docFreq + " != tot docs w/o deletions " + docCount);
                    }
                    if (hasTotalTermFreq)
                    {
                        if (totalTermFreq2 <= 0)
                        {
                            throw RuntimeException.Create("totalTermFreq: " + totalTermFreq2 + " is out of bounds");
                        }
                        sumTotalTermFreq += totalTermFreq;
                        if (totalTermFreq != totalTermFreq2)
                        {
                            throw RuntimeException.Create("term " + term + " totalTermFreq=" + totalTermFreq2 + " != recomputed totalTermFreq=" + totalTermFreq);
                        }
                    }

                    // Test skipping
                    if (hasPositions)
                    {
                        for (int idx = 0; idx < 7; idx++)
                        {
                            int skipDocID = (int)(((idx + 1) * (long)maxDoc) / 8);
                            postings = termsEnum.DocsAndPositions(liveDocs, postings);
                            int docID = postings.Advance(skipDocID);
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            else
                            {
                                if (docID < skipDocID)
                                {
                                    throw RuntimeException.Create("term " + term + ": advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int freq = postings.Freq;
                                if (freq <= 0)
                                {
                                    throw RuntimeException.Create("termFreq " + freq + " is out of bounds");
                                }
                                int lastPosition = -1;
                                int lastOffset = 0;
                                for (int posUpto = 0; posUpto < freq; posUpto++)
                                {
                                    int pos = postings.NextPosition();

                                    if (pos < 0)
                                    {
                                        throw RuntimeException.Create("position " + pos + " is out of bounds");
                                    }
                                    if (pos < lastPosition)
                                    {
                                        throw RuntimeException.Create("position " + pos + " is < lastPosition " + lastPosition);
                                    }
                                    lastPosition = pos;
                                    if (hasOffsets)
                                    {
                                        int startOffset = postings.StartOffset;
                                        int endOffset = postings.EndOffset;
                                        // NOTE: we cannot enforce any bounds whatsoever on vectors... they were a free-for-all before?
                                        // but for offsets in the postings lists these checks are fine: they were always enforced by IndexWriter
                                        if (!isVectors)
                                        {
                                            if (startOffset < 0)
                                            {
                                                throw RuntimeException.Create("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                            }
                                            if (startOffset < lastOffset)
                                            {
                                                throw RuntimeException.Create("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                            }
                                            if (endOffset < 0)
                                            {
                                                throw RuntimeException.Create("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                            }
                                            if (endOffset < startOffset)
                                            {
                                                throw RuntimeException.Create("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
                                            }
                                        }
                                        lastOffset = startOffset;
                                    }
                                }

                                int nextDocID = postings.NextDoc();
                                if (nextDocID == DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    break;
                                }
                                if (nextDocID <= docID)
                                {
                                    throw RuntimeException.Create("term " + term + ": Advance(docID=" + skipDocID + "), then .Next() returned docID=" + nextDocID + " vs prev docID=" + docID);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int idx = 0; idx < 7; idx++)
                        {
                            int skipDocID = (int)(((idx + 1) * (long)maxDoc) / 8);
                            docs = termsEnum.Docs(liveDocs, docs, DocsFlags.NONE);
                            int docID = docs.Advance(skipDocID);
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            else
                            {
                                if (docID < skipDocID)
                                {
                                    throw RuntimeException.Create("term " + term + ": Advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int nextDocID = docs.NextDoc();
                                if (nextDocID == DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    break;
                                }
                                if (nextDocID <= docID)
                                {
                                    throw RuntimeException.Create("term " + term + ": Advance(docID=" + skipDocID + "), then .Next() returned docID=" + nextDocID + " vs prev docID=" + docID);
                                }
                            }
                        }
                    }
                }

                Terms fieldTerms = fields.GetTerms(field);
                if (fieldTerms is null)
                {
                    // Unusual: the FieldsEnum returned a field but
                    // the Terms for that field is null; this should
                    // only happen if it's a ghost field (field with
                    // no terms, eg there used to be terms but all
                    // docs got deleted and then merged away):
                }
                else
                {
                    if (fieldTerms is BlockTreeTermsReader.FieldReader fieldReader)
                    {
                        BlockTreeTermsReader.Stats stats = fieldReader.ComputeStats();
                        if (Debugging.AssertsEnabled) Debugging.Assert(stats != null);
                        if (status.BlockTreeStats is null)
                        {
                            status.BlockTreeStats = new Dictionary<string, BlockTreeTermsReader.Stats>();
                        }
                        status.BlockTreeStats[field] = stats;
                    }

                    if (sumTotalTermFreq != 0)
                    {
                        long v = fields.GetTerms(field).SumTotalTermFreq;
                        if (v != -1 && sumTotalTermFreq != v)
                        {
                            throw RuntimeException.Create("sumTotalTermFreq for field " + field + "=" + v + " != recomputed sumTotalTermFreq=" + sumTotalTermFreq);
                        }
                    }

                    if (sumDocFreq != 0)
                    {
                        long v = fields.GetTerms(field).SumDocFreq;
                        if (v != -1 && sumDocFreq != v)
                        {
                            throw RuntimeException.Create("sumDocFreq for field " + field + "=" + v + " != recomputed sumDocFreq=" + sumDocFreq);
                        }
                    }

                    if (fieldTerms != null)
                    {
                        int v = fieldTerms.DocCount;
                        if (v != -1 && visitedDocs.Cardinality != v)
                        {
                            throw RuntimeException.Create("docCount for field " + field + "=" + v + " != recomputed docCount=" + visitedDocs.Cardinality);
                        }
                    }

                    // Test seek to last term:
                    if (lastTerm != null)
                    {
                        if (termsEnum.SeekCeil(lastTerm) != TermsEnum.SeekStatus.FOUND)
                        {
                            throw RuntimeException.Create("seek to last term " + lastTerm + " failed");
                        }

                        int expectedDocFreq = termsEnum.DocFreq;
                        DocsEnum d = termsEnum.Docs(null, null, DocsFlags.NONE);
                        int docFreq = 0;
                        while (d.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            docFreq++;
                        }
                        if (docFreq != expectedDocFreq)
                        {
                            throw RuntimeException.Create("docFreq for last term " + lastTerm + "=" + expectedDocFreq + " != recomputed docFreq=" + docFreq);
                        }
                    }

                    // check unique term count
                    long termCount = -1;

                    if ((status.DelTermCount + status.TermCount) - termCountStart > 0)
                    {
                        termCount = fields.GetTerms(field).Count;

                        if (termCount != -1 && termCount != status.DelTermCount + status.TermCount - termCountStart)
                        {
                            throw RuntimeException.Create("termCount mismatch " + (status.DelTermCount + termCount) + " vs " + (status.TermCount - termCountStart));
                        }
                    }

                    // Test seeking by ord
                    if (hasOrd && status.TermCount - termCountStart > 0)
                    {
                        int seekCount = (int)Math.Min(10000L, termCount);
                        if (seekCount > 0)
                        {
                            BytesRef[] seekTerms = new BytesRef[seekCount];

                            // Seek by ord
                            for (int i = seekCount - 1; i >= 0; i--)
                            {
                                long ord = i * (termCount / seekCount);
                                termsEnum.SeekExact(ord);
                                seekTerms[i] = BytesRef.DeepCopyOf(termsEnum.Term);
                            }

                            // Seek by term
                            long totDocCount = 0;
                            for (int i = seekCount - 1; i >= 0; i--)
                            {
                                if (termsEnum.SeekCeil(seekTerms[i]) != TermsEnum.SeekStatus.FOUND)
                                {
                                    throw RuntimeException.Create("seek to existing term " + seekTerms[i] + " failed");
                                }

                                docs = termsEnum.Docs(liveDocs, docs, DocsFlags.NONE);
                                if (docs is null)
                                {
                                    throw RuntimeException.Create("null DocsEnum from to existing term " + seekTerms[i]);
                                }

                                while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    totDocCount++;
                                }
                            }

                            long totDocCountNoDeletes = 0;
                            long totDocFreq = 0;
                            for (int i = 0; i < seekCount; i++)
                            {
                                if (!termsEnum.SeekExact(seekTerms[i]))
                                {
                                    throw RuntimeException.Create("seek to existing term " + seekTerms[i] + " failed");
                                }

                                totDocFreq += termsEnum.DocFreq;
                                docs = termsEnum.Docs(null, docs, DocsFlags.NONE);
                                if (docs is null)
                                {
                                    throw RuntimeException.Create("null DocsEnum from to existing term " + seekTerms[i]);
                                }

                                while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    totDocCountNoDeletes++;
                                }
                            }

                            if (totDocCount > totDocCountNoDeletes)
                            {
                                throw RuntimeException.Create("more postings with deletes=" + totDocCount + " than without=" + totDocCountNoDeletes);
                            }

                            if (totDocCountNoDeletes != totDocFreq)
                            {
                                throw RuntimeException.Create("docfreqs=" + totDocFreq + " != recomputed docfreqs=" + totDocCountNoDeletes);
                            }
                        }
                    }
                }
            }

            int fieldCount = fields.Count;

            if (fieldCount != -1)
            {
                if (fieldCount < 0)
                {
                    throw RuntimeException.Create("invalid fieldCount: " + fieldCount);
                }
                if (fieldCount != computedFieldCount)
                {
                    throw RuntimeException.Create("fieldCount mismatch " + fieldCount + " vs recomputed field count " + computedFieldCount);
                }
            }

            // for most implementations, this is boring (just the sum across all fields)
            // but codecs that don't work per-field like preflex actually implement this,
            // but don't implement it on Terms, so the check isn't redundant.
#pragma warning disable 612, 618
            long uniqueTermCountAllFields = fields.UniqueTermCount;
#pragma warning restore 612, 618

            if (uniqueTermCountAllFields != -1 && status.TermCount + status.DelTermCount != uniqueTermCountAllFields)
            {
                throw RuntimeException.Create("termCount mismatch " + uniqueTermCountAllFields + " vs " + (status.TermCount + status.DelTermCount));
            }

            if (doPrint)
            {
                Msg(infoStream, "OK [" + status.TermCount + " terms; " + status.TotFreq + " terms/docs pairs; " + status.TotPos + " tokens]");
            }

            if (verbose && status.BlockTreeStats != null && infoStream != null && status.TermCount > 0)
            {
                foreach (KeyValuePair<string, BlockTreeTermsReader.Stats> ent in status.BlockTreeStats)
                {
                    infoStream.WriteLine("      field \"" + ent.Key + "\":");
                    infoStream.WriteLine("      " + ent.Value.ToString().Replace("\n", "\n      "));
                }
            }

            return status;
        }

        /// <summary>
        /// Test the term index.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.TermIndexStatus TestPostings(AtomicReader reader, TextWriter infoStream)
        {
            return TestPostings(reader, infoStream, false);
        }

        /// <summary>
        /// Test the term index.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.TermIndexStatus TestPostings(AtomicReader reader, TextWriter infoStream, bool verbose)
        {
            // TODO: we should go and verify term vectors match, if
            // crossCheckTermVectors is on...

            Status.TermIndexStatus status;
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;

            try
            {
                infoStream?.Write("    test: terms, freq, prox...");
                
                Fields fields = reader.Fields;
                FieldInfos fieldInfos = reader.FieldInfos;
                status = CheckFields(fields, liveDocs, maxDoc, fieldInfos, true, false, infoStream, verbose);
                if (liveDocs != null)
                {
                    infoStream?.Write("    test (ignoring deletes): terms, freq, prox...");
                    CheckFields(fields, null, maxDoc, fieldInfos, true, false, infoStream, verbose);
                }
            }
            catch (Exception e) when (e.IsThrowable())
            {
                Msg(infoStream, "ERROR: " + e);
                status = new Status.TermIndexStatus();
                status.Error = e;
                
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(e.ToString());
                //infoStream.WriteLine(e.StackTrace);                
            }

            return status;
        }

        /// <summary>
        /// Test stored fields.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.StoredFieldStatus TestStoredFields(AtomicReader reader, TextWriter infoStream)
        {
            Status.StoredFieldStatus status = new Status.StoredFieldStatus();

            try
            {
                infoStream?.Write("    test: stored fields.......");

                // Scan stored fields for all documents
                IBits liveDocs = reader.LiveDocs;
                for (int j = 0; j < reader.MaxDoc; ++j)
                {
                    // Intentionally pull even deleted documents to
                    // make sure they too are not corrupt:
                    Document doc = reader.Document(j);
                    if (liveDocs is null || liveDocs.Get(j))
                    {
                        status.DocCount++;
                        status.TotFields += doc.Fields.Count;
                    }
                }

                // Validate docCount
                if (status.DocCount != reader.NumDocs)
                {
                    throw RuntimeException.Create("docCount=" + status.DocCount + " but saw " + status.DocCount + " undeleted docs");
                }

                Msg(infoStream, "OK [" + status.TotFields + " total field count; avg " + ((((float)status.TotFields) / status.DocCount)).ToString(CultureInfo.InvariantCulture.NumberFormat) + " fields per doc]");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.Error = e;
               
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(e.ToString());
                //infoStream.WriteLine(e.StackTrace);                
            }

            return status;
        }

        /// <summary>
        /// Test docvalues.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.DocValuesStatus TestDocValues(AtomicReader reader, TextWriter infoStream)
        {
            Status.DocValuesStatus status = new Status.DocValuesStatus();
            try
            {
                infoStream?.Write("    test: docvalues...........");
                
                foreach (FieldInfo fieldInfo in reader.FieldInfos)
                {
                    if (fieldInfo.HasDocValues)
                    {
                        status.TotalValueFields++;
                        CheckDocValues(fieldInfo, reader, /*infoStream,*/ status);
                    }
                    else
                    {
                        if (reader.GetBinaryDocValues(fieldInfo.Name) != null || reader.GetNumericDocValues(fieldInfo.Name) != null || reader.GetSortedDocValues(fieldInfo.Name) != null || reader.GetSortedSetDocValues(fieldInfo.Name) != null || reader.GetDocsWithField(fieldInfo.Name) != null)
                        {
                            throw RuntimeException.Create("field: " + fieldInfo.Name + " has docvalues but should omit them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.TotalValueFields + " docvalues fields; " + status.TotalBinaryFields + " BINARY; " + status.TotalNumericFields + " NUMERIC; " + status.TotalSortedFields + " SORTED; " + status.TotalSortedSetFields + " SORTED_SET]");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.Error = e;
                
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(e.ToString());
                //infoStream.WriteLine(e.StackTrace);                
            }
            return status;
        }

        private static void CheckBinaryDocValues(string fieldName, AtomicReader reader, BinaryDocValues dv, IBits docsWithField)
        {
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.Get(i, scratch);
                if (Debugging.AssertsEnabled) Debugging.Assert(scratch.IsValid());
                if (docsWithField.Get(i) == false && scratch.Length > 0)
                {
                    throw RuntimeException.Create("dv for field: " + fieldName + " is missing but has value=" + scratch + " for doc: " + i);
                }
            }
        }

        private static void CheckSortedDocValues(string fieldName, AtomicReader reader, SortedDocValues dv, IBits docsWithField)
        {
            CheckBinaryDocValues(fieldName, reader, dv, docsWithField);
            int maxOrd = dv.ValueCount - 1;
            FixedBitSet seenOrds = new FixedBitSet(dv.ValueCount);
            int maxOrd2 = -1;
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                int ord = dv.GetOrd(i);
                if (ord == -1)
                {
                    if (docsWithField.Get(i))
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " has -1 ord but is not marked missing for doc: " + i);
                    }
                }
                else if (ord < -1 || ord > maxOrd)
                {
                    throw RuntimeException.Create("ord out of bounds: " + ord);
                }
                else
                {
                    if (!docsWithField.Get(i))
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " is missing but has ord=" + ord + " for doc: " + i);
                    }
                    maxOrd2 = Math.Max(maxOrd2, ord);
                    seenOrds.Set(ord);
                }
            }
            if (maxOrd != maxOrd2)
            {
                throw RuntimeException.Create("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality != dv.ValueCount)
            {
                throw RuntimeException.Create("dv for field: " + fieldName + " has holes in its ords, ValueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality);
            }
            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (int i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                if (Debugging.AssertsEnabled) Debugging.Assert(scratch.IsValid());
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckSortedSetDocValues(string fieldName, AtomicReader reader, SortedSetDocValues dv, IBits docsWithField)
        {
            long maxOrd = dv.ValueCount - 1;
            Int64BitSet seenOrds = new Int64BitSet(dv.ValueCount);
            long maxOrd2 = -1;
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.SetDocument(i);
                long lastOrd = -1;
                long ord;
                if (docsWithField.Get(i))
                {
                    int ordCount = 0;
                    while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ord <= lastOrd)
                        {
                            throw RuntimeException.Create("ords out of order: " + ord + " <= " + lastOrd + " for doc: " + i);
                        }
                        if (ord < 0 || ord > maxOrd)
                        {
                            throw RuntimeException.Create("ord out of bounds: " + ord);
                        }
                        if (dv is RandomAccessOrds randomAccessOrds2)
                        {
                            long ord2 = randomAccessOrds2.OrdAt(ordCount);
                            if (ord != ord2)
                            {
                                throw RuntimeException.Create("OrdAt(" + ordCount + ") inconsistent, expected=" + ord + ",got=" + ord2 + " for doc: " + i);
                            }
                        }
                        lastOrd = ord;
                        maxOrd2 = Math.Max(maxOrd2, ord);
                        seenOrds.Set(ord);
                        ordCount++;
                    }
                    if (ordCount == 0)
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " has no ordinals but is not marked missing for doc: " + i);
                    }
                    if (dv is RandomAccessOrds randomAccessOrds)
                    {
                        long ordCount2 = randomAccessOrds.Cardinality;
                        if (ordCount != ordCount2)
                        {
                            throw RuntimeException.Create("cardinality inconsistent, expected=" + ordCount + ",got=" + ordCount2 + " for doc: " + i);
                        }
                    }
                }
                else
                {
                    long o = dv.NextOrd();
                    if (o != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " is marked missing but has ord=" + o + " for doc: " + i);
                    }
                    if (dv is RandomAccessOrds randomAccessOrds)
                    {
                        long ordCount2 = randomAccessOrds.Cardinality;
                        if (ordCount2 != 0)
                        {
                            throw RuntimeException.Create("dv for field: " + fieldName + " is marked missing but has cardinality " + ordCount2 + " for doc: " + i);
                        }
                    }
                }
            }
            if (maxOrd != maxOrd2)
            {
                throw RuntimeException.Create("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality != dv.ValueCount)
            {
                throw RuntimeException.Create("dv for field: " + fieldName + " has holes in its ords, valueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality);
            }

            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (long i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                if (Debugging.AssertsEnabled) Debugging.Assert(scratch.IsValid());
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw RuntimeException.Create("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckNumericDocValues(string fieldName, AtomicReader reader, NumericDocValues ndv, IBits docsWithField)
        {
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                long value = ndv.Get(i);
                if (docsWithField.Get(i) == false && value != 0)
                {
                    throw RuntimeException.Create("dv for field: " + fieldName + " is marked missing but has value=" + value + " for doc: " + i);
                }
            }
        }

        private static void CheckDocValues(FieldInfo fi, AtomicReader reader, /*StreamWriter infoStream,*/ DocValuesStatus status)
        {
            IBits docsWithField = reader.GetDocsWithField(fi.Name);
            if (docsWithField is null)
            {
                throw RuntimeException.Create(fi.Name + " docsWithField does not exist");
            }
            else if (docsWithField.Length != reader.MaxDoc)
            {
                throw RuntimeException.Create(fi.Name + " docsWithField has incorrect length: " + docsWithField.Length + ",expected: " + reader.MaxDoc);
            }
            switch (fi.DocValuesType)
            {
                case DocValuesType.SORTED:
                    status.TotalSortedFields++;
                    CheckSortedDocValues(fi.Name, reader, reader.GetSortedDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw RuntimeException.Create(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case DocValuesType.SORTED_SET:
                    status.TotalSortedSetFields++;
                    CheckSortedSetDocValues(fi.Name, reader, reader.GetSortedSetDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null)
                    {
                        throw RuntimeException.Create(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case DocValuesType.BINARY:
                    status.TotalBinaryFields++;
                    CheckBinaryDocValues(fi.Name, reader, reader.GetBinaryDocValues(fi.Name), docsWithField);
                    if (reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw RuntimeException.Create(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case DocValuesType.NUMERIC:
                    status.TotalNumericFields++;
                    CheckNumericDocValues(fi.Name, reader, reader.GetNumericDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw RuntimeException.Create(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                default:
                    throw AssertionError.Create();
            }
        }

        private static void CheckNorms(FieldInfo fi, AtomicReader reader /*, TextWriter infoStream // LUCENENET: Not used */)
        {
            switch (fi.NormType)
            {
                case DocValuesType.NUMERIC:
                    CheckNumericDocValues(fi.Name, reader, reader.GetNormValues(fi.Name), new Bits.MatchAllBits(reader.MaxDoc));
                    break;

                default:
                    throw AssertionError.Create("wtf: " + fi.NormType);
            }
        }

        /// <summary>
        /// Test term vectors.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, TextWriter infoStream)
        {
            return TestTermVectors(reader, infoStream, false, false);
        }

        /// <summary>
        /// Test term vectors.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, TextWriter infoStream, bool verbose, bool crossCheckTermVectors)
        {
            Status.TermVectorStatus status = new Status.TermVectorStatus();
            FieldInfos fieldInfos = reader.FieldInfos;
            IBits onlyDocIsDeleted = new FixedBitSet(1);

            try
            {
                infoStream?.Write("    test: term vectors........");

                DocsEnum docs = null;
                DocsAndPositionsEnum postings = null;

                // Only used if crossCheckTermVectors is true:
                DocsEnum postingsDocs = null;
                DocsAndPositionsEnum postingsPostings = null;

                IBits liveDocs = reader.LiveDocs;

                Fields postingsFields;
                // TODO: testTermsIndex
                if (crossCheckTermVectors)
                {
                    postingsFields = reader.Fields;
                }
                else
                {
                    postingsFields = null;
                }

                TermsEnum termsEnum = null;
                TermsEnum postingsTermsEnum = null;

                for (int j = 0; j < reader.MaxDoc; ++j)
                {
                    // Intentionally pull/visit (but don't count in
                    // stats) deleted documents to make sure they too
                    // are not corrupt:
                    Fields tfv = reader.GetTermVectors(j);

                    // TODO: can we make a IS(FIR) that searches just
                    // this term vector... to pass for searcher?

                    if (tfv != null)
                    {
                        // First run with no deletions:
                        CheckFields(tfv, null, 1, fieldInfos, false, true, infoStream, verbose);

                        // Again, with the one doc deleted:
                        CheckFields(tfv, onlyDocIsDeleted, 1, fieldInfos, false, true, infoStream, verbose);

                        // Only agg stats if the doc is live:
                        bool doStats = liveDocs is null || liveDocs.Get(j);
                        if (doStats)
                        {
                            status.DocCount++;
                        }

                        foreach (string field in tfv)
                        {
                            if (doStats)
                            {
                                status.TotVectors++;
                            }

                            // Make sure FieldInfo thinks this field is vector'd:
                            FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                            if (!fieldInfo.HasVectors)
                            {
                                throw RuntimeException.Create("docID=" + j + " has term vectors for field=" + field + " but FieldInfo has storeTermVector=false");
                            }

                            if (crossCheckTermVectors)
                            {
                                Terms terms = tfv.GetTerms(field);
                                termsEnum = terms.GetEnumerator(termsEnum);
                                // LUCENENET specific - to avoid boxing, changed from CompareTo() to IndexOptionsComparer.Compare()
                                bool postingsHasFreq = IndexOptionsComparer.Default.Compare(fieldInfo.IndexOptions, IndexOptions.DOCS_AND_FREQS) >= 0;
                                bool postingsHasPayload = fieldInfo.HasPayloads;
                                bool vectorsHasPayload = terms.HasPayloads;

                                Terms postingsTerms = postingsFields.GetTerms(field);
                                if (postingsTerms is null)
                                {
                                    throw RuntimeException.Create("vector field=" + field + " does not exist in postings; doc=" + j);
                                }
                                postingsTermsEnum = postingsTerms.GetEnumerator(postingsTermsEnum);

                                bool hasProx = terms.HasOffsets || terms.HasPositions;
                                BytesRef term;
                                while (termsEnum.MoveNext())
                                {
                                    term = termsEnum.Term;
                                    if (hasProx)
                                    {
                                        postings = termsEnum.DocsAndPositions(null, postings);
                                        if (Debugging.AssertsEnabled) Debugging.Assert(postings != null);
                                        docs = null;
                                    }
                                    else
                                    {
                                        docs = termsEnum.Docs(null, docs);
                                        if (Debugging.AssertsEnabled) Debugging.Assert(docs != null);
                                        postings = null;
                                    }

                                    DocsEnum docs2;
                                    if (hasProx)
                                    {
                                        if (Debugging.AssertsEnabled) Debugging.Assert(postings != null);
                                        docs2 = postings;
                                    }
                                    else
                                    {
                                        if (Debugging.AssertsEnabled) Debugging.Assert(docs != null);
                                        docs2 = docs;
                                    }

                                    DocsEnum postingsDocs2;
                                    if (!postingsTermsEnum.SeekExact(term))
                                    {
                                        throw RuntimeException.Create("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
                                    }
                                    postingsPostings = postingsTermsEnum.DocsAndPositions(null, postingsPostings);
                                    if (postingsPostings is null)
                                    {
                                        // Term vectors were indexed w/ pos but postings were not
                                        postingsDocs = postingsTermsEnum.Docs(null, postingsDocs);
                                        if (postingsDocs is null)
                                        {
                                            throw RuntimeException.Create("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
                                        }
                                    }

                                    if (postingsPostings != null)
                                    {
                                        postingsDocs2 = postingsPostings;
                                    }
                                    else
                                    {
                                        postingsDocs2 = postingsDocs;
                                    }

                                    int advanceDoc = postingsDocs2.Advance(j);
                                    if (advanceDoc != j)
                                    {
                                        throw RuntimeException.Create("vector term=" + term + " field=" + field + ": doc=" + j + " was not found in postings (got: " + advanceDoc + ")");
                                    }

                                    int doc = docs2.NextDoc();

                                    if (doc != 0)
                                    {
                                        throw RuntimeException.Create("vector for doc " + j + " didn't return docID=0: got docID=" + doc);
                                    }

                                    if (postingsHasFreq)
                                    {
                                        int tf = docs2.Freq;
                                        if (postingsHasFreq && postingsDocs2.Freq != tf)
                                        {
                                            throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + ": freq=" + tf + " differs from postings freq=" + postingsDocs2.Freq);
                                        }

                                        if (hasProx)
                                        {
                                            for (int i = 0; i < tf; i++)
                                            {
                                                int pos = postings.NextPosition();
                                                if (postingsPostings != null)
                                                {
                                                    int postingsPos = postingsPostings.NextPosition();
                                                    if (terms.HasPositions && pos != postingsPos)
                                                    {
                                                        throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + ": pos=" + pos + " differs from postings pos=" + postingsPos);
                                                    }
                                                }

                                                // Call the methods to at least make
                                                // sure they don't throw exc:
                                                int startOffset = postings.StartOffset;
                                                int endOffset = postings.EndOffset;
                                                // TODO: these are too anal...?
                                                /*
                                                  if (endOffset < startOffset) {
                                                  throw RuntimeException.Create("vector startOffset=" + startOffset + " is > endOffset=" + endOffset);
                                                  }
                                                  if (startOffset < lastStartOffset) {
                                                  throw RuntimeException.Create("vector startOffset=" + startOffset + " is < prior startOffset=" + lastStartOffset);
                                                  }
                                                  lastStartOffset = startOffset;
                                                */

                                                if (postingsPostings != null)
                                                {
                                                    int postingsStartOffset = postingsPostings.StartOffset;

                                                    int postingsEndOffset = postingsPostings.EndOffset;
                                                    if (startOffset != -1 && postingsStartOffset != -1 && startOffset != postingsStartOffset)
                                                    {
                                                        throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + ": startOffset=" + startOffset + " differs from postings startOffset=" + postingsStartOffset);
                                                    }
                                                    if (endOffset != -1 && postingsEndOffset != -1 && endOffset != postingsEndOffset)
                                                    {
                                                        throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + ": endOffset=" + endOffset + " differs from postings endOffset=" + postingsEndOffset);
                                                    }
                                                }

                                                BytesRef payload = postings.GetPayload();

                                                if (payload != null)
                                                {
                                                    if (Debugging.AssertsEnabled) Debugging.Assert(vectorsHasPayload);
                                                }

                                                if (postingsHasPayload && vectorsHasPayload)
                                                {
                                                    if (Debugging.AssertsEnabled) Debugging.Assert(postingsPostings != null);

                                                    if (payload is null)
                                                    {
                                                        // we have payloads, but not at this position.
                                                        // postings has payloads too, it should not have one at this position
                                                        if (postingsPostings.GetPayload() != null)
                                                        {
                                                            throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + " has no payload but postings does: " + postingsPostings.GetPayload());
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // we have payloads, and one at this position
                                                        // postings should also have one at this position, with the same bytes.
                                                        if (postingsPostings.GetPayload() is null)
                                                        {
                                                            throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but postings does not.");
                                                        }
                                                        BytesRef postingsPayload = postingsPostings.GetPayload();
                                                        if (!payload.Equals(postingsPayload))
                                                        {
                                                            throw RuntimeException.Create("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but differs from postings payload=" + postingsPayload);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                float vectorAvg = status.DocCount == 0 ? 0 : status.TotVectors / (float)status.DocCount;
                Msg(infoStream, "OK [" + status.TotVectors + " total vector count; avg " + vectorAvg.ToString(CultureInfo.InvariantCulture.NumberFormat) + " term/freq vector fields per doc]");
            }
            catch (Exception e) when (e.IsThrowable())
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.Error = e;
               
                // LUCENENET NOTE: Some tests rely on the error type being in
                // the message. We can't get the error type with StackTrace, we
                // need ToString() for that.
                infoStream?.WriteLine(e.ToString());
                //infoStream.WriteLine(e.StackTrace);
            }

            return status;
        }

        /// <summary>
        /// Repairs the index using previously returned result
        /// from <see cref="DoCheckIndex()"/>.  Note that this does not
        /// remove any of the unreferenced files after it's done;
        /// you must separately open an <see cref="IndexWriter"/>, which
        /// deletes unreferenced files when it's created.
        ///
        /// <para/><b>WARNING</b>: this writes a
        /// new segments file into the index, effectively removing
        /// all documents in broken segments from the index.
        /// BE CAREFUL.
        ///
        /// <para/><b>WARNING</b>: Make sure you only call this when the
        /// index is not opened by any writer.
        /// </summary>
        public virtual void FixIndex(Status result)
        {
            if (result is null)
                throw new ArgumentNullException(nameof(result)); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)

            if (result.Partial)
            {
                throw new ArgumentException("can only fix an index that was fully checked (this status checked a subset of segments)");
            }
            result.NewSegments.Changed();
            result.NewSegments.Commit(result.Dir);
        }

        // LUCENENET: Not used
        //private static bool assertsOn;

        //private static bool TestAsserts()
        //{
        //    assertsOn = true;
        //    return true;
        //}

        //private static bool AssertsOn()
        //{
        //    if (Debugging.AssertsEnabled) Debugging.Assert(TestAsserts);
        //    return assertsOn;
        //}

        ///// Command-line interface to check and fix an index.
        /////
        /////  <p>
        /////  Run it like this:
        /////  <pre>
        /////  java -ea:org.apache.lucene... Lucene.Net.Index.CheckIndex pathToIndex [-fix] [-verbose] [-segment X] [-segment Y]
        /////  </pre>
        /////  <ul>
        /////  <li><code>-fix</code>: actually write a new segments_N file, removing any problematic segments
        /////
        /////  <li><code>-segment X</code>: only check the specified
        /////  segment(s).  this can be specified multiple times,
        /////  to check more than one segment, eg <code>-segment _2
        /////  -segment _a</code>.  You can't use this with the -fix
        /////  option.
        /////  </ul>
        /////
        /////  <p><b>WARNING</b>: <code>-fix</code> should only be used on an emergency basis as it will cause
        /////                     documents (perhaps many) to be permanently removed from the index.  Always make
        /////                     a backup copy of your index before running this!  Do not run this tool on an index
        /////                     that is actively being written to.  You have been warned!
        /////
        /////  <p>                Run without -fix, this tool will open the index, report version information
        /////                     and report any exceptions it hits and what action it would take if -fix were
        /////                     specified.  With -fix, this tool will remove any segments that have issues and
        /////                     write a new segments_N file.  this means all documents contained in the affected
        /////                     segments will be removed.
        /////
        /////  <p>
        /////                     this tool exits with exit code 1 if the index cannot be opened or has any
        /////                     corruption, else 0.
        [STAThread]
        public static void Main(string[] args)
        {
            bool doFix = false;
            bool doCrossCheckTermVectors = false;
            bool verbose = false;
            IList<string> onlySegments = new JCG.List<string>();
            string indexPath = null;
            string dirImpl = null;
            int i = 0;
            while (i < args.Length)
            {
                string arg = args[i];
                if ("-fix".Equals(arg, StringComparison.Ordinal))
                {
                    doFix = true;
                }
                else if ("-crossCheckTermVectors".Equals(arg, StringComparison.Ordinal))
                {
                    doCrossCheckTermVectors = true;
                }
                else if (arg.Equals("-verbose", StringComparison.Ordinal))
                {
                    verbose = true;
                }
                else if (arg.Equals("-segment", StringComparison.Ordinal))
                {
                    if (i == args.Length - 1)
                    {
                        // LUCENENET specific - we only output from our CLI wrapper
                        throw new ArgumentException("ERROR: missing name for -segment option");
                        //Console.WriteLine("ERROR: missing name for -segment option");
                        //Environment.Exit(1);
                    }
                    i++;
                    onlySegments.Add(args[i]);
                }
                else if ("-dir-impl".Equals(arg, StringComparison.Ordinal))
                {
                    if (i == args.Length - 1)
                    {
                        // LUCENENET specific - we only output from our CLI wrapper
                        throw new ArgumentException("ERROR: missing value for -dir-impl option");
                        //Console.WriteLine("ERROR: missing value for -dir-impl option");
                        //Environment.Exit(1);
                    }
                    i++;
                    dirImpl = args[i];
                }
                else
                {
                    if (indexPath != null)
                    {
                        // LUCENENET specific - we only output from our CLI wrapper
                        throw new ArgumentException("ERROR: unexpected extra argument '" + args[i] + "'");
                        //Console.WriteLine("ERROR: unexpected extra argument '" + args[i] + "'");
                        //Environment.Exit(1);
                    }
                    indexPath = args[i];
                }
                i++;
            }

            if (indexPath is null)
            {
                // LUCENENET specific - we only output from our CLI wrapper
                throw new ArgumentException("\nERROR: index path not specified");
                //Console.WriteLine("\nERROR: index path not specified");
                //Console.WriteLine("\nUsage: java Lucene.Net.Index.CheckIndex pathToIndex [-fix] [-crossCheckTermVectors] [-segment X] [-segment Y] [-dir-impl X]\n" + "\n" + "  -fix: actually write a new segments_N file, removing any problematic segments\n" + "  -crossCheckTermVectors: verifies that term vectors match postings; this IS VERY SLOW!\n" + "  -codec X: when fixing, codec to write the new segments_N file with\n" + "  -verbose: print additional details\n" + "  -segment X: only check the specified segments.  this can be specified multiple\n" + "              times, to check more than one segment, eg '-segment _2 -segment _a'.\n" + "              You can't use this with the -fix option\n" + "  -dir-impl X: use a specific " + typeof(FSDirectory).Name + " implementation. " + "If no package is specified the " + typeof(FSDirectory).Namespace + " package will be used.\n" + "\n" + "**WARNING**: -fix should only be used on an emergency basis as it will cause\n" + "documents (perhaps many) to be permanently removed from the index.  Always make\n" + "a backup copy of your index before running this!  Do not run this tool on an index\n" + "that is actively being written to.  You have been warned!\n" + "\n" + "Run without -fix, this tool will open the index, report version information\n" + "and report any exceptions it hits and what action it would take if -fix were\n" + "specified.  With -fix, this tool will remove any segments that have issues and\n" + "write a new segments_N file.  this means all documents contained in the affected\n" + "segments will be removed.\n" + "\n" + "this tool exits with exit code 1 if the index cannot be opened or has any\n" + "corruption, else 0.\n");
                //Environment.Exit(1);
            }

            // LUCENENET specific - rather than having the user specify whether to enable asserts, we always run with them enabled.
            Debugging.AssertsEnabled = true;
            //if (!AssertsOn())
            //{
            //    Console.WriteLine("\nNOTE: testing will be more thorough if you run java with '-ea:org.apache.lucene...', so assertions are enabled");
            //}


            if (onlySegments.Count == 0)
            {
                onlySegments = null;
            }
            else if (doFix)
            {
                // LUCENENET specific - we only output from our CLI wrapper
                throw new ArgumentException("ERROR: cannot specify both -fix and -segment");
                //Console.WriteLine("ERROR: cannot specify both -fix and -segment");
                //Environment.Exit(1);
            }

            Console.WriteLine("\nOpening index @ " + indexPath + "\n");
            Directory dir/* = null*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            try
            {
                if (dirImpl is null)
                {
                    dir = FSDirectory.Open(new DirectoryInfo(indexPath));
                }
                else
                {
                    dir = CommandLineUtil.NewFSDirectory(dirImpl, new DirectoryInfo(indexPath));
                }
            }
            catch (Exception t) when (t.IsThrowable())
            {
                // LUCENENET specific - we only output from our CLI wrapper
                throw new ArgumentException("ERROR: could not open directory \"" + indexPath + "\"; exiting\n" + t.ToString());
                //Console.WriteLine("ERROR: could not open directory \"" + indexPath + "\"; exiting");
                //Console.Out.WriteLine(t.StackTrace);
                //Environment.Exit(1);
            }

            CheckIndex checker = new CheckIndex(dir);
            checker.CrossCheckTermVectors = doCrossCheckTermVectors;
            checker.InfoStream = Console.Out;
            checker.InfoStreamIsVerbose = verbose;

            Status result = checker.DoCheckIndex(onlySegments);
            if (result.MissingSegments)
            {
                Environment.Exit(1);
            }

            if (!result.Clean)
            {
                if (!doFix)
                {
                    Console.WriteLine("WARNING: would write new segments file, and " + result.TotLoseDocCount + " documents would be lost, if index fix were specified\n");
                    //Console.WriteLine("WARNING: would write new segments file, and " + result.TotLoseDocCount + " documents would be lost, if -fix were specified\n");
                }
                else
                {
                    Console.WriteLine("WARNING: " + result.TotLoseDocCount + " documents will be lost\n");
                    Console.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + result.TotLoseDocCount + " docs from the index. this IS YOUR LAST CHANCE TO CTRL+C!");
                    for (int s = 0; s < 5; s++)
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine("  " + (5 - s) + "...");
                    }
                    Console.WriteLine("Writing...");
                    checker.FixIndex(result);
                    Console.WriteLine("OK");
                    Console.WriteLine("Wrote new segments file \"" + result.NewSegments.GetSegmentsFileName() + "\"");
                }
            }
            Console.WriteLine();

            int exitCode;
            if (result.Clean == true)
            {
                exitCode = 0;
            }
            else
            {
                exitCode = 1;
            }
            Environment.Exit(exitCode);
        }
    }
}