using System.Collections.Concurrent;
using System.Text;
#nullable enable

namespace Lucene.Net.Support.Text
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
    /// Extension methods for <see cref="Encoding"/>.
    /// </summary>
    internal static class EncodingExtensions
    {
        private static readonly ConcurrentDictionary<Encoding, Encoding> decoderExceptionFallbackCache = new();

        /// <summary>
        /// Returns an <see cref="Encoding"/> instance with the <see cref="DecoderFallback"/> set to throw
        /// an exception when an invalid byte sequence is encountered.
        /// <para />
        /// This is equivalent to Java's <c>CodingErrorAction.REPORT</c> for both <c>onMalformedInput</c> and
        /// <c>onUnmappableCharacter</c> and will throw a <see cref="DecoderFallbackException"/> when failing
        /// to decode a string. This exception is equivalent to Java's <c>CharacterCodingException</c>, which is
        /// a base exception type for both <c>MalformedInputException</c> and <c>UnmappableCharacterException</c>.
        /// Thus, to translate Java code that catches any of those exceptions, you can catch
        /// <see cref="DecoderFallbackException"/>.
        /// </summary>
        /// <param name="encoding">The encoding to clone and set the fallback on.</param>
        /// <returns>An <see cref="Encoding"/> instance with the fallback set to throw an exception.</returns>
        /// <remarks>
        /// Note that it is necessary to clone the <see cref="Encoding"/> instance because
        /// the <see cref="Encoding.DecoderFallback"/> property is read-only without cloning.
        /// </remarks>
        public static Encoding WithDecoderExceptionFallback(this Encoding encoding)
        {
            return decoderExceptionFallbackCache.GetOrAdd(encoding, static e =>
            {
                Encoding newEncoding = (Encoding)e.Clone();
                newEncoding.DecoderFallback = DecoderFallback.ExceptionFallback;
                return newEncoding;
            });
        }
    }
}
