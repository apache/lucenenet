using System;

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

    public class StringCharSequenceWrapper : ICharSequence
    {
        public static readonly StringCharSequenceWrapper Empty = new StringCharSequenceWrapper(string.Empty);

        private readonly string value;

        public StringCharSequenceWrapper(string wrappedValue)
        {
            value = wrappedValue;
        }

        public int Length
        {
            get { return value.Length; }
        }

        public char CharAt(int index)
        {
            return value[index];
        }

        // LUCENENET specific - added to .NETify
        public char this[int index]
        {
            get { return value[index]; }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            return new StringCharSequenceWrapper(value.Substring(start, end - start));
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (!(obj is string))
                return false;

            return string.Equals(value, obj as string, StringComparison.Ordinal);
        }

        public override string ToString()
        {
            return value;
        }
    }
}