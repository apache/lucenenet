using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ja
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
    /// Removes tokens that match a set of part-of-speech tags.
    /// </summary>
    public sealed class JapanesePartOfSpeechStopFilter : FilteringTokenFilter
    {
        private readonly ISet<string> stopTags;
        private readonly IPartOfSpeechAttribute posAtt;

        [Obsolete("EnablePositionIncrements=false is not supported anymore as of Lucene 4.4.")]
        public JapanesePartOfSpeechStopFilter(LuceneVersion version, bool enablePositionIncrements, TokenStream input, ISet<string> stopTags)
                  : base(version, enablePositionIncrements, input)
        {
            this.stopTags = stopTags;
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
        }

        /// <summary>
        /// Create a new <see cref="JapanesePartOfSpeechStopFilter"/>.
        /// </summary>
        /// <param name="version">The Lucene match version.</param>
        /// <param name="input">The <see cref="TokenStream"/> to consume.</param>
        /// <param name="stopTags">The part-of-speech tags that should be removed.</param>
        public JapanesePartOfSpeechStopFilter(LuceneVersion version, TokenStream input, ISet<string> stopTags)
            : base(version, input)
        {
            this.stopTags = stopTags;
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
        }

        protected override bool Accept()
        {
            string pos = posAtt.GetPartOfSpeech();
            return pos is null || !stopTags.Contains(pos);
        }
    }
}
