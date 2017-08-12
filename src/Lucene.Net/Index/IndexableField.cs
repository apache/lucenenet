using System.IO;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    // TODO: how to handle versioning here...?

    // TODO: we need to break out separate StoredField...

    /// <summary>
    /// Represents a single field for indexing. <see cref="IndexWriter"/>
    /// consumes IEnumerable&lt;IndexableField&gt; as a document.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public interface IIndexableField
    {
        /// <summary>
        /// Field name </summary>
        string Name { get; }

        /// <summary>
        /// <see cref="IIndexableFieldType"/> describing the properties
        /// of this field.
        /// </summary>
        // LUCENENET specific: Renamed from FieldType so we can use that name
        // on the Field class and return FieldType instead of IIndexableFieldType
        // to avoid a bunch of casting. In Java, it compiles when you implement this
        // property with a class that derives from IIndexableFieldType, but in .NET it
        // does not. 
        IIndexableFieldType IndexableFieldType { get; }

        /// <summary>
        /// Returns the field's index-time boost.
        /// <para/>
        /// Only fields can have an index-time boost, if you want to simulate
        /// a "document boost", then you must pre-multiply it across all the
        /// relevant fields yourself.
        /// <para/>
        /// The boost is used to compute the norm factor for the field.  By
        /// default, in the <see cref="Search.Similarities.Similarity.ComputeNorm(FieldInvertState)"/> method,
        /// the boost value is multiplied by the length normalization factor and then
        /// rounded by <see cref="Search.Similarities.DefaultSimilarity.EncodeNormValue(float)"/> before it is stored in the
        /// index.  One should attempt to ensure that this product does not overflow
        /// the range of that encoding.
        /// <para/>
        /// It is illegal to return a boost other than 1.0f for a field that is not
        /// indexed (<see cref="IIndexableFieldType.IsIndexed"/> is false) or omits normalization values
        /// (<see cref="IIndexableFieldType.OmitNorms"/> returns true).
        /// </summary>
        /// <seealso cref="Search.Similarities.Similarity.ComputeNorm(FieldInvertState)"/>
        /// <seealso cref="Search.Similarities.DefaultSimilarity.EncodeNormValue(float)"/>
        float Boost { get; }

        /// <summary>
        /// Non-null if this field has a binary value </summary>
        BytesRef GetBinaryValue();

        /// <summary>
        /// Non-null if this field has a string value </summary>
        string GetStringValue();

        /// <summary>
        /// Non-null if this field has a <see cref="TextReader"/> value </summary>
        TextReader GetReaderValue();

        /// <summary>
        /// Non-null if this field has a numeric value </summary>
        object GetNumericValue(); // LUCENENET TODO: Can we eliminate object?

        /// <summary>
        /// Creates the <see cref="TokenStream"/> used for indexing this field.  If appropriate,
        /// implementations should use the given <see cref="Analyzer"/> to create the <see cref="TokenStream"/>s.
        /// </summary>
        /// <param name="analyzer"> <see cref="Analyzer"/> that should be used to create the <see cref="TokenStream"/>s from </param>
        /// <returns> <see cref="TokenStream"/> value for indexing the document.  Should always return
        ///         a non-null value if the field is to be indexed </returns>
        /// <exception cref="IOException"> Can be thrown while creating the <see cref="TokenStream"/> </exception>
        TokenStream GetTokenStream(Analyzer analyzer);
    }
}