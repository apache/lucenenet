/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using Support;


    public class RamUsageTester
    {


        public static long SizeOf(Object obj, Accumulator accumlator)
        {
            return MeasureObjectSize(obj, accumlator);
        }

        public static long SizeOf(Object obj)
        {
            return MeasureObjectSize(obj, new Accumulator());
        }

        public static string HumanSizeOf(Object obj)
        {
            var units = SizeOf(obj);
            return RamUsageEstimator.HumanReadableUnits(units);
        }

        /*
         * Non-recursive version of object descend. this consumes more memory than recursive in-depth 
         * traversal but prevents stack overflows on long chains of objects
         * or complex graphs (a max. recursion depth on my machine was ~5000 objects linked in a chain
         * so not too much).  
         */
        private static long MeasureObjectSize(object root, Accumulator accumulator)
        {
            // Objects seen so far.
            var seen = new IdentityHashSet<object>();

            // Class cache with reference Field and precalculated shallow size. 
            var classCache = new HashMap<Type, ClassCache>();
            
            // Stack of objects pending traversal. Recursion caused stack overflows. 
            var stack = new Stack<object>();
            stack.Push(root);

            long totalSize = 0;

            while (stack.Count > 0)
            {
                object ob = stack.Pop();

                if (ob == null || seen.Contains(ob))
                {
                    continue;
                }

                seen.Add(ob);

                var type = ob.GetType();
                if (type.IsArray)
                {
                    var array = (Array)ob;
                    var shallowSize = RamUsageEstimator.ShallowSizeOf(ob);
                    var values = new List<object>();

                    var elementType = type.GetElementType().GetTypeInfo();
                    if (!elementType.IsPrimitive)
                    {
                        // eaiser than AbstractList();
                        values = new List<object>(array.Cast<object>());

                        
                    }

                    totalSize += accumulator.AccumulateArray(array, shallowSize, values, stack);
                }
                else
                {
                    var cachedInfo = classCache[type];

                    if (cachedInfo == null)
                    {
                        classCache.Add(type, cachedInfo = CreateCacheEntry(type));
                    }

                    var fieldValues = cachedInfo.ReferenceFields.ToDictionary(field => field, field => field.GetValue(ob));

                    totalSize += accumulator.AccumulateObject(ob, cachedInfo.AlignedShallowInstanceSize, fieldValues, stack);
                }
                        
                    
            }

            // Help the GC (?).
            seen.Clear();
            stack.Clear();
            classCache.Clear();

            return totalSize;
        }



        /// <summary>
        /// Create a cached information about shallow size and reference fields for 
        /// a given class.
        /// </summary>
        private static ClassCache CreateCacheEntry(Type instanceType)
        {
            long shallowInstanceSize = RamUsageEstimator.NUM_BYTES_OBJECT_HEADER;
            var referenceFields = new List<FieldInfo>(32);

            // GetRuntimeFields includes inherited fields. 
            var fields = instanceType.GetRuntimeFields().Where(o => !o.IsStatic);
            foreach (var f in fields)
            {
                shallowInstanceSize = RamUsageEstimator.AdjustForField(shallowInstanceSize, f);

                if (!f.FieldType.GetTypeInfo().IsPrimitive)
                {
                    referenceFields.Add(f);
                }

            }

            return new ClassCache(RamUsageEstimator.AlignObjectSize(shallowInstanceSize), referenceFields.ToArray());
        }

        /// <summary>
        /// Cached information about a given class.   
        /// </summary>
        private sealed class ClassCache
        {
            public readonly long AlignedShallowInstanceSize;
            public readonly FieldInfo[] ReferenceFields;

            public ClassCache(long alignedShallowInstanceSize, FieldInfo[] referenceFields)
            {
                this.AlignedShallowInstanceSize = alignedShallowInstanceSize;
                this.ReferenceFields = referenceFields;
            }
        }


        /// <summary>
        /// An accumulator of object references. This class allows for customizing RAM usage estimation.
        /// </summary>
        public class Accumulator
        {

            /// <summary>
            /// Accumulate transitive references for the provided fields of the given
            /// object into<code>queue</code> and return the shallow size of this object.
            /// </summary>
            public virtual long AccumulateObject(Object obj, long shallowSize, Dictionary<FieldInfo, Object> fieldValues, Stack<object> queue)
            {
                foreach (var value in fieldValues.Values)
                    queue.Push(value);

                return shallowSize;
            }

            /// <summary>
            /// Accumulate transitive references for the provided values of the given
            /// array into<code>queue</code> and return the shallow size of this array.
            /// </summary>
            /// <param name="array"></param>
            /// <param name="shallowSize"></param>
            /// <param name="values"></param>
            /// <param name="queue"></param>
            /// <returns></returns>
            public virtual long AccumulateArray(Array array, long shallowSize, List<Object> values, Stack<object> queue)
            {
                foreach (var value in values)
                    queue.Push(value);

                return shallowSize;
            }
        }


        /// <summary>
        /// An identity hash set implemented using open addressing. No null keys are allowed.
        /// 
        /// TODO: If this is useful outside this class, make it public - needs some work
        /// </summary>
        public sealed class IdentityHashSet<TKey> : IEnumerable<TKey>
        {
            /// <summary>
            /// Default load factor.
            /// </summary>
            public const float DEFAULT_LOAD_FACTOR = 0.75f;

            /// <summary>
            /// Minimum capacity for the set.
            /// </summary>
            public const int MIN_CAPACITY = 4;

            /// <summary>
            /// All of set entries. Always of power of two length.
            /// </summary>
            public object[] Keys;

            /// <summary>
            /// Cached number of assigned slots.
            /// </summary>
            public int Assigned;

            /// <summary>
            /// The load factor for this set (fraction of allocated or deleted slots before
            /// the buffers must be rehashed or reallocated).
            /// </summary>
            public readonly float LoadFactor;

            /// <summary>
            /// Cached capacity threshold at which we must resize the buffers.
            /// </summary>
            internal int ResizeThreshold;

            /// <summary>
            /// Creates a hash set with the default capacity of 16.
            /// load factor of {@value #DEFAULT_LOAD_FACTOR}. `
            /// </summary>
            public IdentityHashSet() : this(16, DEFAULT_LOAD_FACTOR)
            {
            }

            /// <summary>
            /// Creates a hash set with the given capacity, load factor of
            /// {@value #DEFAULT_LOAD_FACTOR}.
            /// </summary>
            public IdentityHashSet(int initialCapacity) : this(initialCapacity, DEFAULT_LOAD_FACTOR)
            {
            }

            /// <summary>
            /// Creates a hash set with the given capacity and load factor.
            /// </summary>
            public IdentityHashSet(int initialCapacity, float loadFactor)
            {
                initialCapacity = Math.Max(MIN_CAPACITY, initialCapacity);

                Debug.Assert(initialCapacity > 0, "Initial capacity must be between (0, " + int.MaxValue + "].");
                Debug.Assert(loadFactor > 0 && loadFactor < 1, "Load factor must be between (0, 1).");
                this.LoadFactor = loadFactor;
                AllocateBuffers(RoundCapacity(initialCapacity));
            }

            /// <summary>
            /// Adds a reference to the set. Null keys are not allowed.
            /// </summary>
            public bool Add(TKey e)
            {
                Debug.Assert(e != null, "Null keys not allowed.");

                if (Assigned >= ResizeThreshold)
                {
                    ExpandAndRehash();
                }

                int mask = Keys.Length - 1,
                    slot = Rehash(e) & mask;
                
                object existing;
                while ((existing = Keys[slot]) != null)
                {
                    if (Object.ReferenceEquals(e, existing))
                    {
                        return false; // already found.
                    }
                    slot = (slot + 1) & mask;
                }
                
                Assigned++;
                Keys[slot] = e;
                return true;
            }

            /// <summary>
            /// Checks if the set contains a given ref.
            /// </summary>
            public bool Contains(TKey e)
            {
                int mask = Keys.Length - 1;
                int slot = Rehash(e) & mask;
                object existing;
                while ((existing = Keys[slot]) != null)
                {
                    if (Object.ReferenceEquals(e, existing))
                    {
                        return true;
                    }
                    slot = (slot + 1) & mask;
                }
                return false;
            }

            /// <summary>
            /// Rehash via MurmurHash.
            /// 
            /// <p>The implementation is based on the
            /// finalization step from Austin Appleby's
            /// <code>MurmurHash3</code>.
            /// </summary>
            /// <seealso cref= "http://sites.google.com/site/murmurhash/" </seealso>
            internal static int Rehash(object o)
            {
                int k = o.GetHashCode();
                return k.ComputeMurmurHash3();
            }

            /// <summary>
            /// Expand the internal storage buffers (capacity) or rehash current keys and
            /// values if there are a lot of deleted slots.
            /// </summary>
            internal void ExpandAndRehash()
            {
                object[] oldKeys = this.Keys;

                Debug.Assert(Assigned >= ResizeThreshold);
                AllocateBuffers(NextCapacity(Keys.Length));

                /*
                 * Rehash all assigned slots from the old hash table.
                 */
                int mask = Keys.Length - 1;
                for (int i = 0; i < oldKeys.Length; i++)
                {
                    object key = oldKeys[i];
                    if (key != null)
                    {
                        int slot = Rehash(key) & mask;
                        while (Keys[slot] != null)
                        {
                            slot = (slot + 1) & mask;
                        }
                        Keys[slot] = key;
                    }
                }
                Array.Clear(oldKeys, 0, oldKeys.Length);
            }

            /// <summary>
            /// Allocate internal buffers for a given capacity.
            /// </summary>
            /// <param name="capacity">
            ///          New capacity (must be a power of two). </param>
            internal void AllocateBuffers(int capacity)
            {
                this.Keys = new object[capacity];
                this.ResizeThreshold = (int)(capacity * DEFAULT_LOAD_FACTOR);
            }


           

            /// <summary>
            /// Return the next possible capacity, counting from the current buffers' size.
            /// </summary>
            private int NextCapacity(int current)
            {
               
                Debug.Assert(current > 0 && ((current & (current - 1)) == 0), "Capacity must be a power of two.");
                Debug.Assert((current << 1) > 0, "Maximum capacity exceeded (" + ((int)((uint)0x80000000 >> 1)) + ").");


                if (current < MIN_CAPACITY / 2)
                {
                    current = MIN_CAPACITY / 2;
                }
                return current << 1;
            }

            /// <summary>
            /// Round the capacity to the next allowed value.
            /// </summary>
            private int RoundCapacity(int requestedCapacity)
            {
                // Maximum positive integer that is a power of two.
                if (requestedCapacity > ((int)((uint)0x80000000 >> 1)))
                {
                    return ((int)((uint)0x80000000 >> 1));
                }

                int capacity = MIN_CAPACITY;
                while (capacity < requestedCapacity)
                {
                    capacity <<= 1;
                }

                return capacity;
            }

            public void Clear()
            {
                Assigned = 0;
                Array.Clear(Keys, 0, Keys.Length);
            }

            public int Size()
            {
                return Assigned;
            }

            public bool Empty
            {
                get
                {
                    return Size() == 0;
                }
            }

            public IEnumerator<TKey> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<TKey>
            {
                private readonly IdentityHashSet<TKey> OuterInstance;

                public IteratorAnonymousInnerClassHelper(IdentityHashSet<TKey> outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    pos = -1;
                    nextElement = FetchNext();
                }

                internal int pos;
                internal object nextElement;
                internal TKey current;


                public bool MoveNext()
                {
                    object r = nextElement;
                    if (nextElement == null)
                    {
                        return false;
                    }

                    nextElement = FetchNext();
                    current = (TKey)r;
                    return true;
                }

                public TKey Current
                {
                    get { return current; }
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return Current; }
                }

                private object FetchNext()
                {
                    pos++;
                    while (pos < OuterInstance.Keys.Length && OuterInstance.Keys[pos] == null)
                    {
                        pos++;
                    }

                    return (pos >= OuterInstance.Keys.Length ? null : OuterInstance.Keys[pos]);
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {

                }
            }
        }
    }
}
