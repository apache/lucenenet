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
        private int _pos = 0, _size = 0;
        private string _s = null;

        public string Value
        {
            set
            {
                this._s = value;
                this._size = value.Length;
                this._pos = 0;
            }
        }

        public override int Read()
        {
            if (_pos < _size)
            {
                return _s[_pos++];
            }
            else
            {
                _s = null;
                return -1;
            }
        }

        public override int Read(char[] c, int off, int len)
        {
            if (_pos < _size)
            {
                len = Math.Min(len, _size - _pos);
                _s.CopyTo(_pos, c, off, _pos + len - _pos);
                _pos += len;
                return len;
            }
            else
            {
                _s = null;
                return -1;
            }
        }

        protected override void Dispose(bool disposing)
        {
            _pos = _size; // this prevents NPE when reading after close!
            _s = null;

            base.Dispose(disposing);
        }
    }
}