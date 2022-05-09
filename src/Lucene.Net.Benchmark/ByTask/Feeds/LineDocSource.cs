using J2N.Text;
using Lucene.Net.Benchmarks.ByTask.Tasks;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
    /// A <see cref="ContentSource"/> reading one line at a time as a
    /// <see cref="Documents.Document"/> from a single file. This saves IO
    /// cost (over DirContentSource) of recursing through a directory and opening a
    /// new file for every document.
    /// </summary>
    /// <remarks>
    /// The expected format of each line is (arguments are separated by &lt;TAB&gt;):
    /// <i>title, date, body</i>. If a line is read in a different format, a
    /// <see cref="Exception"/> will be thrown. In general, you should use this
    /// content source for files that were created with <see cref="WriteLineDocTask"/>.
    /// </remarks>
    public class LineDocSource : ContentSource
    {
        // LUCENENET specific - de-nested LineParser, SimpleLineParser, HeaderLineParser

        private FileInfo file;
        private TextReader reader;
        private int readCount;

        private LineParser docDataLineReader = null;
        private bool skipHeaderLine = false;

        private void OpenFile()
        {
            try
            {
                if (reader != null)
                {
                    reader.Dispose();
                }
                Stream @is = StreamUtils.GetInputStream(file);
                reader = new StreamReader(@is, m_encoding);
                if (skipHeaderLine)
                {
                    reader.ReadLine(); // skip one line - the header line - already handled that info
                }
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && reader != null)
            {
                reader.Dispose();
                reader = null;
            }
        }

        public override DocData GetNextDocData(DocData docData)
        {
            string line;
            int myID;


            UninterruptableMonitor.Enter(this);
            try
            {
                line = reader.ReadLine();
                if (line is null)
                {
                    if (!m_forever)
                    {
                        throw new NoMoreDataException();
                    }
                    // Reset the file
                    OpenFile();
                    return GetNextDocData(docData);
                }
                if (docDataLineReader is null)
                { // first line ever, one time initialization,
                    docDataLineReader = CreateDocDataLineReader(line);
                    if (skipHeaderLine)
                    {
                        return GetNextDocData(docData);
                    }
                }
                // increment IDS only once...
                myID = readCount++;
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }

            // The date String was written in the format of DateTools.dateToString.
            docData.Clear();
            docData.ID = myID;
            docDataLineReader.ParseLine(docData, line);
            return docData;
        }

        private LineParser CreateDocDataLineReader(string line)
        {
            string[] header;
            string headIndicator = WriteLineDocTask.FIELDS_HEADER_INDICATOR + WriteLineDocTask.SEP;

            if (line.StartsWith(headIndicator, StringComparison.Ordinal))
            {
                header = line.Substring(headIndicator.Length).Split(WriteLineDocTask.SEP).TrimEnd();
                skipHeaderLine = true; // mark to skip the header line when input file is reopened
            }
            else
            {
                header = WriteLineDocTask.DEFAULT_FIELDS;
            }

            // if a specific DocDataLineReader was configured, must respect it
            string docDataLineReaderClassName = Config.Get("line.parser", null);
            if (docDataLineReaderClassName != null)
            {
                try
                {
                    Type clazz = Type.GetType(docDataLineReaderClassName);
                    return (LineParser)Activator.CreateInstance(clazz, (object)header);
                }
                catch (Exception e) when (e.IsException())
                {
                    throw RuntimeException.Create("Failed to instantiate " + docDataLineReaderClassName, e);
                }
            }

            // if this the simple case,   
            if (Arrays.Equals(header, WriteLineDocTask.DEFAULT_FIELDS))
            {
                return new SimpleLineParser(header);
            }
            return new HeaderLineParser(header);
        }

        public override void ResetInputs()
        {
            base.ResetInputs();
            OpenFile();
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);
            string fileName = config.Get("docs.file", null);
            if (fileName is null)
            {
                throw new ArgumentException("docs.file must be set");
            }
            file = new FileInfo(fileName);
            if (m_encoding is null)
            {
                m_encoding = Encoding.UTF8;
            }
        }
    }

    /// <summary>Reader of a single input line into <see cref="DocData"/>.</summary>
    public abstract class LineParser
    {
        protected readonly string[] m_header;

        /// <summary>
        /// Construct with the header 
        /// </summary>
        /// <param name="header">header line found in the input file, or <c>null</c> if none.</param>
        protected LineParser(string[] header) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_header = header;
        }

        /// <summary>
        /// parse an input line and fill doc data appropriately
        /// </summary>
        public abstract void ParseLine(DocData docData, string line);
    }

    /// <summary>
    /// <see cref="LineParser"/> which ignores the header passed to its constructor
    /// and assumes simply that field names and their order are the same 
    /// as in <see cref="WriteLineDocTask.DEFAULT_FIELDS"/>. 
    /// </summary>
    public class SimpleLineParser : LineParser
    {
        public SimpleLineParser(string[] header)
            : base(header)
        {
        }

        public override void ParseLine(DocData docData, string line)
        {
            int k1 = 0;
            int k2 = line.IndexOf(WriteLineDocTask.SEP, k1);
            if (k2 < 0)
            {
                throw RuntimeException.Create("line: [" + line + "] is in an invalid format (missing: separator title::date)!");
            }
            docData.Title = line.Substring(k1, k2 - k1);
            k1 = k2 + 1;
            k2 = line.IndexOf(WriteLineDocTask.SEP, k1);
            if (k2 < 0)
            {
                throw RuntimeException.Create("line: [" + line + "] is in an invalid format (missing: separator date::body)!");
            }
            docData.SetDate(line.Substring(k1, k2 - k1));
            k1 = k2 + 1;
            k2 = line.IndexOf(WriteLineDocTask.SEP, k1);
            if (k2 >= 0)
            {
                throw RuntimeException.Create("line: [" + line + "] is in an invalid format (too many separators)!");
            }
            // last one
            docData.Body = line.Substring(k1);
        }
    }

    /// <summary>
    /// <see cref="LineParser"/> which sets field names and order by 
    /// the header - any header - of the lines file.
    /// It is less efficient than <see cref="SimpleLineParser"/> but more powerful.
    /// </summary>
    public class HeaderLineParser : LineParser 
    {
        private enum FieldName { NAME, TITLE, DATE, BODY, PROP }
        private readonly FieldName[] posToF;
        public HeaderLineParser(string[] header)
            : base(header)
        {
            posToF = new FieldName[header.Length];
            for (int i = 0; i < header.Length; i++)
            {
                String f = header[i];
                if (DocMaker.NAME_FIELD.Equals(f, StringComparison.Ordinal))
                {
                    posToF[i] = FieldName.NAME;
                }
                else if (DocMaker.TITLE_FIELD.Equals(f, StringComparison.Ordinal))
                {
                    posToF[i] = FieldName.TITLE;
                }
                else if (DocMaker.DATE_FIELD.Equals(f, StringComparison.Ordinal))
                {
                    posToF[i] = FieldName.DATE;
                }
                else if (DocMaker.BODY_FIELD.Equals(f, StringComparison.Ordinal))
                {
                    posToF[i] = FieldName.BODY;
                }
                else
                {
                    posToF[i] = FieldName.PROP;
                }
            }
        }

        public override void ParseLine(DocData docData, string line)
        {
            int n = 0;
            int k1 = 0;
            int k2;
            while ((k2 = line.IndexOf(WriteLineDocTask.SEP, k1)) >= 0)
            {
                if (n >= m_header.Length)
                {
                    throw RuntimeException.Create("input line has invalid format: " + (n + 1) + " fields instead of " + m_header.Length + " :: [" + line + "]");
                }
                SetDocDataField(docData, n, line.Substring(k1, k2 - k1));
                ++n;
                k1 = k2 + 1;
            }
            if (n != m_header.Length - 1)
            {
                throw RuntimeException.Create("input line has invalid format: " + (n + 1) + " fields instead of " + m_header.Length + " :: [" + line + "]");
            }
            // last one
            SetDocDataField(docData, n, line.Substring(k1));
        }

        private void SetDocDataField(DocData docData, int position, string text)
        {
            switch (posToF[position])
            {
                case FieldName.NAME:
                    docData.Name = text;
                    break;
                case FieldName.TITLE:
                    docData.Title = text;
                    break;
                case FieldName.DATE:
                    docData.SetDate(text);
                    break;
                case FieldName.BODY:
                    docData.Body = text;
                    break;
                case FieldName.PROP:
                    var p = docData.Props;
                    if (p is null)
                    {
                        p = new Dictionary<string, string>();
                        docData.Props = p;
                    }
                    p[m_header[position]] = text;
                    break;
            }
        }
    }
}
