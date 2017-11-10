using System;
using System.Text;

namespace Lucene.Net.Support
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
    /// An adapter for <see cref="StringBuilder"/> that implements <see cref="ICharSequence"/>
    /// </summary>
    public class StringBuilderCharSequenceWrapper : ICharSequence
    {
        private readonly StringBuilder value;

        public StringBuilderCharSequenceWrapper(StringBuilder wrappedValue)
        {
            if (wrappedValue == null)
                throw new ArgumentNullException("wrappedValue");

            this.value = wrappedValue;
        }

        // LUCENENET specific - added to .NETify
        public char this[int index]
        {
            get { return value[index]; }
        }

        public int Length
        {
            get { return value.Length; }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            return new StringCharSequenceWrapper(value.ToString(start, end - start));
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is StringBuilder))
                return false;

            return value.Equals(obj as StringBuilder);
        }

        public override string ToString()
        {
            return value.ToString();
        }
    }
}
