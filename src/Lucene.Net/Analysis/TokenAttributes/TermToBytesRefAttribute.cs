using Lucene.Net.Util;

namespace Lucene.Net.Analysis.TokenAttributes
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// This attribute is requested by TermsHashPerField to index the contents.
    /// This attribute can be used to customize the final byte[] encoding of terms.
    /// <para/>
    /// Consumers of this attribute call <see cref="BytesRef"/> up-front, and then
    /// invoke <see cref="FillBytesRef()"/> for each term. Example:
    /// <code>
    ///   TermToBytesRefAttribute termAtt = tokenStream.GetAttribute&lt;TermToBytesRefAttribute&gt;;
    ///   BytesRef bytes = termAtt.BytesRef;
    ///
    ///   while (tokenStream.IncrementToken()
    ///   {
    ///     // you must call termAtt.FillBytesRef() before doing something with the bytes.
    ///     // this encodes the term value (internally it might be a char[], etc) into the bytes.
    ///     int hashCode = termAtt.FillBytesRef();
    ///
    ///     if (IsInteresting(bytes))
    ///     {
    ///       // because the bytes are reused by the attribute (like ICharTermAttribute's char[] buffer),
    ///       // you should make a copy if you need persistent access to the bytes, otherwise they will
    ///       // be rewritten across calls to IncrementToken()
    ///
    ///       DoSomethingWith(new BytesRef(bytes));
    ///     }
    ///   }
    ///   ...
    /// </code>
    /// @lucene.experimental this is a very expert API, please use
    /// <see cref="CharTermAttribute"/> and its implementation of this method
    /// for UTF-8 terms.
    /// </summary>
    public interface ITermToBytesRefAttribute : IAttribute
    {
        /// <summary>
        /// Updates the bytes <see cref="Util.BytesRef"/> to contain this term's
        /// final encoding.
        /// </summary>
        void FillBytesRef();

        /// <summary>
        /// Retrieve this attribute's <see cref="Util.BytesRef"/>. The bytes are updated
        /// from the current term when the consumer calls <see cref="FillBytesRef()"/>.
        /// </summary>
        /// <returns> this <see cref="Util.IAttribute"/>s internal <see cref="Util.BytesRef"/>. </returns>
        BytesRef BytesRef { get; }
    }
}