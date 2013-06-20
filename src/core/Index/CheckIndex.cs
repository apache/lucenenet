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

using Lucene.Net.Codecs;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Directory = Lucene.Net.Store.Directory;
using Document = Lucene.Net.Documents.Document;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{

    /// <summary> Basic tool and API to check the health of an index and
    /// write a new segments file that removes reference to
    /// problematic segments.
    /// 
    /// <p/>As this tool checks every byte in the index, on a large
    /// index it can take quite a long time to run.
    /// 
    /// <p/><b>WARNING</b>: this tool and API is new and
    /// experimental and is subject to suddenly change in the
    /// next release.  Please make a complete backup of your
    /// index before using this to fix your index!
    /// </summary>
    public class CheckIndex
    {
        private StreamWriter infoStream;
        private readonly Directory dir;

        /// <summary> Returned from <see cref="CheckIndex_Renamed_Method()" /> detailing the health and status of the index.
        /// 
        /// <p/><b>WARNING</b>: this API is new and experimental and is
        /// subject to suddenly change in the next release.
        /// 
        /// </summary>

        public class Status
        {

            /// <summary>True if no problems were found with the index. </summary>
            public bool clean;

            /// <summary>True if we were unable to locate and load the segments_N file. </summary>
            public bool missingSegments;

            /// <summary>True if we were unable to open the segments_N file. </summary>
            public bool cantOpenSegments;

            /// <summary>True if we were unable to read the version number from segments_N file. </summary>
            public bool missingSegmentVersion;

            /// <summary>Name of latest segments_N file in the index. </summary>
            public string segmentsFileName;

            /// <summary>Number of segments in the index. </summary>
            public int numSegments;

            /// <summary>String description of the version of the index. </summary>
            public string segmentFormat;

            /// <summary>Empty unless you passed specific segments list to check as optional 3rd argument.</summary>
            /// <seealso>
            ///   <cref>CheckIndex.CheckIndex_Renamed_Method(System.Collections.IList)</cref>
            /// </seealso>
            public IList<string> segmentsChecked = new List<string>();

            /// <summary>True if the index was created with a newer version of Lucene than the CheckIndex tool. </summary>
            public bool toolOutOfDate;

            /// <summary>List of <see cref="SegmentInfoStatus" /> instances, detailing status of each segment. </summary>
            public IList<SegmentInfoStatus> segmentInfos = new List<SegmentInfoStatus>();

            /// <summary>Directory index is in. </summary>
            public Directory dir;

            /// <summary> SegmentInfos instance containing only segments that
            /// had no problems (this is used with the <see cref="CheckIndex.FixIndex" /> 
            /// method to repair the index. 
            /// </summary>
            internal SegmentInfos newSegments;

            /// <summary>How many documents will be lost to bad segments. </summary>
            public int totLoseDocCount;

            /// <summary>How many bad segments were found. </summary>
            public int numBadSegments;

            /// <summary>True if we checked only specific segments (<see cref="CheckIndex.CheckIndex_Renamed_Method(List{string})" />)
            /// was called with non-null
            /// argument). 
            /// </summary>
            public bool partial;

            public int maxSegmentName;

            public bool validCounter;

            /// <summary>Holds the userData of the last commit in the index </summary>
            public IDictionary<string, string> userData;

            /// <summary>Holds the status of each segment in the index.
            /// See <see cref="SegmentInfos" />.
            /// 
            /// <p/><b>WARNING</b>: this API is new and experimental and is
            /// subject to suddenly change in the next release.
            /// </summary>
            public class SegmentInfoStatus
            {
                /// <summary>Name of the segment. </summary>
                public string name;

                public Codec codec;

                /// <summary>Document count (does not take deletions into account). </summary>
                public int docCount;

                /// <summary>True if segment is compound file format. </summary>
                public bool compound;

                /// <summary>Number of files referenced by this segment. </summary>
                public int numFiles;

                /// <summary>Net size (MB) of the files referenced by this
                /// segment. 
                /// </summary>
                public double sizeMB;

                /// <summary>Doc store offset, if this segment shares the doc
                /// store files (stored fields and term vectors) with
                /// other segments.  This is -1 if it does not share. 
                /// </summary>
                public int docStoreOffset = -1;

                /// <summary>String of the shared doc store segment, or null if
                /// this segment does not share the doc store files. 
                /// </summary>
                public string docStoreSegment;

                /// <summary>True if the shared doc store files are compound file
                /// format. 
                /// </summary>
                public bool docStoreCompoundFile;

                /// <summary>True if this segment has pending deletions. </summary>
                public bool hasDeletions;

                public string deletionsGen;

                /// <summary>Number of deleted documents. </summary>
                public int numDeleted;

                /// <summary>True if we were able to open a SegmentReader on this
                /// segment. 
                /// </summary>
                public bool openReaderPassed;

                /// <summary>Number of fields in this segment. </summary>
                internal int numFields;

                /// <summary>Map&lt;String, String&gt; that includes certain
                /// debugging details that IndexWriter records into
                /// each segment it creates 
                /// </summary>
                public IDictionary<string, string> diagnostics;

                /// <summary>Status for testing of field norms (null if field norms could not be tested). </summary>
                public FieldNormStatus fieldNormStatus;

                /// <summary>Status for testing of indexed terms (null if indexed terms could not be tested). </summary>
                public TermIndexStatus termIndexStatus;

                /// <summary>Status for testing of stored fields (null if stored fields could not be tested). </summary>
                public StoredFieldStatus storedFieldStatus;

                /// <summary>Status for testing of term vectors (null if term vectors could not be tested). </summary>
                public TermVectorStatus termVectorStatus;

                public DocValuesStatus docValuesStatus;
            }

            /// <summary> Status from testing field norms.</summary>
            public sealed class FieldNormStatus
            {
                /// <summary>Number of fields successfully tested </summary>
                public long totFields = 0L;

                /// <summary>Exception thrown during term index test (null on success) </summary>
                public Exception error = null;
            }

            /// <summary> Status from testing term index.</summary>
            public sealed class TermIndexStatus
            {
                /// <summary>Total term count </summary>
                public long termCount = 0L;

                public long delTermCount = 0L;

                /// <summary>Total frequency across all terms. </summary>
                public long totFreq = 0L;

                /// <summary>Total number of positions. </summary>
                public long totPos = 0L;

                /// <summary>Exception thrown during term index test (null on success) </summary>
                public Exception error = null;

                public IDictionary<String, BlockTreeTermsReader.Stats> blockTreeStats = null;
            }

            /// <summary> Status from testing stored fields.</summary>
            public sealed class StoredFieldStatus
            {

                /// <summary>Number of documents tested. </summary>
                public int docCount = 0;

                /// <summary>Total number of stored fields tested. </summary>
                public long totFields = 0;

                /// <summary>Exception thrown during stored fields test (null on success) </summary>
                public Exception error = null;
            }

            /// <summary> Status from testing stored fields.</summary>
            public sealed class TermVectorStatus
            {

                /// <summary>Number of documents tested. </summary>
                public int docCount = 0;

                /// <summary>Total number of term vectors tested. </summary>
                public long totVectors = 0;

                /// <summary>Exception thrown during term vector test (null on success) </summary>
                public Exception error = null;
            }

            public sealed class DocValuesStatus
            {
                public int docCount = 0;

                public long totalValueFields = 0L;

                public Exception error = null;
            }
        }

        /// <summary>Create a new CheckIndex on the directory. </summary>
        public CheckIndex(Directory dir)
        {
            this.dir = dir;
            infoStream = null;
        }

        private bool crossCheckTermVectors;

        public bool CrossCheckTermVectors
        {
            get { return crossCheckTermVectors; }
            set { crossCheckTermVectors = value; }
        }

        private bool verbose;

        /// <summary>Set infoStream where messages should go.  If null, no
        /// messages are printed 
        /// </summary>
        public virtual void SetInfoStream(StreamWriter @out, bool verbose)
        {
            infoStream = @out;
            this.verbose = verbose;
        }

        public virtual void SetInfoStream(StreamWriter @out)
        {
            SetInfoStream(@out, false);
        }

        private static void Msg(StreamWriter @out, string msg)
        {
            if (@out != null)
                @out.WriteLine(msg);
        }

        /// <summary>Returns a <see cref="Status" /> instance detailing
        /// the state of the index.
        /// 
        /// <p/>As this method checks every byte in the index, on a large
        /// index it can take quite a long time to run.
        /// 
        /// <p/><b>WARNING</b>: make sure
        /// you only call this when the index is not opened by any
        /// writer. 
        /// </summary>
        public virtual Status CheckIndex_Renamed_Method()
        {
            return CheckIndex_Renamed_Method(null);
        }

        /// <summary>Returns a <see cref="Status" /> instance detailing
        /// the state of the index.
        /// 
        /// </summary>
        /// <param name="onlySegments">list of specific segment names to check
        /// 
        /// <p/>As this method checks every byte in the specified
        /// segments, on a large index it can take quite a long
        /// time to run.
        /// 
        /// <p/><b>WARNING</b>: make sure
        /// you only call this when the index is not opened by any
        /// writer. 
        /// </param>
        public virtual Status CheckIndex_Renamed_Method(IList<string> onlySegments)
        {
            NumberFormatInfo nf = CultureInfo.CurrentCulture.NumberFormat;
            SegmentInfos sis = new SegmentInfos();
            Status result = new Status();
            result.dir = dir;
            try
            {
                sis.Read(dir);
            }
            catch (Exception t)
            {
                Msg(infoStream, "ERROR: could not read any segments file in directory");
                result.missingSegments = true;
                if (infoStream != null)
                    infoStream.WriteLine(t.StackTrace);
                return result;
            }

            // find the oldest and newest segment versions
            String oldest = int.MaxValue.ToString(), newest = int.MinValue.ToString();
            String oldSegs = null;
            bool foundNonNullVersion = false;
            IComparer<String> versionComparator = StringHelper.VersionComparator;
            foreach (SegmentInfoPerCommit si in sis)
            {
                String version = si.info.Version;
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

            int numSegments = sis.Count;
            var segmentsFileName = sis.GetCurrentSegmentFileName();
            IndexInput input = null;
            try
            {
                input = dir.OpenInput(segmentsFileName, IOContext.DEFAULT);
            }
            catch (Exception t)
            {
                Msg(infoStream, "ERROR: could not open segments file in directory");
                if (infoStream != null)
                    infoStream.WriteLine(t.StackTrace);
                result.cantOpenSegments = true;
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
                    infoStream.WriteLine(t.StackTrace);
                result.missingSegmentVersion = true;
                return result;
            }
            finally
            {
                if (input != null)
                    input.Dispose();
            }

            string sFormat = "";
            bool skip = false;

            result.segmentsFileName = segmentsFileName;
            result.numSegments = numSegments;
            result.userData = sis.UserData;
            String userDataString;
            if (sis.UserData.Count > 0)
            {
                userDataString = " userData=" + string.Join(", ", sis.UserData.Select(ud => ud.Key + ":" + ud.Value));
            }
            else
            {
                userDataString = "";
            }

            String versionString = null;
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

            Msg(infoStream, "Segments file=" + segmentsFileName + " numSegments=" + numSegments
                + " " + versionString + " format=" + sFormat + userDataString);

            if (onlySegments != null)
            {
                result.partial = true;
                if (infoStream != null)
                    infoStream.Write("\nChecking only these segments:");
                foreach (String s in onlySegments)
                {
                    if (infoStream != null)
                        infoStream.Write(" " + s);
                }
                result.segmentsChecked.AddRange(onlySegments);
                Msg(infoStream, ":");
            }

            if (skip)
            {
                Msg(infoStream, "\nERROR: this index appears to be created by a newer version of Lucene than this tool was compiled on; please re-compile this tool on the matching version of Lucene; exiting");
                result.toolOutOfDate = true;
                return result;
            }


            result.newSegments = (SegmentInfos)sis.Clone();
            result.newSegments.Clear();
            result.maxSegmentName = -1;

            for (int i = 0; i < numSegments; i++)
            {
                SegmentInfoPerCommit info = sis.Info(i);
                int segmentName = int.Parse(info.info.name.Substring(1));
                if (segmentName > result.maxSegmentName)
                {
                    result.maxSegmentName = segmentName;
                }
                if (onlySegments != null && !onlySegments.Contains(info.info.name))
                {
                    continue;
                }
                Status.SegmentInfoStatus segInfoStat = new Status.SegmentInfoStatus();
                result.segmentInfos.Add(segInfoStat);
                Msg(infoStream, "  " + (1 + i) + " of " + numSegments + ": name=" + info.info.name + " docCount=" + info.info.DocCount);
                segInfoStat.name = info.info.name;
                segInfoStat.docCount = info.info.DocCount;

                int toLoseDocCount = info.info.DocCount;

                AtomicReader reader = null;

                try
                {
                    Codec codec = info.info.Codec;
                    Msg(infoStream, "    codec=" + codec);
                    segInfoStat.codec = codec;
                    Msg(infoStream, "    compound=" + info.info.UseCompoundFile);
                    segInfoStat.compound = info.info.UseCompoundFile;
                    Msg(infoStream, "    numFiles=" + info.Files.Count);
                    segInfoStat.numFiles = info.Files.Count;
                    segInfoStat.sizeMB = info.SizeInBytes / (1024.0 * 1024.0);
                    if (info.info.GetAttribute(Lucene3xSegmentInfoFormat.DS_OFFSET_KEY) == null)
                    {
                        // don't print size in bytes if its a 3.0 segment with shared docstores
                        Msg(infoStream, "    size (MB)=" + segInfoStat.sizeMB.ToString(nf));
                    }
                    IDictionary<String, String> diagnostics = info.info.Diagnostics;
                    segInfoStat.diagnostics = diagnostics;
                    if (diagnostics.Count > 0)
                    {
                        Msg(infoStream, "    diagnostics = " + diagnostics);
                    }

                    IDictionary<String, String> atts = info.info.Attributes;
                    if (atts != null && atts.Count > 0)
                    {
                        Msg(infoStream, "    attributes = " + atts);
                    }

                    if (!info.HasDeletions)
                    {
                        Msg(infoStream, "    no deletions");
                        segInfoStat.hasDeletions = false;
                    }
                    else
                    {
                        Msg(infoStream, "    has deletions [delGen=" + info.DelGen + "]");
                        segInfoStat.hasDeletions = true;
                        segInfoStat.deletionsGen = info.DelGen.ToString();
                    }
                    if (infoStream != null)
                        infoStream.Write("    test: open reader.........");
                    reader = new SegmentReader(info, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, IOContext.DEFAULT);

                    segInfoStat.openReaderPassed = true;

                    int numDocs = reader.NumDocs;
                    toLoseDocCount = numDocs;
                    if (reader.HasDeletions)
                    {
                        if (reader.NumDocs != info.info.DocCount - info.DelCount)
                        {
                            throw new SystemException("delete count mismatch: info=" + (info.info.DocCount - info.DelCount) + " vs reader=" + reader.NumDocs);
                        }
                        if ((info.info.DocCount - reader.NumDocs) > reader.MaxDoc)
                        {
                            throw new SystemException("too many deleted docs: maxDoc()=" + reader.MaxDoc + " vs del count=" + (info.info.DocCount - reader.NumDocs));
                        }
                        if (info.info.DocCount - numDocs != info.DelCount)
                        {
                            throw new SystemException("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.info.DocCount - numDocs));
                        }
                        IBits liveDocs = reader.LiveDocs;
                        if (liveDocs == null)
                        {
                            throw new SystemException("segment should have deletions, but liveDocs is null");
                        }
                        else
                        {
                            int numLive = 0;
                            for (int j = 0; j < liveDocs.Length; j++)
                            {
                                if (liveDocs[j])
                                {
                                    numLive++;
                                }
                            }
                            if (numLive != numDocs)
                            {
                                throw new SystemException("liveDocs count mismatch: info=" + numDocs + ", vs bits=" + numLive);
                            }
                        }

                        segInfoStat.numDeleted = info.info.DocCount - numDocs;
                        Msg(infoStream, "OK [" + (segInfoStat.numDeleted) + " deleted docs]");
                    }
                    else
                    {
                        if (info.DelCount != 0)
                        {
                            throw new SystemException("delete count mismatch: info=" + info.DelCount + " vs reader=" + (info.info.DocCount - numDocs));
                        }
                        IBits liveDocs = reader.LiveDocs;
                        if (liveDocs != null)
                        {
                            // its ok for it to be non-null here, as long as none are set right?
                            for (int j = 0; j < liveDocs.Length; j++)
                            {
                                if (!liveDocs[j])
                                {
                                    throw new SystemException("liveDocs mismatch: info says no deletions but doc " + j + " is deleted.");
                                }
                            }
                        }
                        Msg(infoStream, "OK");
                    }
                    if (reader.MaxDoc != info.info.DocCount)
                    {
                        throw new SystemException("SegmentReader.maxDoc() " + reader.MaxDoc + " != SegmentInfos.docCount " + info.info.DocCount);
                    }

                    // Test getFieldInfos()
                    if (infoStream != null)
                    {
                        infoStream.Write("    test: fields..............");
                    }
                    FieldInfos fieldInfos = reader.FieldInfos;
                    Msg(infoStream, "OK [" + fieldInfos.Size() + " fields]");
                    segInfoStat.numFields = fieldInfos.Size();

                    // Test Field Norms
                    segInfoStat.fieldNormStatus = TestFieldNorms(reader, infoStream);

                    // Test the Term Index
                    segInfoStat.termIndexStatus = TestPostings(reader, infoStream, verbose);

                    // Test Stored Fields
                    segInfoStat.storedFieldStatus = TestStoredFields(reader, infoStream);

                    // Test Term Vectors
                    segInfoStat.termVectorStatus = TestTermVectors(reader, infoStream, verbose, crossCheckTermVectors);

                    segInfoStat.docValuesStatus = TestDocValues(reader, infoStream);

                    // Rethrow the first exception we encountered
                    //  This will cause stats for failed segments to be incremented properly
                    if (segInfoStat.fieldNormStatus.error != null)
                    {
                        throw new SystemException("Field Norm test failed");
                    }
                    else if (segInfoStat.termIndexStatus.error != null)
                    {
                        throw new SystemException("Term Index test failed");
                    }
                    else if (segInfoStat.storedFieldStatus.error != null)
                    {
                        throw new SystemException("Stored Field test failed");
                    }
                    else if (segInfoStat.termVectorStatus.error != null)
                    {
                        throw new SystemException("Term Vector test failed");
                    }
                    else if (segInfoStat.docValuesStatus.error != null)
                    {
                        throw new SystemException("DocValues test failed");
                    }

                    Msg(infoStream, "");

                }
                catch (Exception t)
                {
                    Msg(infoStream, "FAILED");
                    String comment;
                    comment = "fixIndex() would remove reference to this segment";
                    Msg(infoStream, "    WARNING: " + comment + "; full exception:");
                    if (infoStream != null)
                        infoStream.WriteLine(t.StackTrace);
                    Msg(infoStream, "");
                    result.totLoseDocCount += toLoseDocCount;
                    result.numBadSegments++;
                    continue;
                }
                finally
                {
                    if (reader != null)
                        reader.Dispose();
                }

                // Keeper
                result.newSegments.Add((SegmentInfo)info.Clone());
            }

            if (0 == result.numBadSegments)
            {
                result.clean = true;
            }
            else
                Msg(infoStream, "WARNING: " + result.numBadSegments + " broken segments (containing " + result.totLoseDocCount + " documents) detected");

            if (!(result.validCounter = (result.maxSegmentName < sis.counter)))
            {
                result.clean = false;
                result.newSegments.counter = result.maxSegmentName + 1;
                Msg(infoStream, "ERROR: Next segment name counter " + sis.counter + " is not greater than max segment name " + result.maxSegmentName);
            }

            if (result.clean)
            {
                Msg(infoStream, "No problems were detected with this index.\n");
            }

            return result;
        }

        /// <summary> Test field norms.</summary>
        private Status.FieldNormStatus TestFieldNorms(IEnumerable<string> fieldNames, SegmentReader reader)
        {
            var status = new Status.FieldNormStatus();

            try
            {
                // Test Field Norms
                if (infoStream != null)
                {
                    infoStream.Write("    test: field norms.........");
                }

                foreach (FieldInfo info in reader.FieldInfos)
                {
                    if (info.HasNorms)
                    {
                        ////assert reader.hasNorms(info.name); // deprecated path
                        CheckNorms(info, reader, infoStream);
                        ++status.totFields;
                    }
                    else
                    {
                        ////assert !reader.hasNorms(info.name); // deprecated path
                        if (reader.GetNormValues(info.name) != null)
                        {
                            throw new SystemException("field: " + info.name + " should omit norms but has them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.totFields + " fields]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.error = e;
                if (infoStream != null)
                {
                    infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        private static Status.TermIndexStatus CheckFields(Fields fields, IBits liveDocs, int maxDoc, FieldInfos fieldInfos, bool doPrint, bool isVectors, StreamWriter infoStream, bool verbose)
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

            String lastField = null;
            foreach (String field in fields)
            {
                // MultiFieldsEnum relies upon this order...
                if (lastField != null && field.CompareTo(lastField) <= 0)
                {
                    throw new SystemException("fields out of order: lastField=" + lastField + " field=" + field);
                }
                lastField = field;

                // check that the field is in fieldinfos, and is indexed.
                // TODO: add a separate test to check this for different reader impls
                FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                if (fieldInfo == null)
                {
                    throw new SystemException("fieldsEnum inconsistent with fieldInfos, no fieldInfos for: " + field);
                }
                if (!fieldInfo.IsIndexed)
                {
                    throw new SystemException("fieldsEnum inconsistent with fieldInfos, isIndexed == false for: " + field);
                }

                // TODO: really the codec should not return a field
                // from FieldsEnum if it has no Terms... but we do
                // this today:
                // //assert fields.terms(field) != null;
                computedFieldCount++;

                Terms terms = fields.Terms(field);
                if (terms == null)
                {
                    continue;
                }

                bool hasPositions = terms.HasPositions;
                bool hasOffsets = terms.HasOffsets;
                // term vectors cannot omit TF
                bool hasFreqs = isVectors || fieldInfo.IndexOptionsValue.GetValueOrDefault() >= FieldInfo.IndexOptions.DOCS_AND_FREQS;

                TermsEnum termsEnum = terms.Iterator(null);

                bool hasOrd = true;
                long termCountStart = status.delTermCount + status.termCount;

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

                    //assert term.isValid();

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
                            throw new SystemException("terms out of order: lastTerm=" + lastTerm + " term=" + term);
                        }
                        lastTerm.CopyBytes(term);
                    }

                    int docFreq = termsEnum.DocFreq;
                    if (docFreq <= 0)
                    {
                        throw new SystemException("docfreq: " + docFreq + " is out of bounds");
                    }
                    sumDocFreq += docFreq;

                    docs = termsEnum.Docs(liveDocs, docs);
                    postings = termsEnum.DocsAndPositions(liveDocs, postings);

                    if (hasOrd)
                    {
                        long ord = -1;
                        try
                        {
                            ord = termsEnum.Ord;
                        }
                        catch (NotSupportedException uoe)
                        {
                            hasOrd = false;
                        }

                        if (hasOrd)
                        {
                            long ordExpected = status.delTermCount + status.termCount - termCountStart;
                            if (ord != ordExpected)
                            {
                                throw new SystemException("ord mismatch: TermsEnum has ord=" + ord + " vs actual=" + ordExpected);
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
                        status.totFreq++;
                        visitedDocs.Set(doc);
                        int freq = -1;
                        if (hasFreqs)
                        {
                            freq = docs2.Freq;
                            if (freq <= 0)
                            {
                                throw new SystemException("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");
                            }
                            status.totPos += freq;
                            totalTermFreq += freq;
                        }
                        docCount++;

                        if (doc <= lastDoc)
                        {
                            throw new SystemException("term " + term + ": doc " + doc + " <= lastDoc " + lastDoc);
                        }
                        if (doc >= maxDoc)
                        {
                            throw new SystemException("term " + term + ": doc " + doc + " >= maxDoc " + maxDoc);
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
                                    throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
                                }
                                if (pos < lastPos)
                                {
                                    throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
                                }
                                lastPos = pos;
                                BytesRef payload = postings.Payload;
                                if (payload != null)
                                {
                                    //assert payload.isValid();
                                }
                                if (payload != null && payload.length < 1)
                                {
                                    throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + " payload length is out of bounds " + payload.length);
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
                                            throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                        }
                                        if (startOffset < lastOffset)
                                        {
                                            throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                        }
                                        if (endOffset < 0)
                                        {
                                            throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                        }
                                        if (endOffset < startOffset)
                                        {
                                            throw new SystemException("term " + term + ": doc " + doc + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
                                        }
                                    }
                                    lastOffset = startOffset;
                                }
                            }
                        }
                    }

                    if (docCount != 0)
                    {
                        status.termCount++;
                    }
                    else
                    {
                        status.delTermCount++;
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
                            DocsEnum docsNoDel = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
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
                        throw new SystemException("term " + term + " docFreq=" + docFreq + " != tot docs w/o deletions " + docCount);
                    }
                    if (hasTotalTermFreq)
                    {
                        if (totalTermFreq2 <= 0)
                        {
                            throw new SystemException("totalTermFreq: " + totalTermFreq2 + " is out of bounds");
                        }
                        sumTotalTermFreq += totalTermFreq;
                        if (totalTermFreq != totalTermFreq2)
                        {
                            throw new SystemException("term " + term + " totalTermFreq=" + totalTermFreq2 + " != recomputed totalTermFreq=" + totalTermFreq);
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
                                    throw new SystemException("term " + term + ": advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int freq = postings.Freq;
                                if (freq <= 0)
                                {
                                    throw new SystemException("termFreq " + freq + " is out of bounds");
                                }
                                int lastPosition = -1;
                                int lastOffset = 0;
                                for (int posUpto = 0; posUpto < freq; posUpto++)
                                {
                                    int pos = postings.NextPosition();

                                    if (pos < 0)
                                    {
                                        throw new SystemException("position " + pos + " is out of bounds");
                                    }
                                    if (pos < lastPosition)
                                    {
                                        throw new SystemException("position " + pos + " is < lastPosition " + lastPosition);
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
                                                throw new SystemException("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " is out of bounds");
                                            }
                                            if (startOffset < lastOffset)
                                            {
                                                throw new SystemException("term " + term + ": doc " + docID + ": pos " + pos + ": startOffset " + startOffset + " < lastStartOffset " + lastOffset);
                                            }
                                            if (endOffset < 0)
                                            {
                                                throw new SystemException("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " is out of bounds");
                                            }
                                            if (endOffset < startOffset)
                                            {
                                                throw new SystemException("term " + term + ": doc " + docID + ": pos " + pos + ": endOffset " + endOffset + " < startOffset " + startOffset);
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
                                    throw new SystemException("term " + term + ": advance(docID=" + skipDocID + "), then .next() returned docID=" + nextDocID + " vs prev docID=" + docID);
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
                                    throw new SystemException("term " + term + ": advance(docID=" + skipDocID + ") returned docID=" + docID);
                                }
                                int nextDocID = docs.NextDoc();
                                if (nextDocID == DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    break;
                                }
                                if (nextDocID <= docID)
                                {
                                    throw new SystemException("term " + term + ": advance(docID=" + skipDocID + "), then .next() returned docID=" + nextDocID + " vs prev docID=" + docID);
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
                        //assert stats != null;
                        if (status.blockTreeStats == null)
                        {
                            status.blockTreeStats = new HashMap<String, BlockTreeTermsReader.Stats>();
                        }
                        status.blockTreeStats[field] = stats;
                    }

                    if (sumTotalTermFreq != 0)
                    {
                        long v = fields.Terms(field).SumTotalTermFreq;
                        if (v != -1 && sumTotalTermFreq != v)
                        {
                            throw new SystemException("sumTotalTermFreq for field " + field + "=" + v + " != recomputed sumTotalTermFreq=" + sumTotalTermFreq);
                        }
                    }

                    if (sumDocFreq != 0)
                    {
                        long v = fields.Terms(field).SumDocFreq;
                        if (v != -1 && sumDocFreq != v)
                        {
                            throw new SystemException("sumDocFreq for field " + field + "=" + v + " != recomputed sumDocFreq=" + sumDocFreq);
                        }
                    }

                    if (fieldTerms != null)
                    {
                        int v = fieldTerms.DocCount;
                        if (v != -1 && visitedDocs.Cardinality() != v)
                        {
                            throw new SystemException("docCount for field " + field + "=" + v + " != recomputed docCount=" + visitedDocs.Cardinality());
                        }
                    }

                    // Test seek to last term:
                    if (lastTerm != null)
                    {
                        if (termsEnum.SeekCeil(lastTerm) != TermsEnum.SeekStatus.FOUND)
                        {
                            throw new SystemException("seek to last term " + lastTerm + " failed");
                        }

                        int expectedDocFreq = termsEnum.DocFreq;
                        DocsEnum d = termsEnum.Docs(null, null, DocsEnum.FLAG_NONE);
                        int docFreq = 0;
                        while (d.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                        {
                            docFreq++;
                        }
                        if (docFreq != expectedDocFreq)
                        {
                            throw new SystemException("docFreq for last term " + lastTerm + "=" + expectedDocFreq + " != recomputed docFreq=" + docFreq);
                        }
                    }

                    // check unique term count
                    long termCount = -1;

                    if ((status.delTermCount + status.termCount) - termCountStart > 0)
                    {
                        termCount = fields.Terms(field).Size;

                        if (termCount != -1 && termCount != status.delTermCount + status.termCount - termCountStart)
                        {
                            throw new SystemException("termCount mismatch " + (status.delTermCount + termCount) + " vs " + (status.termCount - termCountStart));
                        }
                    }

                    // Test seeking by ord
                    if (hasOrd && status.termCount - termCountStart > 0)
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
                                    throw new SystemException("seek to existing term " + seekTerms[i] + " failed");
                                }

                                docs = termsEnum.Docs(liveDocs, docs, DocsEnum.FLAG_NONE);
                                if (docs == null)
                                {
                                    throw new SystemException("null DocsEnum from to existing term " + seekTerms[i]);
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
                                if (!termsEnum.SeekExact(seekTerms[i], true))
                                {
                                    throw new SystemException("seek to existing term " + seekTerms[i] + " failed");
                                }

                                totDocFreq += termsEnum.DocFreq;
                                docs = termsEnum.Docs(null, docs, DocsEnum.FLAG_NONE);
                                if (docs == null)
                                {
                                    throw new SystemException("null DocsEnum from to existing term " + seekTerms[i]);
                                }

                                while (docs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                                {
                                    totDocCountNoDeletes++;
                                }
                            }

                            if (totDocCount > totDocCountNoDeletes)
                            {
                                throw new SystemException("more postings with deletes=" + totDocCount + " than without=" + totDocCountNoDeletes);
                            }

                            if (totDocCountNoDeletes != totDocFreq)
                            {
                                throw new SystemException("docfreqs=" + totDocFreq + " != recomputed docfreqs=" + totDocCountNoDeletes);
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
                    throw new SystemException("invalid fieldCount: " + fieldCount);
                }
                if (fieldCount != computedFieldCount)
                {
                    throw new SystemException("fieldCount mismatch " + fieldCount + " vs recomputed field count " + computedFieldCount);
                }
            }

            // for most implementations, this is boring (just the sum across all fields)
            // but codecs that don't work per-field like preflex actually implement this,
            // but don't implement it on Terms, so the check isn't redundant.
            long uniqueTermCountAllFields = fields.UniqueTermCount;

            if (uniqueTermCountAllFields != -1 && status.termCount + status.delTermCount != uniqueTermCountAllFields)
            {
                throw new SystemException("termCount mismatch " + uniqueTermCountAllFields + " vs " + (status.termCount + status.delTermCount));
            }

            if (doPrint)
            {
                Msg(infoStream, "OK [" + status.termCount + " terms; " + status.totFreq + " terms/docs pairs; " + status.totPos + " tokens]");
            }

            if (verbose && status.blockTreeStats != null && infoStream != null && status.termCount > 0)
            {
                foreach (KeyValuePair<String, BlockTreeTermsReader.Stats> ent in status.blockTreeStats)
                {
                    infoStream.WriteLine("      field \"" + ent.Key + "\":");
                    infoStream.WriteLine("      " + ent.Value.ToString().Replace("\n", "\n      "));
                }
            }

            return status;
        }

        public static Status.TermIndexStatus TestPostings(AtomicReader reader, StreamWriter infoStream)
        {
            return TestPostings(reader, infoStream, false);
        }

        public static Status.TermIndexStatus TestPostings(AtomicReader reader, StreamWriter infoStream, bool verbose)
        {

            // TODO: we should go and verify term vectors match, if
            // crossCheckTermVectors is on...

            Status.TermIndexStatus status;
            int maxDoc = reader.MaxDoc;
            IBits liveDocs = reader.LiveDocs;

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
                status.error = e;
                if (infoStream != null)
                {
                    infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary> Test stored fields for a segment.</summary>
        private Status.StoredFieldStatus TestStoredFields(AtomicReader reader, StreamWriter infoStream)
        {
            var status = new Status.StoredFieldStatus();

            try
            {
                if (infoStream != null)
                {
                    infoStream.Write("    test: stored fields.......");
                }

                // Scan stored fields for all documents
                IBits liveDocs = reader.LiveDocs;
                for (int j = 0; j < reader.MaxDoc; ++j)
                {
                    // Intentionally pull even deleted documents to
                    // make sure they too are not corrupt:
                    Document doc = reader.Document(j);
                    if (liveDocs == null || liveDocs[j])
                    {
                        status.docCount++;
                        status.totFields += doc.GetFields().Count;
                    }
                }

                // Validate docCount
                if (status.docCount != reader.NumDocs)
                {
                    throw new SystemException("docCount=" + status.docCount + " but saw " + status.docCount + " undeleted docs");
                }

                Msg(infoStream, "OK [" + status.totFields + " total field count; avg " +
                        ((((float)status.totFields) / status.docCount)).ToString(CultureInfo.InvariantCulture.NumberFormat) + " fields per doc]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.error = e;
                if (infoStream != null)
                {
                    infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        public static Status.DocValuesStatus TestDocValues(AtomicReader reader, StreamWriter infoStream)
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
                    if (fieldInfo.HasDocValues)
                    {
                        status.totalValueFields++;
                        CheckDocValues(fieldInfo, reader, infoStream);
                    }
                    else
                    {
                        if (reader.GetBinaryDocValues(fieldInfo.name) != null ||
                            reader.GetNumericDocValues(fieldInfo.name) != null ||
                            reader.GetSortedDocValues(fieldInfo.name) != null ||
                            reader.GetSortedSetDocValues(fieldInfo.name) != null)
                        {
                            throw new SystemException("field: " + fieldInfo.name + " has docvalues but should omit them!");
                        }
                    }
                }

                Msg(infoStream, "OK [" + status.docCount + " total doc count; " + status.totalValueFields + " docvalues fields]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.error = e;
                if (infoStream != null)
                {
                    infoStream.WriteLine(e.StackTrace);
                }
            }
            return status;
        }

        private static void CheckBinaryDocValues(String fieldName, AtomicReader reader, BinaryDocValues dv)
        {
            BytesRef scratch = new BytesRef();
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.Get(i, scratch);
                //assert scratch.isValid();
            }
        }

        private static void CheckSortedDocValues(String fieldName, AtomicReader reader, SortedDocValues dv)
        {
            CheckBinaryDocValues(fieldName, reader, dv);
            int maxOrd = dv.ValueCount - 1;
            FixedBitSet seenOrds = new FixedBitSet(dv.ValueCount);
            int maxOrd2 = -1;
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                int ord = dv.GetOrd(i);
                if (ord < 0 || ord > maxOrd)
                {
                    throw new SystemException("ord out of bounds: " + ord);
                }
                maxOrd2 = Math.Max(maxOrd2, ord);
                seenOrds.Set(ord);
            }
            if (maxOrd != maxOrd2)
            {
                throw new SystemException("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality() != dv.ValueCount)
            {
                throw new SystemException("dv for field: " + fieldName + " has holes in its ords, valueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality());
            }
            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (int i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                //assert scratch.isValid();
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw new SystemException("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckSortedSetDocValues(String fieldName, AtomicReader reader, SortedSetDocValues dv)
        {
            long maxOrd = dv.ValueCount - 1;
            OpenBitSet seenOrds = new OpenBitSet(dv.ValueCount);
            long maxOrd2 = -1;
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                dv.SetDocument(i);
                long lastOrd = -1;
                long ord;
                while ((ord = dv.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    if (ord <= lastOrd)
                    {
                        throw new SystemException("ords out of order: " + ord + " <= " + lastOrd + " for doc: " + i);
                    }
                    if (ord < 0 || ord > maxOrd)
                    {
                        throw new SystemException("ord out of bounds: " + ord);
                    }
                    lastOrd = ord;
                    maxOrd2 = Math.Max(maxOrd2, ord);
                    seenOrds.Set(ord);
                }
            }
            if (maxOrd != maxOrd2)
            {
                throw new SystemException("dv for field: " + fieldName + " reports wrong maxOrd=" + maxOrd + " but this is not the case: " + maxOrd2);
            }
            if (seenOrds.Cardinality != dv.ValueCount)
            {
                throw new SystemException("dv for field: " + fieldName + " has holes in its ords, valueCount=" + dv.ValueCount + " but only used: " + seenOrds.Cardinality);
            }

            BytesRef lastValue = null;
            BytesRef scratch = new BytesRef();
            for (long i = 0; i <= maxOrd; i++)
            {
                dv.LookupOrd(i, scratch);
                //assert scratch.isValid();
                if (lastValue != null)
                {
                    if (scratch.CompareTo(lastValue) <= 0)
                    {
                        throw new SystemException("dv for field: " + fieldName + " has ords out of order: " + lastValue + " >=" + scratch);
                    }
                }
                lastValue = BytesRef.DeepCopyOf(scratch);
            }
        }

        private static void CheckNumericDocValues(String fieldName, AtomicReader reader, NumericDocValues ndv)
        {
            for (int i = 0; i < reader.MaxDoc; i++)
            {
                ndv.Get(i);
            }
        }

        private static void CheckDocValues(FieldInfo fi, AtomicReader reader, StreamWriter infoStream)
        {
            switch (fi.DocValuesTypeValue.GetValueOrDefault())
            {
                case FieldInfo.DocValuesType.SORTED:
                    CheckSortedDocValues(fi.name, reader, reader.GetSortedDocValues(fi.name));
                    if (reader.GetBinaryDocValues(fi.name) != null ||
                        reader.GetNumericDocValues(fi.name) != null ||
                        reader.GetSortedSetDocValues(fi.name) != null)
                    {
                        throw new SystemException(fi.name + " returns multiple docvalues types!");
                    }
                    break;
                case FieldInfo.DocValuesType.SORTED_SET:
                    CheckSortedSetDocValues(fi.name, reader, reader.GetSortedSetDocValues(fi.name));
                    if (reader.GetBinaryDocValues(fi.name) != null ||
                        reader.GetNumericDocValues(fi.name) != null ||
                        reader.GetSortedDocValues(fi.name) != null)
                    {
                        throw new SystemException(fi.name + " returns multiple docvalues types!");
                    }
                    break;
                case FieldInfo.DocValuesType.BINARY:
                    CheckBinaryDocValues(fi.name, reader, reader.GetBinaryDocValues(fi.name));
                    if (reader.GetNumericDocValues(fi.name) != null ||
                        reader.GetSortedDocValues(fi.name) != null ||
                        reader.GetSortedSetDocValues(fi.name) != null)
                    {
                        throw new SystemException(fi.name + " returns multiple docvalues types!");
                    }
                    break;
                case FieldInfo.DocValuesType.NUMERIC:
                    CheckNumericDocValues(fi.name, reader, reader.GetNumericDocValues(fi.name));
                    if (reader.GetBinaryDocValues(fi.name) != null ||
                        reader.GetSortedDocValues(fi.name) != null ||
                        reader.GetSortedSetDocValues(fi.name) != null)
                    {
                        throw new SystemException(fi.name + " returns multiple docvalues types!");
                    }
                    break;
                default:
                    throw new SystemException();
            }
        }

        private static void CheckNorms(FieldInfo fi, AtomicReader reader, StreamWriter infoStream)
        {
            switch (fi.NormType.GetValueOrDefault())
            {
                case FieldInfo.DocValuesType.NUMERIC:
                    CheckNumericDocValues(fi.name, reader, reader.GetNormValues(fi.name));
                    break;
                default:
                    throw new SystemException("wtf: " + fi.NormType);
            }
        }

        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, StreamWriter infoStream)
        {
            return TestTermVectors(reader, infoStream, false, false);
        }

        public static Status.TermVectorStatus TestTermVectors(AtomicReader reader, StreamWriter infoStream, bool verbose, bool crossCheckTermVectors)
        {
            Status.TermVectorStatus status = new Status.TermVectorStatus();
            FieldInfos fieldInfos = reader.FieldInfos;
            IBits onlyDocIsDeleted = new FixedBitSet(1);

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
                        bool doStats = liveDocs == null || liveDocs[j];
                        if (doStats)
                        {
                            status.docCount++;
                        }

                        foreach (String field in tfv)
                        {
                            if (doStats)
                            {
                                status.totVectors++;
                            }

                            // Make sure FieldInfo thinks this field is vector'd:
                            FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                            if (!fieldInfo.HasVectors)
                            {
                                throw new SystemException("docID=" + j + " has term vectors for field=" + field + " but FieldInfo has storeTermVector=false");
                            }

                            if (crossCheckTermVectors)
                            {
                                Terms terms = tfv.Terms(field);
                                termsEnum = terms.Iterator(termsEnum);
                                bool postingsHasFreq = fieldInfo.IndexOptionsValue.GetValueOrDefault() >= FieldInfo.IndexOptions.DOCS_AND_FREQS;
                                bool postingsHasPayload = fieldInfo.HasPayloads;
                                bool vectorsHasPayload = terms.HasPayloads;

                                Terms postingsTerms = postingsFields.Terms(field);
                                if (postingsTerms == null)
                                {
                                    throw new SystemException("vector field=" + field + " does not exist in postings; doc=" + j);
                                }
                                postingsTermsEnum = postingsTerms.Iterator(postingsTermsEnum);

                                bool hasProx = terms.HasOffsets || terms.HasPositions;
                                BytesRef term = null;
                                while ((term = termsEnum.Next()) != null)
                                {

                                    if (hasProx)
                                    {
                                        postings = termsEnum.DocsAndPositions(null, postings);
                                        //assert postings != null;
                                        docs = null;
                                    }
                                    else
                                    {
                                        docs = termsEnum.Docs(null, docs);
                                        //assert docs != null;
                                        postings = null;
                                    }

                                    DocsEnum docs2;
                                    if (hasProx)
                                    {
                                        //assert postings != null;
                                        docs2 = postings;
                                    }
                                    else
                                    {
                                        //assert docs != null;
                                        docs2 = docs;
                                    }

                                    DocsEnum postingsDocs2;
                                    if (!postingsTermsEnum.SeekExact(term, true))
                                    {
                                        throw new SystemException("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
                                    }
                                    postingsPostings = postingsTermsEnum.DocsAndPositions(null, postingsPostings);
                                    if (postingsPostings == null)
                                    {
                                        // Term vectors were indexed w/ pos but postings were not
                                        postingsDocs = postingsTermsEnum.Docs(null, postingsDocs);
                                        if (postingsDocs == null)
                                        {
                                            throw new SystemException("vector term=" + term + " field=" + field + " does not exist in postings; doc=" + j);
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
                                        throw new SystemException("vector term=" + term + " field=" + field + ": doc=" + j + " was not found in postings (got: " + advanceDoc + ")");
                                    }

                                    int doc = docs2.NextDoc();

                                    if (doc != 0)
                                    {
                                        throw new SystemException("vector for doc " + j + " didn't return docID=0: got docID=" + doc);
                                    }

                                    if (postingsHasFreq)
                                    {
                                        int tf = docs2.Freq;
                                        if (postingsHasFreq && postingsDocs2.Freq != tf)
                                        {
                                            throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + ": freq=" + tf + " differs from postings freq=" + postingsDocs2.Freq);
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
                                                        throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + ": pos=" + pos + " differs from postings pos=" + postingsPos);
                                                    }
                                                }

                                                // Call the methods to at least make
                                                // sure they don't throw exc:
                                                int startOffset = postings.StartOffset;
                                                int endOffset = postings.EndOffset;
                                                // TODO: these are too anal...?
                                                /*
                                                  if (endOffset < startOffset) {
                                                  throw new SystemException("vector startOffset=" + startOffset + " is > endOffset=" + endOffset);
                                                  }
                                                  if (startOffset < lastStartOffset) {
                                                  throw new SystemException("vector startOffset=" + startOffset + " is < prior startOffset=" + lastStartOffset);
                                                  }
                                                  lastStartOffset = startOffset;
                                                */

                                                if (postingsPostings != null)
                                                {
                                                    int postingsStartOffset = postingsPostings.StartOffset;

                                                    int postingsEndOffset = postingsPostings.EndOffset;
                                                    if (startOffset != -1 && postingsStartOffset != -1 && startOffset != postingsStartOffset)
                                                    {
                                                        throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + ": startOffset=" + startOffset + " differs from postings startOffset=" + postingsStartOffset);
                                                    }
                                                    if (endOffset != -1 && postingsEndOffset != -1 && endOffset != postingsEndOffset)
                                                    {
                                                        throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + ": endOffset=" + endOffset + " differs from postings endOffset=" + postingsEndOffset);
                                                    }
                                                }

                                                BytesRef payload = postings.Payload;

                                                if (payload != null)
                                                {
                                                    //assert vectorsHasPayload;
                                                }

                                                if (postingsHasPayload && vectorsHasPayload)
                                                {
                                                    //assert postingsPostings != null;

                                                    if (payload == null)
                                                    {
                                                        // we have payloads, but not at this position. 
                                                        // postings has payloads too, it should not have one at this position
                                                        if (postingsPostings.Payload != null)
                                                        {
                                                            throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + " has no payload but postings does: " + postingsPostings.Payload);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // we have payloads, and one at this position
                                                        // postings should also have one at this position, with the same bytes.
                                                        if (postingsPostings.Payload == null)
                                                        {
                                                            throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but postings does not.");
                                                        }
                                                        BytesRef postingsPayload = postingsPostings.Payload;
                                                        if (!payload.Equals(postingsPayload))
                                                        {
                                                            throw new SystemException("vector term=" + term + " field=" + field + " doc=" + j + " has payload=" + payload + " but differs from postings payload=" + postingsPayload);
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
                float vectorAvg = status.docCount == 0 ? 0 : status.totVectors / (float)status.docCount;
                Msg(infoStream, "OK [" + status.totVectors + " total vector count; avg " +
                    vectorAvg.ToString(CultureInfo.InvariantCulture.NumberFormat) + " term/freq vector fields per doc]");
            }
            catch (Exception e)
            {
                Msg(infoStream, "ERROR [" + e.Message + "]");
                status.error = e;
                if (infoStream != null)
                {
                    infoStream.WriteLine(e.StackTrace);
                }
            }

            return status;
        }

        /// <summary>Repairs the index using previously returned result
        /// from <see cref="CheckIndex" />.  Note that this does not
        /// remove any of the unreferenced files after it's done;
        /// you must separately open an <see cref="IndexWriter" />, which
        /// deletes unreferenced files when it's created.
        /// 
        /// <p/><b>WARNING</b>: this writes a
        /// new segments file into the index, effectively removing
        /// all documents in broken segments from the index.
        /// BE CAREFUL.
        /// 
        /// <p/><b>WARNING</b>: Make sure you only call this when the
        /// index is not opened  by any writer. 
        /// </summary>
        public virtual void FixIndex(Status result, Codec codec)
        {
            if (result.partial)
                throw new System.ArgumentException("can only fix an index that was fully checked (this status checked a subset of segments)");
            result.newSegments.Changed();
            result.newSegments.Commit(result.dir);
        }

        private static bool assertsOn;

        private static bool TestAsserts()
        {
            assertsOn = true;
            return true;
        }

        private static bool AssertsOn()
        {
            System.Diagnostics.Debug.Assert(TestAsserts());
            return assertsOn;
        }

        /// <summary>Command-line interface to check and fix an index.
        /// <p/>
        /// Run it like this:
        /// <code>
        /// java -ea:Lucene.Net... Lucene.Net.Index.CheckIndex pathToIndex [-fix] [-segment X] [-segment Y]
        /// </code>
        /// <list type="bullet">
        /// <item><c>-fix</c>: actually write a new segments_N file, removing any problematic segments</item>
        /// <item><c>-segment X</c>: only check the specified
        /// segment(s).  This can be specified multiple times,
        /// to check more than one segment, eg <c>-segment _2
        /// -segment _a</c>.  You can't use this with the -fix
        /// option.</item>
        /// </list>
        /// <p/><b>WARNING</b>: <c>-fix</c> should only be used on an emergency basis as it will cause
        /// documents (perhaps many) to be permanently removed from the index.  Always make
        /// a backup copy of your index before running this!  Do not run this tool on an index
        /// that is actively being written to.  You have been warned!
        /// <p/>                Run without -fix, this tool will open the index, report version information
        /// and report any exceptions it hits and what action it would take if -fix were
        /// specified.  With -fix, this tool will remove any segments that have issues and
        /// write a new segments_N file.  This means all documents contained in the affected
        /// segments will be removed.
        /// <p/>
        /// This tool exits with exit code 1 if the index cannot be opened or has any
        /// corruption, else 0.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            bool doFix = false;
            bool doCrossCheckTermVectors = false;
            Codec codec = Codec.Default; // only used when fixing
            bool verbose = false;
            List<String> onlySegments = new List<String>();
            String indexPath = null;
            String dirImpl = null;
            int i = 0;
            while (i < args.Length)
            {
                String arg = args[i];
                if ("-fix".Equals(arg))
                {
                    doFix = true;
                }
                else if ("-crossCheckTermVectors".Equals(arg))
                {
                    doCrossCheckTermVectors = true;
                }
                else if ("-codec".Equals(arg))
                {
                    if (i == args.Length - 1)
                    {
                        Console.Out.WriteLine("ERROR: missing name for -codec option");
                        Environment.Exit(1);
                    }
                    i++;
                    codec = Codec.ForName(args[i]);
                }
                else if (arg.Equals("-verbose"))
                {
                    verbose = true;
                }
                else if (arg.Equals("-segment"))
                {
                    if (i == args.Length - 1)
                    {
                        Console.Out.WriteLine("ERROR: missing name for -segment option");
                        Environment.Exit(1);
                    }
                    i++;
                    onlySegments.Add(args[i]);
                }
                else if ("-dir-impl".Equals(arg))
                {
                    if (i == args.Length - 1)
                    {
                        Console.Out.WriteLine("ERROR: missing value for -dir-impl option");
                        Environment.Exit(1);
                    }
                    i++;
                    dirImpl = args[i];
                }
                else
                {
                    if (indexPath != null)
                    {
                        Console.Out.WriteLine("ERROR: unexpected extra argument '" + args[i] + "'");
                        Environment.Exit(1);
                    }
                    indexPath = args[i];
                }
                i++;
            }

            if (indexPath == null)
            {
                Console.Out.WriteLine("\nERROR: index path not specified");
                Console.Out.WriteLine("\nUsage: java org.apache.lucene.index.CheckIndex pathToIndex [-fix] [-crossCheckTermVectors] [-segment X] [-segment Y] [-dir-impl X]\n" +
                                   "\n" +
                                   "  -fix: actually write a new segments_N file, removing any problematic segments\n" +
                                   "  -crossCheckTermVectors: verifies that term vectors match postings; THIS IS VERY SLOW!\n" +
                                   "  -codec X: when fixing, codec to write the new segments_N file with\n" +
                                   "  -verbose: print additional details\n" +
                                   "  -segment X: only check the specified segments.  This can be specified multiple\n" +
                                   "              times, to check more than one segment, eg '-segment _2 -segment _a'.\n" +
                                   "              You can't use this with the -fix option\n" +
                                   "  -dir-impl X: use a specific " + typeof(FSDirectory).Name + " implementation. " +
                                   "If no package is specified the " + typeof(FSDirectory).Namespace + " package will be used.\n" +
                                   "\n" +
                                   "**WARNING**: -fix should only be used on an emergency basis as it will cause\n" +
                                   "documents (perhaps many) to be permanently removed from the index.  Always make\n" +
                                   "a backup copy of your index before running this!  Do not run this tool on an index\n" +
                                   "that is actively being written to.  You have been warned!\n" +
                                   "\n" +
                                   "Run without -fix, this tool will open the index, report version information\n" +
                                   "and report any exceptions it hits and what action it would take if -fix were\n" +
                                   "specified.  With -fix, this tool will remove any segments that have issues and\n" +
                                   "write a new segments_N file.  This means all documents contained in the affected\n" +
                                   "segments will be removed.\n" +
                                   "\n" +
                                   "This tool exits with exit code 1 if the index cannot be opened or has any\n" +
                                   "corruption, else 0.\n");
                Environment.Exit(1);
            }

            if (!AssertsOn())
                Console.Out.WriteLine("\nNOTE: testing will be more thorough if you run java with '-ea:org.apache.lucene...', so assertions are enabled");

            if (onlySegments.Count == 0)
                onlySegments = null;
            else if (doFix)
            {
                Console.Out.WriteLine("ERROR: cannot specify both -fix and -segment");
                Environment.Exit(1);
            }

            Console.Out.WriteLine("\nOpening index @ " + indexPath + "\n");
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
                Console.Out.WriteLine("ERROR: could not open directory \"" + indexPath + "\"; exiting");
                Console.Out.WriteLine(t.StackTrace);
                Environment.Exit(1);
            }

            CheckIndex checker = new CheckIndex(dir);
            checker.CrossCheckTermVectors = doCrossCheckTermVectors;
            checker.SetInfoStream(new StreamWriter(Console.OpenStandardOutput()), verbose);

            Status result = checker.CheckIndex_Renamed_Method(onlySegments);
            if (result.missingSegments)
            {
                Environment.Exit(1);
            }

            if (!result.clean)
            {
                if (!doFix)
                {
                    Console.Out.WriteLine("WARNING: would write new segments file, and " + result.totLoseDocCount + " documents would be lost, if -fix were specified\n");
                }
                else
                {
                    Console.Out.WriteLine("WARNING: " + result.totLoseDocCount + " documents will be lost\n");
                    Console.Out.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + result.totLoseDocCount + " docs from the index. THIS IS YOUR LAST CHANCE TO CTRL+C!");
                    for (int s = 0; s < 5; s++)
                    {
                        Thread.Sleep(1000);
                        Console.Out.WriteLine("  " + (5 - s) + "...");
                    }
                    Console.Out.WriteLine("Writing...");
                    checker.FixIndex(result, codec);
                    Console.Out.WriteLine("OK");
                    Console.Out.WriteLine("Wrote new segments file \"" + result.newSegments.SegmentsFileName + "\"");
                }
            }
            Console.Out.WriteLine("");

            int exitCode;
            if (result.clean == true)
                exitCode = 0;
            else
                exitCode = 1;
            Environment.Exit(exitCode);
        }
    }
}