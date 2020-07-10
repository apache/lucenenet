#if FEATURE_BREAKITERATOR
using ICU4N.Text;
using System.Text;

namespace Lucene.Net.Search.VectorHighlight
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
    /// A <see cref="IBoundaryScanner"/> implementation that uses <see cref="BreakIterator"/> to find
    /// boundaries in the text.
    /// </summary>
    /// <seealso cref="BreakIterator"/>
    public class BreakIteratorBoundaryScanner : IBoundaryScanner
    {
        internal readonly BreakIterator bi;

        public BreakIteratorBoundaryScanner(BreakIterator bi)
        {
            this.bi = bi;
        }

        public virtual int FindStartOffset(StringBuilder buffer, int start)
        {
            // avoid illegal start offset
            if (start > buffer.Length || start < 1) return start;
            bi.SetText(buffer.ToString(0, start - 0));
            bi.Last();
            return bi.Previous();
        }

        public virtual int FindEndOffset(StringBuilder buffer, int start)
        {
            // avoid illegal start offset
            if (start > buffer.Length || start < 0) return start;
            bi.SetText(buffer.ToString(start, buffer.Length - start));
            return bi.Next() + start;
        }
    }
}
#endif