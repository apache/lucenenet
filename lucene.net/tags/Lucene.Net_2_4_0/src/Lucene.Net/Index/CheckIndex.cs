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

using System;
using System.Collections.Generic;

using Document = Lucene.Net.Documents.Document;
using Directory = Lucene.Net.Store.Directory;
using FSDirectory = Lucene.Net.Store.FSDirectory;
using IndexInput = Lucene.Net.Store.IndexInput;

namespace Lucene.Net.Index
{
    /// <summary>
    /// Basic tool and API to check the health of an index and
    /// write a new segments file that removes reference to
    /// problematic segments.
    /// 
    /// <p>As this tool checks every byte in the index, on a large
    /// index it can take quite a long time to run.
    ///
    /// <p><b>WARNING</b>: this tool and API is new and
    /// experimental and is subject to suddenly change in the
    /// next release.  Please make a complete backup of your
    /// index before using this to fix your index!	
    /// </summary>
    public class CheckIndex
    {
        /// <summary>
        /// Default print stream for all CheckIndex instances.
        /// </summary>
        [Obsolete("use SetInfoStream per instance instead")]
        public static System.IO.TextWriter out_Renamed = null;

        private System.IO.TextWriter infoStream;
        private Directory dir;

        /**
         * Returned from {@link #CheckIndex()} detailing the health and status of the index.
         *
         * <p><b>WARNING</b>: this API is new and experimental and is
         * subject to suddenly change in the next release.
         **/

        public class Status
        {

            /** True if no problems were found with the index. */
            public bool clean;

            /** True if we were unable to locate and load the segments_N file. */
            public bool missingSegments;

            /** True if we were unable to open the segments_N file. */
            public bool cantOpenSegments;

            /** True if we were unable to read the version number from segments_N file. */
            public bool missingSegmentVersion;

            /** Name of latest segments_N file in the index. */
            public string segmentsFileName;

            /** Number of segments in the index. */
            public int numSegments;

            /** string description of the version of the index. */
            public string segmentFormat;

            /** Empty unless you passed specific segments list to check as optional 3rd argument.
             *  @see CheckIndex#CheckIndex(List) */
            //public IList<string> segmentsChecked = new List<string>();
            public IList<object> segmentsChecked = new List<object>();

            /** True if the index was created with a newer version of Lucene than the CheckIndex tool. */
            public bool toolOutOfDate;

            /** List of {@link SegmentInfoStatus} instances, detailing status of each segment. */
            public IList<SegmentInfoStatus> segmentInfos = new List<SegmentInfoStatus>();

            /** Directory index is in. */
            public Directory dir;

            /** SegmentInfos instance containing only segments that
             *  had no problems (this is used with the {@link
             *  CheckIndex#fix} method to repair the index. */
            internal SegmentInfos newSegments;

            /** How many documents will be lost to bad segments. */
            public int totLoseDocCount;

            /** How many bad segments were found. */
            public int numBadSegments;

            /** True if we checked only specific segments ({@link
             * #CheckIndex(List)}) was called with non-null
             * argument). */
            public bool partial;

            /** Holds the status of each segment in the index.
             *  See {@link #segmentInfos}.
             *
             * <p><b>WARNING</b>: this API is new and experimental and is
             * subject to suddenly change in the next release.
             */
            public class SegmentInfoStatus
            {
                /** Name of the segment. */
                public string name;

                /** Document count (does not take deletions into account). */
                public int docCount;

                /** True if segment is compound file format. */
                public bool compound;

                /** Number of files referenced by this segment. */
                public int numFiles;

                /** Net size (MB) of the files referenced by this
                 *  segment. */
                public double sizeMB;

                /** Doc store offset, if this segment shares the doc
                 *  store files (stored fields and term vectors) with
                 *  other segments.  This is -1 if it does not share. */
                public int docStoreOffset = -1;

                /** string of the shared doc store segment, or null if
                 *  this segment does not share the doc store files. */
                public string docStoreSegment;

                /** True if the shared doc store files are compound file
                 *  format. */
                public bool docStoreCompoundFile;

                /** True if this segment has pending deletions. */
                public bool hasDeletions;

                /** Name of the current deletions file name. */
                public string deletionsFileName;

                /** Number of deleted documents. */
                public int numDeleted;

                /** True if we were able to open a SegmentReader on this
                 *  segment. */
                public bool openReaderPassed;

                /** Number of fields in this segment. */
                public int numFields;

                /** True if at least one of the fields in this segment
                 *  does not omitTf.
                 *  @see Fieldable#setOmitTf */
                public bool hasProx;
            }
        }

        /** Create a new CheckIndex on the directory. */
        public CheckIndex(Directory dir)
        {
            this.dir = dir;
            infoStream = out_Renamed;
        }

        /** Set infoStream where messages should go.  If null, no
         *  messages are printed */
        public void SetInfoStream(System.IO.TextWriter out_Renamed)
        {
            infoStream = out_Renamed;
        }

        private void Msg(string msg)
        {
            if (infoStream != null)
                infoStream.WriteLine(msg);
        }


        private class MySegmentTermDocs : SegmentTermDocs
        {

            internal int delCount;

            internal MySegmentTermDocs(SegmentReader p)
                : base(p)
            {
            }

            public override void Seek(Term term)
            {
                base.Seek(term);
                delCount = 0;
            }

            protected internal override void SkippingDoc()
            {
                delCount++;
            }
        }


        /** Returns true if index is clean, else false. 
   *  @deprecated Please instantiate a CheckIndex and then use {@link #CheckIndex()} instead */
        public static bool Check(Directory dir, bool doFix)
        {
            return Check(dir, doFix, null);
        }

        /** Returns true if index is clean, else false.
         *  @deprecated Please instantiate a CheckIndex and then use {@link #CheckIndex(List)} instead */
        public static bool Check(Directory dir, bool doFix, IList<object> onlySegments)
        {
            CheckIndex checker = new CheckIndex(dir);
            Status status = checker.CheckIndex_Renamed(onlySegments);
            if (doFix && !status.clean)
                checker.FixIndex(status);

            return status.clean;
        }

        /** Returns a {@link Status} instance detailing
         *  the state of the index.
         *
         *  <p>As this method checks every byte in the index, on a large
         *  index it can take quite a long time to run.
         *
         *  <p><b>WARNING</b>: make sure
         *  you only call this when the index is not opened by any
         *  writer. */
        public Status CheckIndex_Renamed()
        {
            return CheckIndex_Renamed(null);
        }

        /** Returns a {@link Status} instance detailing
         *  the state of the index.
         * 
         *  @param onlySegments list of specific segment names to check
         *
         *  <p>As this method checks every byte in the specified
         *  segments, on a large index it can take quite a long
         *  time to run.
         *
         *  <p><b>WARNING</b>: make sure
         *  you only call this when the index is not opened by any
         *  writer. */
        public Status CheckIndex_Renamed(IList<object> onlySegments)
        {
            System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
            SegmentInfos sis = new SegmentInfos();
            Status result = new Status();
            result.dir = dir;
            try
            {
                sis.Read(dir);
            }
            catch (System.Exception t)
            {
                Msg("ERROR: could not read any segments file in directory");
                result.missingSegments = true;
                if (infoStream != null)
                    infoStream.WriteLine(t.StackTrace);
                return result;
            }

            int numSegments = sis.Count;
            string segmentsFileName = sis.GetCurrentSegmentFileName();
            IndexInput input = null;
            try
            {
                input = dir.OpenInput(segmentsFileName);
            }
            catch (System.Exception t)
            {
                Msg("ERROR: could not open segments file in directory");
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
            catch (System.Exception t)
            {
                Msg("ERROR: could not read segment file version in directory");
                if (infoStream != null)
                    infoStream.WriteLine(t.StackTrace);
                result.missingSegmentVersion = true;
                return result;
            }
            finally
            {
                if (input != null)
                    input.Close();
            }

            string sFormat = "";
            bool skip = false;

            if (format == SegmentInfos.FORMAT)
                sFormat = "FORMAT [Lucene Pre-2.1]";
            if (format == SegmentInfos.FORMAT_LOCKLESS)
                sFormat = "FORMAT_LOCKLESS [Lucene 2.1]";
            else if (format == SegmentInfos.FORMAT_SINGLE_NORM_FILE)
                sFormat = "FORMAT_SINGLE_NORM_FILE [Lucene 2.2]";
            else if (format == SegmentInfos.FORMAT_SHARED_DOC_STORE)
                sFormat = "FORMAT_SHARED_DOC_STORE [Lucene 2.3]";
            else
            {
                if (format == SegmentInfos.FORMAT_CHECKSUM)
                    sFormat = "FORMAT_CHECKSUM [Lucene 2.4]";
                else if (format == SegmentInfos.FORMAT_DEL_COUNT)
                    sFormat = "FORMAT_DEL_COUNT [Lucene 2.4]";
                else if (format == SegmentInfos.FORMAT_HAS_PROX)
                    sFormat = "FORMAT_HAS_PROX [Lucene 2.4]";
                else if (format < SegmentInfos.CURRENT_FORMAT)
                {
                    sFormat = "int=" + format + " [newer version of Lucene than this tool]";
                    skip = true;
                }
                else
                {
                    sFormat = format + " [Lucene 1.3 or prior]";
                }
            }

            Msg("Segments file=" + segmentsFileName + " numSegments=" + numSegments + " version=" + sFormat);
            result.segmentsFileName = segmentsFileName;
            result.numSegments = numSegments;
            result.segmentFormat = sFormat;

            if (onlySegments != null)
            {
                result.partial = true;
                if (infoStream != null)
                    infoStream.Write("\nChecking only these segments:");
                IEnumerator<object> it = onlySegments.GetEnumerator();
                while (it.MoveNext())
                {
                    if (infoStream != null)
                        infoStream.Write(" " + it.Current);
                }
                SupportClass.CollectionsSupport.AddAll(onlySegments, (System.Collections.Generic.IList<object>)(result.segmentsChecked));
                Msg(":");
            }

            if (skip)
            {
                Msg("\nERROR: this index appears to be created by a newer version of Lucene than this tool was compiled on; please re-compile this tool on the matching version of Lucene; exiting");
                result.toolOutOfDate = true;
                return result;
            }


            result.newSegments = (SegmentInfos)sis.Clone();
            result.newSegments.Clear();

            for (int i = 0; i < numSegments; i++)
            {
                SegmentInfo info = sis.Info(i);
                if (onlySegments != null && !onlySegments.Contains(info.name))
                    continue;
                Status.SegmentInfoStatus segInfoStat = new Status.SegmentInfoStatus();
                result.segmentInfos.Add(segInfoStat);
                Msg("  " + (1 + i) + " of " + numSegments + ": name=" + info.name + " docCount=" + info.docCount);
                segInfoStat.name = info.name;
                segInfoStat.docCount = info.docCount;

                int toLoseDocCount = info.docCount;

                SegmentReader reader = null;

                try
                {
                    Msg("    compound=" + info.GetUseCompoundFile());
                    segInfoStat.compound = info.GetUseCompoundFile();
                    Msg("    hasProx=" + info.GetHasProx());
                    segInfoStat.hasProx = info.GetHasProx();
                    Msg("    numFiles=" + info.Files().Count);
                    segInfoStat.numFiles = info.Files().Count;
                    //msg("    size (MB)=" + nf.Format(info.SizeInBytes()/(1024.*1024.)));
                    Msg(string.Format(nf, "    size (MB)={0:f}", new object[] { (info.SizeInBytes() / (1024.0 * 1024.0)) }));
                    segInfoStat.sizeMB = info.SizeInBytes() / (1024.0 * 1024.0);


                    int docStoreOffset = info.GetDocStoreOffset();
                    if (docStoreOffset != -1)
                    {
                        Msg("    docStoreOffset=" + docStoreOffset);
                        segInfoStat.docStoreOffset = docStoreOffset;
                        Msg("    docStoreSegment=" + info.GetDocStoreSegment());
                        segInfoStat.docStoreSegment = info.GetDocStoreSegment();
                        Msg("    docStoreIsCompoundFile=" + info.GetDocStoreIsCompoundFile());
                        segInfoStat.docStoreCompoundFile = info.GetDocStoreIsCompoundFile();
                    }
                    string delFileName = info.GetDelFileName();
                    if (delFileName == null)
                    {
                        Msg("    no deletions");
                        segInfoStat.hasDeletions = false;
                    }
                    else
                    {
                        Msg("    has deletions [delFileName=" + delFileName + "]");
                        segInfoStat.hasDeletions = true;
                        segInfoStat.deletionsFileName = delFileName;
                    }
                    if (infoStream != null)
                        infoStream.Write("    test: open reader.........");
                    reader = SegmentReader.Get(info);
                    int numDocs = reader.NumDocs();
                    toLoseDocCount = numDocs;
                    if (reader.HasDeletions())
                    {
                        if (info.docCount - numDocs != info.GetDelCount())
                        {
                            throw new System.SystemException("delete count mismatch: info=" + info.GetDelCount() + " vs reader=" + (info.docCount - numDocs));
                        }
                        segInfoStat.numDeleted = info.docCount - numDocs;
                        Msg("OK [" + (segInfoStat.numDeleted) + " deleted docs]");
                    }
                    else
                    {
                        if (info.GetDelCount() != 0)
                        {
                            throw new System.SystemException("delete count mismatch: info=" + info.GetDelCount() + " vs reader=" + (info.docCount - numDocs));
                        }
                        Msg("OK");
                    }

                    if (infoStream != null)
                        infoStream.Write("    test: fields, norms.......");
                    ICollection<string> fieldNames = reader.GetFieldNames(IndexReader.FieldOption.ALL);
                    IEnumerator<string> it = fieldNames.GetEnumerator();
                    while (it.MoveNext())
                    {
                        string fieldName = it.Current;
                        byte[] b = reader.Norms(fieldName);
                        if (b.Length != info.docCount)
                            throw new System.SystemException("norms for field \"" + fieldName + "\" is length " + b.Length + " != maxDoc " + info.docCount);

                    }
                    Msg("OK [" + fieldNames.Count + " fields]");
                    segInfoStat.numFields = fieldNames.Count;
                    if (infoStream != null)
                        infoStream.Write("    test: terms, freq, prox...");
                    TermEnum termEnum = reader.Terms();
                    TermPositions termPositions = reader.TermPositions();

                    // Used only to count up # deleted docs for this
                    // term
                    MySegmentTermDocs myTermDocs = new MySegmentTermDocs(reader);

                    long termCount = 0;
                    long totFreq = 0;
                    long totPos = 0;
                    while (termEnum.Next())
                    {
                        termCount++;
                        Term term = termEnum.Term();
                        int docFreq = termEnum.DocFreq();
                        termPositions.Seek(term);
                        int lastDoc = -1;
                        int freq0 = 0;
                        totFreq += docFreq;
                        while (termPositions.Next())
                        {
                            freq0++;
                            int doc = termPositions.Doc();
                            int freq = termPositions.Freq();
                            if (doc <= lastDoc)
                                throw new System.SystemException("term " + term + ": doc " + doc + " <= lastDoc " + lastDoc);
                            lastDoc = doc;
                            if (freq <= 0)
                                throw new System.SystemException("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");

                            int lastPos = -1;
                            totPos += freq;
                            for (int j = 0; j < freq; j++)
                            {
                                int pos = termPositions.NextPosition();
                                if (pos < -1)
                                    throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
                                if (pos < lastPos)
                                    throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
                            }
                        }

                        // Now count how many deleted docs occurred in
                        // this term:
                        int delCount;
                        if (reader.HasDeletions())
                        {
                            myTermDocs.Seek(term);
                            while (myTermDocs.Next())
                            {
                            }
                            delCount = myTermDocs.delCount;
                        }
                        else
                            delCount = 0;

                        if (freq0 + delCount != docFreq)
                            throw new System.SystemException("term " + term + " docFreq=" + docFreq + " != num docs seen " + freq0 + " + num docs deleted " + delCount);
                    }

                    Msg("OK [" + termCount + " terms; " + totFreq + " terms/docs pairs; " + totPos + " tokens]");

                    if (infoStream != null)
                        infoStream.Write("    test: stored fields.......");
                    int docCount = 0;
                    long totFields = 0;
                    for (int j = 0; j < info.docCount; j++)
                        if (!reader.IsDeleted(j))
                        {
                            docCount++;
                            Document doc = reader.Document(j);
                            totFields += doc.GetFields().Count;
                        }

                    if (docCount != reader.NumDocs())
                        throw new System.SystemException("docCount=" + docCount + " but saw " + docCount + " undeleted docs");

                    //msg("OK [" + totFields + " total field count; avg " + nf.Format((((float) totFields)/docCount)) + " fields per doc]");
                    Msg(string.Format(nf, "OK [{0:d} total field count; avg {1:f} fields per doc]", new object[] { totFields, (((float)totFields) / docCount) }));

                    if (infoStream != null)
                        infoStream.Write("    test: term vectors........");
                    int totVectors = 0;
                    for (int j = 0; j < info.docCount; j++)
                        if (!reader.IsDeleted(j))
                        {
                            TermFreqVector[] tfv = reader.GetTermFreqVectors(j);
                            if (tfv != null)
                                totVectors += tfv.Length;
                        }

                    //msg("OK [" + totVectors + " total vector count; avg " + nf.Format((((float) totVectors)/docCount)) + " term/freq vector fields per doc]");
                    Msg(string.Format(nf, "OK [{0:d} total vector count; avg {1:f} term/freq vector fields per doc]", new object[] { totVectors, (((float)totVectors) / docCount) }));
                    Msg("");

                }
                catch (System.Exception t)
                {
                    Msg("FAILED");
                    string comment;
                    comment = "FixIndex() would remove reference to this segment";
                    Msg("    WARNING: " + comment + "; full exception:");
                    if (infoStream != null)
                        infoStream.WriteLine(t.StackTrace);
                    Msg("");
                    result.totLoseDocCount += toLoseDocCount;
                    result.numBadSegments++;
                    continue;
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }

                // Keeper
                result.newSegments.Add(info.Clone());
            }

            if (0 == result.numBadSegments)
            {
                result.clean = true;
                Msg("No problems were detected with this index.\n");
            }
            else
                Msg("WARNING: " + result.numBadSegments + " broken segments (containing " + result.totLoseDocCount + " documents) detected");

            return result;
        }

        /** Repairs the index using previously returned result
         *  from {@link #checkIndex}.  Note that this does not
         *  remove any of the unreferenced files after it's done;
         *  you must separately open an {@link IndexWriter}, which
         *  deletes unreferenced files when it's created.
         *
         * <p><b>WARNING</b>: this writes a
         *  new segments file into the index, effectively removing
         *  all documents in broken segments from the index.
         *  BE CAREFUL.
         *
         * <p><b>WARNING</b>: Make sure you only call this when the
         *  index is not opened  by any writer. */
        public void FixIndex(Status result)
        {
            if (result.partial)
                throw new ArgumentException("can only fix an index that was fully checked (this status checked a subset of segments)");
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

        /** Command-line interface to check and fix an index.

          <p>
          Run it like this:
          <pre>
          java -ea:org.apache.lucene... org.apache.lucene.index.CheckIndex pathToIndex [-fix] [-segment X] [-segment Y]
          </pre>
          <ul>
          <li><code>-fix</code>: actually write a new segments_N file, removing any problematic segments

          <li><code>-segment X</code>: only check the specified
          segment(s).  This can be specified multiple times,
          to check more than one segment, eg <code>-segment _2
          -segment _a</code>.  You can't use this with the -fix
          option.
          </ul>

          <p><b>WARNING</b>: <code>-fix</code> should only be used on an emergency basis as it will cause
                             documents (perhaps many) to be permanently removed from the index.  Always make
                             a backup copy of your index before running this!  Do not run this tool on an index
                             that is actively being written to.  You have been warned!

          <p>                Run without -fix, this tool will open the index, report version information
                             and report any exceptions it hits and what action it would take if -fix were
                             specified.  With -fix, this tool will remove any segments that have issues and
                             write a new segments_N file.  This means all documents contained in the affected
                             segments will be removed.

          <p>
                             This tool exits with exit code 1 if the index cannot be opened or has any
                             corruption, else 0.
         */
        [STAThread]
        public static void Main(string[] args)
        {

            bool doFix = false;
            IList<object> onlySegments = new List<object>();
            string indexPath = null;
            int i = 0;
            while (i < args.Length)
            {
                if (args[i].Equals("-fix"))
                {
                    doFix = true;
                    i++;
                }
                else if (args[i].Equals("-segment"))
                {
                    if (i == args.Length - 1)
                    {
                        System.Console.WriteLine("ERROR: missing name for -segment option");
                        System.Environment.Exit(1);
                    }
                    onlySegments.Add(args[i + 1]);
                    i += 2;
                }
                else
                {
                    if (indexPath != null)
                    {
                        System.Console.WriteLine("ERROR: unexpected extra argument '" + args[i] + "'");
                        System.Environment.Exit(1);
                    }
                    indexPath = args[i];
                    i++;
                }
            }

            if (indexPath == null)
            {
                System.Console.WriteLine("\nERROR: index path not specified");
                System.Console.WriteLine("\nUsage: java org.apache.lucene.index.CheckIndex pathToIndex [-fix] [-segment X] [-segment Y]\n" +
                                   "\n" +
                                   "  -fix: actually write a new segments_N file, removing any problematic segments\n" +
                                   "  -segment X: only check the specified segments.  This can be specified multiple\n" +
                                   "              times, to check more than one segment, eg '-segment _2 -segment _a'.\n" +
                                   "              You can't use this with the -fix option\n" +
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
                System.Environment.Exit(1);
            }

            if (!AssertsOn())
                System.Console.WriteLine("\nNOTE: testing will be more thorough if you run java with '-ea:org.apache.lucene...', so assertions are enabled");

            if (onlySegments.Count == 0)
                onlySegments = null;
            else if (doFix)
            {
                System.Console.WriteLine("ERROR: cannot specify both -fix and -segment");
                System.Environment.Exit(1);
            }

            System.Console.WriteLine("\nOpening index @ " + indexPath + "\n");
            Directory dir = null;
            try
            {
                dir = FSDirectory.GetDirectory(indexPath);
            }
            catch (System.Exception t)
            {
                System.Console.WriteLine("ERROR: could not open directory \"" + indexPath + "\"; exiting");
                System.Console.WriteLine(t.StackTrace);
                System.Environment.Exit(1);
            }

            CheckIndex checker = new CheckIndex(dir);
            checker.SetInfoStream(new System.IO.StreamWriter(System.Console.OpenStandardOutput(), System.Console.Out.Encoding));

            Status result = checker.CheckIndex_Renamed(onlySegments);

            if (!result.clean)
            {
                if (!doFix)
                {
                    System.Console.WriteLine("WARNING: would write new segments file, and " + result.totLoseDocCount + " documents would be lost, if -fix were specified\n");
                }
                else
                {
                    System.Console.WriteLine("WARNING: " + result.totLoseDocCount + " documents will be lost\n");
                    System.Console.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + result.totLoseDocCount + " docs from the index. THIS IS YOUR LAST CHANCE TO CTRL+C!");
                    for (int s = 0; s < 5; s++)
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64)1000 * 1000));
                        }
                        catch (System.Threading.ThreadInterruptedException)
                        {
                            SupportClass.ThreadClass.Current().Interrupt();
                            s--;
                            continue;
                        }

                        System.Console.WriteLine("  " + (5 - s) + "...");
                    }
                    System.Console.WriteLine("Writing...");
                    checker.FixIndex(result);
                    System.Console.WriteLine("OK");
                    System.Console.WriteLine("Wrote new segments file \"" + result.newSegments.GetCurrentSegmentFileName() + "\"");
                }
            }
            System.Console.WriteLine("");

            int exitCode;
            if (result != null && result.clean == true)
                exitCode = 0;
            else
                exitCode = 1;
            System.Environment.Exit(exitCode);
        }
    }
}
//    /// <summary>Returns true if index is clean, else false.</summary>
//    public static bool Check(Directory dir, bool doFix)
//    {
//        System.Globalization.NumberFormatInfo nf = System.Globalization.CultureInfo.CurrentCulture.NumberFormat;
//        SegmentInfos sis = new SegmentInfos();

//        try
//        {
//            sis.Read(dir);
//        }
//        catch (System.Exception t)
//        {
//            out_Renamed.WriteLine("ERROR: could not read any segments file in directory");
//            out_Renamed.Write(t.StackTrace);
//            out_Renamed.Flush();
//            return false;
//        }

//        int numSegments = sis.Count;
//        System.string segmentsFileName = sis.GetCurrentSegmentFileName();
//        IndexInput input = null;
//        try
//        {
//            input = dir.OpenInput(segmentsFileName);
//        }
//        catch (System.Exception t)
//        {
//            out_Renamed.WriteLine("ERROR: could not open segments file in directory");
//            out_Renamed.Write(t.StackTrace);
//            out_Renamed.Flush();
//            return false;
//        }
//        int format = 0;
//        try
//        {
//            format = input.ReadInt();
//        }
//        catch (System.Exception t)
//        {
//            out_Renamed.WriteLine("ERROR: could not read segment file version in directory");
//            out_Renamed.Write(t.StackTrace);
//            out_Renamed.Flush();
//            return false;
//        }
//        finally
//        {
//            if (input != null)
//                input.Close();
//        }

//        System.string sFormat = "";
//        bool skip = false;

//        if (format == SegmentInfos.FORMAT)
//            sFormat = "FORMAT [Lucene Pre-2.1]";
//        if (format == SegmentInfos.FORMAT_LOCKLESS)
//            sFormat = "FORMAT_LOCKLESS [Lucene 2.1]";
//        else if (format == SegmentInfos.FORMAT_SINGLE_NORM_FILE)
//            sFormat = "FORMAT_SINGLE_NORM_FILE [Lucene 2.2]";
//        else if (format == SegmentInfos.FORMAT_SHARED_DOC_STORE)
//            sFormat = "FORMAT_SHARED_DOC_STORE [Lucene 2.3]";
//        else if (format < SegmentInfos.FORMAT_SHARED_DOC_STORE)
//        {
//            sFormat = "int=" + format + " [newer version of Lucene than this tool]";
//            skip = true;
//        }
//        else
//        {
//            sFormat = format + " [Lucene 1.3 or prior]";
//        }

//        out_Renamed.WriteLine("Segments file=" + segmentsFileName + " numSegments=" + numSegments + " version=" + sFormat);

//        if (skip)
//        {
//            out_Renamed.WriteLine("\nERROR: this index appears to be created by a newer version of Lucene than this tool was compiled on; please re-compile this tool on the matching version of Lucene; exiting");
//            return false;
//        }

//        SegmentInfos newSIS = (SegmentInfos) sis.Clone();
//        newSIS.Clear();
//        bool changed = false;
//        int totLoseDocCount = 0;
//        int numBadSegments = 0;
//        for (int i = 0; i < numSegments; i++)
//        {
//            SegmentInfo info = sis.Info(i);
//            out_Renamed.WriteLine("  " + (1 + i) + " of " + numSegments + ": name=" + info.name + " docCount=" + info.docCount);
//            int toLoseDocCount = info.docCount;

//            SegmentReader reader = null;

//            try
//            {
//                out_Renamed.WriteLine("    compound=" + info.GetUseCompoundFile());
//                out_Renamed.WriteLine("    numFiles=" + info.Files().Count);
//                out_Renamed.WriteLine(string.Format(nf, "    size (MB)={0:f}", new object[] { (info.SizeInBytes() / (1024.0 * 1024.0)) }));
//                int docStoreOffset = info.GetDocStoreOffset();
//                if (docStoreOffset != - 1)
//                {
//                    out_Renamed.WriteLine("    docStoreOffset=" + docStoreOffset);
//                    out_Renamed.WriteLine("    docStoreSegment=" + info.GetDocStoreSegment());
//                    out_Renamed.WriteLine("    docStoreIsCompoundFile=" + info.GetDocStoreIsCompoundFile());
//                }
//                System.string delFileName = info.GetDelFileName();
//                if (delFileName == null)
//                    out_Renamed.WriteLine("    no deletions");
//                else
//                    out_Renamed.WriteLine("    has deletions [delFileName=" + delFileName + "]");
//                out_Renamed.Write("    test: open reader.........");
//                reader = SegmentReader.Get(info);
//                int numDocs = reader.NumDocs();
//                toLoseDocCount = numDocs;
//                if (reader.HasDeletions())
//                    out_Renamed.WriteLine("OK [" + (info.docCount - numDocs) + " deleted docs]");
//                else
//                    out_Renamed.WriteLine("OK");

//                out_Renamed.Write("    test: fields, norms.......");
//                System.Collections.ICollection fieldNames = reader.GetFieldNames(IndexReader.FieldOption.ALL);
//                System.Collections.IEnumerator it = fieldNames.GetEnumerator();
//                while (it.MoveNext())
//                {
//                    System.string fieldName = (System.string) it.Current;
//                    byte[] b = reader.Norms(fieldName);
//                    if (b.Length != info.docCount)
//                        throw new System.SystemException("norms for field \"" + fieldName + "\" is length " + b.Length + " != maxDoc " + info.docCount);
//                }
//                out_Renamed.WriteLine("OK [" + fieldNames.Count + " fields]");

//                out_Renamed.Write("    test: terms, freq, prox...");
//                TermEnum termEnum = reader.Terms();
//                TermPositions termPositions = reader.TermPositions();

//                // Used only to count up # deleted docs for this
//                // term
//                MySegmentTermDocs myTermDocs = new MySegmentTermDocs(reader);

//                long termCount = 0;
//                long totFreq = 0;
//                long totPos = 0;
//                while (termEnum.Next())
//                {
//                    termCount++;
//                    Term term = termEnum.Term();
//                    int docFreq = termEnum.DocFreq();
//                    termPositions.Seek(term);
//                    int lastDoc = - 1;
//                    int freq0 = 0;
//                    totFreq += docFreq;
//                    while (termPositions.Next())
//                    {
//                        freq0++;
//                        int doc = termPositions.Doc();
//                        int freq = termPositions.Freq();
//                        if (doc <= lastDoc)
//                        {
//                            throw new System.SystemException("term " + term + ": doc " + doc + " < lastDoc " + lastDoc);
//                        }
//                        lastDoc = doc;
//                        if (freq <= 0)
//                        {
//                            throw new System.SystemException("term " + term + ": doc " + doc + ": freq " + freq + " is out of bounds");
//                        }

//                        int lastPos = - 1;
//                        totPos += freq;
//                        for (int j = 0; j < freq; j++)
//                        {
//                            int pos = termPositions.NextPosition();
//                            if (pos < -1)
//                            {
//                                throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " is out of bounds");
//                            }
//                            if (pos < lastPos)
//                            {
//                                throw new System.SystemException("term " + term + ": doc " + doc + ": pos " + pos + " < lastPos " + lastPos);
//                            }
//                        }
//                    }

//                    // Now count how many deleted docs occurred in
//                    // this term:
//                    int delCount;
//                    if (reader.HasDeletions())
//                    {
//                        myTermDocs.Seek(term);
//                        while (myTermDocs.Next())
//                        {
//                        }
//                        delCount = myTermDocs.delCount;
//                    }
//                    else
//                        delCount = 0;

//                    if (freq0 + delCount != docFreq)
//                    {
//                        throw new System.SystemException("term " + term + " docFreq=" + docFreq + " != num docs seen " + freq0 + " + num docs deleted " + delCount);
//                    }
//                }

//                out_Renamed.WriteLine("OK [" + termCount + " terms; " + totFreq + " terms/docs pairs; " + totPos + " tokens]");

//                out_Renamed.Write("    test: stored fields.......");
//                int docCount = 0;
//                long totFields = 0;
//                for (int j = 0; j < info.docCount; j++)
//                    if (!reader.IsDeleted(j))
//                    {
//                        docCount++;
//                        Document doc = reader.Document(j);
//                        totFields += doc.GetFields().Count;
//                    }

//                if (docCount != reader.NumDocs())
//                    throw new System.SystemException("docCount=" + docCount + " but saw " + docCount + " undeleted docs");

//                out_Renamed.WriteLine(string.Format(nf, "OK [{0:d} total field count; avg {1:f} fields per doc]", new object[] { totFields, (((float)totFields) / docCount) }));

//                out_Renamed.Write("    test: term vectors........");
//                int totVectors = 0;
//                for (int j = 0; j < info.docCount; j++)
//                    if (!reader.IsDeleted(j))
//                    {
//                        TermFreqVector[] tfv = reader.GetTermFreqVectors(j);
//                        if (tfv != null)
//                            totVectors += tfv.Length;
//                    }

//                out_Renamed.WriteLine(string.Format(nf, "OK [{0:d} total vector count; avg {1:f} term/freq vector fields per doc]", new object[] { totVectors, (((float)totVectors) / docCount) }));
//                out_Renamed.WriteLine("");
//            }
//            catch (System.Exception t)
//            {
//                out_Renamed.WriteLine("FAILED");
//                System.string comment;
//                if (doFix)
//                    comment = "will remove reference to this segment (-fix is specified)";
//                else
//                    comment = "would remove reference to this segment (-fix was not specified)";
//                out_Renamed.WriteLine("    WARNING: " + comment + "; full exception:");
//                out_Renamed.Write(t.StackTrace);
//                out_Renamed.Flush();
//                out_Renamed.WriteLine("");
//                totLoseDocCount += toLoseDocCount;
//                numBadSegments++;
//                changed = true;
//                continue;
//            }
//            finally
//            {
//                if (reader != null)
//                    reader.Close();
//            }

//            // Keeper
//            newSIS.Add(info.Clone());
//        }

//        if (!changed)
//        {
//            out_Renamed.WriteLine("No problems were detected with this index.\n");
//            return true;
//        }
//        else
//        {
//            out_Renamed.WriteLine("WARNING: " + numBadSegments + " broken segments detected");
//            if (doFix)
//                out_Renamed.WriteLine("WARNING: " + totLoseDocCount + " documents will be lost");
//            else
//                out_Renamed.WriteLine("WARNING: " + totLoseDocCount + " documents would be lost if -fix were specified");
//            out_Renamed.WriteLine();
//        }

//        if (doFix)
//        {
//            out_Renamed.WriteLine("NOTE: will write new segments file in 5 seconds; this will remove " + totLoseDocCount + " docs from the index. THIS IS YOUR LAST CHANCE TO CTRL+C!");
//            for (int i = 0; i < 5; i++)
//            {
//                try
//                {
//                    System.Threading.Thread.Sleep(new System.TimeSpan((System.Int64) 10000 * 1000));
//                }
//                catch (System.Threading.ThreadInterruptedException)
//                {
//                    SupportClass.ThreadClass.Current().Interrupt();
//                    i--;
//                    continue;
//                }

//                out_Renamed.WriteLine("  " + (5 - i) + "...");
//            }
//            out_Renamed.Write("Writing...");
//            try
//            {
//                newSIS.Write(dir);
//            }
//            catch (System.Exception t)
//            {
//                out_Renamed.WriteLine("FAILED; exiting");
//                out_Renamed.Write(t.StackTrace);
//                out_Renamed.Flush();
//                return false;
//            }
//            out_Renamed.WriteLine("OK");
//            out_Renamed.WriteLine("Wrote new segments file \"" + newSIS.GetCurrentSegmentFileName() + "\"");
//        }
//        else
//        {
//            out_Renamed.WriteLine("NOTE: would write new segments file [-fix was not specified]");
//        }
//        out_Renamed.WriteLine("");

//        return false;
//    }

//    static bool assertsOn;

//    private static bool TestAsserts()
//    {
//        assertsOn = true;
//        return true;
//    }

//    [STAThread]
//    public static void  Main(System.string[] args)
//    {

//        bool doFix = false;
//        for (int i = 0; i < args.Length; i++)
//            if (args[i].Equals("-fix"))
//            {
//                doFix = true;
//                break;
//            }

//        if (args.Length != (doFix ? 2 : 1))
//        {
//            out_Renamed.WriteLine("\nUsage: java Lucene.Net.Index.CheckIndex pathToIndex [-fix]\n" + "\n" + "  -fix: actually write a new segments_N file, removing any problematic segments\n" + "\n" + "**WARNING**: -fix should only be used on an emergency basis as it will cause\n" + "documents (perhaps many) to be permanently removed from the index.  Always make\n" + "a backup copy of your index before running this!  Do not run this tool on an index\n" + "that is actively being written to.  You have been warned!\n" + "\n" + "Run without -fix, this tool will open the index, report version information\n" + "and report any exceptions it hits and what action it would take if -fix were\n" + "specified.  With -fix, this tool will remove any segments that have issues and\n" + "write a new segments_N file.  This means all documents contained in the affected\n" + "segments will be removed.\n" + "\n" + "This tool exits with exit code 1 if the index cannot be opened or has has any\n" + "corruption, else 0.\n");
//            System.Environment.Exit(1);
//        }

//        System.Diagnostics.Debug.Assert(TestAsserts());
//        if (!assertsOn)
//            System.Console.WriteLine("\nNote: testing will be more thorough if you run with System.Diagnostic.Debug.Assert() enabled.");

//        System.string dirName = args[0];
//        out_Renamed.WriteLine("\nOpening index @ " + dirName + "\n");
//        Directory dir = null;
//        try
//        {
//            dir = FSDirectory.GetDirectory(dirName);
//        }
//        catch (System.Exception t)
//        {
//            out_Renamed.WriteLine("ERROR: could not open directory \"" + dirName + "\"; exiting");
//            out_Renamed.Write(t.StackTrace);
//            out_Renamed.Flush();
//            System.Environment.Exit(1);
//        }

//        bool isClean = Check(dir, doFix);

//        int exitCode;
//        if (isClean)
//            exitCode = 0;
//        else
//            exitCode = 1;
//        System.Environment.Exit(exitCode);
//    }
//    static CheckIndex()
//    {
//        System.IO.StreamWriter temp_writer;
//        temp_writer = new System.IO.StreamWriter(System.Console.OpenStandardOutput(), System.Console.Out.Encoding);
//        temp_writer.AutoFlush = true;
//        out_Renamed = temp_writer;
//    }
