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
    /// <see cref="MutableValue"/> implementation of type
    /// <see cref="string"/>.
    /// </summary>
    public class MutableValueStr : MutableValue
    {
        public BytesRef Value { get; set; }

        public MutableValueStr()
        {
            Value = new BytesRef();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object ToObject()
        {
            return Exists ? Value.Utf8ToString() : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Copy(MutableValue source)
        {
            MutableValueStr s = (MutableValueStr)source;
            Exists = s.Exists;
            Value.CopyBytes(s.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MutableValue Duplicate()
        {
            MutableValueStr v = new MutableValueStr();
            v.Value.CopyBytes(Value);
            v.Exists = this.Exists;
            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool EqualsSameType(object other)
        {
            MutableValueStr b = (MutableValueStr)other;
            return Value.Equals(b.Value) && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            MutableValueStr b = (MutableValueStr)other;
            int c = Value.CompareTo(b.Value);
            if (c != 0)
            {
                return c;
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
            return Value.GetHashCode();
        }
    }
}