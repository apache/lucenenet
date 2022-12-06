using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

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
    using IndexOptions = Lucene.Net.Index.IndexOptions;
    using IOContext = Store.IOContext;
    using IOUtils = Util.IOUtils;
    using StringHelper = Util.StringHelper;

    /// <summary>
    /// Reads plain text field infos files.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextFieldInfosReader : FieldInfosReader
    {
        public override FieldInfos Read(Directory directory, string segmentName, string segmentSuffix,
            IOContext iocontext)
        {
            var fileName = IndexFileNames.SegmentFileName(segmentName, segmentSuffix,
                SimpleTextFieldInfosWriter.FIELD_INFOS_EXTENSION);
            var input = directory.OpenChecksumInput(fileName, iocontext);
            var scratch = new BytesRef();

            var success = false;
            try
            {

                SimpleTextUtil.ReadLine(input, scratch);
                if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NUMFIELDS));
                var size = Convert.ToInt32(ReadString(SimpleTextFieldInfosWriter.NUMFIELDS.Length, scratch), CultureInfo.InvariantCulture);
                var infos = new FieldInfo[size];

                for (var i = 0; i < size; i++)
                {
                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NAME));
                    string name = ReadString(SimpleTextFieldInfosWriter.NAME.Length, scratch);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NUMBER));
                    int fieldNumber = Convert.ToInt32(ReadString(SimpleTextFieldInfosWriter.NUMBER.Length, scratch), CultureInfo.InvariantCulture);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.ISINDEXED));
                    bool isIndexed = Convert.ToBoolean(ReadString(SimpleTextFieldInfosWriter.ISINDEXED.Length, scratch), CultureInfo.InvariantCulture);

                    IndexOptions indexOptions;
                    if (isIndexed)
                    {
                        SimpleTextUtil.ReadLine(input, scratch);
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.INDEXOPTIONS));
                        indexOptions = (IndexOptions)Enum.Parse(typeof(IndexOptions), ReadString(SimpleTextFieldInfosWriter.INDEXOPTIONS.Length,
                                scratch));
                    }
                    else
                    {
                        indexOptions = IndexOptions.NONE;
                    }

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.STORETV));
                    bool storeTermVector =
                        Convert.ToBoolean(ReadString(SimpleTextFieldInfosWriter.STORETV.Length, scratch), CultureInfo.InvariantCulture);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.PAYLOADS));
                    bool storePayloads =
                        Convert.ToBoolean(ReadString(SimpleTextFieldInfosWriter.PAYLOADS.Length, scratch), CultureInfo.InvariantCulture);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NORMS));
                    bool omitNorms = !Convert.ToBoolean(ReadString(SimpleTextFieldInfosWriter.NORMS.Length, scratch), CultureInfo.InvariantCulture);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NORMS_TYPE));
                    string nrmType = ReadString(SimpleTextFieldInfosWriter.NORMS_TYPE.Length, scratch);
                    Index.DocValuesType normsType = DocValuesType(nrmType);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.DOCVALUES));
                    string dvType = ReadString(SimpleTextFieldInfosWriter.DOCVALUES.Length, scratch);
                    Index.DocValuesType docValuesType = DocValuesType(dvType);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.DOCVALUES_GEN));
                    long dvGen = Convert.ToInt64(ReadString(SimpleTextFieldInfosWriter.DOCVALUES_GEN.Length, scratch), CultureInfo.InvariantCulture);

                    SimpleTextUtil.ReadLine(input, scratch);
                    if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.NUM_ATTS));
                    int numAtts = Convert.ToInt32(ReadString(SimpleTextFieldInfosWriter.NUM_ATTS.Length, scratch), CultureInfo.InvariantCulture);
                    IDictionary<string, string> atts = new Dictionary<string, string>();

                    for (int j = 0; j < numAtts; j++)
                    {
                        SimpleTextUtil.ReadLine(input, scratch);
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.ATT_KEY));
                        string key = ReadString(SimpleTextFieldInfosWriter.ATT_KEY.Length, scratch);

                        SimpleTextUtil.ReadLine(input, scratch);
                        if (Debugging.AssertsEnabled) Debugging.Assert(StringHelper.StartsWith(scratch, SimpleTextFieldInfosWriter.ATT_VALUE));
                        string value = ReadString(SimpleTextFieldInfosWriter.ATT_VALUE.Length, scratch);
                        atts[key] = value;
                    }

                    infos[i] = new FieldInfo(name, isIndexed, fieldNumber, storeTermVector, omitNorms, storePayloads,
                        indexOptions, docValuesType, normsType, new ReadOnlyDictionary<string,string>(atts))
                    {
                        DocValuesGen = dvGen
                    };
                }

                SimpleTextUtil.CheckFooter(input);

                var fieldInfos = new FieldInfos(infos);
                success = true;
                return fieldInfos;
            }
            finally
            {
                if (success)
                {
                    input.Dispose();
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(input);
                }
            }
        }

        public virtual Index.DocValuesType DocValuesType(string dvType)
        {
            return "false".Equals(dvType, StringComparison.Ordinal) ? Index.DocValuesType.NONE : (Index.DocValuesType)Enum.Parse(typeof(Index.DocValuesType), dvType, true);
        }

        private static string ReadString(int offset, BytesRef scratch) // LUCENENET: CA1822: Mark members as static
        {
            return Encoding.UTF8.GetString(scratch.Bytes, scratch.Offset + offset, scratch.Length - offset);
        }
    }
}