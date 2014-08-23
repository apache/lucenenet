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

namespace Java.Lang
{
    using System;
    using System.Collections.Generic;

    public class String : IEnumerable<char>
    {
        private readonly char[] value;


        public String()
        {
            this.value = new char[0];
        }

        public String(string value)
        {
            this.value = value.ToCharArray();
        }

        public String(char[] value)
        {
            this.value = value;
        }

        public static explicit operator String(string value)
        {
            return new String(value);
        }

        public static explicit operator String(char[] value)
        {
            return new String(value);
        }


        public int Length
        {
            get { return this.value.Length; }
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            return new CharEnumerator(this.value);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new CharEnumerator(this.value);
        }


        private class CharEnumerator : IEnumerator<char>
        {
            private char[] values;
            private int position;

            public CharEnumerator(char[] values)
            {
                this.values = values;
                this.position = -1;
            }

            public char Current
            {
                get { return this.values[this.position]; }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                return this.position++ < this.values.Length;

            }

            public void Reset()
            {
                this.position = -1;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                this.Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                if(!disposing)
                    return;

                this.values = null;
            }

            ~CharEnumerator()
            {
                this.Dispose(false);
            }
        }

    }
}
