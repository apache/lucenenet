using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace Lucene.Net.Codecs.SimpleText
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

    using BytesRef = Util.BytesRef;
    using Directory = Store.Directory;
    using FieldInfo = Index.FieldInfo;
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using IndexOutput = Store.IndexOutput;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;

    /// <summary>
    /// Writes plain-text term vectors.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
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

        private readonly Directory _directory;
        private readonly string _segment;
#pragma warning disable CA2213 // Disposable fields should be disposed
        private IndexOutput _output;
#pragma warning restore CA2213 // Disposable fields should be disposed
        private int _numDocsWritten;
        private readonly BytesRef _scratch = new BytesRef();
        private bool _offsets;
        private bool _positions;
        private bool _payloads;

        public SimpleTextTermVectorsWriter(Directory directory, string segment, IOContext context)
        {
            _directory = directory;
            _segment = segment;
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
            Write(Convert.ToString(_numDocsWritten, CultureInfo.InvariantCulture));
            NewLine();

            Write(NUMFIELDS);
            Write(Convert.ToString(numVectorFields, CultureInfo.InvariantCulture));
            NewLine();
            _numDocsWritten++;
        }

        public override void StartField(FieldInfo info, int numTerms, bool positions, bool offsets, bool payloads)
        {
            Write(FIELD);
            Write(Convert.ToString(info.Number, CultureInfo.InvariantCulture));
            NewLine();

            Write(FIELDNAME);
            Write(info.Name);
            NewLine();

            Write(FIELDPOSITIONS);
            Write(Convert.ToString(positions, CultureInfo.InvariantCulture).ToLowerInvariant());
            NewLine();

            Write(FIELDOFFSETS);
            Write(Convert.ToString(offsets, CultureInfo.InvariantCulture).ToLowerInvariant());
            NewLine();

            Write(FIELDPAYLOADS);
            Write(Convert.ToString(payloads, CultureInfo.InvariantCulture).ToLowerInvariant());
            NewLine();

            Write(FIELDTERMCOUNT);
            Write(Convert.ToString(numTerms, CultureInfo.InvariantCulture));
            NewLine();

            _positions = positions;
            _offsets = offsets;
            _payloads = payloads;
        }

        public override void StartTerm(BytesRef term, int freq)
        {
            Write(TERMTEXT);
            Write(term);
            NewLine();

            Write(TERMFREQ);
            Write(Convert.ToString(freq, CultureInfo.InvariantCulture));
            NewLine();
        }

        public override void AddPosition(int position, int startOffset, int endOffset, BytesRef payload)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(_positions || _offsets);

            if (_positions)
            {
                Write(POSITION);
                Write(Convert.ToString(position, CultureInfo.InvariantCulture));
                NewLine();

                if (_payloads)
                {
                    Write(PAYLOAD);
                    if (payload != null)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(payload.Length > 0);
                        Write(payload);
                    }
                    NewLine();
                }
            }

            if (_offsets)
            {
                Write(STARTOFFSET);
                Write(Convert.ToString(startOffset, CultureInfo.InvariantCulture));
                NewLine();

                Write(ENDOFFSET);
                Write(Convert.ToString(endOffset, CultureInfo.InvariantCulture));
                NewLine();
            }
        }

        public override sealed void Abort()
        {
            try
            {
                Dispose();
            }
            catch (Exception t) when (t.IsThrowable())
            {
            }
            IOUtils.DeleteFilesIgnoringExceptions(_directory,
                    IndexFileNames.SegmentFileName(_segment, "", VECTORS_EXTENSION));
        }

        public override void Finish(FieldInfos fis, int numDocs)
        {
            if (_numDocsWritten != numDocs)
            {
                throw RuntimeException.Create("mergeVectors produced an invalid result: mergedDocs is " + numDocs +
                                    " but vec numDocs is " + _numDocsWritten + " file=" + _output +
                                    "; now aborting this merge to prevent index corruption");
            }
            Write(END);
            NewLine();
            SimpleTextUtil.WriteChecksum(_output, _scratch);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            try
            {
                IOUtils.Dispose(_output);
            }
            finally
            {
                _output = null;
            }
        }

        public override IComparer<BytesRef> Comparer => BytesRef.UTF8SortedAsUnicodeComparer;

        private void Write(string s)
        {
            SimpleTextUtil.Write(_output, s, _scratch);
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