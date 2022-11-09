// Lucene version compatibility level 8.2.0
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Analysis.Morfologik.TokenAttributes
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
    /// Morfologik provides morphosyntactic annotations for
    /// surface forms. For the exact format and description of these,
    /// see the project's documentation.
    /// </summary>
    public interface IMorphosyntacticTagsAttribute : IAttribute
    {
        /// <summary>
        /// Gets or sets the POS tag of the term. A single word may have multiple POS tags,
        /// depending on the interpretation (context disambiguation is typically needed
        /// to determine which particular tag is appropriate).
        /// <para/>
        /// The default value (no-value) is null. Returns a list of POS tags corresponding to current lemma.
        /// </summary>
        IList<StringBuilder> Tags { get; set; }

        /// <summary>Clear to default value.</summary>
        void Clear();
    }
}
