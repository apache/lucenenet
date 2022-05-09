using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Implements a <see cref="ContentSource"/> over the TREC collection.
    /// </summary>
    /// <remarks>
    /// Supports the following configuration parameters (on top of
    /// <see cref="ContentSource"/>):
    /// <list type="bullet">
    ///     <item><term>work.dir</term><description>specifies the working directory. Required if "docs.dir"
    ///         denotes a relative path (<b>default=work</b>).</description></item>
    ///     <item><term>docs.dir</term><description>specifies the directory where the TREC files reside. 
    ///         Can be set to a relative path if "work.dir" is also specified
    ///         (<b>default=trec</b>).
    ///     </description></item>
    ///     <item><term>trec.doc.parser</term><description>specifies the <see cref="TrecDocParser"/> class to use for
    ///         parsing the TREC documents content (<b>default=TrecGov2Parser</b>).
    ///     </description></item>
    ///     <item><term>html.parser</term><description>specifies the <see cref="IHTMLParser"/> class to use for
    ///         parsing the HTML parts of the TREC documents content (<b>default=DemoHTMLParser</b>).
    ///     </description></item>
    ///     <item><term>content.source.encoding</term><description>if not specified, ISO-8859-1 is used.</description></item>
    ///     <item>content.source.excludeIteration<term></term><description>if <c>true</c>, do not append iteration number to docname</description></item>
    /// </list>
    /// </remarks>
    public class TrecContentSource : ContentSource
    {
        // LUCENENET specific - DateFormatInfo not used

        public static readonly string DOCNO = "<DOCNO>";
        public static readonly string TERMINATING_DOCNO = "</DOCNO>";
        public static readonly string DOC = "<DOC>";
        public static readonly string TERMINATING_DOC = "</DOC>";

        /// <summary>separator between lines in the buffer</summary>
        public static readonly string NEW_LINE = Environment.NewLine;

        private static readonly string[] DATE_FORMATS = {
            // LUCENENET specific: in JAVA, they don't care if it is an abbreviated or a full month name when parsing
            // so we provide definitions for both ways.
            "ddd, dd MMM yyyy hh:mm:ss zzz",   // Tue, 09 Dec 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd, dd MMMM yyyy hh:mm:ss zzz",  // Tue, 09 December 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd MMM dd hh:mm:ss yyyy zzz",    // Tue Dec 09 16:45:08 2003 EST (format not supported in .NET, must specify +5:00/+4:00 instead of EST)
            "ddd MMMM dd hh:mm:ss yyyy zzz",   // Tue December 09 16:45:08 2003 EST (format not supported in .NET, must specify +5:00/+4:00 instead of EST)
            "ddd, dd-MMM-':'y hh:mm:ss zzz",   // Tue, 09 Dec 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd, dd-MMMM-':'y hh:mm:ss zzz",  // Tue, 09 December 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd, dd-MMM-yyy hh:mm:ss zzz",    // Tue, 09 Dec 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd, dd-MMMM-yyy hh:mm:ss zzz",   // Tue, 09 December 2003 22:39:08 GMT (format not supported in .NET, must specify +0:00 instead of GMT)
            "ddd MMM dd hh:mm:ss yyyy",        // Tue Dec 09 16:45:08 2003
            "ddd MMMM dd hh:mm:ss yyyy",       // Tue December 09 16:45:08 2003
            "dd MMM yyyy",                     // 1 Mar 1994
            "dd MMMM yyyy",                    // 1 March 1994
            "MMM dd, yyyy",                    // Feb 3, 1994
            "MMMM dd, yyyy",                   // February 3, 1994
            "yyMMdd",                          // 910513
            "hhmm zzz MMM dd, yyyy",           // 0901 u.t.c. Apr 28, 1994 (format not supported in .NET, must specify +0:00 instead of u.t.c.)
            "hhmm zzz MMMM dd, yyyy",          // 0901 u.t.c. April 28, 1994 (format not supported in .NET, must specify +0:00 instead of u.t.c.)
        };

        private readonly DisposableThreadLocal<StringBuilder> trecDocBuffer = new DisposableThreadLocal<StringBuilder>();
        private DirectoryInfo dataDir = null;
        private readonly IList<FileInfo> inputFiles = new JCG.List<FileInfo>();
        private int nextFile = 0;
        // Use to synchronize threads on reading from the TREC documents.
        private readonly object @lock = new object(); // LUCENENET: marked readonly

        // Required for test
        internal TextReader reader;
        internal int iteration = 0;
        internal IHTMLParser htmlParser;

        private bool excludeDocnameIteration;
        private TrecDocParser trecDocParser = new TrecGov2Parser(); // default
        internal TrecDocParser.ParsePathType currPathType; // not private for tests

        private StringBuilder GetDocBuffer()
        {
            StringBuilder sb = trecDocBuffer.Value;
            if (sb is null)
            {
                sb = new StringBuilder();
                trecDocBuffer.Value = sb;
            }
            return sb;
        }

        internal IHTMLParser HtmlParser => htmlParser;

        /// <summary>
        /// Read until a line starting with the specified <paramref name="lineStart"/>.
        /// </summary>
        /// <param name="buf">Buffer for collecting the data if so specified.</param>
        /// <param name="lineStart">Line start to look for, must not be <c>null</c>.</param>
        /// <param name="collectMatchLine">Whether to collect the matching line into <c>buffer</c>.</param>
        /// <param name="collectAll">Whether to collect all lines into <c>buffer</c>.</param>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        /// <exception cref="NoMoreDataException">If the source is exhausted.</exception>
        private void Read(StringBuilder buf, string lineStart,
            bool collectMatchLine, bool collectAll)
        {
            string sep = "";
            while (true)
            {
                string line = reader.ReadLine();

                if (line is null)
                {
                    OpenNextFile();
                    continue;
                }

                var _ = line.Length;

                if (lineStart != null && line.StartsWith(lineStart, StringComparison.Ordinal))
                {
                    if (collectMatchLine)
                    {
                        buf.Append(sep).Append(line);
                        //sep = NEW_LINE; // LUCENENET: IDE0059: Remove unnecessary value assignment - this skips out of the loop
                    }
                    return;
                }

                if (collectAll)
                {
                    buf.Append(sep).Append(line);
                    sep = NEW_LINE;
                }
            }
        }

        internal virtual void OpenNextFile()
        {
            DoClose();
            //currPathType = null; 
            while (true)
            {
                if (nextFile >= inputFiles.Count)
                {
                    // exhausted files, start a new round, unless forever set to false.
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    nextFile = 0;
                    iteration++;
                }
                FileInfo f = inputFiles[nextFile++];
                if (m_verbose)
                {
                    Console.WriteLine("opening: " + f + " length: " + f.Length);
                }
                try
                {
                    Stream inputStream = StreamUtils.GetInputStream(f); // support either gzip, bzip2, or regular text file, by extension  
                    reader = new StreamReader(inputStream, m_encoding);
                    currPathType = TrecDocParser.PathType(f);
                    return;
                }
                catch (Exception e) when (e.IsException())
                {
                    if (m_verbose)
                    {
                        Console.WriteLine("Skipping 'bad' file " + f.FullName + " due to " + e.Message);
                        continue;
                    }
                    throw new NoMoreDataException();
                }
            }
        }

        public virtual DateTime? ParseDate(string dateStr)
        {
            dateStr = dateStr.Trim();
            if (DateTime.TryParseExact(dateStr, DATE_FORMATS, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d))
            {
                return d.ToUniversalTime();
            }
            else if (DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
            {
                return d.ToUniversalTime();
            }

            // do not fail test just because a date could not be parsed
            if (m_verbose)
            {
                Console.WriteLine("failed to parse date (assigning 'now') for: " + dateStr);
            }
            return null;
        }

        private void DoClose() // LUCENENET specific - separate disposing from closing so those tasks that "reopen" can continue
        {
            if (reader is null)
            {
                return;
            }

            try
            {
                reader?.Dispose();
            }
            catch (Exception e) when (e.IsIOException())
            {
                if (m_verbose)
                {
                    Console.WriteLine("failed to dispose reader !");
                    Console.WriteLine(e.ToString());
                }
            }
            reader = null;
        }

        /// <summary>
        /// Releases resources used by the <see cref="TrecContentSource"/> and
        /// if overridden in a derived class, optionally releases unmanaged resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DoClose();
                trecDocBuffer?.Dispose(); // LUCENENET specific
            }
        }

        public override DocData GetNextDocData(DocData docData)
        {
            string name = null;
            StringBuilder docBuf = GetDocBuffer();
            TrecDocParser.ParsePathType parsedPathType;

            // protect reading from the TREC files by multiple threads. The rest of the
            // method, i.e., parsing the content and returning the DocData can run unprotected.
            UninterruptableMonitor.Enter(@lock);
            try
            {
                if (reader is null)
                {
                    OpenNextFile();
                }

                // 1. skip until doc start - required for all TREC formats
                docBuf.Length = 0;
                Read(docBuf, DOC, false, false);

                // save parsedFile for passing trecDataParser after the sync block, in 
                // case another thread will open another file in between.
                parsedPathType = currPathType;

                // 2. name - required for all TREC formats
                docBuf.Length = 0;
                Read(docBuf, DOCNO, true, false);
                name = docBuf.ToString(DOCNO.Length, docBuf.IndexOf(TERMINATING_DOCNO,
                    DOCNO.Length, StringComparison.Ordinal) - DOCNO.Length).Trim();

                if (!excludeDocnameIteration)
                {
                    name = name + "_" + iteration;
                }

                // 3. read all until end of doc
                docBuf.Length = 0;
                Read(docBuf, TERMINATING_DOC, false, true);
            }
            finally
            {
                UninterruptableMonitor.Exit(@lock);
            }

            // count char length of text to be parsed (may be larger than the resulted plain doc body text).
            AddBytes(docBuf.Length);

            // This code segment relies on HtmlParser being thread safe. When we get 
            // here, everything else is already private to that thread, so we're safe.
            docData = trecDocParser.Parse(docData, name, this, docBuf, parsedPathType);
            AddItem();

            return docData;
        }

        public override void ResetInputs()
        {
            UninterruptableMonitor.Enter(@lock);
            try
            {
                base.ResetInputs();
                DoClose();
                nextFile = 0;
                iteration = 0;
            }
            finally
            {
                UninterruptableMonitor.Exit(@lock);
            }
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);
            // dirs
            DirectoryInfo workDir = new DirectoryInfo(config.Get("work.dir", "work"));
            string d = config.Get("docs.dir", "trec");
            dataDir = new DirectoryInfo(Path.Combine(workDir.FullName, d));
            // files
            CollectFiles(dataDir, inputFiles);
            if (inputFiles.Count == 0)
            {
                throw new ArgumentException("No files in dataDir: " + dataDir);
            }
            // trec doc parser
            try
            {
                string trecDocParserClassName = config.Get("trec.doc.parser", "Lucene.Net.Benchmarks.ByTask.Feeds.TrecGov2Parser, Lucene.Net.Benchmark");
                trecDocParser = (TrecDocParser)Activator.CreateInstance(Type.GetType(trecDocParserClassName));
            }
            catch (Exception e) when (e.IsException())
            {
                // Should not get here. Throw runtime exception.
                throw RuntimeException.Create(e);
            }
            // html parser
            try
            {
                string htmlParserClassName = config.Get("html.parser",
                    "Lucene.Net.Benchmarks.ByTask.Feeds.DemoHTMLParser, Lucene.Net.Benchmark");
                htmlParser = (IHTMLParser)Activator.CreateInstance(Type.GetType(htmlParserClassName));
            }
            catch (Exception e)
            {
                // Should not get here. Throw runtime exception.
                throw RuntimeException.Create(e);
            }
            // encoding
            if (m_encoding is null)
            {
                m_encoding = Encoding.GetEncoding("iso-8859-1"); //StandardCharsets.ISO_8859_1.name();
            }
            // iteration exclusion in doc name 
            excludeDocnameIteration = config.Get("content.source.excludeIteration", false);
        }
    }
}
