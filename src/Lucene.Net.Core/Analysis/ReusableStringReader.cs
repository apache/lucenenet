using System;

namespace Lucene.Net.Analysis
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
    /// Internal class to enable reuse of the string reader by <see cref="Analyzer.GetTokenStream(string, string)"/>
    /// </summary>
    public sealed class ReusableStringReader : System.IO.TextReader
    {
        private int pos = 0, size = 0;
        private string s = null;

        internal void SetValue(string s)
        {
            this.s = s;
            this.size = s.Length;
            this.pos = 0;
        }

        public override int Read()
        {
            if (pos < size)
            {
                return s[pos++];
            }
            else
            {
                s = null;
                return -1;
            }
        }

        public override int Read(char[] c, int off, int len)
        {
            if (pos < size)
            {
                len = Math.Min(len, size - pos);
                s.CopyTo(pos, c, off, pos + len - pos);
                pos += len;
                return len;
            }
            else
            {
                s = null;
                return -1;
            }
        }

        // LUCENENET-150: ReadToEnd() method required for .NET
        public override string ReadToEnd()
        {
            if (pos < size)
            {
                string result = s.Substring(pos);
                pos = size;
                return result;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            pos = size; // this prevents NPE when reading after close!
            s = null;

            base.Dispose(disposing);
        }
    }
}