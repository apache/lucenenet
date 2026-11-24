using System;

namespace Lucene.Net.Util
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
    /// Represents an array of strings that can be backed by either
    /// individual character spans or an existing string array.
    /// </summary>
    /// <remarks>
    /// This type is implicitly convertible from either <c>string[]</c>,
    /// <c>ReadOnlySpan&lt;string&gt;</c> or <c>ReadOnlySpan&lt;char&gt;</c>.
    /// </remarks>
    internal readonly ref struct ValueStringArray
    {
        private readonly int length;
        private readonly bool isNull;
        private readonly bool isStringArray;
        private readonly ReadOnlySpan<string> strings;

        private readonly ReadOnlySpan<char> value0;
        private readonly ReadOnlySpan<char> value1;
        private readonly ReadOnlySpan<char> value2;
        private readonly ReadOnlySpan<char> value3;
        private readonly ReadOnlySpan<char> value4;
        private readonly ReadOnlySpan<char> value5;
        private readonly ReadOnlySpan<char> value6;
        private readonly ReadOnlySpan<char> value7;
        private readonly ReadOnlySpan<char> value8;
        private readonly ReadOnlySpan<char> value9;
        private readonly ReadOnlySpan<char> value10;
        private readonly ReadOnlySpan<char> value11;
        private readonly ReadOnlySpan<char> value12;
        private readonly ReadOnlySpan<char> value13;
        private readonly ReadOnlySpan<char> value14;
        private readonly ReadOnlySpan<char> value15;

        public bool IsNull => isNull;

        public int Length => length;

        public ValueStringArray(string[] values)
        {
            isNull = values is null;
            isStringArray = true;
            strings = values;
            length = values?.Length ?? 0;

            value0 = default;
            value1 = default;
            value2 = default;
            value3 = default;
            value4 = default;
            value5 = default;
            value6 = default;
            value7 = default;
            value8 = default;
            value9 = default;
            value10 = default;
            value11 = default;
            value12 = default;
            value13 = default;
            value14 = default;
            value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<string> values)
        {
            isNull = false;
            isStringArray = true;
            strings = values;
            length = values.Length;

            value0 = default;
            value1 = default;
            value2 = default;
            value3 = default;
            value4 = default;
            value5 = default;
            value6 = default;
            value7 = default;
            value8 = default;
            value9 = default;
            value10 = default;
            value11 = default;
            value12 = default;
            value13 = default;
            value14 = default;
            value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            length = 1;

            value0 = value;
            value1 = default;
            value2 = default;
            value3 = default;
            value4 = default;
            value5 = default;
            value6 = default;
            value7 = default;
            value8 = default;
            value9 = default;
            value10 = default;
            value11 = default;
            value12 = default;
            value13 = default;
            value14 = default;
            value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 2;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = default;
            this.value3 = default;
            this.value4 = default;
            this.value5 = default;
            this.value6 = default;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 3;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = default;
            this.value4 = default;
            this.value5 = default;
            this.value6 = default;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 4;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = default;
            this.value5 = default;
            this.value6 = default;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 5;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = default;
            this.value6 = default;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 6;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = default;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 7;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = default;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 8;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = default;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 9;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = default;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 10;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = default;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 11;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = default;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10, ReadOnlySpan<char> value11)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 12;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = value11;
            this.value12 = default;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10, ReadOnlySpan<char> value11, ReadOnlySpan<char> value12)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 13;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = value11;
            this.value12 = value12;
            this.value13 = default;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10, ReadOnlySpan<char> value11, ReadOnlySpan<char> value12, ReadOnlySpan<char> value13)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 14;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = value11;
            this.value12 = value12;
            this.value13 = value13;
            this.value14 = default;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10, ReadOnlySpan<char> value11, ReadOnlySpan<char> value12, ReadOnlySpan<char> value13, ReadOnlySpan<char> value14)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 15;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = value11;
            this.value12 = value12;
            this.value13 = value13;
            this.value14 = value14;
            this.value15 = default;
        }

        public ValueStringArray(ReadOnlySpan<char> value0, ReadOnlySpan<char> value1, ReadOnlySpan<char> value2, ReadOnlySpan<char> value3, ReadOnlySpan<char> value4, ReadOnlySpan<char> value5, ReadOnlySpan<char> value6, ReadOnlySpan<char> value7, ReadOnlySpan<char> value8, ReadOnlySpan<char> value9, ReadOnlySpan<char> value10, ReadOnlySpan<char> value11, ReadOnlySpan<char> value12, ReadOnlySpan<char> value13, ReadOnlySpan<char> value14, ReadOnlySpan<char> value15)
        {
            isNull = false;
            isStringArray = false;
            strings = default;
            this.length = 16;

            this.value0 = value0;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
            this.value4 = value4;
            this.value5 = value5;
            this.value6 = value6;
            this.value7 = value7;
            this.value8 = value8;
            this.value9 = value9;
            this.value10 = value10;
            this.value11 = value11;
            this.value12 = value12;
            this.value13 = value13;
            this.value14 = value14;
            this.value15 = value15;
        }

        public ReadOnlySpan<char> this[int index]
        {
            get
            {
                if ((uint)index >= (uint)length)
                    throw new IndexOutOfRangeException();

                if (isStringArray)
                    return strings[index].AsSpan();

                return index switch
                {
                    0 => value0,
                    1 => value1,
                    2 => value2,
                    3 => value3,
                    4 => value4,
                    5 => value5,
                    6 => value6,
                    7 => value7,
                    8 => value8,
                    9 => value9,
                    10 => value10,
                    11 => value11,
                    12 => value12,
                    13 => value13,
                    14 => value14,
                    15 => value15,
                    _ => throw new IndexOutOfRangeException()
                };
            }
        }

        public static implicit operator ValueStringArray(string[] values)
            => new ValueStringArray(values);

        public static implicit operator ValueStringArray(ReadOnlySpan<string> values)
            => new ValueStringArray(values);

        public static implicit operator ValueStringArray(ReadOnlySpan<char> value)
            => new ValueStringArray(value);
    }
}
