using System.Text;

namespace Lucene.Net.Search.Highlight
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

    /// <summary> Simple <see cref="IFormatter"/> implementation to highlight terms with a pre and post tag</summary>
    /// <author>  MAHarwood </author>
    public class SimpleHTMLFormatter : IFormatter
    {
        private const string DEFAULT_PRE_TAG = "<B>";
        private const string DEFAULT_POST_TAG = "</B>";

        internal string preTag;
        internal string postTag;

        public SimpleHTMLFormatter(string preTag, string postTag)
        {
            this.preTag = preTag;
            this.postTag = postTag;
        }

        /// <summary> 
        /// Default constructor uses HTML: &lt;B&gt; tags to markup terms
        /// </summary>
        public SimpleHTMLFormatter() : this(DEFAULT_PRE_TAG, DEFAULT_POST_TAG) { }

        /// <summary>
        /// <seealso cref="IFormatter.HighlightTerm(string, TokenGroup)"/>
        /// </summary>
        public virtual string HighlightTerm(string originalText, TokenGroup tokenGroup)
        {
            if (tokenGroup.TotalScore <= 0)
            {
                return originalText;
            }

            // Allocate StringBuilder with the right number of characters from the
            // beginning, to avoid char[] allocations in the middle of appends.
            StringBuilder returnBuffer = new StringBuilder(preTag.Length + originalText.Length + postTag.Length);           
            returnBuffer.Append(preTag);
            returnBuffer.Append(originalText);
            returnBuffer.Append(postTag);
            return returnBuffer.ToString();
        }
    }
}
