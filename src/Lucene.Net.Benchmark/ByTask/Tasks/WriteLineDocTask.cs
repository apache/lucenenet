using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Feeds;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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

    /// <summary>
    /// A task which writes documents, one line per document. Each line is in the
    /// following format: title &lt;TAB&gt; date &lt;TAB&gt; body. The output of this
    /// task can be consumed by <see cref="LineDocSource"/> and is intended
    /// to save the IO overhead of opening a file per document to be indexed.
    /// </summary>
    /// <remarks>
    /// The format of the output is set according to the output file extension.
    /// Compression is recommended when the output file is expected to be large.
    /// See info on file extensions in <see cref="FileType"/>.
    /// <para/>
    /// Supports the following parameters:
    /// <list type="bullet">
    ///     <item><term>line.file.out</term><description>the name of the file to write the output to. That parameter is mandatory. <b>NOTE:</b> the file is re-created.</description></item>
    ///     <item><term>line.fields</term><description>which fields should be written in each line. (optional, default: <see cref="DEFAULT_FIELDS"/>).</description></item>
    ///     <item><term>sufficient.fields</term><description>
    ///         list of field names, separated by comma, which, 
    ///         if all of them are missing, the document will be skipped. For example, to require 
    ///         that at least one of f1,f2 is not empty, specify: "f1,f2" in this field. To specify
    ///         that no field is required, i.e. that even empty docs should be emitted, specify <b>","</b>
    ///         (optional, default: <see cref="DEFAULT_SUFFICIENT_FIELDS"/>).
    ///     </description></item>
    /// </list>
    /// <para/>
    /// <b>NOTE:</b> this class is not thread-safe and if used by multiple threads the
    /// output is unspecified (as all will write to the same output file in a
    /// non-synchronized way).
    /// </remarks>
    public class WriteLineDocTask : PerfTask
    {
        public const string FIELDS_HEADER_INDICATOR = "FIELDS_HEADER_INDICATOR###";

        public const char SEP = '\t';

        /// <summary>
        /// Fields to be written by default
        /// </summary>
        public static readonly string[] DEFAULT_FIELDS = new string[] {
            DocMaker.TITLE_FIELD,
            DocMaker.DATE_FIELD,
            DocMaker.BODY_FIELD,
        };

        /// <summary>
        /// Default fields which at least one of them is required to not skip the doc.
        /// </summary>
        public static readonly string DEFAULT_SUFFICIENT_FIELDS = DocMaker.TITLE_FIELD + ',' + DocMaker.BODY_FIELD;

        private int docSize = 0;
        protected readonly string m_fname;
        // LUCENENET specific - changed to protected to allow subclass access in case
        // it needs to be used in WriteHeader from the subclass's constructor
        protected readonly TextWriter m_lineFileOut;
        private readonly DocMaker docMaker;
        private readonly DisposableThreadLocal<StringBuilder> threadBuffer = new DisposableThreadLocal<StringBuilder>();
        private readonly DisposableThreadLocal<Regex> threadNormalizer = new DisposableThreadLocal<Regex>();
        private readonly string[] fieldsToWrite;
        private readonly bool[] sufficientFields;
        private readonly bool checkSufficientFields;

        private readonly object lineFileLock = new object(); // LUCENENET specific - lock to ensure writes don't collide for this instance

        public WriteLineDocTask(PerfRunData runData)
            : this(runData, performWriteHeader: true)
        {
        }

        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S1699:Constructors should only call non-overridable methods", Justification = "Required for continuity with Lucene's design")]
        public WriteLineDocTask(PerfRunData runData, bool performWriteHeader)
            : base(runData)
        {
            Config config = runData.Config;
            m_fname = config.Get("line.file.out", null);
            if (m_fname is null)
            {
                throw new ArgumentException("line.file.out must be set");
            }
            Stream @out = StreamUtils.GetOutputStream(new FileInfo(m_fname));
            m_lineFileOut = new StreamWriter(@out, Encoding.UTF8);
            docMaker = runData.DocMaker;

            // init fields 
            string f2r = config.Get("line.fields", null);
            if (f2r is null)
            {
                fieldsToWrite = DEFAULT_FIELDS;
            }
            else
            {
                if (f2r.IndexOf(SEP) >= 0)
                {
                    throw new ArgumentException("line.fields " + f2r + " should not contain the separator char: " + SEP);
                }
                fieldsToWrite = f2r.Split(',').TrimEnd();
            }

            // init sufficient fields
            sufficientFields = new bool[fieldsToWrite.Length];
            string suff = config.Get("sufficient.fields", DEFAULT_SUFFICIENT_FIELDS);
            if (",".Equals(suff, StringComparison.Ordinal))
            {
                checkSufficientFields = false;
            }
            else
            {
                checkSufficientFields = true;
                ISet<string> sf = new JCG.HashSet<string>(suff.Split(',').TrimEnd());
                for (int i = 0; i < fieldsToWrite.Length; i++)
                {
                    if (sf.Contains(fieldsToWrite[i]))
                    {
                        sufficientFields[i] = true;
                    }
                }
            }

            if (performWriteHeader)
            {
                WriteHeader(m_lineFileOut);
            }
        }

        /// <summary>
        /// Write header to the lines file - indicating how to read the file later.
        /// </summary>
        protected virtual void WriteHeader(TextWriter @out)
        {
            StringBuilder sb = threadBuffer.Value;
            if (sb is null)
            {
                sb = new StringBuilder();
                threadBuffer.Value = sb;
            }
            sb.Length = 0;
            sb.Append(FIELDS_HEADER_INDICATOR);
            foreach (string f in fieldsToWrite)
            {
                sb.Append(SEP).Append(f);
            }
            UninterruptableMonitor.Enter(lineFileLock);
            try // LUCENENET specific - lock to ensure writes don't collide for this instance
            {
                @out.WriteLine(sb.ToString());
            }
            finally
            {
                UninterruptableMonitor.Exit(lineFileLock);
            }
        }

        protected override string GetLogMessage(int recsCount)
        {
            return "Wrote " + recsCount + " line docs";
        }

        public override int DoLogic()
        {
            Document doc = docSize > 0 ? docMaker.MakeDocument(docSize) : docMaker.MakeDocument();

            Regex matcher = threadNormalizer.Value;
            if (matcher is null)
            {
                matcher = new Regex("[\t\r\n]+", RegexOptions.Compiled);
                threadNormalizer.Value = matcher;
            }

            StringBuilder sb = threadBuffer.Value;
            if (sb is null)
            {
                sb = new StringBuilder();
                threadBuffer.Value = sb;
            }
            sb.Length = 0;

            bool sufficient = !checkSufficientFields;
            for (int i = 0; i < fieldsToWrite.Length; i++)
            {
                IIndexableField f = doc.GetField(fieldsToWrite[i]);
                string text = f is null ? "" : matcher.Replace(f.GetStringValue(), " ").Trim();
                sb.Append(text).Append(SEP);
                sufficient |= text.Length > 0 && sufficientFields[i];
            }
            if (sufficient)
            {
                sb.Length--; // remove redundant last separator
                // lineFileOut is a PrintWriter, which synchronizes internally in println.
                UninterruptableMonitor.Enter(lineFileLock); // LUCENENET specific - lock to ensure writes don't collide for this instance
                try
                {
                    LineFileOut(doc).WriteLine(sb.ToString());
                }
                finally
                {
                    UninterruptableMonitor.Exit(lineFileLock);
                }
            }

            return 1;
        }

        /// <summary>
        /// Selects output line file by written doc.
        /// Default: original output line file.
        /// </summary>
        protected virtual TextWriter LineFileOut(Document doc)
        {
            return m_lineFileOut;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                threadBuffer.Dispose(); // LUCENENET specific: ThreadLocal is disposable
                threadNormalizer.Dispose(); // LUCENENET specific: ThreadLocal is disposable
                m_lineFileOut.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Set the params (docSize only)
        /// </summary>
        /// <param name="params">docSize, or 0 for no limit.</param>
        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            docSize = (int)float.Parse(@params, CultureInfo.InvariantCulture);
        }

        public override bool SupportsParams => true;
    }
}
