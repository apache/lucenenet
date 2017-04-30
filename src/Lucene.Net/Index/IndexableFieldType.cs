using Lucene.Net.Documents;

namespace Lucene.Net.Index
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
    /// Describes the properties of a field.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IIndexableFieldType
    {
        /// <summary>
        /// <c>true</c> if this field should be indexed (inverted) </summary>
        bool IsIndexed { get; }

        /// <summary>
        /// <c>true</c> if the field's value should be stored </summary>
        bool IsStored { get; }

        /// <summary>
        /// <c>true</c> if this field's value should be analyzed by the
        /// <see cref="Analysis.Analyzer"/>.
        /// <para/>
        /// This has no effect if <see cref="IsIndexed"/> returns <c>false</c>.
        /// </summary>
        bool IsTokenized { get; }

        /// <summary>
        /// <c>true</c> if this field's indexed form should be also stored
        /// into term vectors.
        /// <para/>
        /// this builds a miniature inverted-index for this field which
        /// can be accessed in a document-oriented way from
        /// <see cref="IndexReader.GetTermVector(int, string)"/>.
        /// <para/>
        /// This option is illegal if <see cref="IsIndexed"/> returns <c>false</c>.
        /// </summary>
        bool StoreTermVectors { get; }

        /// <summary>
        /// <c>true</c> if this field's token character offsets should also
        /// be stored into term vectors.
        /// <para/>
        /// This option is illegal if term vectors are not enabled for the field
        /// (<see cref="StoreTermVectors"/> is <c>false</c>)
        /// </summary>
        bool StoreTermVectorOffsets { get; }

        /// <summary>
        /// <c>true</c> if this field's token positions should also be stored
        /// into the term vectors.
        /// <para/>
        /// This option is illegal if term vectors are not enabled for the field
        /// (<see cref="StoreTermVectors"/> is <c>false</c>).
        /// </summary>
        bool StoreTermVectorPositions { get; }

        /// <summary>
        /// <c>true</c> if this field's token payloads should also be stored
        /// into the term vectors.
        /// <para/>
        /// This option is illegal if term vector positions are not enabled
        /// for the field (<see cref="StoreTermVectors"/> is <c>false</c>).
        /// </summary>
        bool StoreTermVectorPayloads { get; }

        /// <summary>
        /// <c>true</c> if normalization values should be omitted for the field.
        /// <para/>
        /// This saves memory, but at the expense of scoring quality (length normalization
        /// will be disabled), and if you omit norms, you cannot use index-time boosts.
        /// </summary>
        bool OmitNorms { get; }

        /// <summary>
        /// <see cref="Index.IndexOptions"/>, describing what should be
        /// recorded into the inverted index
        /// </summary>
        IndexOptions IndexOptions { get; }

        /// <summary>
        /// DocValues <see cref="DocValuesType"/>: if not <see cref="DocValuesType.NONE"/> then the field's value
        /// will be indexed into docValues.
        /// </summary>
        DocValuesType DocValueType { get; }
    }
}