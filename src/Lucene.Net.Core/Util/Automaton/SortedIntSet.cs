using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

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

    // Just holds a set of int[] states, plus a corresponding
    // int[] count per state.  Used by
    // BasicOperations.determinize
    internal sealed class SortedIntSet : IEquatable<SortedIntSet>, IEquatable<SortedIntSet.FrozenIntSet>
    {
        internal int[] values;
        internal int[] counts;
        internal int upto;
        private int hashCode;

        // If we hold more than this many states, we switch from
        // O(N^2) linear ops to O(N log(N)) TreeMap
        private const int TREE_MAP_CUTOVER = 30;

        private readonly IDictionary<int, int> map = new SortedDictionary<int, int>();

        private bool useTreeMap;

        internal State state;

        public SortedIntSet(int capacity)
        {
            values = new int[capacity];
            counts = new int[capacity];
        }

        // Adds this state to the set
        public void Incr(int num)
        {
            if (useTreeMap)
            {
                int key = num;
                int val;
                if (!map.TryGetValue(key, out val))
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
            Debug.Assert(false);
        }

        public void ComputeHash()
        {
            if (useTreeMap)
            {
                if (map.Count > values.Length)
                {
                    int size = ArrayUtil.Oversize(map.Count, RamUsageEstimator.NUM_BYTES_INT);
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

        public FrozenIntSet ToFrozenIntSet() // LUCENENET TODO: This didn't exist in the original
        {
            int[] c = new int[upto];
            Array.Copy(values, 0, c, 0, upto);
            return new FrozenIntSet(c, this.hashCode, this.state);
        }

        public FrozenIntSet Freeze(State state)
        {
            int[] c = new int[upto];
            Array.Copy(values, 0, c, 0, upto);
            return new FrozenIntSet(c, hashCode, state);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override bool Equals(object _other)
        {
            throw new NotImplementedException("ObjectEquals"); // LUCENENET TODO: Remove error message
            if (_other == null)
            {
                return false;
            }
            if (!(_other is FrozenIntSet))
            {
                return false;
            }
            FrozenIntSet other = (FrozenIntSet)_other;
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

        public bool Equals(SortedIntSet other) // LUCENENET TODO: This didn't exist in the original
        {
            throw new NotImplementedException("SortedIntSet Equals");
        }

        public bool Equals(FrozenIntSet other) // LUCENENET TODO: This didn't exist in the original
        {
            if (other == null)
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

        public sealed class FrozenIntSet : IEquatable<SortedIntSet>, IEquatable<FrozenIntSet>
        {
            internal readonly int[] values;
            internal readonly int hashCode;
            internal readonly State state;

            public FrozenIntSet(int[] values, int hashCode, State state)
            {
                this.values = values;
                this.hashCode = hashCode;
                this.state = state;
            }

            public FrozenIntSet(int num, State state)
            {
                this.values = new int[] { num };
                this.state = state;
                this.hashCode = 683 + num;
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(object _other)
            {
                if (_other == null)
                {
                    return false;
                }
                if (_other is FrozenIntSet)
                {
                    FrozenIntSet other = (FrozenIntSet)_other;
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
                else if (_other is SortedIntSet)
                {
                    SortedIntSet other = (SortedIntSet)_other;
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

            public bool Equals(SortedIntSet other) // LUCENENET TODO: This didn't exist in the original
            {
                if (other == null)
                {
                    return false;
                }

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

            public bool Equals(FrozenIntSet other) // LUCENENET TODO: This didn't exist in the original
            {
                if (other == null)
                {
                    return false;
                }

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