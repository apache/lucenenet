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

    /// <summary>
    /// Plain text segments file format.
    /// <para>
    /// <b><font color="red">FOR RECREATIONAL USE ONLY</font></b>
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class SimpleTextSegmentInfoFormat : SegmentInfoFormat
    {
        private readonly SegmentInfoReader _reader = new SimpleTextSegmentInfoReader();
        private readonly SegmentInfoWriter _writer = new SimpleTextSegmentInfoWriter();

        public const string SI_EXTENSION = "si";

        public override SegmentInfoReader SegmentInfoReader => _reader;

        public override SegmentInfoWriter SegmentInfoWriter => _writer;
    }
}