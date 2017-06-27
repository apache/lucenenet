// commons-codec version compatibility level: 1.9
namespace Lucene.Net.Analysis.Phonetic.Language
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
    /// Defines common encoding methods for <see cref="string"/> encoders.
    /// </summary>
    public interface IStringEncoder
    {
        /// <summary>
        /// Encodes a <see cref="string"/> and returns a <see cref="string"/>.
        /// </summary>
        /// <param name="source">the <see cref="string"/> to encode</param>
        /// <returns>the encoded <see cref="string"/></returns>
        // LUCENENET specific - EncoderException not ported, as it was only thrown on a coversion from object to string type
        // <exception cref="EncoderException">thrown if there is an error condition during the encoding process.</exception>
        string Encode(string source);
    }
}
