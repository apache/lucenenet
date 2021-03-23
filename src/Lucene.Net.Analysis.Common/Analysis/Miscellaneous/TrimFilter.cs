// Lucene version compatibility level 4.8.1
using System;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Miscellaneous
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
    /// Trims leading and trailing whitespace from Tokens in the stream.
    /// <para>As of Lucene 4.4, this filter does not support updateOffsets=true anymore
    /// as it can lead to broken token streams.
    /// </para>
    /// </summary>
    public sealed class TrimFilter : TokenFilter
    {
        private readonly bool updateOffsets;
        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;

        /// <summary>
        /// Create a new <see cref="TrimFilter"/>. </summary>
        /// <param name="version">       the Lucene match version </param>
        /// <param name="in">            the stream to consume </param>
        /// <param name="updateOffsets"> whether to update offsets </param>
        /// @deprecated Offset updates are not supported anymore as of Lucene 4.4. 
        [Obsolete("Offset updates are not supported anymore as of Lucene 4.4.")]
        public TrimFilter(LuceneVersion version, TokenStream @in, bool updateOffsets)
            : base(@in)
        {
            if (updateOffsets && version.OnOrAfter(LuceneVersion.LUCENE_44))
            {
                throw new ArgumentException("updateOffsets=true is not supported anymore as of Lucene 4.4");
            }
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            this.updateOffsets = updateOffsets;
        }

        /// <summary>
        /// Create a new <see cref="TrimFilter"/> on top of <paramref name="in"/>. </summary>
        public TrimFilter(LuceneVersion version, TokenStream @in)
#pragma warning disable 612, 618
            : this(version, @in, false)
#pragma warning restore 612, 618
        {
        }

        public override bool IncrementToken()
        {
            if (!m_input.IncrementToken())
            {
                return false;
            }

            char[] termBuffer = termAtt.Buffer;
            int len = termAtt.Length;
            //TODO: Is this the right behavior or should we return false?  Currently, "  ", returns true, so I think this should
            //also return true
            if (len == 0)
            {
                return true;
            }
            int start; // LUCENENET: IDE0059: Remove unnecessary value assignment
            int end; // LUCENENET: IDE0059: Remove unnecessary value assignment
            int endOff = 0;

            // eat the first characters
            for (start = 0; start < len && char.IsWhiteSpace(termBuffer[start]); start++)
            {
            }
            // eat the end characters
            for (end = len; end >= start && char.IsWhiteSpace(termBuffer[end - 1]); end--)
            {
                endOff++;
            }
            if (start > 0 || end < len)
            {
                if (start < end)
                {
                    termAtt.CopyBuffer(termBuffer, start, (end - start));
                }
                else
                {
                    termAtt.SetEmpty();
                }
                if (updateOffsets && len == offsetAtt.EndOffset - offsetAtt.StartOffset)
                {
                    int newStart = offsetAtt.StartOffset + start;
                    int newEnd = offsetAtt.EndOffset - (start < end ? endOff : 0);
                    offsetAtt.SetOffset(newStart, newEnd);
                }
            }

            return true;
        }
    }
}