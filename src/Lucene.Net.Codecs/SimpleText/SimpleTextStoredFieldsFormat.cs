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

    using FieldInfos = Index.FieldInfos;
    using SegmentInfo = Index.SegmentInfo;
    using Directory = Store.Directory;
    using IOContext = Store.IOContext;

    /// <summary>
    /// Plain text stored fields format.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextStoredFieldsFormat : StoredFieldsFormat
    {
        public override StoredFieldsReader FieldsReader(Directory directory, SegmentInfo si, FieldInfos fn,
            IOContext context)
        {
            return new SimpleTextStoredFieldsReader(directory, si, fn, context);
        }

        public override StoredFieldsWriter FieldsWriter(Directory directory, SegmentInfo si, IOContext context)
        {
            return new SimpleTextStoredFieldsWriter(directory, si.Name, context);
        }
    }
}