using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Mutable
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
    /// <see cref="MutableValue"/> implementation of type <see cref="long"/>.
    /// <para/>
    /// NOTE: This was MutableValueLong in Lucene
    /// </summary>
    public class MutableValueInt64 : MutableValue
    {
        public long Value { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object ToObject()
        {
            return Exists ? (object)Value : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Copy(MutableValue source)
        {
            MutableValueInt64 s = (MutableValueInt64)source;
            Exists = s.Exists;
            Value = s.Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MutableValue Duplicate()
        {
            return new MutableValueInt64
            {
                Value = this.Value,
                Exists = this.Exists
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool EqualsSameType(object other)
        {
            MutableValueInt64 b = (MutableValueInt64)other;
            return Value == b.Value && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            MutableValueInt64 b = (MutableValueInt64)other;
            long bv = b.Value;
            if (Value < bv)
            {
                return -1;
            }
            if (Value > bv)
            {
                return 1;
            }
            if (Exists == b.Exists)
            {
                return 0;
            }
            return Exists ? 1 : -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return (int)Value + (int)(Value >> 32);
        }
    }
}