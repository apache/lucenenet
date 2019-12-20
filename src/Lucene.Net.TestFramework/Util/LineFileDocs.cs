using Lucene.Net.Documents;
using Lucene.Net.Support;
using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using AtomicInt32 = J2N.Threading.Atomic.AtomicInt32;
using Console = Lucene.Net.Support.SystemConsole;

namespace Lucene.Net.Util
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

    using Document = Documents.Document;
    using Field = Field;
    using FieldType = FieldType;
    using SortedDocValuesField = SortedDocValuesField;
    using StringField = StringField;
    using TextField = TextField;

    /// <summary>
    /// Minimal port of benchmark's LneDocSource +
    /// DocMaker, so tests can enum docs from a line file created
    /// by benchmark's WriteLineDoc task
    /// </summary>
    public class LineFileDocs : IDisposable
    {
        private TextReader reader;
        //private static readonly int BUFFER_SIZE = 1 << 16; // 64K // LUCENENET NOTE: Not used because we don't have a BufferedReader in .NET
        private readonly AtomicInt32 id = new AtomicInt32();
        private readonly string path;
        private readonly bool useDocValues;

        /// <summary>
        /// If forever is true, we rewind the file at EOF (repeat
        /// the docs over and over)
        /// </summary>
        public LineFileDocs(Random random, string path, bool useDocValues)
        {
            this.path = path;
            this.useDocValues = useDocValues;
            Open(random);
        }

        public LineFileDocs(Random random)
            : this(random, LuceneTestCase.TEST_LINE_DOCS_FILE, true)
        {
        }

        public LineFileDocs(Random random, bool useDocValues)
            : this(random, LuceneTestCase.TEST_LINE_DOCS_FILE, useDocValues)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // LUCENENET specific: Implemented dispose pattern
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this)
                {
                    threadDocs?.Dispose();
                    if (reader != null)
                    {
                        reader.Dispose();
                        reader = null;
                    }
                }
            }
        }

        private long RandomSeekPos(Random random, long size)
        {
            if (random == null || size <= 3L)
            {
                return 0L;
            }
            return (random.NextInt64() & long.MaxValue) % (size / 3);
        }

        private void Open(Random random)
        {
            lock (this)
            {
                Stream @is;
                bool needSkip = true, failed = false;
                long size = 0L, seekTo = 0L;

                try
                {
                    // LUCENENET: We have embedded the default file, so if that filename is passed,
                    // open the local resource instead of an external file.
                    if (path == LuceneTestCase.DEFAULT_LINE_DOCS_FILE)
                    {
                        @is = this.GetType().getResourceAsStream(path);
                    }
                    else
                    {
                        @is = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    }
                }
                catch (Exception)
                {
                    failed = true;
                    // if its not in classpath, we load it as absolute filesystem path (e.g. Hudson's home dir)
                    FileInfo file = new FileInfo(path);
                    size = file.Length;
                    if (path.EndsWith(".gz", StringComparison.Ordinal))
                    {
                        // if it is a gzip file, we need to use InputStream and slowly skipTo:
                        @is = new FileStream(file.FullName, FileMode.Append, FileAccess.Write, FileShare.Read);
                    }
                    else
                    {
                        // optimized seek using RandomAccessFile:
                        seekTo = RandomSeekPos(random, size);
                        FileStream channel = new FileStream(path, FileMode.Open);
                        if (LuceneTestCase.VERBOSE)
                        {
                            Console.WriteLine("TEST: LineFileDocs: file seek to fp=" + seekTo + " on open");
                        }
                        channel.Position = seekTo;
                        @is = new FileStream(channel.ToString(), FileMode.Append, FileAccess.Write, FileShare.Read);
                        needSkip = false;
                    }
                }
                if (!failed)
                {
                    // if the file comes from Classpath:
                    size = @is.Length;// available();
                }

                if (path.EndsWith(".gz", StringComparison.Ordinal))
                {
                    using (var gzs = new GZipStream(@is, CompressionMode.Decompress))
                    {
                        var temp = new MemoryStream();
                        gzs.CopyTo(temp);
                        // Free up the previous stream
                        @is.Dispose();
                        // Use the decompressed stream now
                        @is = temp;
                    }
                    // guestimate:
                    size = (long)(size * 2.8);
                }

                // If we only have an InputStream, we need to seek now,
                // but this seek is a scan, so very inefficient!!!
                if (needSkip)
                {
                    seekTo = RandomSeekPos(random, size);
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("TEST: LineFileDocs: stream skip to fp=" + seekTo + " on open");
                    }
                    @is.Position = seekTo;
                }

                // if we seeked somewhere, read until newline char
                if (seekTo > 0L)
                {
                    int b;
                    byte[] bytes = new byte[sizeof(int)];
                    do
                    {
                        @is.Read(bytes, 0, sizeof(int));
                        b = BitConverter.ToInt32(bytes, 0);
                    } while (b >= 0 && b != 13 && b != 10);
                }

                //CharsetDecoder decoder = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT);
                MemoryStream ms = new MemoryStream();
                @is.CopyTo(ms);
                reader = new StringReader(Encoding.UTF8.GetString(ms.ToArray()));//, BUFFER_SIZE);

                if (seekTo > 0L)
                {
                    // read one more line, to make sure we are not inside a Windows linebreak (\r\n):
                    reader.ReadLine();
                }

                @is.Dispose();
            }
        }

        public virtual void Reset(Random random)
        {
            lock (this)
            {
                Dispose();
                Open(random);
                id.Value = 0;
            }
        }

        private const char SEP = '\t';

        private sealed class DocState
        {
            internal Document Doc { get; private set; }
            internal Field TitleTokenized { get; private set; }
            internal Field Title { get; private set; }
            internal Field TitleDV { get; private set; }
            internal Field Body { get; private set; }
            internal Field Id { get; private set; }
            internal Field Date { get; private set; }

            public DocState(bool useDocValues)
            {
                Doc = new Document();

                Title = new StringField("title", "", Field.Store.NO);
                Doc.Add(Title);

                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = true;
                ft.StoreTermVectorOffsets = true;
                ft.StoreTermVectorPositions = true;

                TitleTokenized = new Field("titleTokenized", "", ft);
                Doc.Add(TitleTokenized);

                Body = new Field("body", "", ft);
                Doc.Add(Body);

                Id = new StringField("docid", "", Field.Store.YES);
                Doc.Add(Id);

                Date = new StringField("date", "", Field.Store.YES);
                Doc.Add(Date);

                if (useDocValues)
                {
                    TitleDV = new SortedDocValuesField("titleDV", new BytesRef());
                    Doc.Add(TitleDV);
                }
                else
                {
                    TitleDV = null;
                }
            }
        }

        private readonly ThreadLocal<DocState> threadDocs = new ThreadLocal<DocState>();

        /// <summary>
        /// Note: Document instance is re-used per-thread </summary>
        public virtual Document NextDoc()
        {
            string line;
            lock (this)
            {
                line = reader.ReadLine();
                if (line == null)
                {
                    // Always rewind at end:
                    if (LuceneTestCase.VERBOSE)
                    {
                        Console.WriteLine("TEST: LineFileDocs: now rewind file...");
                    }
                    Dispose();
                    Open(null);
                    line = reader.ReadLine();
                }
            }

            DocState docState = threadDocs.Value;
            if (docState == null)
            {
                docState = new DocState(useDocValues);
                threadDocs.Value = docState;
            }

            int spot = line.IndexOf(SEP);
            if (spot == -1)
            {
                throw new Exception("line: [" + line + "] is in an invalid format !");
            }
            int spot2 = line.IndexOf(SEP, 1 + spot);
            if (spot2 == -1)
            {
                throw new Exception("line: [" + line + "] is in an invalid format !");
            }

            docState.Body.SetStringValue(line.Substring(1 + spot2, line.Length - (1 + spot2)));
            string title = line.Substring(0, spot);
            docState.Title.SetStringValue(title);
            if (docState.TitleDV != null)
            {
                docState.TitleDV.SetBytesValue(new BytesRef(title));
            }
            docState.TitleTokenized.SetStringValue(title);
            docState.Date.SetStringValue(line.Substring(1 + spot, spot2 - (1 + spot)));
            docState.Id.SetStringValue(Convert.ToString(id.GetAndIncrement(), CultureInfo.InvariantCulture));
            return docState.Doc;
        }
    }
}