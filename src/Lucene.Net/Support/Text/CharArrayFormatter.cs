namespace Lucene.Net.Support.Text
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
    /// LUCENENET specific simple formatter to pass a value to
    /// <see cref="Lucene.Net.Diagnostics.Debugging.Assert{T0}(bool, string, T0)"/>
    /// in order to defer allocating until the assert fails.
    /// </summary>
    internal struct CharArrayFormatter
    {
        private char[] value;
        private int startIndex;
        private int length;
        public CharArrayFormatter(char[] value, int startIndex, int length)
        {
            this.value = value;
            this.startIndex = startIndex;
            this.length = length;
        }

        public override string ToString()
        {
            return new string(value, startIndex, length);
        }
    }
}
