using System;
using System.Collections.Generic;
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
    using FieldInfos = Index.FieldInfos;
    using IndexFileNames = Index.IndexFileNames;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using SegmentInfo = Index.SegmentInfo;

    /// <summary>
    /// Writes plain text segments files.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextSegmentInfoWriter : SegmentInfoWriter
    {
        internal static readonly BytesRef SI_VERSION = new BytesRef("    version ");
        internal static readonly BytesRef SI_DOCCOUNT = new BytesRef("    number of documents ");
        internal static readonly BytesRef SI_USECOMPOUND = new BytesRef("    uses compound file ");
        internal static readonly BytesRef SI_NUM_DIAG = new BytesRef("    diagnostics ");
        internal static readonly BytesRef SI_DIAG_KEY = new BytesRef("      key ");
        internal static readonly BytesRef SI_DIAG_VALUE = new BytesRef("      value ");
        internal static readonly BytesRef SI_NUM_FILES = new BytesRef("    files ");
        internal static readonly BytesRef SI_FILE = new BytesRef("      file ");

        public override void Write(Directory dir, SegmentInfo si, FieldInfos fis, IOContext ioContext)
        {
            var segFileName = IndexFileNames.SegmentFileName(si.Name, "", SimpleTextSegmentInfoFormat.SI_EXTENSION);
            si.AddFile(segFileName);

            var success = false;
            var output = dir.CreateOutput(segFileName, ioContext);

            try
            {
                var scratch = new BytesRef();

                SimpleTextUtil.Write(output, SI_VERSION);
                SimpleTextUtil.Write(output, si.Version, scratch);
                SimpleTextUtil.WriteNewline(output);

                SimpleTextUtil.Write(output, SI_DOCCOUNT);
                SimpleTextUtil.Write(output, Convert.ToString(si.DocCount, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(output);

                SimpleTextUtil.Write(output, SI_USECOMPOUND);
                SimpleTextUtil.Write(output, Convert.ToString(si.UseCompoundFile, CultureInfo.InvariantCulture).ToLowerInvariant(), scratch);
                SimpleTextUtil.WriteNewline(output);

                IDictionary<string, string> diagnostics = si.Diagnostics;
                int numDiagnostics = diagnostics is null ? 0 : diagnostics.Count;
                SimpleTextUtil.Write(output, SI_NUM_DIAG);
                SimpleTextUtil.Write(output, Convert.ToString(numDiagnostics, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(output);

                if (numDiagnostics > 0)
                {
                    foreach (var diagEntry in diagnostics)
                    {
                        SimpleTextUtil.Write(output, SI_DIAG_KEY);
                        SimpleTextUtil.Write(output, diagEntry.Key, scratch);
                        SimpleTextUtil.WriteNewline(output);

                        SimpleTextUtil.Write(output, SI_DIAG_VALUE);
                        SimpleTextUtil.Write(output, diagEntry.Value, scratch);
                        SimpleTextUtil.WriteNewline(output);
                    }
                }

                var files = si.GetFiles();
                var numFiles = files is null ? 0 : files.Count;
                SimpleTextUtil.Write(output, SI_NUM_FILES);
                SimpleTextUtil.Write(output, Convert.ToString(numFiles, CultureInfo.InvariantCulture), scratch);
                SimpleTextUtil.WriteNewline(output);

                if (numFiles > 0)
                {
                    foreach (var fileName in files)
                    {
                        SimpleTextUtil.Write(output, SI_FILE);
                        SimpleTextUtil.Write(output, fileName, scratch);
                        SimpleTextUtil.WriteNewline(output);
                    }
                }

                SimpleTextUtil.WriteChecksum(output, scratch);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(output);
                    try
                    {
                        dir.DeleteFile(segFileName);
                    }
                    catch (Exception t) when (t.IsThrowable())
                    {
                        //Esnure we throw original exeception
                    }
                }
                else
                {
                    output.Dispose();
                }
            }
        }
    }
}