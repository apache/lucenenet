using System;
using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// HTML Parsing Interface for test purposes.
    /// </summary>
    public interface IHTMLParser
    {
        /// <summary>
        /// Parse the input TextReader and return DocData.
        /// The provided name, title, date are used for the result, unless when they're null, 
        /// in which case an attempt is made to set them from the parsed data.
        /// </summary>
        /// <param name="docData">Result reused.</param>
        /// <param name="name">Name of the result doc data.</param>
        /// <param name="date">Date of the result doc data. If null, attempt to set by parsed data.</param>
        /// <param name="reader">Reader of html text to parse.</param>
        /// <param name="trecSrc">The <see cref="TrecContentSource"/> used to parse dates.</param>
        /// <returns>Parsed doc data.</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        DocData Parse(DocData docData, string name, DateTime? date, TextReader reader, TrecContentSource trecSrc);
    }
}
