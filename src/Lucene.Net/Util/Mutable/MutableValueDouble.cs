using J2N.Numerics;
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
    /// <see cref="double"/>.
    /// </summary>
    public class MutableValueDouble : MutableValue
    {
        public double Value { get; set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override object ToObject()
        {
            return Exists ? (object)Value : null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Copy(MutableValue source)
        {
            MutableValueDouble s = (MutableValueDouble)source;
            Value = s.Value;
            Exists = s.Exists;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override MutableValue Duplicate()
        {
            return new MutableValueDouble
            {
                Value = this.Value,
                Exists = this.Exists
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool EqualsSameType(object other)
        {
            MutableValueDouble b = (MutableValueDouble)other;
            // LUCENENET specific - compare bits rather than using equality operators to prevent these comparisons from failing in x86 in .NET Framework with optimizations enabled
            return NumericUtils.DoubleToSortableInt64(Value) == NumericUtils.DoubleToSortableInt64(b.Value)
                && Exists == b.Exists;
        }

        public override int CompareSameType(object other)
        {
            MutableValueDouble b = (MutableValueDouble)other;
            int c = Value.CompareTo(b.Value);
            if (c != 0)
            {
                return c;
            }
            if (!Exists)
            {
                return -1;
            }
            if (!b.Exists)
            {
                return 1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            long x = J2N.BitConversion.DoubleToInt64Bits(Value);
            return (int)x + (int)x.TripleShift(32);
        }
    }
}