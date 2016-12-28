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
        internal int[] Values;
        internal int[] Counts;
        internal int Upto;
        private int HashCode_Renamed;

        // If we hold more than this many states, we switch from
        // O(N^2) linear ops to O(N log(N)) TreeMap
        private const int TREE_MAP_CUTOVER = 30;

        private readonly IDictionary<int, int> Map = new SortedDictionary<int, int>();

        private bool UseTreeMap;

        internal State State;

        public SortedIntSet(int capacity)
        {
            Values = new int[capacity];
            Counts = new int[capacity];
        }

        // Adds this state to the set
        public void Incr(int num)
        {
            if (UseTreeMap)
            {
                int key = num;
                int val;
                if (!Map.TryGetValue(key, out val))
                {
                    Map[key] = 1;
                }
                else
                {
                    Map[key] = 1 + val;
                }
                return;
            }

            if (Upto == Values.Length)
            {
                Values = ArrayUtil.Grow(Values, 1 + Upto);
                Counts = ArrayUtil.Grow(Counts, 1 + Upto);
            }

            for (int i = 0; i < Upto; i++)
            {
                if (Values[i] == num)
                {
                    Counts[i]++;
                    return;
                }
                else if (num < Values[i])
                {
                    // insert here
                    int j = Upto - 1;
                    while (j >= i)
                    {
                        Values[1 + j] = Values[j];
                        Counts[1 + j] = Counts[j];
                        j--;
                    }
                    Values[i] = num;
                    Counts[i] = 1;
                    Upto++;
                    return;
                }
            }

            // append
            Values[Upto] = num;
            Counts[Upto] = 1;
            Upto++;

            if (Upto == TREE_MAP_CUTOVER)
            {
                UseTreeMap = true;
                for (int i = 0; i < Upto; i++)
                {
                    Map[Values[i]] = Counts[i];
                }
            }
        }

        // Removes this state from the set, if count decrs to 0
        public void Decr(int num)
        {
            if (UseTreeMap)
            {
                int count = Map[num];
                if (count == 1)
                {
                    Map.Remove(num);
                }
                else
                {
                    Map[num] = count - 1;
                }
                // Fall back to simple arrays once we touch zero again
                if (Map.Count == 0)
                {
                    UseTreeMap = false;
                    Upto = 0;
                }
                return;
            }

            for (int i = 0; i < Upto; i++)
            {
                if (Values[i] == num)
                {
                    Counts[i]--;
                    if (Counts[i] == 0)
                    {
                        int limit = Upto - 1;
                        while (i < limit)
                        {
                            Values[i] = Values[i + 1];
                            Counts[i] = Counts[i + 1];
                            i++;
                        }
                        Upto = limit;
                    }
                    return;
                }
            }
            Debug.Assert(false);
        }

        public void ComputeHash()
        {
            if (UseTreeMap)
            {
                if (Map.Count > Values.Length)
                {
                    int size = ArrayUtil.Oversize(Map.Count, RamUsageEstimator.NUM_BYTES_INT);
                    Values = new int[size];
                    Counts = new int[size];
                }
                HashCode_Renamed = Map.Count;
                Upto = 0;
                foreach (int state in Map.Keys)
                {
                    HashCode_Renamed = 683 * HashCode_Renamed + state;
                    Values[Upto++] = state;
                }
            }
            else
            {
                HashCode_Renamed = Upto;
                for (int i = 0; i < Upto; i++)
                {
                    HashCode_Renamed = 683 * HashCode_Renamed + Values[i];
                }
            }
        }

        public FrozenIntSet ToFrozenIntSet() // LUCENENET TODO: This didn't exist in the original
        {
            int[] c = new int[Upto];
            Array.Copy(Values, 0, c, 0, Upto);
            return new FrozenIntSet(c, this.HashCode_Renamed, this.State);
        }

        public FrozenIntSet Freeze(State state)
        {
            int[] c = new int[Upto];
            Array.Copy(Values, 0, c, 0, Upto);
            return new FrozenIntSet(c, HashCode_Renamed, state);
        }

        public override int GetHashCode()
        {
            return HashCode_Renamed;
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
            if (HashCode_Renamed != other.HashCode_Renamed)
            {
                return false;
            }
            if (other.Values.Length != Upto)
            {
                return false;
            }
            for (int i = 0; i < Upto; i++)
            {
                if (other.Values[i] != Values[i])
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

            if (HashCode_Renamed != other.HashCode_Renamed)
            {
                return false;
            }
            if (other.Values.Length != Upto)
            {
                return false;
            }

            for (int i = 0; i < Upto; i++)
            {
                if (other.Values[i] != Values[i])
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = (new StringBuilder()).Append('[');
            for (int i = 0; i < Upto; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(Values[i]).Append(':').Append(Counts[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }

        public sealed class FrozenIntSet : IEquatable<SortedIntSet>, IEquatable<FrozenIntSet>
        {
            internal readonly int[] Values;
            internal readonly int HashCode_Renamed;
            internal readonly State State;

            public FrozenIntSet(int[] values, int hashCode, State state)
            {
                this.Values = values;
                this.HashCode_Renamed = hashCode;
                this.State = state;
            }

            public FrozenIntSet(int num, State state)
            {
                this.Values = new int[] { num };
                this.State = state;
                this.HashCode_Renamed = 683 + num;
            }

            public override int GetHashCode()
            {
                return HashCode_Renamed;
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
                    if (HashCode_Renamed != other.HashCode_Renamed)
                    {
                        return false;
                    }
                    if (other.Values.Length != Values.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < Values.Length; i++)
                    {
                        if (other.Values[i] != Values[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else if (_other is SortedIntSet)
                {
                    SortedIntSet other = (SortedIntSet)_other;
                    if (HashCode_Renamed != other.HashCode_Renamed)
                    {
                        return false;
                    }
                    if (other.Values.Length != Values.Length)
                    {
                        return false;
                    }
                    for (int i = 0; i < Values.Length; i++)
                    {
                        if (other.Values[i] != Values[i])
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

                if (HashCode_Renamed != other.HashCode_Renamed)
                {
                    return false;
                }
                if (other.Values.Length != Values.Length)
                {
                    return false;
                }
                for (int i = 0; i < Values.Length; i++)
                {
                    if (other.Values[i] != Values[i])
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

                if (HashCode_Renamed != other.HashCode_Renamed)
                {
                    return false;
                }
                if (other.Values.Length != Values.Length)
                {
                    return false;
                }
                for (int i = 0; i < Values.Length; i++)
                {
                    if (other.Values[i] != Values[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            public override string ToString()
            {
                StringBuilder sb = (new StringBuilder()).Append('[');
                for (int i = 0; i < Values.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(Values[i]);
                }
                sb.Append(']');
                return sb.ToString();
            }
        }
    }
}