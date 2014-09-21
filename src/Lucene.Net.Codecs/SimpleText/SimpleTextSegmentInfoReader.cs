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

////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_DIAG_KEY;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_DIAG_VALUE;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_DOCCOUNT;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_FILE;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_NUM_DIAG;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_NUM_FILES;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_USECOMPOUND;
////JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to .NET:
//    import static Lucene.Net.Codecs.SimpleText.SimpleTextSegmentInfoWriter.SI_VERSION;


    using System;
    using System.Diagnostics;
    using System.Collections.Generic;

    using IndexFileNames = Index.IndexFileNames;
    using SegmentInfo = Index.SegmentInfo;
    using ChecksumIndexInput = Store.ChecksumIndexInput;
    using Directory = Store.Directory;
    using IOContext = Store.IOContext;
    using BytesRef = Util.BytesRef;
    using IOUtils = Util.IOUtils;
    using StringHelper = Util.StringHelper;

    /// <summary>
    /// reads plaintext segments files
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></B>
    /// @lucene.experimental
    /// </para>
    /// </summary>
    public class SimpleTextSegmentInfoReader : SegmentInfoReader
    {

        public override SegmentInfo Read(Directory directory, string segmentName, IOContext context)
        {
            BytesRef scratch = new BytesRef();
            string segFileName = IndexFileNames.SegmentFileName(segmentName, "",
                SimpleTextSegmentInfoFormat.SI_EXTENSION);
            ChecksumIndexInput input = directory.OpenChecksumInput(segFileName, context);
            bool success = false;
            try
            {
                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SI_VERSION));
                string version = ReadString(SI_VERSION.length, scratch);

                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SI_DOCCOUNT));
                int docCount = Convert.ToInt32(ReadString(SI_DOCCOUNT.length, scratch));

                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SI_USECOMPOUND));
                bool isCompoundFile = Convert.ToBoolean(ReadString(SI_USECOMPOUND.length, scratch));

                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SI_NUM_DIAG));
                int numDiag = Convert.ToInt32(ReadString(SI_NUM_DIAG.length, scratch));
                IDictionary<string, string> diagnostics = new Dictionary<string, string>();

                for (int i = 0; i < numDiag; i++)
                {
                    SimpleTextUtil.ReadLine(input, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, SI_DIAG_KEY));
                    string key = ReadString(SI_DIAG_KEY.length, scratch);

                    SimpleTextUtil.ReadLine(input, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, SI_DIAG_VALUE));
                    string value = ReadString(SI_DIAG_VALUE.length, scratch);
                    diagnostics[key] = value;
                }

                SimpleTextUtil.ReadLine(input, scratch);
                Debug.Assert(StringHelper.StartsWith(scratch, SI_NUM_FILES));
                int numFiles = Convert.ToInt32(ReadString(SI_NUM_FILES.length, scratch));
                HashSet<string> files = new HashSet<string>();

                for (int i = 0; i < numFiles; i++)
                {
                    SimpleTextUtil.ReadLine(input, scratch);
                    Debug.Assert(StringHelper.StartsWith(scratch, SI_FILE));
                    string fileName = ReadString(SI_FILE.length, scratch);
                    files.Add(fileName);
                }

                SimpleTextUtil.CheckFooter(input);

                SegmentInfo info = new SegmentInfo(directory, version, segmentName, docCount, isCompoundFile, null,
                    diagnostics);
                info.Files = files;
                success = true;
                return info;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(input);
                }
                else
                {
                    input.Close();
                }
            }
        }

        private string ReadString(int offset, BytesRef scratch)
        {
            return new string(scratch.Bytes, scratch.Offset + offset, scratch.Length - offset, StandardCharsets.UTF_8);
        }
    }
}