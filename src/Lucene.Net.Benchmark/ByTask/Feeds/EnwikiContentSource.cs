// LUCENENET TODO: Use HTML Agility pack instead of SAX ?

using J2N.Threading;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using Sax;
using Sax.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

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
    /// A <see cref="ContentSource"/> which reads the English Wikipedia dump. You can read
    /// the <c>.bz2</c> file directly (it will be decompressed on the fly). Config
    /// properties:
    /// <list type="bullet">
    ///     <item><term>keep.image.only.docs</term><description>false|true (default <b>true</b>).</description></item>
    ///     <item><term>docs.file</term><description>&lt;path to the file&gt;</description></item>
    /// </list>
    /// </summary>
    public class EnwikiContentSource : ContentSource
    {
        private class Parser : DefaultHandler//, IRunnable
        {
            private ThreadJob t;
            private bool threadDone;
            private bool stopped = false;
            private string[] tuple;
            private NoMoreDataException nmde;
            private readonly StringBuilder contents = new StringBuilder();
            private string title;
            private string body;
            private string time;
            private string id;

            private readonly EnwikiContentSource outerInstance;

            public Parser(EnwikiContentSource outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal string[] Next()
            {
                if (t is null)
                {
                    threadDone = false;
                    t = new ThreadJob(Run);
                    t.IsBackground = true;
                    t.Start();
                }
                string[] result;
                UninterruptableMonitor.Enter(this);
                try
                {
                    while (tuple is null && nmde is null && !threadDone && !stopped)
                    {
                        try
                        {
                            UninterruptableMonitor.Wait(this);
                        }
                        catch (Exception ie) when (ie.IsInterruptedException())
                        {
                            throw new Util.ThreadInterruptedException(ie);
                        }
                    }
                    if (tuple != null)
                    {
                        result = tuple;
                        tuple = null;
                        Monitor.Pulse(this);// notify();
                        return result;
                    }
                    if (nmde != null)
                    {
                        // Set to null so we will re-start thread in case
                        // we are re-used:
                        t = null;
                        throw nmde;
                    }
                    // The thread has exited yet did not hit end of
                    // data, so this means it hit an exception.  We
                    // throw NoMorDataException here to force
                    // benchmark to stop the current alg:
                    throw new NoMoreDataException();
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }

            internal static string Time(string original) // LUCENENET: CA1822: Mark members as static
            {
                StringBuilder buffer = new StringBuilder(24); // LUCENENET: We always have 24 chars, so allocate it up front

#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                buffer.Append(original.AsSpan(8, 10 - 8));
                buffer.Append('-');
                buffer.Append(months[int.Parse(original.AsSpan(5, 7 - 5), NumberStyles.Integer, CultureInfo.InvariantCulture) - 1]);
                buffer.Append('-');
                buffer.Append(original.AsSpan(0, 4 - 0));
                buffer.Append(' ');
                buffer.Append(original.AsSpan(11, 19 - 11));
                buffer.Append(".000");
#else
                buffer.Append(original.Substring(8, 10 - 8));
                buffer.Append('-');
                buffer.Append(months[int.Parse(original.Substring(5, 7 - 5), NumberStyles.Integer, CultureInfo.InvariantCulture) - 1]);
                buffer.Append('-');
                buffer.Append(original.Substring(0, 4 - 0));
                buffer.Append(' ');
                buffer.Append(original.Substring(11, 19 - 11));
                buffer.Append(".000");
#endif

                return buffer.ToString();
            }

            public override void Characters(char[] ch, int start, int length)
            {
                contents.Append(ch, start, length);
            }

            public override void EndElement(string @namespace, string simple, string qualified)
            {
                int elemType = GetElementType(qualified);
                switch (elemType)
                {
                    case PAGE:
                        // the body must be null and we either are keeping image docs or the
                        // title does not start with Image:
                        if (body != null && (outerInstance.keepImages || !title.StartsWith("Image:", StringComparison.Ordinal)))
                        {
                            string[] tmpTuple = new string[LENGTH];
                            tmpTuple[TITLE] = title.Replace('\t', ' ');
                            tmpTuple[DATE] = time.Replace('\t', ' ');
                            tmpTuple[BODY] = Regex.Replace(body, "[\t\n]", " ");
                            tmpTuple[ID] = id;
                            UninterruptableMonitor.Enter(this);
                            try
                            {
                                while (tuple != null && !stopped)
                                {
                                    try
                                    {
                                        UninterruptableMonitor.Wait(this); //wait();
                                    }
                                    catch (System.Threading.ThreadInterruptedException ie)
                                    {
                                        throw new Util.ThreadInterruptedException(ie);
                                    }
                                }
                                tuple = tmpTuple;
                                UninterruptableMonitor.Pulse(this); //notify();
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(this);
                            }
                        }
                        break;
                    case BODY:
                        body = contents.ToString();
                        //workaround that startswith doesn't have an ignore case option, get at least 10 chars.
                        string startsWith = body.Substring(0, Math.Min(10, contents.Length) - 0).ToLowerInvariant();
                        if (startsWith.StartsWith("#redirect", StringComparison.Ordinal))
                        {
                            body = null;
                        }
                        break;
                    case DATE:
                        time = Time(contents.ToString());
                        break;
                    case TITLE:
                        title = contents.ToString();
                        break;
                    case ID:
                        //the doc id is the first one in the page.  All other ids after that one can be ignored according to the schema
                        if (id is null)
                        {
                            id = contents.ToString();
                        }
                        break;
                    default:
                        // this element should be discarded.
                        break;
                }
            }

            public void Run()
            {

                try
                {
                    Sax.IXMLReader reader = new TagSoup.Parser(); //XMLReaderFactory.createXMLReader();
                    reader.ContentHandler = this;
                    reader.ErrorHandler = this;

                    while (!stopped)
                    {
                        Stream localFileIS = outerInstance.@is;
                        if (localFileIS != null)
                        { // null means fileIS was closed on us 
                            try
                            {
                                // To work around a bug in XERCES (XERCESJ-1257), we assume the XML is always UTF8, so we simply provide reader.
                                reader.Parse(new InputSource(IOUtils.GetDecodingReader(localFileIS, Encoding.UTF8)));
                            }
                            catch (Exception ioe) when (ioe.IsIOException())
                            {
                                UninterruptableMonitor.Enter(outerInstance);
                                try
                                {
                                    if (localFileIS != outerInstance.@is)
                                    {
                                        // fileIS was closed on us, so, just fall through
                                    }
                                    else
                                        // Exception is real
                                        throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
                                }
                                finally
                                {
                                    UninterruptableMonitor.Exit(outerInstance);
                                }
                            }
                        }
                        UninterruptableMonitor.Enter(this);
                        try
                        {
                            if (stopped || !outerInstance.m_forever)
                            {
                                nmde = new NoMoreDataException();
                                UninterruptableMonitor.Pulse(this); //notify();
                                return;
                            }
                            else if (localFileIS == outerInstance.@is)
                            {
                                // If file is not already re-opened then re-open it now
                                outerInstance.@is = outerInstance.OpenInputStream();
                            }
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(this);
                        }
                    }
                }
                catch (SAXException sae)
                {
                    throw RuntimeException.Create(sae);
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create(ioe);
                }
                finally
                {
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        threadDone = true;
                        UninterruptableMonitor.Pulse(this); //Notify();
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            public override void StartElement(string @namespace, string simple, string qualified,
                                     IAttributes attributes)
            {
                int elemType = GetElementType(qualified);
                switch (elemType)
                {
                    case PAGE:
                        title = null;
                        body = null;
                        time = null;
                        id = null;
                        break;
                    // intentional fall-through.
                    case BODY:
                    case DATE:
                    case TITLE:
                    case ID:
                        contents.Length = 0;
                        break;
                    default:
                        // this element should be discarded.
                        break;
                }
            }

            internal void Stop()
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    stopped = true;
                    if (tuple != null)
                    {
                        tuple = null;
                        UninterruptableMonitor.Pulse(this); //Notify();
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        private static readonly IDictionary<string, int> ELEMENTS = new Dictionary<string, int> // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            { "page", PAGE },
            { "text", BODY },
            { "timestamp", DATE },
            { "title", TITLE },
            { "id", ID }
        };
        private const int TITLE = 0;
        private const int DATE = TITLE + 1;
        private const int BODY = DATE + 1;
        private const int ID = BODY + 1;
        private const int LENGTH = ID + 1;
        // LENGTH is used as the size of the tuple, so whatever constants we need that
        // should not be part of the tuple, we should define them after LENGTH.
        private const int PAGE = LENGTH + 1;

        private static readonly string[] months = {"JAN", "FEB", "MAR", "APR",
                                  "MAY", "JUN", "JUL", "AUG",
                                  "SEP", "OCT", "NOV", "DEC"};

        public EnwikiContentSource()
        {
            parser = new Parser(this);
        }

        /// <summary>
        /// Returns the type of the element if defined, otherwise returns -1. This
        /// method is useful in startElement and endElement, by not needing to compare
        /// the element qualified name over and over.
        /// </summary>
        private static int GetElementType(string elem)
        {
            return ELEMENTS.TryGetValue(elem, out int val) ? val : -1;
        }

        private FileInfo file;
        private bool keepImages = true;
        private Stream @is;
        private readonly Parser parser;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                UninterruptableMonitor.Enter(this);
                try
                {
                    parser.Stop();
                    if (@is != null)
                    {
                        @is.Dispose();
                        @is = null;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(this);
                }
            }
        }

        public override DocData GetNextDocData(DocData docData)
        {
            string[] tuple = parser.Next();
            docData.Clear();
            docData.Name = tuple[ID];
            docData.Body = tuple[BODY];
            docData.SetDate(tuple[DATE]);
            docData.Title = tuple[TITLE];
            return docData;
        }

        public override void ResetInputs()
        {
            base.ResetInputs();
            @is = OpenInputStream();
        }

        /// <summary>Open the input stream.</summary>
        protected virtual Stream OpenInputStream()
        {
            return StreamUtils.GetInputStream(file);
        }

        public override void SetConfig(Config config)
        {
            base.SetConfig(config);
            keepImages = config.Get("keep.image.only.docs", true);
            string fileName = config.Get("docs.file", null);
            if (fileName != null)
            {
                file = new FileInfo(fileName);
            }
        }
    }
}
