using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Analysis.TokenAttributes;
using System.Text;

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
    /// A <see cref="TokenFilter"/> that replaces the term
    /// attribute with the reading of a token in either katakana or romaji form.
    /// The default reading form is katakana.
    /// </summary>
    public sealed class JapaneseReadingFormFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttr;
        private readonly IReadingAttribute readingAttr;

        private readonly StringBuilder buffer = new StringBuilder(); // LUCENENET: marked readonly
        private readonly bool useRomaji; // LUCENENET: marked readonly

        public JapaneseReadingFormFilter(TokenStream input, bool useRomaji)
            : base(input)
        {
            this.useRomaji = useRomaji;
            this.termAttr = AddAttribute<ICharTermAttribute>();
            this.readingAttr = AddAttribute<IReadingAttribute>();
        }

        public JapaneseReadingFormFilter(TokenStream input)
            : this(input, false)
        {
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                string reading = readingAttr.GetReading();

                if (useRomaji)
                {
                    if (reading is null)
                    {
                        // if its an OOV term, just try the term text
                        buffer.Length = 0;
                        ToStringUtil.GetRomanization(buffer, termAttr.ToString());
                        termAttr.SetEmpty().Append(buffer);
                    }
                    else
                    {
                        buffer.Length = 0;
                        ToStringUtil.GetRomanization(buffer, reading);
                        termAttr.SetEmpty().Append(buffer);
                    }
                }
                else
                {
                    // just replace the term text with the reading, if it exists
                    if (reading != null)
                    {
                        termAttr.SetEmpty().Append(reading);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
