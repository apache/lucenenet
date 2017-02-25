namespace Lucene.Net.Codecs.Memory
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

    using SegmentReadState = Index.SegmentReadState;
	using SegmentWriteState = Index.SegmentWriteState;
	using ArrayUtil = Util.ArrayUtil;

    /// <summary>
    /// In-memory docvalues format that does no (or very little)
    ///  compression.  Indexed values are stored on disk, but
    ///  then at search time all values are loaded into memory as
    ///  simple java arrays.  For numeric values, it uses
    ///  byte[], short[], int[], long[] as necessary to fit the
    ///  range of the values.  For binary values, there is an int
    ///  (4 bytes) overhead per value.
    /// 
    ///  <para>Limitations:
    ///  <ul>
    ///    <li>For binary and sorted fields the total space
    ///        required for all binary values cannot exceed about
    ///        2.1 GB (see #MAX_TOTAL_BYTES_LENGTH).</li>
    /// 
    ///    <li>For sorted set fields, the sum of the size of each
    ///        document's set of values cannot exceed about 2.1 B
    ///        values (see #MAX_SORTED_SET_ORDS).  For example,
    ///        if every document has 10 values (10 instances of
    ///        <seealso cref="SortedSetDocValuesField"/>) added, then no
    ///        more than ~210 M documents can be added to one
    ///        segment. </li>
    ///  </ul> 
    /// </para>
    /// </summary>
    public class DirectDocValuesFormat : DocValuesFormat
    {
        /// <summary>
        /// The sum of all byte lengths for binary field, or for
        ///  the unique values in sorted or sorted set fields, cannot
        ///  exceed this. 
        /// </summary>
        public static readonly int MAX_TOTAL_BYTES_LENGTH = ArrayUtil.MAX_ARRAY_LENGTH;

        /// <summary>
        /// The sum of the number of values across all documents
        ///  in a sorted set field cannot exceed this. 
        /// </summary>
        public static readonly int MAX_SORTED_SET_ORDS = ArrayUtil.MAX_ARRAY_LENGTH;

        /// <summary>
        /// Sole constructor. </summary>
        public DirectDocValuesFormat() 
            : base()
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            return new DirectDocValuesConsumer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            return new DirectDocValuesProducer(state, DATA_CODEC, DATA_EXTENSION, METADATA_CODEC, METADATA_EXTENSION);
        }

        internal static readonly string DATA_CODEC = "DirectDocValuesData";
        internal static readonly string DATA_EXTENSION = "dvdd";
        internal static readonly string METADATA_CODEC = "DirectDocValuesMetadata";
        internal static readonly string METADATA_EXTENSION = "dvdm";
    }
}