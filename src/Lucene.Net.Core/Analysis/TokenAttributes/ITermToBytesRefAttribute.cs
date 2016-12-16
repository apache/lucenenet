namespace Lucene.Net.Analysis.TokenAttributes
{
    using Lucene.Net.Util;

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
    /// this attribute is requested by TermsHashPerField to index the contents.
    /// this attribute can be used to customize the final byte[] encoding of terms.
    /// <p>
    /// Consumers of this attribute call <seealso cref="#getBytesRef()"/> up-front, and then
    /// invoke <seealso cref="#fillBytesRef()"/> for each term. Example:
    /// <pre class="prettyprint">
    ///   final TermToBytesRefAttribute termAtt = tokenStream.getAttribute(TermToBytesRefAttribute.class);
    ///   final BytesRef bytes = termAtt.getBytesRef();
    ///
    ///   while (tokenStream.IncrementToken() {
    ///
    ///     // you must call termAtt.fillBytesRef() before doing something with the bytes.
    ///     // this encodes the term value (internally it might be a char[], etc) into the bytes.
    ///     int hashCode = termAtt.fillBytesRef();
    ///
    ///     if (isInteresting(bytes)) {
    ///
    ///       // because the bytes are reused by the attribute (like CharTermAttribute's char[] buffer),
    ///       // you should make a copy if you need persistent access to the bytes, otherwise they will
    ///       // be rewritten across calls to IncrementToken()
    ///
    ///       doSomethingWith(new BytesRef(bytes));
    ///     }
    ///   }
    ///   ...
    /// </pre>
    /// @lucene.experimental this is a very expert API, please use
    /// <seealso cref="CharTermAttributeImpl"/> and its implementation of this method
    /// for UTF-8 terms.
    /// </summary>
    public interface ITermToBytesRefAttribute : IAttribute
    {
        /// <summary>
        /// Updates the bytes <seealso cref="#getBytesRef()"/> to contain this term's
        /// final encoding.
        /// </summary>
        void FillBytesRef();

        /// <summary>
        /// Retrieve this attribute's BytesRef. The bytes are updated
        /// from the current term when the consumer calls <seealso cref="#fillBytesRef()"/>. </summary>
        /// <returns> this Attributes internal BytesRef. </returns>
        BytesRef BytesRef { get; }
    }
}