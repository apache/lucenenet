using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Simple boundary scanner implementation that divides fragments
    /// based on a set of separator characters.
    /// </summary>
    public class SimpleBoundaryScanner : IBoundaryScanner
    {
        public static readonly int DEFAULT_MAX_SCAN = 20;
        public static readonly char[] DEFAULT_BOUNDARY_CHARS = { '.', ',', '!', '?', ' ', '\t', '\n' };

        protected int m_maxScan;
        protected ISet<char> m_boundaryChars;

        public SimpleBoundaryScanner()
            : this(DEFAULT_MAX_SCAN, DEFAULT_BOUNDARY_CHARS)
        {
        }

        public SimpleBoundaryScanner(int maxScan)
            : this(maxScan, DEFAULT_BOUNDARY_CHARS)
        {
        }

        public SimpleBoundaryScanner(char[] boundaryChars)
            : this(DEFAULT_MAX_SCAN, boundaryChars)
        {
        }

        public SimpleBoundaryScanner(int maxScan, char[] boundaryChars)
        {
            this.m_maxScan = maxScan;
            this.m_boundaryChars = new JCG.HashSet<char>();
            this.m_boundaryChars.UnionWith(boundaryChars);
        }

        public SimpleBoundaryScanner(int maxScan, ISet<char> boundaryChars)
        {
            this.m_maxScan = maxScan;
            this.m_boundaryChars = boundaryChars;
        }

        public virtual int FindStartOffset(StringBuilder buffer, int start)
        {
            // avoid illegal start offset
            if (start > buffer.Length || start < 1) return start;
            int offset, count = m_maxScan;
            for (offset = start; offset > 0 && count > 0; count--)
            {
                // found?
                if (m_boundaryChars.Contains(buffer[offset - 1])) return offset;
                offset--;
            }
            // if we scanned up to the start of the text, return it, its a "boundary"
            if (offset == 0)
            {
                return 0;
            }
            // not found
            return start;
        }

        public virtual int FindEndOffset(StringBuilder buffer, int start)
        {
            // avoid illegal start offset
            if (start > buffer.Length || start < 0) return start;
            int offset, count = m_maxScan;
            //for( offset = start; offset <= buffer.length() && count > 0; count-- ){
            for (offset = start; offset < buffer.Length && count > 0; count--)
            {
                // found?
                if (m_boundaryChars.Contains(buffer[offset])) return offset;
                offset++;
            }
            // not found
            return start;
        }
    }
}
