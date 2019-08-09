using ICU4N.Text;
using Lucene.Net.Support;
using System.Globalization;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// Mock of the original Lucene <see cref="PostingsHighlighter"/> that is backed
    /// by a <see cref="JdkBreakIterator"/> with custom rules to act
    /// (sort of) like the JDK. This is just to verify we can make the behavior work
    /// similar to the implementation in Lucene by customizing the <see cref="BreakIterator"/>.
    /// </summary>
    public class PostingsHighlighter : ICUPostingsHighlighter
    {
        public PostingsHighlighter()
            : base()
        {
        }

        public PostingsHighlighter(int maxLength)
            : base(maxLength)
        {
        }
        protected override BreakIterator GetBreakIterator(string field)
        {
            return JdkBreakIterator.GetSentenceInstance(CultureInfo.InvariantCulture);
        }
    }
}
