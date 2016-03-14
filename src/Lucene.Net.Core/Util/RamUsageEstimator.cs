using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

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
    /// Estimates the size (memory representation) of Java objects.
    /// </summary>
    /// <seealso cref= #sizeOf(Object) </seealso>
    /// <seealso cref= #shallowSizeOf(Object) </seealso>
    /// <seealso cref= #shallowSizeOfInstance(Class)
    ///
    /// @lucene.internal </seealso>
    public sealed class RamUsageEstimator
    {
        /// <summary>
        /// JVM info string for debugging and reports. </summary>
        public static readonly string JVM_INFO_STRING;

        /// <summary>
        /// One kilobyte bytes. </summary>
        public const long ONE_KB = 1024;

        /// <summary>
        /// One megabyte bytes. </summary>
        public static readonly long ONE_MB = ONE_KB * ONE_KB;

        /// <summary>
        /// One gigabyte bytes. </summary>
        public static readonly long ONE_GB = ONE_KB * ONE_MB;

        /// <summary>
        /// No instantiation. </summary>
        private RamUsageEstimator()
        {
        }

        public const int NUM_BYTES_BOOLEAN = 1;
        public const int NUM_BYTES_BYTE = 1;
        public const int NUM_BYTES_CHAR = 2;
        public const int NUM_BYTES_SHORT = 2;
        public const int NUM_BYTES_INT = 4;
        public const int NUM_BYTES_FLOAT = 4;
        public const int NUM_BYTES_LONG = 8;
        public const int NUM_BYTES_DOUBLE = 8;

        /// <summary>
        /// Number of bytes this jvm uses to represent an object reference.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_REF;

        /// <summary>
        /// Number of bytes to represent an object header (no fields, no alignments).
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_HEADER;

        /// <summary>
        /// Number of bytes to represent an array header (no content, but with alignments).
        /// </summary>
        public static readonly int NUM_BYTES_ARRAY_HEADER;

        /// <summary>
        /// A constant specifying the object alignment boundary inside the JVM. Objects will
        /// always take a full multiple of this constant, possibly wasting some space.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_ALIGNMENT;

        /// <summary>
        /// Sizes of primitive classes.
        /// </summary>
        private static readonly IDictionary<Type, int> PrimitiveSizes;

        static RamUsageEstimator()
        {
            PrimitiveSizes = new HashMap<Type, int>(8);
            PrimitiveSizes[typeof(bool)] = Convert.ToInt32(NUM_BYTES_BOOLEAN);
            PrimitiveSizes[typeof(sbyte)] = Convert.ToInt32(NUM_BYTES_BYTE);
            PrimitiveSizes[typeof(char)] = Convert.ToInt32(NUM_BYTES_CHAR);
            PrimitiveSizes[typeof(short)] = Convert.ToInt32(NUM_BYTES_SHORT);
            PrimitiveSizes[typeof(int)] = Convert.ToInt32(NUM_BYTES_INT);
            PrimitiveSizes[typeof(float)] = Convert.ToInt32(NUM_BYTES_FLOAT);
            PrimitiveSizes[typeof(double)] = Convert.ToInt32(NUM_BYTES_DOUBLE);
            PrimitiveSizes[typeof(long)] = Convert.ToInt32(NUM_BYTES_LONG);
            // Initialize empirically measured defaults. We'll modify them to the current
            // JVM settings later on if possible.
            int referenceSize = Constants.JRE_IS_64BIT ? 8 : 4;
            int objectHeader = Constants.JRE_IS_64BIT ? 16 : 8;
            // The following is objectHeader + NUM_BYTES_INT, but aligned (object alignment)
            // so on 64 bit JVMs it'll be align(16 + 4, @8) = 24.
            int arrayHeader = Constants.JRE_IS_64BIT ? 24 : 12;
            int objectAlignment = Constants.JRE_IS_64BIT ? 8 : 4;

            /* LUCENE-TODO

		    Type unsafeClass = null;
		    object tempTheUnsafe = null;
		    try
		    {
		      unsafeClass = Type.GetType("sun.misc.Unsafe");
		      FieldInfo unsafeField = unsafeClass.getDeclaredField("theUnsafe");
		      unsafeField.Accessible = true;
		      tempTheUnsafe = unsafeField.get(null);
		    }
		    catch (Exception e)
		    {
		      // Ignore.
		    }
		    TheUnsafe = tempTheUnsafe;

		    // get object reference size by getting scale factor of Object[] arrays:
		    try
		    {
		      Method arrayIndexScaleM = unsafeClass.GetMethod("arrayIndexScale", typeof(Type));
		      referenceSize = (int)((Number) arrayIndexScaleM.invoke(TheUnsafe, typeof(object[])));
		    }
		    catch (Exception e)
		    {
		      // ignore.
		    }

		    // "best guess" based on reference size. We will attempt to modify
		    // these to exact values if there is supported infrastructure.
		    objectHeader = Constants.JRE_IS_64BIT ? (8 + referenceSize) : 8;
		    arrayHeader = Constants.JRE_IS_64BIT ? (8 + 2 * referenceSize) : 12;

		    // get the object header size:
		    // - first try out if the field offsets are not scaled (see warning in Unsafe docs)
		    // - get the object header size by getting the field offset of the first field of a dummy object
		    // If the scaling is byte-wise and unsafe is available, enable dynamic size measurement for
		    // estimateRamUsage().
		    Method tempObjectFieldOffsetMethod = null;
		    try
		    {
		      Method objectFieldOffsetM = unsafeClass.GetMethod("objectFieldOffset", typeof(FieldInfo));
		      FieldInfo dummy1Field = typeof(DummyTwoLongObject).getDeclaredField("dummy1");
		      int ofs1 = (int)((Number) objectFieldOffsetM.invoke(TheUnsafe, dummy1Field));
		      FieldInfo dummy2Field = typeof(DummyTwoLongObject).getDeclaredField("dummy2");
		      int ofs2 = (int)((Number) objectFieldOffsetM.invoke(TheUnsafe, dummy2Field));
		      if (Math.Abs(ofs2 - ofs1) == NUM_BYTES_LONG)
		      {
			    FieldInfo baseField = typeof(DummyOneFieldObject).getDeclaredField("base");
			    objectHeader = (int)((Number) objectFieldOffsetM.invoke(TheUnsafe, baseField));
			    tempObjectFieldOffsetMethod = objectFieldOffsetM;
		      }
		    }
		    catch (Exception e)
		    {
		      // Ignore.
		    }
		    ObjectFieldOffsetMethod = tempObjectFieldOffsetMethod;

		    // Get the array header size by retrieving the array base offset
		    // (offset of the first element of an array).
		    try
		    {
		      Method arrayBaseOffsetM = unsafeClass.GetMethod("arrayBaseOffset", typeof(Type));
		      // we calculate that only for byte[] arrays, it's actually the same for all types:
		      arrayHeader = (int)((Number) arrayBaseOffsetM.invoke(TheUnsafe, typeof(sbyte[])));
		    }
		    catch (Exception e)
		    {
		      // Ignore.
		    }
            */
            NUM_BYTES_OBJECT_REF = referenceSize;
            NUM_BYTES_OBJECT_HEADER = objectHeader;
            NUM_BYTES_ARRAY_HEADER = arrayHeader;

            /* LUCENE-TODO
          // Try to get the object alignment (the default seems to be 8 on Hotspot,
          // regardless of the architecture).
          int objectAlignment = 8;
          try
          {
            Type beanClazz = Type.GetType("com.sun.management.HotSpotDiagnosticMXBean").asSubclass(typeof(PlatformManagedObject));
            object hotSpotBean = ManagementFactory.getPlatformMXBean(beanClazz);
            if (hotSpotBean != null)
            {
              Method getVMOptionMethod = beanClazz.GetMethod("getVMOption", typeof(string));
              object vmOption = getVMOptionMethod.invoke(hotSpotBean, "ObjectAlignmentInBytes");
              objectAlignment = Convert.ToInt32(vmOption.GetType().GetMethod("getValue").invoke(vmOption).ToString());
            }
          }
          catch (Exception e)
          {
            // Ignore.
          }
            */
            NUM_BYTES_OBJECT_ALIGNMENT = objectAlignment;

            JVM_INFO_STRING = "[JVM: " + Constants.JVM_NAME + ", " + Constants.JVM_VERSION + ", " + Constants.JVM_VENDOR + ", " + Constants.JAVA_VENDOR + ", " + Constants.JAVA_VERSION + "]";
        }

        /// <summary>
        /// A handle to <code>sun.misc.Unsafe</code>.
        /// </summary>
        private static readonly object TheUnsafe;

        /// <summary>
        /// A handle to <code>sun.misc.Unsafe#fieldOffset(Field)</code>.
        /// </summary>
        //private static readonly Method ObjectFieldOffsetMethod;

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

        // Object with just one field to determine the object header size by getting the offset of the dummy field:
        private sealed class DummyOneFieldObject
        {
            public sbyte @base;
        }

        // Another test object for checking, if the difference in offsets of dummy1 and dummy2 is 8 bytes.
        // Only then we can be sure that those are real, unscaled offsets:
        private sealed class DummyTwoLongObject
        {
            public long Dummy1, Dummy2;
        }

        /// <summary>
        /// Aligns an object size to be the next multiple of <seealso cref="#NUM_BYTES_OBJECT_ALIGNMENT"/>.
        /// </summary>
        public static long AlignObjectSize(long size)
        {
            size += (long)NUM_BYTES_OBJECT_ALIGNMENT - 1L;
            return size - (size % NUM_BYTES_OBJECT_ALIGNMENT);
        }

        /// <summary>
        /// Returns the size in bytes of the byte[] object. </summary>
        public static long SizeOf(sbyte[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the boolean[] object. </summary>
        public static long SizeOf(bool[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the char[] object. </summary>
        public static long SizeOf(char[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_CHAR * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the short[] object. </summary>
        public static long SizeOf(short[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_SHORT * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the int[] object. </summary>
        public static long SizeOf(int[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the float[] object. </summary>
        public static long SizeOf(float[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_FLOAT * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the long[] object. </summary>
        public static long SizeOf(long[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_LONG * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the double[] object. </summary>
        public static long SizeOf(double[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_DOUBLE * arr.Length);
        }

        /// <summary>
        /// Estimates the RAM usage by the given object. It will
        /// walk the object tree and sum up all referenced objects.
        ///
        /// <p><b>Resource Usage:</b> this method internally uses a set of
        /// every object seen during traversals so it does allocate memory
        /// (it isn't side-effect free). After the method exits, this memory
        /// should be GCed.</p>
        /// </summary>
        public static long SizeOf(object obj)
        {
            return MeasureObjectSize(obj);
        }

        /// <summary>
        /// Estimates a "shallow" memory usage of the given object. For arrays, this will be the
        /// memory taken by array storage (no subreferences will be followed). For objects, this
        /// will be the memory taken by the fields.
        ///
        /// JVM object alignments are also applied.
        /// </summary>
        public static long ShallowSizeOf(object obj)
        {
            if (obj == null)
            {
                return 0;
            }
            Type clz = obj.GetType();
            if (clz.IsArray)
            {
                return ShallowSizeOfArray((Array)obj);
            }
            else
            {
                return ShallowSizeOfInstance(clz);
            }
        }

        /// <summary>
        /// Returns the shallow instance size in bytes an instance of the given class would occupy.
        /// this works with all conventional classes and primitive types, but not with arrays
        /// (the size then depends on the number of elements and varies from object to object).
        /// </summary>
        /// <seealso cref= #shallowSizeOf(Object) </seealso>
        /// <exception cref="IllegalArgumentException"> if {@code clazz} is an array class.  </exception>
        public static long ShallowSizeOfInstance(Type clazz)
        {
            if (clazz.IsArray)
            {
                throw new System.ArgumentException("this method does not work with array classes.");
            }
            if (clazz.GetTypeInfo().IsPrimitive)
            {
                return PrimitiveSizes[clazz];
            }

            long size = NUM_BYTES_OBJECT_HEADER;

            // Walk type hierarchy
            for (; clazz != null; clazz = clazz.GetTypeInfo().BaseType)
            {
                FieldInfo[] fields = clazz.GetFields(BindingFlags.Public);
                foreach (FieldInfo f in fields)
                {
                    if (!f.IsStatic)
                    {
                        size = AdjustForField(size, f);
                    }
                }
            }
            return AlignObjectSize(size);
        }

        /// <summary>
        /// Return shallow size of any <code>array</code>.
        /// </summary>
        private static long ShallowSizeOfArray(Array array)
        {
            long size = NUM_BYTES_ARRAY_HEADER;
            int len = array.Length;
            if (len > 0)
            {
                Type arrayElementClazz = array.GetType().GetElementType();
                if (arrayElementClazz.GetTypeInfo().IsPrimitive)
                {
                    size += (long)len * PrimitiveSizes[arrayElementClazz];
                }
                else
                {
                    size += (long)NUM_BYTES_OBJECT_REF * len;
                }
            }
            return AlignObjectSize(size);
        }

        /*
         * Non-recursive version of object descend. this consumes more memory than recursive in-depth
         * traversal but prevents stack overflows on long chains of objects
         * or complex graphs (a max. recursion depth on my machine was ~5000 objects linked in a chain
         * so not too much).
         */

        private static long MeasureObjectSize(object root)
        {
            // Objects seen so far.
            IdentityHashSet<object> seen = new IdentityHashSet<object>();
            // Class cache with reference Field and precalculated shallow size.
            HashMap<Type, ClassCache> classCache = new HashMap<Type, ClassCache>();
            // Stack of objects pending traversal. Recursion caused stack overflows.
            Stack<object> stack = new Stack<object>();
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

                Type obClazz = ob.GetType();
                if (obClazz.IsArray)
                {
                    /*
                     * Consider an array, possibly of primitive types. Push any of its references to
                     * the processing stack and accumulate this array's shallow size.
                     */
                    long size = NUM_BYTES_ARRAY_HEADER;
                    Array array = (Array)ob;
                    int len = array.Length;
                    if (len > 0)
                    {
                        Type componentClazz = obClazz.GetElementType();
                        if (componentClazz.GetTypeInfo().IsPrimitive)
                        {
                            size += (long)len * PrimitiveSizes[componentClazz];
                        }
                        else
                        {
                            size += (long)NUM_BYTES_OBJECT_REF * len;

                            // Push refs for traversal later.
                            for (int i = len; --i >= 0; )
                            {
                                object o = array.GetValue(i);
                                if (o != null && !seen.Contains(o))
                                {
                                    stack.Push(o);
                                }
                            }
                        }
                    }
                    totalSize += AlignObjectSize(size);
                }
                else
                {
                    /*
                     * Consider an object. Push any references it has to the processing stack
                     * and accumulate this object's shallow size.
                     */
                    try
                    {
                        ClassCache cachedInfo = classCache[obClazz];
                        if (cachedInfo == null)
                        {
                            classCache[obClazz] = cachedInfo = CreateCacheEntry(obClazz);
                        }

                        foreach (FieldInfo f in cachedInfo.ReferenceFields)
                        {
                            // Fast path to eliminate redundancies.
                            object o = f.GetValue(ob);
                            if (o != null && !seen.Contains(o))
                            {
                                stack.Push(o);
                            }
                        }

                        totalSize += cachedInfo.AlignedShallowInstanceSize;
                    }
                    catch (Exception e)
                    {
                        // this should never happen as we enabled setAccessible().
                        throw new Exception("Reflective field access failed?", e);
                    }
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
        private static ClassCache CreateCacheEntry(Type clazz)
        {
            ClassCache cachedInfo;
            long shallowInstanceSize = NUM_BYTES_OBJECT_HEADER;
            List<FieldInfo> referenceFields = new List<FieldInfo>(32);
            for (Type c = clazz; c != null; c = c.GetTypeInfo().BaseType)
            {
                FieldInfo[] fields = c.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo f in fields)
                {
                    if (!f.IsStatic)
                    {
                        shallowInstanceSize = AdjustForField(shallowInstanceSize, f);

                        if (!f.FieldType.GetTypeInfo().IsPrimitive)
                        {
                            referenceFields.Add(f);
                        }
                    }
                }
            }

            cachedInfo = new ClassCache(AlignObjectSize(shallowInstanceSize), referenceFields.ToArray());
            return cachedInfo;
        }

        /// <summary>
        /// this method returns the maximum representation size of an object. <code>sizeSoFar</code>
        /// is the object's size measured so far. <code>f</code> is the field being probed.
        ///
        /// <p>The returned offset will be the maximum of whatever was measured so far and
        /// <code>f</code> field's offset and representation size (unaligned).
        /// </summary>
        private static long AdjustForField(long sizeSoFar, FieldInfo f)
        {
            Type type = f.FieldType;
            int fsize = type.GetTypeInfo().IsPrimitive ? PrimitiveSizes[type] : NUM_BYTES_OBJECT_REF;
            /* LUCENE-TODO I dont think this will ever not be null
            if (ObjectFieldOffsetMethod != null)
            {
              try
              {
                long offsetPlusSize = (long)((Number) ObjectFieldOffsetMethod.invoke(TheUnsafe, f)) + fsize;
                return Math.Max(sizeSoFar, offsetPlusSize);
              }
              catch (Exception ex)
              {
                throw new Exception("Access problem with sun.misc.Unsafe", ex);
              }
            }
            else
            {
              // TODO: No alignments based on field type/ subclass fields alignments?
              return sizeSoFar + fsize;
            }*/
            return sizeSoFar + fsize;
        }

        /// <summary>
        /// Returns <code>size</code> in human-readable units (GB, MB, KB or bytes).
        /// </summary>
        public static string HumanReadableUnits(long bytes)
        {
            return HumanReadableUnits(bytes, new NumberFormatInfo() { NumberDecimalDigits = 1 });
        }

        /// <summary>
        /// Returns <code>size</code> in human-readable units (GB, MB, KB or bytes).
        /// </summary>
        public static string HumanReadableUnits(long bytes, IFormatProvider df)
        {
            if (bytes / ONE_GB > 0)
            {
                return Convert.ToString(((float)bytes / ONE_GB), df) + " GB";
            }
            else if (bytes / ONE_MB > 0)
            {
                return Convert.ToString(((float)bytes / ONE_MB), df) + " MB";
            }
            else if (bytes / ONE_KB > 0)
            {
                return Convert.ToString(((float)bytes / ONE_KB), df) + " KB";
            }
            else
            {
                return Convert.ToString(bytes) + " bytes";
            }
        }

        /// <summary>
        /// Return a human-readable size of a given object. </summary>
        /// <seealso cref= #sizeOf(Object) </seealso>
        /// <seealso cref= #humanReadableUnits(long) </seealso>
        public static string HumanSizeOf(object @object)
        {
            return HumanReadableUnits(SizeOf(@object));
        }

        /// <summary>
        /// An identity hash set implemented using open addressing. No null keys are allowed.
        ///
        /// TODO: If this is useful outside this class, make it public - needs some work
        /// </summary>
        public sealed class IdentityHashSet<KType> : IEnumerable<KType>
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
            public IdentityHashSet()
                : this(16, DEFAULT_LOAD_FACTOR)
            {
            }

            /// <summary>
            /// Creates a hash set with the given capacity, load factor of
            /// {@value #DEFAULT_LOAD_FACTOR}.
            /// </summary>
            public IdentityHashSet(int initialCapacity)
                : this(initialCapacity, DEFAULT_LOAD_FACTOR)
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
            public bool Add(KType e)
            {
                Debug.Assert(e != null, "Null keys not allowed.");

                if (Assigned >= ResizeThreshold)
                {
                    ExpandAndRehash();
                }

                int mask = Keys.Length - 1;
                int slot = Rehash(e) & mask;
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
            public bool Contains(KType e)
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
                k ^= (int)((uint)k >> 16);
                k *= unchecked((int)0x85ebca6b);
                k ^= (int)((uint)k >> 13);
                k *= unchecked((int)0xc2b2ae35);
                k ^= (int)((uint)k >> 16);
                return k;
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
            protected internal int NextCapacity(int current)
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

            public IEnumerator<KType> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<KType>
            {
                private readonly IdentityHashSet<KType> OuterInstance;

                public IteratorAnonymousInnerClassHelper(IdentityHashSet<KType> outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    pos = -1;
                    nextElement = FetchNext();
                }

                internal int pos;
                internal object nextElement;
                internal KType current;

                public bool MoveNext()
                {
                    object r = nextElement;
                    if (nextElement == null)
                    {
                        return false;
                    }

                    nextElement = FetchNext();
                    current = (KType)r;
                    return true;
                }

                public KType Current
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