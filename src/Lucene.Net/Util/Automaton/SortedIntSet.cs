using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util.Automaton
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
    /// Just holds a set of <see cref="T:int[]"/> states, plus a corresponding
    /// <see cref="T:int[]"/> count per state.  Used by
    /// <see cref="BasicOperations.Determinize(Automaton)"/>.
    /// <para/>
    /// NOTE: This was SortedIntSet in Lucene
    /// </summary>
    internal sealed class SortedInt32Set
    {
        internal int[] values;
        internal int[] counts;
        internal int upto;
        private int hashCode;

        // If we hold more than this many states, we switch from
        // O(N^2) linear ops to O(N log(N)) TreeMap
        private const int TREE_MAP_CUTOVER = 30;

        private readonly IDictionary<int, int> map = new JCG.SortedDictionary<int, int>();

        private bool useTreeMap;

        internal State state;

        public SortedInt32Set(int capacity)
        {
            values = new int[capacity];
            counts = new int[capacity];
        }

        // Adds this state to the set
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Incr(int num)
        {
            if (useTreeMap)
            {
                int key = num;
                if (!map.TryGetValue(key, out int val))
                {
                    map[key] = 1;
                }
                else
                {
                    map[key] = 1 + val;
                }
                return;
            }

            if (upto == values.Length)
            {
                values = ArrayUtil.Grow(values, 1 + upto);
                counts = ArrayUtil.Grow(counts, 1 + upto);
            }

            for (int i = 0; i < upto; i++)
            {
                if (values[i] == num)
                {
                    counts[i]++;
                    return;
                }
                else if (num < values[i])
                {
                    // insert here
                    int j = upto - 1;
                    while (j >= i)
                    {
                        values[1 + j] = values[j];
                        counts[1 + j] = counts[j];
                        j--;
                    }
                    values[i] = num;
                    counts[i] = 1;
                    upto++;
                    return;
                }
            }

            // append
            values[upto] = num;
            counts[upto] = 1;
            upto++;

            if (upto == TREE_MAP_CUTOVER)
            {
                useTreeMap = true;
                for (int i = 0; i < upto; i++)
                {
                    map[values[i]] = counts[i];
                }
            }
        }

        // Removes this state from the set, if count decrs to 0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decr(int num)
        {
            if (useTreeMap)
            {
                int count = map[num];
                if (count == 1)
                {
                    map.Remove(num);
                }
                else
                {
                    map[num] = count - 1;
                }
                // Fall back to simple arrays once we touch zero again
                if (map.Count == 0)
                {
                    useTreeMap = false;
                    upto = 0;
                }
                return;
            }

            for (int i = 0; i < upto; i++)
            {
                if (values[i] == num)
                {
                    counts[i]--;
                    if (counts[i] == 0)
                    {
                        int limit = upto - 1;
                        while (i < limit)
                        {
                            values[i] = values[i + 1];
                            counts[i] = counts[i + 1];
                            i++;
                        }
                        upto = limit;
                    }
                    return;
                }
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ComputeHash()
        {
            if (useTreeMap)
            {
                if (map.Count > values.Length)
                {
                    int size = ArrayUtil.Oversize(map.Count, RamUsageEstimator.NUM_BYTES_INT32);
                    values = new int[size];
                    counts = new int[size];
                }
                hashCode = map.Count;
                upto = 0;
                foreach (int state in map.Keys)
                {
                    hashCode = 683 * hashCode + state;
                    values[upto++] = state;
                }
            }
            else
            {
                hashCode = upto;
                for (int i = 0; i < upto; i++)
                {
                    hashCode = 683 * hashCode + values[i];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrozenInt32Set ToFrozenInt32Set() // LUCENENET specific
        {
            int[] c = new int[upto];
            Arrays.Copy(values, 0, c, 0, upto);
            return new FrozenInt32Set(c, this.hashCode, this.state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrozenInt32Set Freeze(State state)
        {
            int[] c = new int[upto];
            Arrays.Copy(values, 0, c, 0, upto);
            return new FrozenInt32Set(c, hashCode, state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }
            if (!(obj is SortedInt32Set other)) // LUCENENET specific - don't compare against FrozenInt32Set
            {
                return false;
            }
            if (hashCode != other.hashCode)
            {
                return false;
            }
            if (other.values.Length != upto)
            {
                return false;
            }
            for (int i = 0; i < upto; i++)
            {
                if (other.values[i] != values[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = (new StringBuilder()).Append('[');
            for (int i = 0; i < upto; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(values[i]).Append(':').Append(counts[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// NOTE: This was FrozenIntSet in Lucene
        /// </summary>
        public struct FrozenInt32Set : IEquatable<FrozenInt32Set>
        {
            internal int[] values;
            internal int hashCode;
            internal State state;

            public FrozenInt32Set(int[] values, int hashCode, State state)
            {
                this.values = values;
                this.hashCode = hashCode;
                this.state = state;
            }

            public FrozenInt32Set(int num, State state)
            {
                this.values = new int[] { num };
                this.state = state;
                this.hashCode = 683 + num;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }
                if (obj is FrozenInt32Set other)
                {
                    if (hashCode != other.hashCode)
                    {
                        return false;
                    }
                    if (other.values.Length != values.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (other.values[i] != values[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }

                return false;
            }

            public bool Equals(FrozenInt32Set other) // LUCENENET specific - implemented IEquatable<FrozenInt32Set>
            {
                if (hashCode != other.hashCode)
                {
                    return false;
                }
                if (other.values.Length != values.Length)
                {
                    return false;
                }
                for (int i = 0; i < values.Length; i++)
                {
                    if (other.values[i] != values[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public override string ToString()
            {
                StringBuilder sb = (new StringBuilder()).Append('[');
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(values[i]);
                }
                sb.Append(']');
                return sb.ToString();
            }
        }
    }
}