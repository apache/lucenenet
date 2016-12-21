using Lucene.Net.Search.Similarities;

namespace Lucene.Net.Index
{
    using System.IO;

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

    // javadocs
    // javadocs
    using BytesRef = Lucene.Net.Util.BytesRef;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    // TODO: how to handle versioning here...?

    // TODO: we need to break out separate StoredField...

    /// <summary>
    /// Represents a single field for indexing.  IndexWriter
    ///  consumes Iterable&lt;IndexableField&gt; as a document.
    ///
    ///  @lucene.experimental
    /// </summary>

    public interface IIndexableField
    {
        /// <summary>
        /// Field name </summary>
        string Name { get; }

        /// <summary>
        /// <seealso cref="IIndexableFieldType"/> describing the properties
        /// of this field.
        /// </summary>
        IIndexableFieldType FieldType { get; }

        /// <summary>
        /// Returns the field's index-time boost.
        /// <p>
        /// Only fields can have an index-time boost, if you want to simulate
        /// a "document boost", then you must pre-multiply it across all the
        /// relevant fields yourself.
        /// <p>The boost is used to compute the norm factor for the field.  By
        /// default, in the <seealso cref="Similarity#computeNorm(FieldInvertState)"/> method,
        /// the boost value is multiplied by the length normalization factor and then
        /// rounded by <seealso cref="DefaultSimilarity#encodeNormValue(float)"/> before it is stored in the
        /// index.  One should attempt to ensure that this product does not overflow
        /// the range of that encoding.
        /// <p>
        /// It is illegal to return a boost other than 1.0f for a field that is not
        /// indexed (<seealso cref="IIndexableFieldType#indexed()"/> is false) or omits normalization values
        /// (<seealso cref="IIndexableFieldType#omitNorms()"/> returns true).
        /// </summary>
        /// <seealso cref= Similarity#computeNorm(FieldInvertState) </seealso>
        /// <seealso cref= DefaultSimilarity#encodeNormValue(float) </seealso>
        float Boost { get; }

        /// <summary>
        /// Non-null if this field has a binary value </summary>
        BytesRef GetBinaryValue();

        /// <summary>
        /// Non-null if this field has a string value </summary>
        string GetStringValue();

        /// <summary>
        /// Non-null if this field has a TextReader value </summary>
        TextReader GetReaderValue();

        /// <summary>
        /// Non-null if this field has a numeric value </summary>
        object GetNumericValue(); // LUCENENET TODO: Can we eliminate object?

        /// <summary>
        /// Creates the TokenStream used for indexing this field.  If appropriate,
        /// implementations should use the given Analyzer to create the TokenStreams.
        /// </summary>
        /// <param name="analyzer"> Analyzer that should be used to create the TokenStreams from </param>
        /// <returns> TokenStream value for indexing the document.  Should always return
        ///         a non-null value if the field is to be indexed </returns>
        /// <exception cref="IOException"> Can be thrown while creating the TokenStream </exception>
        TokenStream GetTokenStream(Analyzer analyzer);
    }
}