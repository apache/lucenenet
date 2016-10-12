using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.Globalization;
    using System.IO;
    using Bits = Lucene.Net.Util.Bits;

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

    using BlockTreeTermsReader = Lucene.Net.Codecs.BlockTreeTermsReader;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Codec = Lucene.Net.Codecs.Codec;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using DocValuesStatus = Lucene.Net.Index.CheckIndex.Status.DocValuesStatus;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using LongBitSet = Lucene.Net.Util.LongBitSet;
    using Lucene3xSegmentInfoFormat = Lucene.Net.Codecs.Lucene3x.Lucene3xSegmentInfoFormat;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// Basic tool and API to check the health of an index and
    /// write a new segments file that removes reference to
    /// problematic segments.
    ///
    /// <p>As this tool checks every byte in the index, on a large
    /// index it can take quite a long time to run.
    ///
    /// @lucene.experimental Please make a complete backup of your
    /// index before using this to fix your index!
    /// </summary>
    public class CheckIndex
    {
        private TextWriter infoStream;
        private Directory Dir;

        /// <summary>
        /// Returned from <seealso cref="#checkIndex()"/> detailing the health and status of the index.
        ///
        /// @lucene.experimental
        ///
        /// </summary>

        public class Status
        {
            internal Status()
            {
            }

            /// <summary>
            /// True if no problems were found with the index. </summary>
            public bool Clean;

            /// <summary>
            /// True if we were unable to locate and load the segments_N file. </summary>
            public bool MissingSegments;

            /// <summary>
            /// True if we were unable to open the segments_N file. </summary>
            public bool CantOpenSegments;

            /// <summary>
            /// True if we were unable to read the version number from segments_N file. </summary>
            public bool MissingSegmentVersion;

            /// <summary>
            /// Name of latest segments_N file in the index. </summary>
            public string SegmentsFileName;

            /// <summary>
            /// Number of segments in the index. </summary>
            public int NumSegments;

            /// <summary>
            /// Empty unless you passed specific segments list to check as optional 3rd argument. </summary>
            ///  <seealso cref= CheckIndex#checkIndex(List)  </seealso>
            public IList<string> SegmentsChecked = new List<string>();

            /// <summary>
            /// True if the index was created with a newer version of Lucene than the CheckIndex tool. </summary>
            public bool ToolOutOfDate;

            /// <summary>
            /// List of <seealso cref="SegmentInfoStatus"/> instances, detailing status of each segment. </summary>
            public IList<SegmentInfoStatus> SegmentInfos = new List<SegmentInfoStatus>();

            /// <summary>
            /// Directory index is in. </summary>
            public Directory Dir;

            /// <summary>
            /// SegmentInfos instance containing only segments that
            /// had no problems (this is used with the <seealso cref="CheckIndex#fixIndex"/>
            /// method to repair the index.
            /// </summary>
            internal SegmentInfos NewSegments;

            /// <summary>
            /// How many documents will be lost to bad segments. </summary>
            public int TotLoseDocCount;

            /// <summary>
            /// How many bad segments were found. </summary>
            public int NumBadSegments;

            /// <summary>
            /// True if we checked only specific segments ({@link
            /// #checkIndex(List)}) was called with non-null
            /// argument).
            /// </summary>
            public bool Partial;

            /// <summary>
            /// The greatest segment name. </summary>
            public int MaxSegmentName;

            /// <summary>
            /// Whether the SegmentInfos.counter is greater than any of the segments' names. </summary>
            public bool ValidCounter;

            /// <summary>
            /// Holds the userData of the last commit in the index </summary>
            public IDictionary<string, string> UserData;

            /// <summary>
            /// Holds the status of each segment in the index.
            ///  See <seealso cref="#segmentInfos"/>.
            ///
            /// @lucene.experimental
            /// </summary>
            public class SegmentInfoStatus
            {
                internal SegmentInfoStatus()
                {
                }

                /// <summary>
                /// Name of the segment. </summary>
                public string Name;

                /// <summary>
                /// Codec used to read this segment. </summary>
                public Codec Codec;

                /// <summary>
                /// Document count (does not take deletions into account). </summary>
                public int DocCount;

                /// <summary>
                /// True if segment is compound file format. </summary>
                public bool Compound;

                /// <summary>
                /// Number of files referenced by this segment. </summary>
                public int NumFiles;

                /// <summary>
                /// Net size (MB) of the files referenced by this
                ///  segment.
                /// </summary>
                public double SizeMB;

                /// <summary>
                /// Doc store offset, if this segment shares the doc
                ///  store files (stored fields and term vectors) with
                ///  other segments.  this is -1 if it does not share.
                /// </summary>
                public int DocStoreOffset = -1;

                /// <summary>
                /// String of the shared doc store segment, or null if
                ///  this segment does not share the doc store files.
                /// </summary>
                public string DocStoreSegment;

                /// <summary>
                /// True if the shared doc store files are compound file
                ///  format.
                /// </summary>
                public bool DocStoreCompoundFile;

                /// <summary>
                /// True if this segment has pending deletions. </summary>
                public bool HasDeletions;

                /// <summary>
                /// Current deletions generation. </summary>
                public long DeletionsGen;

                /// <summary>
                /// Number of deleted documents. </summary>
                public int NumDeleted;

                /// <summary>
                /// True if we were able to open an AtomicReader on this
                ///  segment.
                /// </summary>
                public bool OpenReaderPassed;

                /// <summary>
                /// Number of fields in this segment. </summary>
                internal int NumFields;

                /// <summary>
                /// Map that includes certain
                ///  debugging details that IndexWriter records into
                ///  each segment it creates
                /// </summary>
                public IDictionary<string, string> Diagnostics;

                /// <summary>
                /// Status for testing of field norms (null if field norms could not be tested). </summary>
                public FieldNormStatus FieldNormStatus;

                /// <summary>
                /// Status for testing of indexed terms (null if indexed terms could not be tested). </summary>
                public TermIndexStatus TermIndexStatus;

                /// <summary>
                /// Status for testing of stored fields (null if stored fields could not be tested). </summary>
                public StoredFieldStatus StoredFieldStatus;

                /// <summary>
                /// Status for testing of term vectors (null if term vectors could not be tested). </summary>
                public TermVectorStatus TermVectorStatus;

                /// <summary>
                /// Status for testing of DocValues (null if DocValues could not be tested). </summary>
                public DocValuesStatus DocValuesStatus;
            }

            /// <summary>
            /// Status from testing field norms.
            /// </summary>
            public sealed class FieldNormStatus
            {
                internal FieldNormStatus()
                {
                }

                /// <summary>
                /// Number of fields successfully tested </summary>
                public long TotFields = 0L;

                /// <summary>
                /// Exception thrown during term index test (null on success) </summary>
                public Exception Error = null;
            }

            /// <summary>
            /// Status from testing term index.
            /// </summary>
            public sealed class TermIndexStatus
            {
                internal TermIndexStatus()
                {
                }

                /// <summary>
                /// Number of terms with at least one live doc. </summary>
                public long TermCount = 0L;

                /// <summary>
                /// Number of terms with zero live docs docs. </summary>
                public long DelTermCount = 0L;

                /// <summary>
                /// Total frequency across all terms. </summary>
                public long TotFreq = 0L;

                /// <summary>
                /// Total number of positions. </summary>
                public long TotPos = 0L;

                /// <summary>
                /// Exception thrown during term index test (null on success) </summary>
                public Exception Error = null;

                /// <summary>
                /// Holds details of block allocations in the block
                ///  tree terms dictionary (this is only set if the
                ///  <seealso cref="PostingsFormat"/> for this segment uses block
                ///  tree.
                /// </summary>
                public IDictionary<string, BlockTreeTermsReader.Stats> BlockTreeStats = null;
            }

            /// <summary>
            /// Status from testing stored fields.
            /// </summary>
            public sealed class StoredFieldStatus
            {
                internal StoredFieldStatus()
                {
                }

                /// <summary>
                /// Number of documents tested. </summary>
                public int DocCount = 0;

                /// <summary>
                /// Total number of stored fields tested. </summary>
                public long TotFields = 0;

                /// <summary>
                /// Exception thrown during stored fields test (null on success) </summary>
                public Exception Error = null;
            }

            /// <summary>
            /// Status from testing stored fields.
            /// </summary>
            public sealed class TermVectorStatus
            {
                internal TermVectorStatus()
                {
                }

                /// <summary>
                /// Number of documents tested. </summary>
                public int DocCount = 0;

                /// <summary>
                /// Total number of term vectors tested. </summary>
                public long TotVectors = 0;

                /// <summary>
                /// Exception thrown during term vector test (null on success) </summary>
                public Exception Error = null;
            }

            /// <summary>
            /// Status from testing DocValues
            /// </summary>
            public sealed class DocValuesStatus
            {
                internal DocValuesStatus()
                {
                }

                /// <summary>
                /// Total number of docValues tested. </summary>
                public long TotalValueFields;

                /// <summary>
                /// Total number of numeric fields </summary>
                public long TotalNumericFields;

                /// <summary>
                /// Total number of binary fields </summary>
                public long TotalBinaryFields;

                /// <summary>
                /// Total number of sorted fields </summary>
                public long TotalSortedFields;

                /// <summary>
                /// Total number of sortedset fields </summary>
                public long TotalSortedSetFields;

                /// <summary>
                /// Exception thrown during doc values test (null on success) </summary>
                public Exception Error = null;
            }
        }

        /// <summary>
        /// Create a new CheckIndex on the directory. </summary>
        public CheckIndex(Directory dir)
        {
            this.Dir = dir;
            infoStream = null;
        }

        private bool CrossCheckTermVectors_Renamed;

        /// <summary>
        /// If true, term vectors are compared against postings to
        ///  make sure they are the same.  this will likely
        ///  drastically increase time it takes to run CheckIndex!
        /// </summary>
        public virtual bool CrossCheckTermVectors
        {
            set
            {
                CrossCheckTermVectors_Renamed = value;
            }
            get
            {
                return CrossCheckTermVectors_Renamed;
            }
        }

        private bool Verbose;

        /// <summary>
        /// Set infoStream where messages should go.  If null, no
        ///  messages are printed.  If verbose is true then more
        ///  details are printed.
        /// </summary>
        public virtual void SetInfoStream(TextWriter @out, bool verbose)
        {
            infoStream = @out;
            this.Verbose = verbose;
        }

        /// <summary>
        /// Set infoStream where messages should go. See <seealso cref="#setInfoStream(PrintStream,boolean)"/>. </summary>
        public virtual TextWriter InfoStream
        {
            get
            {
                return infoStream;
            }
            set
            {
                SetInfoStream(value, false);
            }
        }

        public void FlushInfoStream()
        {
            infoStream.Flush();
        }

        private static void Msg(TextWriter @out, string msg)
        {
            if (@out != null)
            {
                @out.WriteLine(msg);
            }
        }

        /// <summary>
        /// Returns a <seealso cref="Status"/> instance detailing
        ///  the state of the index.
        ///
        ///  <p>As this method checks every byte in the index, on a large
        ///  index it can take quite a long time to run.
        ///
        ///  <p><b>WARNING</b>: make sure
        ///  you only call this when the index is not opened by any
        ///  writer.
        /// </summary>
        public virtual Status DoCheckIndex()
        {
            return DoCheckIndex(null);
        }

        /// <summary>
        /// Returns a <seealso cref="Status"/> instance detailing
        ///  the state of the index.
        /// </summary>
        ///  <param name="onlySegments"> list of specific segment names to check
        ///
        ///  <p>As this method checks every byte in the specified
        ///  segments, on a large index it can take quite a long
        ///  time to run.
        ///
        ///  <p><b>WARNING</b>: make sure
        ///  you only call this when the index is not opened by any
        ///  writer.  </param>
        public virtual Status DoCheckIndex(IList<string> onlySegments)
        {
            NumberFormatInfo nf = CultureInfo.CurrentCulture.NumberFormat;
            SegmentInfos sis = new SegmentInfos();
            Status result = new Status();
            result.Dir = Dir;
            try
            {
                sis.Read(Dir);
            }
            catch (Exception t)
            {
                Msg(infoStream, "ERROR: could not read any segments file in directory");
                result.MissingSegments = true;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(t.ToString());
                    //infoStream.WriteLine(t.StackTrace);
                }
                return result;
            }

            // find the oldest and newest segment versions
            string oldest = Convert.ToString(int.MaxValue), newest = Convert.ToString(int.MinValue);
            string oldSegs = null;
            bool foundNonNullVersion = false;
            IComparer<string> versionComparator = StringHelper.VersionComparator;
            foreach (SegmentCommitInfo si in sis.Segments)
            {
                string version = si.Info.Version;
                if (version == null)
                {
                    // pre-3.1 segment
                    oldSegs = "pre-3.1";
                }
                else
                {
                    foundNonNullVersion = true;
                    if (versionComparator.Compare(version, oldest) < 0)
                    {
                        oldest = version;
                    }
                    if (versionComparator.Compare(version, newest) > 0)
                    {
                        newest = version;
                    }
                }
            }

            int numSegments = sis.Size();
            string segmentsFileName = sis.SegmentsFileName;
            // note: we only read the format byte (required preamble) here!
            IndexInput input = null;
            try
            {
                input = Dir.OpenInput(segmentsFileName, IOContext.READONCE);
            }
            catch (Exception t)
            {
                Msg(infoStream, "ERROR: could not open segments file in directory");
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(t.ToString());
                    //infoStream.WriteLine(t.StackTrace);
                }
                result.CantOpenSegments = true;
                return result;
            }
            int format = 0;
            try
            {
                format = input.ReadInt();
            }
            catch (Exception t)
            {
                Msg(infoStream, "ERROR: could not read segment file version in directory");
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(t.ToString());
                    //infoStream.WriteLine(t.StackTrace);
                }
                result.MissingSegmentVersion = true;
                return result;
            }
            finally
            {
                if (input != null)
                {
                    input.Dispose();
                }
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

            string versionString = null;
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
                versionString = oldest.Equals(newest) ? ("version=" + oldest) : ("versions=[" + oldest + " .. " + newest + "]");
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

            for (int i = 0; i < numSegments; i++)
            {
                SegmentCommitInfo info = sis.Info(i);
                int segmentName = 0;
                try
                {
                    segmentName = int.Parse /*Convert.ToInt32*/(info.Info.Name.Substring(1));
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
                if (info.Info.DocCount <= 0 && version != null && versionComparator.Compare(version, "4.5") >= 0)
                {
                    throw new Exception("illegal number of documents: maxDoc=" + info.Info.DocCount);
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
                    Msg(infoStream, "    numFiles=" + info.Files().Count);
                    segInfoStat.NumFiles = info.Files().Count;
                    segInfoStat.SizeMB = info.SizeInBytes() / (1024.0 * 1024.0);
                    if (info.Info.GetAttribute(Lucene3xSegmentInfoFormat.DS_OFFSET_KEY) == null)
                    {
                        // don't print size in bytes if its a 3.0 segment with shared docstores
                        Msg(infoStream, "    size (MB)=" + segInfoStat.SizeMB.ToString(nf));
                    }
                    IDictionary<string, string> diagnostics = info.Info.Diagnostics;
                    segInfoStat.Diagnostics = diagnostics;
                    if (diagnostics.Count > 0)
                    {
                        Msg(infoStream, "    diagnostics = " + diagnostics);
                    }

                    if (!info.HasDeletions())
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
                    if (infoStream != null)
                    {
                        infoStream.Write("    test: open reader.........");
                    }
                    reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.DEFAULT);
                    Msg(infoStream, "OK");

                    segInfoStat.OpenReaderPassed = true;

                    if (infoStream != null)
                    {
                        infoStream.Write("    test: check integrity.....");
                    }
                    reader.CheckIntegrity();
                    Msg(infoStream, "OK");

                    if (infoStream != null)
                    {
                        infoStream.Write("    test: check live docs.....");
                    }
                    int numDocs = reader.NumDocs;
                    toLoseDocCount = numDocs;
                    if (reader.HasDeletions)
                    {
                        if (reader.NumDocs != info.Info.DocCount - info.DelCount)
                        {
                            throw new Exception("delete count mismatch: info=" + (info.Info.DocCount - info.DelCount) + " vs reader=" + reader.NumDocs);
                        }
                        if ((info.Info.DocCount - reader.NumDocs) > reader.MaxDoc)
                        {
                            throw new Exception("too many deleted docs: maxDoc()=" + reader.MaxDoc + " vs del count=" + (info.Info.DocCount - reader.NumDocs));
                        }
                        if (info.Info.DocCount - numDocs != info.DelCount)
                        {
                            throw new Exception("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.Info.DocCount - numDocs));
                        }
                        Bits liveDocs = reader.LiveDocs;
                        if (liveDocs == null)
                        {
                            throw new Exception("segment should have deletions, but liveDocs is null");
                        }
                        else
                        {
                            int numLive = 0;
                            for (int j = 0; j < liveDocs.Length(); j++)
                            {
                                if (liveDocs.Get(j))
                                {
                                    numLive++;
                                }
                            }
                            if (numLive != numDocs)
                            {
                                throw new Exception("liveDocs count mismatch: info=" + numDocs + ", vs bits=" + numLive);
                            }
                        }

                        segInfoStat.NumDeleted = info.Info.DocCount - numDocs;
                        Msg(infoStream, "OK [" + (segInfoStat.NumDeleted) + " deleted docs]");
                    }
                    else
                    {
                        if (info.DelCount != 0)
                        {
                            throw new Exception("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.Info.DocCount - numDocs));
                        }
                        Bits liveDocs = reader.LiveDocs;
                        if (liveDocs != null)
                        {
                            // its ok for it to be non-null here, as long as none are set right?
                            for (int j = 0; j < liveDocs.Length(); j++)
                            {
                                if (!liveDocs.Get(j))
                                {
                                    throw new Exception("liveDocs mismatch: info says no deletions but doc " + j + " is deleted.");
                                }
                            }
                        }
                        Msg(infoStream, "OK");
                    }
                    if (reader.MaxDoc != info.Info.DocCount)
                    {
                        throw new Exception("SegmentReader.maxDoc() " + reader.MaxDoc + " != SegmentInfos.docCount " + info.Info.DocCount);
                    }

                    // Test getFieldInfos()
                    if (infoStream != null)
                    {
                        infoStream.Write("    test: fields..............");
                    }
                    FieldInfos fieldInfos = reader.FieldInfos;
                    Msg(infoStream, "OK [" + fieldInfos.Size() + " fields]");
                    segInfoStat.NumFields = fieldInfos.Size();

                    // Test Field Norms
                    segInfoStat.FieldNormStatus = TestFieldNorms(reader, infoStream);

                    // Test the Term Index
                    segInfoStat.TermIndexStatus = TestPostings(reader, infoStream, Verbose);

                    // Test Stored Fields
                    segInfoStat.StoredFieldStatus = TestStoredFields(reader, infoStream);

                    // Test Term Vectors
                    segInfoStat.TermVectorStatus = TestTermVectors(reader, infoStream, Verbose, CrossCheckTermVectors_Renamed);

                    segInfoStat.DocValuesStatus = TestDocValues(reader, infoStream);

                    // Rethrow the first exception we encountered
                    //  this will cause stats for failed segments to be incremented properly
                    if (segInfoStat.FieldNormStatus.Error != null)
                    {
                        throw new Exception("Field Norm test failed");
                    }
                    else if (segInfoStat.TermIndexStatus.Error != null)
                    {
                        throw new Exception("Term Index test failed");
                    }
                    else if (segInfoStat.StoredFieldStatus.Error != null)
                    {
                        throw new Exception("Stored Field test failed");
                    }
                    else if (segInfoStat.TermVectorStatus.Error != null)
                    {
                        throw new Exception("Term Vector test failed");
                    }
                    else if (segInfoStat.DocValuesStatus.Error != null)
                    {
                        throw new Exception("DocValues test failed");
                    }

                    Msg(infoStream, "");
                }
                catch (Exception t)
                {
                    Msg(infoStream, "FAILED");
                    string comment;
                    comment = "fixIndex() would remove reference to this segment";
                    Msg(infoStream, "    WARNING: " + comment + "; full exception:");
                    if (infoStream != null)
                    {
                        // LUCENENET NOTE: Some tests rely on the error type being in
                        // the message. We can't get the error type with StackTrace, we
                        // need ToString() for that.
                        infoStream.WriteLine(t.ToString());
                        //infoStream.WriteLine(t.StackTrace);
                    }
                    Msg(infoStream, "");
                    result.TotLoseDocCount += toLoseDocCount;
                    result.NumBadSegments++;
                    continue;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Dispose();
                    }
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
        /// @lucene.experimental
        /// </summary>
        public static Status.FieldNormStatus TestFieldNorms(AtomicReader reader, TextWriter infoStream)
        {
            Status.FieldNormStatus status = new Status.FieldNormStatus();

            try
            {
                // Test Field Norms
                if (infoStream != null)
                {
                    infoStream.Write("    test: field norms.........");
                }
                foreach (FieldInfo info in reader.FieldInfos)
                {
                    if (info.HasNorms())
                    {
                        Debug.Assert(reader.HasNorms(info.Name)); // deprecated path
                        CheckNorms(info, reader, infoStream);
                        ++status.TotFields;
                    }
                    else
                    {
                        Debug.Assert(!reader.HasNorms(info.Name)); // deprecated path
                        if (reader.GetNormValues(info.Name) != null)
                        {
                            throw new Exception("field: " + info.Name + " should omit norms but has them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.TotFields + " fields]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + Convert.ToString(e.Message) + "]");
                status.Error = e;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(e.ToString());
                    //infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary>
        /// checks Fields api is consistent with itself.
        /// searcher is optional, to verify with queries. Can be null.
        /// </summary>
        private static Status.TermIndexStatus CheckFields(Fields fields, Bits liveDocs, int maxDoc, FieldInfos fieldInfos, bool doPrint, bool isVectors, TextWriter infoStream, bool verbose)
        {
            // TODO: we should probably return our own stats thing...?!

            Status.TermIndexStatus status = new Status.TermIndexStatus();
            int computedFieldCount = 0;

            if (fields == null)
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
                if (lastField != null && field.CompareTo(lastField) <= 0)
                {
                    throw new Exception("fields out of order: lastField=" + lastField + " field=" + field);
                }
                lastField = field;

                // check that the field is in fieldinfos, and is indexed.
                // TODO: add a separate test to check this for different reader impls
                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    throw new Exception("fieldsEnum inconsistent with fieldInfos, no fieldInfos for: " + field);
                }
                if (!fieldInfo.Indexed)
                {
                    throw new Exception("fieldsEnum inconsistent with fieldInfos, isIndexed == false for: " + field);
                }

                // TODO: really the codec should not return a field
                // from FieldsEnum if it has no Terms... but we do
                // this today:
                // assert fields.terms(field) != null;
                computedFieldCount++;

                Terms terms = fields.Terms(field);
                if (terms == null)
                {
                    continue;
                }

                bool hasFreqs = terms.HasFreqs();
                bool hasPositions = terms.HasPositions();
                bool hasPayloads = terms.HasPayloads();
                bool hasOffsets = terms.HasOffsets();

                // term vectors cannot omit TF:
                bool expectedHasFreqs = (isVectors || fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS);

                if (hasFreqs != expectedHasFreqs)
                {
                    throw new Exception("field \"" + field + "\" should have hasFreqs=" + expectedHasFreqs + " but got " + hasFreqs);
                }

                if (hasFreqs == false)
                {
                    if (terms.SumTotalTermFreq != -1)
                    {
                        throw new Exception("field \"" + field + "\" hasFreqs is false, but Terms.getSumTotalTermFreq()=" + terms.SumTotalTermFreq + " (should be -1)");
                    }
                }

                if (!isVectors)
                {
                    bool expectedHasPositions = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
                    if (hasPositions != expectedHasPositions)
                    {
                        throw new Exception("field \"" + field + "\" should have hasPositions=" + expectedHasPositions + " but got " + hasPositions);
                    }

                    bool expectedHasPayloads = fieldInfo.HasPayloads();
                    if (hasPayloads != expectedHasPayloads)
                    {
                        throw new Exception("field \"" + field + "\" should have hasPayloads=" + expectedHasPayloads + " but got " + hasPayloads);
                    }

                    bool expectedHasOffsets = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
                    if (hasOffsets != expectedHasOffsets)
                    {
                        throw new Exception("field \"" + field + "\" should have hasOffsets=" + expectedHasOffsets + " but got " + hasOffsets);
                    }
                }

                TermsEnum termsEnum = terms.Iterator(null);

                bool hasOrd = true;
                long termCountStart = status.DelTermCount + status.TermCount;

                BytesRef lastTerm = null;

                IComparer<BytesRef> termComp = terms.Comparator;

                long sumTotalTermFreq = 0;
                long sumDocFreq = 0;
                FixedBitSet visitedDocs = new FixedBitSet(maxDoc);
                while (true)
                {
                    BytesRef term = termsEnum.Next();
                    if (term == null)
                    {
                        break;
                    }

                    Debug.Assert(term.Valid);

                    // make sure terms arrive in order according to
                    // the comp
                    if (lastTerm == null)
                    {
                        lastTerm = BytesRef.DeepCopyOf(term);
                    }
                    else
                    {
                        if (termComp.Compare(lastTerm, term) >= 0)
                        {
                            throw new Exception("terms out of order: lastTerm=" + lastTerm + " term=" + term);
                        }
                        lastTerm.CopyBytes(term);
                    }

                    int docFreq = termsEnum.DocFreq();
                    if (docFreq <= 0)
                    {
                        throw new Exception("docfreq: " + docFreq + " is out of bounds");
                    }
                    sumDocFreq += docFreq;

                    docs = termsEnum.Docs(liveDocs, docs);
                    postings = termsEnum.DocsAndPositions(liveDocs, postings);

                    if (hasFreqs == false)
                    {
                        if (termsEnum.TotalTermFreq() != -1)
                        {
                            throw new Exception("field \"" + field + "\" hasFreqs is false, but TermsEnum.totalTermFreq()=" + termsEnum.TotalTermFreq() + " (should be -1)");
                        }
                    }

                    if (hasOrd)
                    {
                        long ord = -1;
                        try
                        {
                            ord = termsEnum.Ord();
                        }
                        catch (System.NotSupportedException uoe)
                        {
                            hasOrd = false;
                        }

                        if (hasOrd)
                        {
                            long ordExpected = status.DelTermCount + status.TermCount - termCountStart;
                            if (ord != ordExpected)
                            {
                                throw new Exception("ord mismatch: TermsEnum has ord=" + ord + " vs actual=" + ordExpected);
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
                            freq = docs2.Freq();
                            if (freq <= 0)
                            {
                                throw new Exception("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");
                            }
                            status.TotPos += freq;
                            totalTermFreq += freq;
                        }
                        else
                        {
                            // When a field didn't index freq, it must
                            // consistently "lie" and pretend that freq was
                            // 1:
                            if (docs2.Freq() != 1)
                            {
                                throw new Exception("term " + term + ": doc " + doc + ": freq " + freq + " != 1 when Terms.hasFreqs() is false");
                            }
                        }
                        docCount++;

                        if (doc <= lastDoc)
                        {
                            throw new Exception("term " + term + ": doc " + doc + " <= lastDoc " + lastDoc);
                        }
                        if (doc >= maxDoc)
                        {
                            throw new Exception("term " + term + ": doc " + doc + " >= maxDoc " + maxDoc);
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
                                    throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
                                }
                                if (pos < lastPos)
                                {
                                    throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
                                }
                                lastPos = pos;
                                BytesRef payload = postings.Payload;
                                if (payload != null)
                                {
                                    Debug.Assert(payload.Valid);
                                }
                                if (payload != null && payload.Length < 1)
                                {
                                    throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + " payload length is out of bounds " + payload.Length);
                                }
                                if (hasOffsets)
                                {
                                    int startOffset = postings.StartOffset();
                                    int endOffset = postings.EndOffset();
                                    // NOTE: we cannot enforce any bounds whatsoever on vectors... they were a free-for-all before?
                                    // but for offsets in the postings lists these checks are fine: they were always enforced by IndexWriter
                                    if (!isVectors)
                                    {
                                        if (startOffset < 0)
                                        {
                                            throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                        }
                                        if (startOffset < lastOffset)
                                        {
                                            throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                        }
                                        if (endOffset < 0)
                                        {
                                            throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                        }
                                        if (endOffset < startOffset)
                                        {
                                            throw new Exception("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
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

                    long totalTermFreq2 = termsEnum.TotalTermFreq();
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
                                visitedDocs.Set(docsNoDel.DocID());
                                docCount++;
                                totalTermFreq += docsNoDel.Freq();
                            }
                        }
                        else
                        {
                            DocsEnum docsNoDel = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
                            docCount = 0;
                            totalTermFreq = -1;
                            while (docsNoDel.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                            {
                                visitedDocs.Set(docsNoDel.DocID());
                                docCount++;
                            }
                        }
                    }

                    if (docCount != docFreq)
                    {
                        throw new Exception("term " + term + " docFreq=" + docFreq + " != tot docs w/o deletions " + docCount);
                    }
                    if (hasTotalTermFreq)
                    {
                        if (totalTermFreq2 <= 0)
                        {
                            throw new Exception("totalTermFreq: " + totalTermFreq2 + " is out of bounds");
                        }
                        sumTotalTermFreq += totalTermFreq;
                        if (totalTermFreq != totalTermFreq2)
                        {
                            throw new Exception("term " + term + " totalTermFreq=" + totalTermFreq2 + " != recomputed totalTermFreq=" + totalTermFreq);
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
                                    throw new Exception("term " + term + ": advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int freq = postings.Freq();
                                if (freq <= 0)
                                {
                                    throw new Exception("termFreq " + freq + " is out of bounds");
                                }
                                int lastPosition = -1;
                                int lastOffset = 0;
                                for (int posUpto = 0; posUpto < freq; posUpto++)
                                {
                                    int pos = postings.NextPosition();

                                    if (pos < 0)
                                    {
                                        throw new Exception("position " + pos + " is out of bounds");
                                    }
                                    if (pos < lastPosition)
                                    {
                                        throw new Exception("position " + pos + " is < lastPosition " + lastPosition);
                                    }
                                    lastPosition = pos;
                                    if (hasOffsets)
                                    {
                                        int startOffset = postings.StartOffset();
                                        int endOffset = postings.EndOffset();
                                        // NOTE: we cannot enforce any bounds whatsoever on vectors... they were a free-for-all before?
                                        // but for offsets in the postings lists these checks are fine: they were always enforced by IndexWriter
                                        if (!isVectors)
                                        {
                                            if (startOffset < 0)
                                            {
                                                throw new Exception("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                            }
                                            if (startOffset < lastOffset)
                                            {
                                                throw new Exception("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                            }
                                            if (endOffset < 0)
                                            {
                                                throw new Exception("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                            }
                                            if (endOffset < startOffset)
                                            {
                                                throw new Exception("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
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
                                    throw new Exception("term " + term + ": advance(docID=" + skipDocID + "), then .next() returned docID=" + nextDocID + " vs prev docID=" + docID);
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int idx = 0; idx < 7; idx++)
                        {
                            int skipDocID = (int)(((idx + 1) * (long)maxDoc) / 8);
                            docs = termsEnum.Docs(liveDocs, docs, DocsEnum.FLAG_NONE);
                            int docID = docs.Advance(skipDocID);
                            if (docID == DocIdSetIterator.NO_MORE_DOCS)
                            {
                                break;
                            }
                            else
                            {
                                if (docID < skipDocID)
                                {
                                    throw new Exception("term " + term + ": advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int nextDocID = docs.NextDoc();
                                if (nextDocID == DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    break;
                                }
                                if (nextDocID <= docID)
                                {
                                    throw new Exception("term " + term + ": advance(docID=" + skipDocID + "), then .next() returned docID=" + nextDocID + " vs prev docID=" + docID);
                                }
                            }
                        }
                    }
                }

                Terms fieldTerms = fields.Terms(field);
                if (fieldTerms == null)
                {
                    // Unusual: the FieldsEnum returned a field but
                    // the Terms for that field is null; this should
                    // only happen if it's a ghost field (field with
                    // no terms, eg there used to be terms but all
                    // docs got deleted and then merged away):
                }
                else
                {
                    if (fieldTerms is BlockTreeTermsReader.FieldReader)
                    {
                        BlockTreeTermsReader.Stats stats = ((BlockTreeTermsReader.FieldReader)fieldTerms).ComputeStats();
                        Debug.Assert(stats != null);
                        if (status.BlockTreeStats == null)
                        {
                            status.BlockTreeStats = new Dictionary<string, BlockTreeTermsReader.Stats>();
                        }
                        status.BlockTreeStats[field] = stats;
                    }

                    if (sumTotalTermFreq != 0)
                    {
                        long v = fields.Terms(field).SumTotalTermFreq;
                        if (v != -1 && sumTotalTermFreq != v)
                        {
                            throw new Exception("sumTotalTermFreq for field " + field + "=" + v + " != recomputed sumTotalTermFreq=" + sumTotalTermFreq);
                        }
                    }

                    if (sumDocFreq != 0)
                    {
                        long v = fields.Terms(field).SumDocFreq;
                        if (v != -1 && sumDocFreq != v)
                        {
                            throw new Exception("sumDocFreq for field " + field + "=" + v + " != recomputed sumDocFreq=" + sumDocFreq);
                        }
                    }

                    if (fieldTerms != null)
                    {
                        int v = fieldTerms.DocCount;
                        if (v != -1 && visitedDocs.Cardinality() != v)
                        {
                            throw new Exception("docCount for field " + field + "=" + v + " != recomputed docCount=" + visitedDocs.Cardinality());
                        }
                    }

                    // Test seek to last term:
                    if (lastTerm != null)
                    {
                        if (termsEnum.SeekCeil(lastTerm) != TermsEnum.SeekStatus.FOUND)
                        {
                            throw new Exception("seek to last term " + lastTerm + " failed");
                        }

                        int expectedDocFreq = termsEnum.DocFreq();
                        DocsEnum d = termsEnum.Docs(null, null, DocsEnum.FLAG_NONE);
                        int docFreq = 0;
                        while (d.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            docFreq++;
                        }
                        if (docFreq != expectedDocFreq)
                        {
                            throw new Exception("docFreq for last term " + lastTerm + "=" + expectedDocFreq + " != recomputed docFreq=" + docFreq);
                        }
                    }

                    // check unique term count
                    long termCount = -1;

                    if ((status.DelTermCount + status.TermCount) - termCountStart > 0)
                    {
                        termCount = fields.Terms(field).Size();

                        if (termCount != -1 && termCount != status.DelTermCount + status.TermCount - termCountStart)
                        {
                            throw new Exception("termCount mismatch " + (status.DelTermCount + termCount) + " vs " + (status.TermCount - termCountStart));
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
                                seekTerms[i] = BytesRef.DeepCopyOf(termsEnum.Term());
                            }

                            // Seek by term
                            long totDocCount = 0;
                            for (int i = seekCount - 1; i >= 0; i--)
                            {
                                if (termsEnum.SeekCeil(seekTerms[i]) != TermsEnum.SeekStatus.FOUND)
                                {
                                    throw new Exception("seek to existing term " + seekTerms[i] + " failed");
                                }

                                docs = termsEnum.Docs(liveDocs, docs, DocsEnum.FLAG_NONE);
                                if (docs == null)
                                {
                                    throw new Exception("null DocsEnum from to existing term " + seekTerms[i]);
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
                                    throw new Exception("seek to existing term " + seekTerms[i] + " failed");
                                }

                                totDocFreq += termsEnum.DocFreq();
                                docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
                                if (docs == null)
                                {
                                    throw new Exception("null DocsEnum from to existing term " + seekTerms[i]);
                                }

                                while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    totDocCountNoDeletes++;
                                }
                            }

                            if (totDocCount > totDocCountNoDeletes)
                            {
                                throw new Exception("more postings with deletes=" + totDocCount + " than without=" + totDocCountNoDeletes);
                            }

                            if (totDocCountNoDeletes != totDocFreq)
                            {
                                throw new Exception("docfreqs=" + totDocFreq + " != recomputed docfreqs=" + totDocCountNoDeletes);
                            }
                        }
                    }
                }
            }

            int fieldCount = fields.Size;

            if (fieldCount != -1)
            {
                if (fieldCount < 0)
                {
                    throw new Exception("invalid fieldCount: " + fieldCount);
                }
                if (fieldCount != computedFieldCount)
                {
                    throw new Exception("fieldCount mismatch " + fieldCount + " vs recomputed field count " + computedFieldCount);
                }
            }

            // for most implementations, this is boring (just the sum across all fields)
            // but codecs that don't work per-field like preflex actually implement this,
            // but don't implement it on Terms, so the check isn't redundant.
            long uniqueTermCountAllFields = fields.UniqueTermCount;

            if (uniqueTermCountAllFields != -1 && status.TermCount + status.DelTermCount != uniqueTermCountAllFields)
            {
                throw new Exception("termCount mismatch " + uniqueTermCountAllFields + " vs " + (status.TermCount + status.DelTermCount));
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
        /// @lucene.experimental
        /// </summary>
        public static Status.TermIndexStatus TestPostings(AtomicReader reader, TextWriter infoStream)
        {
            return TestPostings(reader, infoStream, false);
        }

        /// <summary>
        /// Test the term index.
        /// @lucene.experimental
        /// </summary>
        public static Status.TermIndexStatus TestPostings(AtomicReader reader, TextWriter infoStream, bool verbose)
        {
            // TODO: we should go and verify term vectors match, if
            // crossCheckTermVectors is on...

            Status.TermIndexStatus status;
            int maxDoc = reader.MaxDoc;
            Bits liveDocs = reader.LiveDocs;

            try
            {
                if (infoStream != null)
                {
                    infoStream.Write("    test: terms, freq, prox...");
                }

                Fields fields = reader.Fields;
                FieldInfos fieldInfos = reader.FieldInfos;
                status = CheckFields(fields, liveDocs, maxDoc, fieldInfos, true, false, infoStream, verbose);
                if (liveDocs != null)
                {
                    if (infoStream != null)
                    {
                        infoStream.Write("    test (ignoring deletes): terms, freq, prox...");
                    }
                    CheckFields(fields, null, maxDoc, fieldInfos, true, false, infoStream, verbose);
                }
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR: " + e);
                status = new Status.TermIndexStatus();
                status.Error = e;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(e.ToString());
                    //infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary>
        /// Test stored fields.
        /// @lucene.experimental
        /// </summary>
        public static Status.StoredFieldStatus TestStoredFields(AtomicReader reader, TextWriter infoStream)
        {
            Status.StoredFieldStatus status = new Status.StoredFieldStatus();

            try
            {
                if (infoStream != null)
                {
                    infoStream.Write("    test: stored fields.......");
                }

                // Scan stored fields for all documents
                Bits liveDocs = reader.LiveDocs;
                for (int j = 0; j < reader.MaxDoc; ++j)
                {
                    // Intentionally pull even deleted documents to
                    // make sure they too are not corrupt:
                    Document doc = reader.Document(j);
                    if (liveDocs == null || liveDocs.Get(j))
                    {
                        status.DocCount++;
                        status.TotFields += doc.Fields.Count;
                    }
                }

                // Validate docCount
                if (status.DocCount != reader.NumDocs)
                {
                    throw new Exception("docCount=" + status.DocCount + " but saw " + status.DocCount + " undeleted docs");
                }

                Msg(infoStream, "OK [" + status.TotFields + " total field count; avg " + ((((float)status.TotFields) / status.DocCount)).ToString(CultureInfo.InvariantCulture.NumberFormat) + " fields per doc]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + Convert.ToString(e.Message) + "]");
                status.Error = e;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(e.ToString());
                    //infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary>
        /// Test docvalues.
        /// @lucene.experimental
        /// </summary>
        public static Status.DocValuesStatus TestDocValues(AtomicReader reader, TextWriter infoStream)
        {
            Status.DocValuesStatus status = new Status.DocValuesStatus();
            try
            {
                if (infoStream != null)
                {
                    infoStream.Write("    test: docvalues...........");
                }
                foreach (FieldInfo fieldInfo in reader.FieldInfos)
                {
                    if (fieldInfo.HasDocValues())
                    {
                        status.TotalValueFields++;
                        CheckDocValues(fieldInfo, reader, /*infoStream,*/ status);
                    }
                    else
                    {
                        if (reader.GetBinaryDocValues(fieldInfo.Name) != null || reader.GetNumericDocValues(fieldInfo.Name) != null || reader.GetSortedDocValues(fieldInfo.Name) != null || reader.GetSortedSetDocValues(fieldInfo.Name) != null || reader.GetDocsWithField(fieldInfo.Name) != null)
                        {
                            throw new Exception("field: " + fieldInfo.Name + " has docvalues but should omit them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.TotalValueFields + " docvalues fields; " + status.TotalBinaryFields + " BINARY; " + status.TotalNumericFields + " NUMERIC; " + status.TotalSortedFields + " SORTED; " + status.TotalSortedSetFields + " SORTED_SET]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + Convert.ToString(e.Message) + "]");
                status.Error = e;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(e.ToString());
                    //infoStream.WriteLine(e.StackTrace);
                }
            }
            return status;
        }

        private static void CheckBinaryDocValues(string fieldName, AtomicReader reader, BinaryDocValues dv, Bits docsWithField)
        {
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.Get(i, scratch);
                Debug.Assert(scratch.Valid);
                if (docsWithField.Get(i) == false && scratch.Length > 0)
                {
                    throw new Exception("dv for field: " + fieldName + " is missing but has value=" + scratch + " for doc: " + i);
                }
            }
        }

        private static void CheckSortedDocValues(string fieldName, AtomicReader reader, SortedDocValues dv, Bits docsWithField)
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
                        throw new Exception("dv for field: " + fieldName + " has -1 ord but is not marked missing for doc: " + i);
                    }
                }
                else if (ord < -1 || ord > maxOrd)
                {
                    throw new Exception("ord out of bounds: " + ord);
                }
                else
                {
                    if (!docsWithField.Get(i))
                    {
                        throw new Exception("dv for field: " + fieldName + " is missing but has ord=" + ord + " for doc: " + i);
                    }
                    maxOrd2 = Math.Max(maxOrd2, ord);
                    seenOrds.Set(ord);
                }
            }
            if (maxOrd != maxOrd2)
            {
                throw new Exception("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality() != dv.ValueCount)
            {
                throw new Exception("dv for field: " + fieldName + " has holes in its ords, valueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality());
            }
            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (int i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                Debug.Assert(scratch.Valid);
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw new Exception("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckSortedSetDocValues(string fieldName, AtomicReader reader, SortedSetDocValues dv, Bits docsWithField)
        {
            long maxOrd = dv.ValueCount - 1;
            LongBitSet seenOrds = new LongBitSet(dv.ValueCount);
            long maxOrd2 = -1;
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.Document = i;
                long lastOrd = -1;
                long ord;
                if (docsWithField.Get(i))
                {
                    int ordCount = 0;
                    while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        if (ord <= lastOrd)
                        {
                            throw new Exception("ords out of order: " + ord + " <= " + lastOrd + " for doc: " + i);
                        }
                        if (ord < 0 || ord > maxOrd)
                        {
                            throw new Exception("ord out of bounds: " + ord);
                        }
                        if (dv is RandomAccessOrds)
                        {
                            long ord2 = ((RandomAccessOrds)dv).OrdAt(ordCount);
                            if (ord != ord2)
                            {
                                throw new Exception("ordAt(" + ordCount + ") inconsistent, expected=" + ord + ",got=" + ord2 + " for doc: " + i);
                            }
                        }
                        lastOrd = ord;
                        maxOrd2 = Math.Max(maxOrd2, ord);
                        seenOrds.Set(ord);
                        ordCount++;
                    }
                    if (ordCount == 0)
                    {
                        throw new Exception("dv for field: " + fieldName + " has no ordinals but is not marked missing for doc: " + i);
                    }
                    if (dv is RandomAccessOrds)
                    {
                        long ordCount2 = ((RandomAccessOrds)dv).Cardinality();
                        if (ordCount != ordCount2)
                        {
                            throw new Exception("cardinality inconsistent, expected=" + ordCount + ",got=" + ordCount2 + " for doc: " + i);
                        }
                    }
                }
                else
                {
                    long o = dv.NextOrd();
                    if (o != SortedSetDocValues.NO_MORE_ORDS)
                    {
                        throw new Exception("dv for field: " + fieldName + " is marked missing but has ord=" + o + " for doc: " + i);
                    }
                    if (dv is RandomAccessOrds)
                    {
                        long ordCount2 = ((RandomAccessOrds)dv).Cardinality();
                        if (ordCount2 != 0)
                        {
                            throw new Exception("dv for field: " + fieldName + " is marked missing but has cardinality " + ordCount2 + " for doc: " + i);
                        }
                    }
                }
            }
            if (maxOrd != maxOrd2)
            {
                throw new Exception("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality() != dv.ValueCount)
            {
                throw new Exception("dv for field: " + fieldName + " has holes in its ords, valueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality());
            }

            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (long i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                Debug.Assert(scratch.Valid);
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw new Exception("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckNumericDocValues(string fieldName, AtomicReader reader, NumericDocValues ndv, Bits docsWithField)
        {
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                long value = ndv.Get(i);
                if (docsWithField.Get(i) == false && value != 0)
                {
                    throw new Exception("dv for field: " + fieldName + " is marked missing but has value=" + value + " for doc: " + i);
                }
            }
        }

        private static void CheckDocValues(FieldInfo fi, AtomicReader reader, /*StreamWriter infoStream,*/ DocValuesStatus status)
        {
            Bits docsWithField = reader.GetDocsWithField(fi.Name);
            if (docsWithField == null)
            {
                throw new Exception(fi.Name + " docsWithField does not exist");
            }
            else if (docsWithField.Length() != reader.MaxDoc)
            {
                throw new Exception(fi.Name + " docsWithField has incorrect length: " + docsWithField.Length() + ",expected: " + reader.MaxDoc);
            }
            switch (fi.DocValuesType)
            {
                case FieldInfo.DocValuesType_e.SORTED:
                    status.TotalSortedFields++;
                    CheckSortedDocValues(fi.Name, reader, reader.GetSortedDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw new Exception(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case FieldInfo.DocValuesType_e.SORTED_SET:
                    status.TotalSortedSetFields++;
                    CheckSortedSetDocValues(fi.Name, reader, reader.GetSortedSetDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null)
                    {
                        throw new Exception(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case FieldInfo.DocValuesType_e.BINARY:
                    status.TotalBinaryFields++;
                    CheckBinaryDocValues(fi.Name, reader, reader.GetBinaryDocValues(fi.Name), docsWithField);
                    if (reader.GetNumericDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw new Exception(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                case FieldInfo.DocValuesType_e.NUMERIC:
                    status.TotalNumericFields++;
                    CheckNumericDocValues(fi.Name, reader, reader.GetNumericDocValues(fi.Name), docsWithField);
                    if (reader.GetBinaryDocValues(fi.Name) != null || reader.GetSortedDocValues(fi.Name) != null || reader.GetSortedSetDocValues(fi.Name) != null)
                    {
                        throw new Exception(fi.Name + " returns multiple docvalues types!");
                    }
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        private static void CheckNorms(FieldInfo fi, AtomicReader reader, TextWriter infoStream)
        {
            switch (fi.NormType)
            {
                case FieldInfo.DocValuesType_e.NUMERIC:
                    CheckNumericDocValues(fi.Name, reader, reader.GetNormValues(fi.Name), new Lucene.Net.Util.Bits_MatchAllBits(reader.MaxDoc));
                    break;

                default:
                    throw new InvalidOperationException("wtf: " + fi.NormType);
            }
        }

        /// <summary>
        /// Test term vectors.
        /// @lucene.experimental
        /// </summary>
        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, TextWriter infoStream)
        {
            return TestTermVectors(reader, infoStream, false, false);
        }

        /// <summary>
        /// Test term vectors.
        /// @lucene.experimental
        /// </summary>
        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, TextWriter infoStream, bool verbose, bool crossCheckTermVectors)
        {
            Status.TermVectorStatus status = new Status.TermVectorStatus();
            FieldInfos fieldInfos = reader.FieldInfos;
            Bits onlyDocIsDeleted = new FixedBitSet(1);

            try
            {
                if (infoStream != null)
                {
                    infoStream.Write("    test: term vectors........");
                }

                DocsEnum docs = null;
                DocsAndPositionsEnum postings = null;

                // Only used if crossCheckTermVectors is true:
                DocsEnum postingsDocs = null;
                DocsAndPositionsEnum postingsPostings = null;

                Bits liveDocs = reader.LiveDocs;

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
                        bool doStats = liveDocs == null || liveDocs.Get(j);
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
                            if (!fieldInfo.HasVectors())
                            {
                                throw new Exception("docID=" + j + " has term vectors for field=" + field + " but FieldInfo has storeTermVector=false");
                            }

                            if (crossCheckTermVectors)
                            {
                                Terms terms = tfv.Terms(field);
                                termsEnum = terms.Iterator(termsEnum);
                                bool postingsHasFreq = fieldInfo.FieldIndexOptions >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                                bool postingsHasPayload = fieldInfo.HasPayloads();
                                bool vectorsHasPayload = terms.HasPayloads();

                                Terms postingsTerms = postingsFields.Terms(field);
                                if (postingsTerms == null)
                                {
                                    throw new Exception("vector field=" + field + " does not exist in postings; doc=" + j);
                                }
                                postingsTermsEnum = postingsTerms.Iterator(postingsTermsEnum);

                                bool hasProx = terms.HasOffsets() || terms.HasPositions();
                                BytesRef term = null;
                                while ((term = termsEnum.Next()) != null)
                                {
                                    if (hasProx)
                                    {
                                        postings = termsEnum.DocsAndPositions(null, postings);
                                        Debug.Assert(postings != null);
                                        docs = null;
                                    }
                                    else
                                    {
                                        docs = termsEnum.Docs(null, docs);
                                        Debug.Assert(docs != null);
                                        postings = null;
                                    }

                                    DocsEnum docs2;
                                    if (hasProx)
                                    {
                                        Debug.Assert(postings != null);
                                        docs2 = postings;
                                    }
                                    else
                                    {
                                        Debug.Assert(docs != null);
                                        docs2 = docs;
                                    }

                                    DocsEnum postingsDocs2;
                                    if (!postingsTermsEnum.SeekExact(term))
                                    {
                                        throw new Exception("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
                                    }
                                    postingsPostings = postingsTermsEnum.DocsAndPositions(null, postingsPostings);
                                    if (postingsPostings == null)
                                    {
                                        // Term vectors were indexed w/ pos but postings were not
                                        postingsDocs = postingsTermsEnum.Docs(null, postingsDocs);
                                        if (postingsDocs == null)
                                        {
                                            throw new Exception("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
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
                                        throw new Exception("vector term=" + term + " field=" + field + ": doc=" + j + " was not found in postings (got: " + advanceDoc + ")");
                                    }

                                    int doc = docs2.NextDoc();

                                    if (doc != 0)
                                    {
                                        throw new Exception("vector for doc " + j + " didn't return docID=0: got docID=" + doc);
                                    }

                                    if (postingsHasFreq)
                                    {
                                        int tf = docs2.Freq();
                                        if (postingsHasFreq && postingsDocs2.Freq() != tf)
                                        {
                                            throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + ": freq=" + tf + " differs from postings freq=" + postingsDocs2.Freq());
                                        }

                                        if (hasProx)
                                        {
                                            for (int i = 0; i < tf; i++)
                                            {
                                                int pos = postings.NextPosition();
                                                if (postingsPostings != null)
                                                {
                                                    int postingsPos = postingsPostings.NextPosition();
                                                    if (terms.HasPositions() && pos != postingsPos)
                                                    {
                                                        throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + ": pos=" + pos + " differs from postings pos=" + postingsPos);
                                                    }
                                                }

                                                // Call the methods to at least make
                                                // sure they don't throw exc:
                                                int startOffset = postings.StartOffset();
                                                int endOffset = postings.EndOffset();
                                                // TODO: these are too anal...?
                                                /*
                                                  if (endOffset < startOffset) {
                                                  throw new RuntimeException("vector startOffset=" + startOffset + " is > endOffset=" + endOffset);
                                                  }
                                                  if (startOffset < lastStartOffset) {
                                                  throw new RuntimeException("vector startOffset=" + startOffset + " is < prior startOffset=" + lastStartOffset);
                                                  }
                                                  lastStartOffset = startOffset;
                                                */

                                                if (postingsPostings != null)
                                                {
                                                    int postingsStartOffset = postingsPostings.StartOffset();

                                                    int postingsEndOffset = postingsPostings.EndOffset();
                                                    if (startOffset != -1 && postingsStartOffset != -1 && startOffset != postingsStartOffset)
                                                    {
                                                        throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + ": startOffset=" + startOffset + " differs from postings startOffset=" + postingsStartOffset);
                                                    }
                                                    if (endOffset != -1 && postingsEndOffset != -1 && endOffset != postingsEndOffset)
                                                    {
                                                        throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + ": endOffset=" + endOffset + " differs from postings endOffset=" + postingsEndOffset);
                                                    }
                                                }

                                                BytesRef payload = postings.Payload;

                                                if (payload != null)
                                                {
                                                    Debug.Assert(vectorsHasPayload);
                                                }

                                                if (postingsHasPayload && vectorsHasPayload)
                                                {
                                                    Debug.Assert(postingsPostings != null);

                                                    if (payload == null)
                                                    {
                                                        // we have payloads, but not at this position.
                                                        // postings has payloads too, it should not have one at this position
                                                        if (postingsPostings.Payload != null)
                                                        {
                                                            throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + " has no payload but postings does: " + postingsPostings.Payload);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // we have payloads, and one at this position
                                                        // postings should also have one at this position, with the same bytes.
                                                        if (postingsPostings.Payload == null)
                                                        {
                                                            throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but postings does not.");
                                                        }
                                                        BytesRef postingsPayload = postingsPostings.Payload;
                                                        if (!payload.Equals(postingsPayload))
                                                        {
                                                            throw new Exception("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but differs from postings payload=" + postingsPayload);
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
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + Convert.ToString(e.Message) + "]");
                status.Error = e;
                if (infoStream != null)
                {
                    // LUCENENET NOTE: Some tests rely on the error type being in
                    // the message. We can't get the error type with StackTrace, we
                    // need ToString() for that.
                    infoStream.WriteLine(e.ToString());
                    //infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary>
        /// Repairs the index using previously returned result
        ///  from <seealso cref="#checkIndex"/>.  Note that this does not
        ///  remove any of the unreferenced files after it's done;
        ///  you must separately open an <seealso cref="IndexWriter"/>, which
        ///  deletes unreferenced files when it's created.
        ///
        /// <p><b>WARNING</b>: this writes a
        ///  new segments file into the index, effectively removing
        ///  all documents in broken segments from the index.
        ///  BE CAREFUL.
        ///
        /// <p><b>WARNING</b>: Make sure you only call this when the
        ///  index is not opened  by any writer.
        /// </summary>
        public virtual void FixIndex(Status result)
        {
            if (result.Partial)
            {
                throw new System.ArgumentException("can only fix an index that was fully checked (this status checked a subset of segments)");
            }
            result.NewSegments.Changed();
            result.NewSegments.Commit(result.Dir);
        }

        private static bool AssertsOn_Renamed;

        private static bool TestAsserts()
        {
            AssertsOn_Renamed = true;
            return true;
        }

        private static bool AssertsOn()
        {
            Debug.Assert(TestAsserts());
            return AssertsOn_Renamed;
        }

        /*
        /// Command-line interface to check and fix an index.
        ///
        ///  <p>
        ///  Run it like this:
        ///  <pre>
        ///  java -ea:org.apache.lucene... Lucene.Net.Index.CheckIndex pathToIndex [-fix] [-verbose] [-segment X] [-segment Y]
        ///  </pre>
        ///  <ul>
        ///  <li><code>-fix</code>: actually write a new segments_N file, removing any problematic segments
        ///
        ///  <li><code>-segment X</code>: only check the specified
        ///  segment(s).  this can be specified multiple times,
        ///  to check more than one segment, eg <code>-segment _2
        ///  -segment _a</code>.  You can't use this with the -fix
        ///  option.
        ///  </ul>
        ///
        ///  <p><b>WARNING</b>: <code>-fix</code> should only be used on an emergency basis as it will cause
        ///                     documents (perhaps many) to be permanently removed from the index.  Always make
        ///                     a backup copy of your index before running this!  Do not run this tool on an index
        ///                     that is actively being written to.  You have been warned!
        ///
        ///  <p>                Run without -fix, this tool will open the index, report version information
        ///                     and report any exceptions it hits and what action it would take if -fix were
        ///                     specified.  With -fix, this tool will remove any segments that have issues and
        ///                     write a new segments_N file.  this means all documents contained in the affected
        ///                     segments will be removed.
        ///
        ///  <p>
        ///                     this tool exits with exit code 1 if the index cannot be opened or has any
        ///                     corruption, else 0.
        */
        /*[STAThread]
        public static void Main(string[] args)
        {
          bool doFix = false;
          bool doCrossCheckTermVectors = false;
          bool verbose = false;
          IList<string> onlySegments = new List<string>();
          string indexPath = null;
          string dirImpl = null;
          int i = 0;
          while (i < args.Length)
          {
            string arg = args[i];
            if ("-fix".Equals(arg))
            {
              doFix = true;
            }
            else if ("-crossCheckTermVectors".Equals(arg))
            {
              doCrossCheckTermVectors = true;
            }
            else if (arg.Equals("-verbose"))
            {
              verbose = true;
            }
            else if (arg.Equals("-segment"))
            {
              if (i == args.Length - 1)
              {
                Console.WriteLine("ERROR: missing name for -segment option");
                Environment.Exit(1);
              }
              i++;
              onlySegments.Add(args[i]);
            }
            else if ("-dir-impl".Equals(arg))
            {
              if (i == args.Length - 1)
              {
                Console.WriteLine("ERROR: missing value for -dir-impl option");
                Environment.Exit(1);
              }
              i++;
              dirImpl = args[i];
            }
            else
            {
              if (indexPath != null)
              {
                Console.WriteLine("ERROR: unexpected extra argument '" + args[i] + "'");
                Environment.Exit(1);
              }
              indexPath = args[i];
            }
            i++;
          }

          if (indexPath == null)
          {
            Console.WriteLine("\nERROR: index path not specified");
            Console.WriteLine("\nUsage: java Lucene.Net.Index.CheckIndex pathToIndex [-fix] [-crossCheckTermVectors] [-segment X] [-segment Y] [-dir-impl X]\n" + "\n" + "  -fix: actually write a new segments_N file, removing any problematic segments\n" + "  -crossCheckTermVectors: verifies that term vectors match postings; this IS VERY SLOW!\n" + "  -codec X: when fixing, codec to write the new segments_N file with\n" + "  -verbose: print additional details\n" + "  -segment X: only check the specified segments.  this can be specified multiple\n" + "              times, to check more than one segment, eg '-segment _2 -segment _a'.\n" + "              You can't use this with the -fix option\n" + "  -dir-impl X: use a specific " + typeof(FSDirectory).Name + " implementation. " + "If no package is specified the " + typeof(FSDirectory).Namespace + " package will be used.\n" + "\n" + "**WARNING**: -fix should only be used on an emergency basis as it will cause\n" + "documents (perhaps many) to be permanently removed from the index.  Always make\n" + "a backup copy of your index before running this!  Do not run this tool on an index\n" + "that is actively being written to.  You have been warned!\n" + "\n" + "Run without -fix, this tool will open the index, report version information\n" + "and report any exceptions it hits and what action it would take if -fix were\n" + "specified.  With -fix, this tool will remove any segments that have issues and\n" + "write a new segments_N file.  this means all documents contained in the affected\n" + "segments will be removed.\n" + "\n" + "this tool exits with exit code 1 if the index cannot be opened or has any\n" + "corruption, else 0.\n");
            Environment.Exit(1);
          }

          if (!AssertsOn())
          {
            Console.WriteLine("\nNOTE: testing will be more thorough if you run java with '-ea:org.apache.lucene...', so assertions are enabled");
          }

          if (onlySegments.Count == 0)
          {
            onlySegments = null;
          }
          else if (doFix)
          {
            Console.WriteLine("ERROR: cannot specify both -fix and -segment");
            Environment.Exit(1);
          }

          Console.WriteLine("\nOpening index @ " + indexPath + "\n");
          Directory dir = null;
          try
          {
            if (dirImpl == null)
            {
              dir = FSDirectory.Open(new DirectoryInfo(indexPath));
            }
            else
            {
              dir = CommandLineUtil.NewFSDirectory(dirImpl, new DirectoryInfo(indexPath));
            }
          }
          catch (Exception t)
          {
            Console.WriteLine("ERROR: could not open directory \"" + indexPath + "\"; exiting");
            Console.Out.WriteLine(t.StackTrace);
            Environment.Exit(1);
          }

          CheckIndex checker = new CheckIndex(dir);
          checker.CrossCheckTermVectors = doCrossCheckTermVectors;
          checker.SetInfoStream(new StreamWriter(Console.OpenStandardOutput()), verbose);

          Status result = checker.DoCheckIndex(onlySegments);
          if (result.MissingSegments)
          {
            Environment.Exit(1);
          }

          if (!result.Clean)
          {
            if (!doFix)
            {
              Console.WriteLine("WARNING: would write new segments file, and " + result.TotLoseDocCount + " documents would be lost, if -fix were specified\n");
            }
            else
            {
              Console.WriteLine("WARNING: " + result.TotLoseDocCount + " documents will be lost\n");
              Console.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + result.TotLoseDocCount + " docs from the index. this IS YOUR LAST CHANCE TO CTRL+C!");
              for (int s = 0;s < 5;s++)
              {
                Thread.Sleep(1000);
                Console.WriteLine("  " + (5 - s) + "...");
              }
              Console.WriteLine("Writing...");
              checker.FixIndex(result);
              Console.WriteLine("OK");
              Console.WriteLine("Wrote new segments file \"" + result.NewSegments.SegmentsFileName + "\"");
            }
          }
          Console.WriteLine("");

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
        }*/
    }
}