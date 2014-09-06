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
    /// Internal class to enable reuse of the string reader by <seealso cref="Analyzer#tokenStream(String,String)"/> </summary>
    public sealed class ReusableStringReader : System.IO.TextReader
    {
        private int Pos = 0, Size = 0;
        private string s = null;

        public ReusableStringReader()
        {
        }

        public string Value
        {
            set
            {
                this.s = value;
                this.Size = value.Length;
                this.Pos = 0;
            }
        }

        public override int Read()
        {
            if (Pos < Size)
            {
                return s[Pos++];
            }
            else
            {
                s = null;
                return -1;
            }
        }

        public override int Read(char[] c, int off, int len)
        {
            if (Pos < Size)
            {
                len = Math.Min(len, Size - Pos);
                s.CopyTo(Pos, c, off, Pos + len - Pos);
                Pos += len;
                return len;
            }
            else
            {
                s = null;
                return -1;
            }
        }

        public override void Close()
        {
            Pos = Size; // this prevents NPE when reading after close!
            s = null;
        }
    }
}