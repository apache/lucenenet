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

namespace Lucene.Net.Codecs.SimpleText
{

    using System;
    using System.Diagnostics;
    using System.Collections.Generic;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using Directory = Store.Directory;
    using IOContext = Store.IOContext;
    using IndexOutput = Store.IndexOutput;
    using BytesRef = Util.BytesRef;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// Writes plain-text term vectors.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextTermVectorsWriter : TermVectorsWriter
    {

        internal static readonly BytesRef END = new BytesRef("END");
        internal static readonly BytesRef DOC = new BytesRef("doc ");
        internal static readonly BytesRef NUMFIELDS = new BytesRef("  numfields ");
        internal static readonly BytesRef FIELD = new BytesRef("  field ");
        internal static readonly BytesRef FIELDNAME = new BytesRef("    name ");
        internal static readonly BytesRef FIELDPOSITIONS = new BytesRef("    positions ");
        internal static readonly BytesRef FIELDOFFSETS = new BytesRef("    offsets   ");
        internal static readonly BytesRef FIELDPAYLOADS = new BytesRef("    payloads  ");
        internal static readonly BytesRef FIELDTERMCOUNT = new BytesRef("    numterms ");
        internal static readonly BytesRef TERMTEXT = new BytesRef("    term ");
        internal static readonly BytesRef TERMFREQ = new BytesRef("      freq ");
        internal static readonly BytesRef POSITION = new BytesRef("      position ");
        internal static readonly BytesRef PAYLOAD = new BytesRef("        payload ");
        internal static readonly BytesRef STARTOFFSET = new BytesRef("        startoffset ");
        internal static readonly BytesRef ENDOFFSET = new BytesRef("        endoffset ");

        internal const string VECTORS_EXTENSION = "vec";

        private readonly Directory directory;
        private readonly string segment;
        private IndexOutput _output;
        private int numDocsWritten = 0;
        private readonly BytesRef scratch = new BytesRef();
        private bool offsets;
        private bool positions;
        private bool payloads;

        public SimpleTextTermVectorsWriter(Directory directory, string segment, IOContext context)
        {
            this.directory = directory;
            this.segment = segment;
            bool success = false;
            try
            {
                _output = directory.CreateOutput(IndexFileNames.SegmentFileName(segment, "", VECTORS_EXTENSION), context);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    Abort();
                }
            }
        }

        public override void StartDocument(int numVectorFields)
        {
            Write(DOC);
            Write(Convert.ToString(numDocsWritten));
            NewLine();

            Write(NUMFIELDS);
            Write(Convert.ToString(numVectorFields));
            NewLine();
            numDocsWritten++;
        }

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            Write(FIELD);
            Write(Convert.ToString(info.Number));
            NewLine();

            Write(FIELDNAME);
            Write(info.Name);
            NewLine();

            Write(FIELDPOSITIONS);
            Write(Convert.ToString(positions));
            NewLine();

            Write(FIELDOFFSETS);
            Write(Convert.ToString(offsets));
            NewLine();

            Write(FIELDPAYLOADS);
            Write(Convert.ToString(payloads));
            NewLine();

            Write(FIELDTERMCOUNT);
            Write(Convert.ToString(numTerms));
            NewLine();

            this.positions = positions;
            this.offsets = offsets;
            this.payloads = payloads;
        }

        public override void StartTerm(BytesRef term, int freq)
        {
            Write(TERMTEXT);
            Write(term);
            NewLine();

            Write(TERMFREQ);
            Write(Convert.ToString(freq));
            NewLine();
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            Debug.Assert(positions || offsets);

            if (positions)
            {
                Write(POSITION);
                Write(Convert.ToString(position));
                NewLine();

                if (payloads)
                {
                    Write(PAYLOAD);
                    if (payload != null)
                    {
                        Debug.Assert(payload.Length > 0);
                        Write(payload);
                    }
                    NewLine();
                }
            }

            if (offsets)
            {
                Write(STARTOFFSET);
                Write(Convert.ToString(startOffset));
                NewLine();

                Write(ENDOFFSET);
                Write(Convert.ToString(endOffset));
                NewLine();
            }
        }

        public override void Abort()
        {
            try
            {
                Dispose();
            }
            finally
            {

                IOUtils.DeleteFilesIgnoringExceptions(directory,
                    IndexFileNames.SegmentFileName(segment, "", VECTORS_EXTENSION));
            }
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (numDocsWritten != numDocs)
            {
                throw new Exception("mergeVectors produced an invalid result: mergedDocs is " + numDocs +
                                    " but vec numDocs is " + numDocsWritten + " file=" + _output.ToString() +
                                    "; now aborting this merge to prevent index corruption");
            }
            Write(END);
            NewLine();
            SimpleTextUtil.WriteChecksum(_output, scratch);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) return;

            try
            {
                IOUtils.Close(_output);
            }
            finally
            {
                _output = null;
            }
        }

        public override IComparer<BytesRef> Comparator
        {
            get { return BytesRef.UTF8SortedAsUnicodeComparer; }
        }

        private void Write(string s)
        {
            SimpleTextUtil.Write(_output, s, scratch);
        }

        private void Write(BytesRef bytes)
        {
            SimpleTextUtil.Write(_output, bytes);
        }

        private void NewLine()
        {
            SimpleTextUtil.WriteNewline(_output);
        }
    }
}