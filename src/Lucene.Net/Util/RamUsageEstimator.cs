using J2N.Numerics;
using J2N.Runtime.CompilerServices;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    /// Estimates the size (memory representation) of .NET objects.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="SizeOf(object)"/>
    /// <seealso cref="ShallowSizeOf(object)"/>
    /// <seealso cref="ShallowSizeOfInstance(Type)"/>
    public sealed class RamUsageEstimator
    {
        ///// <summary>
        ///// JVM info string for debugging and reports. </summary>
        //public static readonly string JVM_INFO_STRING; // LUCENENET specific - this is not being used

        /// <summary>
        /// One kilobyte bytes. </summary>
        public const long ONE_KB = 1024;

        /// <summary>
        /// One megabyte bytes. </summary>
        public const long ONE_MB = ONE_KB * ONE_KB;

        /// <summary>
        /// One gigabyte bytes. </summary>
        public const long ONE_GB = ONE_KB * ONE_MB;

        /// <summary>
        /// No instantiation. </summary>
        private RamUsageEstimator()
        {
        }

        public const int NUM_BYTES_BOOLEAN = sizeof(bool); //1;
        public const int NUM_BYTES_BYTE = sizeof(byte); //1;
        public const int NUM_BYTES_CHAR = sizeof(char); //2;

        /// <summary>
        /// NOTE: This was NUM_BYTES_SHORT in Lucene
        /// </summary>
        public const int NUM_BYTES_INT16 = sizeof(short); //2;

        /// <summary>
        /// NOTE: This was NUM_BYTES_INT in Lucene
        /// </summary>
        public const int NUM_BYTES_INT32 = sizeof(int); //4;

        /// <summary>
        /// NOTE: This was NUM_BYTES_FLOAT in Lucene
        /// </summary>
        public const int NUM_BYTES_SINGLE = sizeof(float); //4;

        /// <summary>
        /// NOTE: This was NUM_BYTES_LONG in Lucene
        /// </summary>
        public const int NUM_BYTES_INT64 = sizeof(long); //8;
        public const int NUM_BYTES_DOUBLE = sizeof(double); //8;

        /// <summary>
        /// Number of bytes this .NET runtime uses to represent an object reference.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_REF = Constants.RUNTIME_IS_64BIT ? 8 : 4; // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

        /// <summary>
        /// Number of bytes to represent an object header (no fields, no alignments).
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_HEADER = Constants.RUNTIME_IS_64BIT ? (8 + 8) : 8; // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

        /// <summary>
        /// Number of bytes to represent an array header (no content, but with alignments).
        /// </summary>
        public static readonly int NUM_BYTES_ARRAY_HEADER = Constants.RUNTIME_IS_64BIT ? (8 + 2 * 8) : 12; // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

        /// <summary>
        /// A constant specifying the object alignment boundary inside the .NET runtime. Objects will
        /// always take a full multiple of this constant, possibly wasting some space.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_ALIGNMENT = Constants.RUNTIME_IS_64BIT ? 8 : 4; // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)

        /// <summary>
        /// Sizes of primitive classes.
        /// </summary>
        // LUCENENET specific - Identity comparer is not necessary here because Type is already representing an identity
        private static readonly IDictionary<Type, int> primitiveSizes = new Dictionary<Type, int>(/*IdentityEqualityComparer<Type>.Default*/) // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            [typeof(bool)] = NUM_BYTES_BOOLEAN,
            [typeof(sbyte)] = NUM_BYTES_BYTE,
            [typeof(byte)] = NUM_BYTES_BYTE,
            [typeof(char)] = NUM_BYTES_CHAR,
            [typeof(short)] = NUM_BYTES_INT16,
            [typeof(ushort)] = NUM_BYTES_INT16,
            [typeof(int)] = NUM_BYTES_INT32,
            [typeof(uint)] = NUM_BYTES_INT32,
            [typeof(float)] = NUM_BYTES_SINGLE,
            [typeof(double)] = NUM_BYTES_DOUBLE,
            [typeof(long)] = NUM_BYTES_INT64,
            [typeof(ulong)] = NUM_BYTES_INT64
        };

        // LUCENENET specific: Moved all estimates to static initializers to avoid using a static constructor
        //static RamUsageEstimator()
        //{
        //    Initialize empirically measured defaults. We'll modify them to the current
        //     JVM settings later on if possible.
        //    int referenceSize = Constants.RUNTIME_IS_64BIT ? 8 : 4;
        //    int objectHeader = Constants.RUNTIME_IS_64BIT ? 16 : 8;
        //    The following is objectHeader + NUM_BYTES_INT32, but aligned(object alignment)
        //     so on 64 bit JVMs it'll be align(16 + 4, @8) = 24.
        //    int arrayHeader = Constants.RUNTIME_IS_64BIT ? 24 : 12;
        //    int objectAlignment = Constants.RUNTIME_IS_64BIT ? 8 : 4;


        //    Type unsafeClass = null;
        //    object tempTheUnsafe = null;
        //    try
        //    {
        //        unsafeClass = Type.GetType("sun.misc.Unsafe");
        //        FieldInfo unsafeField = unsafeClass.getDeclaredField("theUnsafe");
        //        unsafeField.Accessible = true;
        //        tempTheUnsafe = unsafeField.get(null);
        //    }
        //    catch (Exception e)
        //    {
        //        // Ignore.
        //    }
        //    TheUnsafe = tempTheUnsafe;

        //    // get object reference size by getting scale factor of Object[] arrays:
        //    try
        //    {
        //        Method arrayIndexScaleM = unsafeClass.GetMethod("arrayIndexScale", typeof(Type));
        //        referenceSize = (int)((Number)arrayIndexScaleM.invoke(TheUnsafe, typeof(object[])));
        //    }
        //    catch (Exception e)
        //    {
        //        // ignore.
        //    }

        //    // "best guess" based on reference size. We will attempt to modify
        //    // these to exact values if there is supported infrastructure.
        //    objectHeader = Constants.RUNTIME_IS_64BIT ? (8 + referenceSize) : 8;
        //    arrayHeader = Constants.RUNTIME_IS_64BIT ? (8 + 2 * referenceSize) : 12;

        //    // get the object header size:
        //    // - first try out if the field offsets are not scaled (see warning in Unsafe docs)
        //    // - get the object header size by getting the field offset of the first field of a dummy object
        //    // If the scaling is byte-wise and unsafe is available, enable dynamic size measurement for
        //    // estimateRamUsage().
        //    Method tempObjectFieldOffsetMethod = null;
        //    try
        //    {
        //        Method objectFieldOffsetM = unsafeClass.GetMethod("objectFieldOffset", typeof(FieldInfo));
        //        FieldInfo dummy1Field = typeof(DummyTwoLongObject).getDeclaredField("dummy1");
        //        int ofs1 = (int)((Number)objectFieldOffsetM.invoke(TheUnsafe, dummy1Field));
        //        FieldInfo dummy2Field = typeof(DummyTwoLongObject).getDeclaredField("dummy2");
        //        int ofs2 = (int)((Number)objectFieldOffsetM.invoke(TheUnsafe, dummy2Field));
        //        if (Math.Abs(ofs2 - ofs1) == NUM_BYTES_LONG)
        //        {
        //            FieldInfo baseField = typeof(DummyOneFieldObject).getDeclaredField("base");
        //            objectHeader = (int)((Number)objectFieldOffsetM.invoke(TheUnsafe, baseField));
        //            tempObjectFieldOffsetMethod = objectFieldOffsetM;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Ignore.
        //    }
        //    ObjectFieldOffsetMethod = tempObjectFieldOffsetMethod;

        //    // Get the array header size by retrieving the array base offset
        //    // (offset of the first element of an array).
        //    try
        //    {
        //        Method arrayBaseOffsetM = unsafeClass.GetMethod("arrayBaseOffset", typeof(Type));
        //        // we calculate that only for byte[] arrays, it's actually the same for all types:
        //        arrayHeader = (int)((Number)arrayBaseOffsetM.invoke(TheUnsafe, typeof(sbyte[])));
        //    }
        //    catch (Exception e)
        //    {
        //        // Ignore.
        //    }

        //    NUM_BYTES_OBJECT_REF = referenceSize;
        //    NUM_BYTES_OBJECT_HEADER = objectHeader;
        //    NUM_BYTES_ARRAY_HEADER = arrayHeader;

        //    // Try to get the object alignment (the default seems to be 8 on Hotspot,
        //    // regardless of the architecture).
        //    int objectAlignment = 8;
        //    try
        //    {
        //        Type beanClazz = Type.GetType("com.sun.management.HotSpotDiagnosticMXBean").asSubclass(typeof(PlatformManagedObject));
        //        object hotSpotBean = ManagementFactory.getPlatformMXBean(beanClazz);
        //        if (hotSpotBean != null)
        //        {
        //            Method getVMOptionMethod = beanClazz.GetMethod("getVMOption", typeof(string));
        //            object vmOption = getVMOptionMethod.invoke(hotSpotBean, "ObjectAlignmentInBytes");
        //            objectAlignment = Convert.ToInt32(vmOption.GetType().GetMethod("getValue").invoke(vmOption).ToString(), CultureInfo.InvariantCulture);
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Ignore.
        //    }

        //    NUM_BYTES_OBJECT_ALIGNMENT = objectAlignment;

        //   // LUCENENET specific -this is not being used
        //    JVM_INFO_STRING = "[JVM: " + Constants.JVM_NAME + ", " + Constants.JVM_VERSION + ", " + Constants.JVM_VENDOR + ", " + Constants.JAVA_VENDOR + ", " + Constants.JAVA_VERSION + "]";
        //}

        ///// <summary>
        ///// A handle to <code>sun.misc.Unsafe</code>.
        ///// </summary>
        //private static readonly object TheUnsafe;

        ///// <summary>
        ///// A handle to <code>sun.misc.Unsafe#fieldOffset(Field)</code>.
        ///// </summary>
        //private static readonly Method ObjectFieldOffsetMethod;

        /// <summary>
        /// Cached information about a given class.
        /// </summary>
        private sealed class ClassCache
        {
            public long AlignedShallowInstanceSize { get; private set; }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public FieldInfo[] ReferenceFields { get; private set; }

            public ClassCache(long alignedShallowInstanceSize, FieldInfo[] referenceFields)
            {
                this.AlignedShallowInstanceSize = alignedShallowInstanceSize;
                this.ReferenceFields = referenceFields;
            }
        }

        //// Object with just one field to determine the object header size by getting the offset of the dummy field:
        //private sealed class DummyOneFieldObject
        //{
        //    public sbyte @base;
        //}

        //// Another test object for checking, if the difference in offsets of dummy1 and dummy2 is 8 bytes.
        //// Only then we can be sure that those are real, unscaled offsets:
        //private sealed class DummyTwoLongObject
        //{
        //    public long Dummy1, Dummy2;
        //}

        /// <summary>
        /// Aligns an object size to be the next multiple of <see cref="NUM_BYTES_OBJECT_ALIGNMENT"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long AlignObjectSize(long size)
        {
            size += (long)NUM_BYTES_OBJECT_ALIGNMENT - 1L;
            return size - (size % NUM_BYTES_OBJECT_ALIGNMENT);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:byte[]"/> object. </summary>
        // LUCENENET specific overload for CLS compliance
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(byte[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:sbyte[]"/> object. </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(sbyte[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:bool[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(bool[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:char[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(char[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_CHAR * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:short[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(short[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT16 * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:int[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(int[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT32 * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:float[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(float[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_SINGLE * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:long[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(long[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT64 * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:double[]"/> object. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(double[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_DOUBLE * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:ulong[]"/> object. </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(ulong[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT64 * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:uint[]"/> object. </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(uint[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT32 * arr.Length);
        }

        /// <summary>
        /// Returns the size in bytes of the <see cref="T:ushort[]"/> object. </summary>
        [CLSCompliant(false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(ushort[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT16 * arr.Length);
        }

        /// <summary>
        /// Estimates the RAM usage by the given object. It will
        /// walk the object tree and sum up all referenced objects.
        ///
        /// <para><b>Resource Usage:</b> this method internally uses a set of
        /// every object seen during traversals so it does allocate memory
        /// (it isn't side-effect free). After the method exits, this memory
        /// should be GCed.</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long SizeOf(object obj)
        {
            return MeasureObjectSize(obj);
        }

        /// <summary>
        /// Estimates a "shallow" memory usage of the given object. For arrays, this will be the
        /// memory taken by array storage (no subreferences will be followed). For objects, this
        /// will be the memory taken by the fields.
        /// <para/>
        /// .NET object alignments are also applied.
        /// </summary>
        public static long ShallowSizeOf(object obj)
        {
            if (obj is null)
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
        /// This works with all conventional classes and primitive types, but not with arrays
        /// (the size then depends on the number of elements and varies from object to object).
        /// </summary>
        /// <seealso cref="ShallowSizeOf(object)"/>
        /// <exception cref="ArgumentException"> if <paramref name="clazz"/> is an array class. </exception>
        public static long ShallowSizeOfInstance(Type clazz)
        {
            // LUCENENET: Added guard clause for null
            if (clazz is null)
                throw new ArgumentNullException(nameof(clazz));

            if (clazz.IsArray)
            {
                throw new ArgumentException("this method does not work with array classes.");
            }
            if (clazz.IsPrimitive)
            {
                return primitiveSizes[clazz];
            }

            long size = NUM_BYTES_OBJECT_HEADER;

            // Walk type hierarchy
            for (; clazz != null; clazz = clazz.BaseType)
            {
                FieldInfo[] fields = clazz.GetFields(
                    BindingFlags.Instance | 
                    BindingFlags.NonPublic | 
                    BindingFlags.Public | 
                    BindingFlags.DeclaredOnly | 
                    BindingFlags.Static);
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
        /// Return shallow size of any <paramref name="array"/>.
        /// </summary>
        private static long ShallowSizeOfArray(Array array)
        {
            long size = NUM_BYTES_ARRAY_HEADER;
            int len = array.Length;
            if (len > 0)
            {
                Type arrayElementClazz = array.GetType().GetElementType();
                if (arrayElementClazz.IsPrimitive)
                {
                    size += (long)len * primitiveSizes[arrayElementClazz];
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
            IDictionary<Type, ClassCache> classCache = new JCG.Dictionary<Type, ClassCache>(IdentityEqualityComparer<Type>.Default);
            // Stack of objects pending traversal. Recursion caused stack overflows.
            Stack<object> stack = new Stack<object>();
            stack.Push(root);

            long totalSize = 0;
            while (stack.Count > 0)
            {
                object ob = stack.Pop();

                if (ob is null || seen.Contains(ob))
                {
                    continue;
                }
                seen.Add(ob);

                Type obClazz = ob.GetType();
                // LUCENENET specific - .NET cannot return a null type for an object, so no need to assert it
                if (obClazz.Equals(typeof(string)))
                {
                    // LUCENENET specific - we can get a closer estimate of a string
                    // by using simple math. Reference: http://stackoverflow.com/a/8171099.
                    // This fixes the TestSanity test.
                    totalSize += (2 * (((string)ob).Length + 1));
                }
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
                        if (componentClazz.IsPrimitive)
                        {
                            size += (long)len * primitiveSizes[componentClazz];
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
                        if (!classCache.TryGetValue(obClazz, out ClassCache cachedInfo) || cachedInfo is null)
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
                    catch (Exception e) when (e.IsIllegalAccessException())
                    {
                        // this should never happen as we enabled setAccessible().
                        throw RuntimeException.Create("Reflective field access failed?", e);
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ClassCache CreateCacheEntry(Type clazz)
        {
            ClassCache cachedInfo;
            long shallowInstanceSize = NUM_BYTES_OBJECT_HEADER;
            JCG.List<FieldInfo> referenceFields = new JCG.List<FieldInfo>(32);
            for (Type c = clazz; c != null; c = c.BaseType)
            {
                FieldInfo[] fields = c.GetFields(
                    BindingFlags.Instance | 
                    BindingFlags.NonPublic | 
                    BindingFlags.Public | 
                    BindingFlags.DeclaredOnly | 
                    BindingFlags.Static);
                foreach (FieldInfo f in fields)
                {
                    // LUCENENET specific - exclude fields that are marked with the ExcludeFromRamUsageEstimationAttribute
                    if (!f.IsStatic && f.GetCustomAttribute<ExcludeFromRamUsageEstimationAttribute>(inherit: false) is null)
                    {
                        shallowInstanceSize = AdjustForField(shallowInstanceSize, f);

                        if (!f.FieldType.IsPrimitive)
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
        /// This method returns the maximum representation size of an object. <paramref name="sizeSoFar"/>
        /// is the object's size measured so far. <paramref name="f"/> is the field being probed.
        ///
        /// <para/>The returned offset will be the maximum of whatever was measured so far and
        /// <paramref name="f"/> field's offset and representation size (unaligned).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long AdjustForField(long sizeSoFar, FieldInfo f)
        {
            Type type = f.FieldType;
            int fsize = 0;
            
            if (!(typeof(IntPtr) == type) && !(typeof(UIntPtr) == type))
                fsize = type.IsPrimitive ? primitiveSizes[type] : NUM_BYTES_OBJECT_REF;

            // LUCENENET NOTE: I dont think this will ever not be null
            //if (ObjectFieldOffsetMethod != null)
            //{
            //  try
            //  {
            //    long offsetPlusSize = (long)((Number) ObjectFieldOffsetMethod.invoke(TheUnsafe, f)) + fsize;
            //    return Math.Max(sizeSoFar, offsetPlusSize);
            //  }
            //  catch (Exception ex)
            //  {
            //    throw RuntimeException.Create("Access problem with sun.misc.Unsafe", ex);
            //  }
            //}
            //else
            //{
            //  // TODO: No alignments based on field type/ subclass fields alignments?
            //  return sizeSoFar + fsize;
            //}
            return sizeSoFar + fsize;
        }

        /// <summary>
        /// Returns <c>size</c> in human-readable units (GB, MB, KB or bytes).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HumanReadableUnits(long bytes)
        {
            return HumanReadableUnits(bytes, new NumberFormatInfo() { NumberDecimalDigits = 1 });
        }

        /// <summary>
        /// Returns <c>size</c> in human-readable units (GB, MB, KB or bytes).
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
        /// <seealso cref="SizeOf(object)"/>
        /// <seealso cref="HumanReadableUnits(long)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HumanSizeOf(object @object)
        {
            return HumanReadableUnits(SizeOf(@object));
        }

        /// <summary>
        /// Return a human-readable size of a given object. </summary>
        /// <seealso cref="SizeOf(object)"/>
        /// <seealso cref="HumanReadableUnits(long)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HumanSizeOf(object @object, IFormatProvider df)
        {
            return HumanReadableUnits(SizeOf(@object), df);
        }

        /// <summary>
        /// An identity hash set implemented using open addressing. No null keys are allowed.
        /// <para/>
        /// TODO: If this is useful outside this class, make it public - needs some work
        /// </summary>
        internal sealed class IdentityHashSet<KType> : IEnumerable<KType>
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
            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public object[] Keys
            {
                get => keys;
                set => keys = value;
            }
            private object[] keys;

            /// <summary>
            /// Cached number of assigned slots.
            /// </summary>
            public int Assigned { get; set; }

            /// <summary>
            /// The load factor for this set (fraction of allocated or deleted slots before
            /// the buffers must be rehashed or reallocated).
            /// </summary>
            public float LoadFactor { get; private set; }

            /// <summary>
            /// Cached capacity threshold at which we must resize the buffers.
            /// </summary>
            private int resizeThreshold;

            /// <summary>
            /// Creates a hash set with the default capacity of 16,
            /// load factor of <see cref="DEFAULT_LOAD_FACTOR"/>. 
            /// </summary>
            public IdentityHashSet()
                : this(16, DEFAULT_LOAD_FACTOR)
            {
            }

            /// <summary>
            /// Creates a hash set with the given capacity, load factor of
            /// <see cref="DEFAULT_LOAD_FACTOR"/>.
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

                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(initialCapacity > 0, "Initial capacity must be between (0, {0}].", int.MaxValue);
                    Debugging.Assert(loadFactor > 0 && loadFactor < 1, "Load factor must be between (0, 1).");
                }
                this.LoadFactor = loadFactor;
                AllocateBuffers(RoundCapacity(initialCapacity));
            }

            /// <summary>
            /// Adds a reference to the set. Null keys are not allowed.
            /// </summary>
            public bool Add(KType e)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(e != null, "Null keys not allowed.");

                if (Assigned >= resizeThreshold)
                {
                    ExpandAndRehash();
                }

                int mask = keys.Length - 1;
                int slot = Rehash(e) & mask;
                object existing;
                while ((existing = keys[slot]) != null)
                {
                    if (object.ReferenceEquals(e, existing))
                    {
                        return false; // already found.
                    }
                    slot = (slot + 1) & mask;
                }
                Assigned++;
                keys[slot] = e;
                return true;
            }

            /// <summary>
            /// Checks if the set contains a given ref.
            /// </summary>
            public bool Contains(KType e)
            {
                int mask = keys.Length - 1;
                int slot = Rehash(e) & mask;
                object existing;
                while ((existing = keys[slot]) != null)
                {
                    if (object.ReferenceEquals(e, existing))
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
            /// <para/>The implementation is based on the
            /// finalization step from Austin Appleby's
            /// <c>MurmurHash3</c>.
            /// 
            /// See <a target="_blank" href="http://sites.google.com/site/murmurhash/">http://sites.google.com/site/murmurhash/</a>.
            /// </summary>
            private static int Rehash(object o)
            {
                int k = RuntimeHelpers.GetHashCode(o);
                unchecked
                {
                    k ^= k.TripleShift(16);
                    k *= (int)0x85ebca6b;
                    k ^= k.TripleShift(13);
                    k *= (int)0xc2b2ae35;
                    k ^= k.TripleShift(16);
                }

                return k;
            }

            /// <summary>
            /// Expand the internal storage buffers (capacity) or rehash current keys and
            /// values if there are a lot of deleted slots.
            /// </summary>
            private void ExpandAndRehash()
            {
                object[] oldKeys = this.keys;

                if (Debugging.AssertsEnabled) Debugging.Assert(Assigned >= resizeThreshold);
                AllocateBuffers(NextCapacity(keys.Length));

                /*
                 * Rehash all assigned slots from the old hash table.
                 */
                int mask = keys.Length - 1;
                for (int i = 0; i < oldKeys.Length; i++)
                {
                    object key = oldKeys[i];
                    if (key != null)
                    {
                        int slot = Rehash(key) & mask;
                        while (keys[slot] != null)
                        {
                            slot = (slot + 1) & mask;
                        }
                        keys[slot] = key;
                    }
                }
                Arrays.Fill(oldKeys, null);
            }

            /// <summary>
            /// Allocate internal buffers for a given <paramref name="capacity"/>.
            /// </summary>
            /// <param name="capacity">
            ///          New capacity (must be a power of two). </param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void AllocateBuffers(int capacity)
            {
                this.keys = new object[capacity];
                this.resizeThreshold = (int)(capacity * DEFAULT_LOAD_FACTOR);
            }

            /// <summary>
            /// Return the next possible capacity, counting from the current buffers' size.
            /// </summary>
            private static int NextCapacity(int current) // LUCENENET NOTE: made private, since protected is not valid in a sealed class // LUCENENET: CA1822: Mark members as static
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(current > 0 && ((current & (current - 1)) == 0), "Capacity must be a power of two.");
                    Debugging.Assert((current << 1) > 0, "Maximum capacity exceeded ({0}).", ((int)(0x80000000 >> 1))); // LUCENENET: No need to cast to uint because it already is
                }

                if (current < MIN_CAPACITY / 2)
                {
                    current = MIN_CAPACITY / 2;
                }
                return current << 1;
            }

            /// <summary>
            /// Round the capacity to the next allowed value.
            /// </summary>
            private static int RoundCapacity(int requestedCapacity) // LUCENENET NOTE: made private, since protected is not valid in a sealed class // LUCENENET: CA1822: Mark members as static
            {
                // Maximum positive integer that is a power of two.
                if (requestedCapacity > ((int)(0x80000000 >> 1))) // LUCENENET: No need to cast to uint because it already is
                {
                    return ((int)(0x80000000 >> 1)); // LUCENENET: No need to cast to uint because it already is
                }

                int capacity = MIN_CAPACITY;
                while (capacity < requestedCapacity)
                {
                    capacity <<= 1;
                }

                return capacity;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Clear()
            {
                Assigned = 0;
                Arrays.Fill(keys, null);
            }

            public int Count => Assigned; // LUCENENET NOTE: This was size() in Lucene.

            //public bool IsEmpty // LUCENENET NOTE: in .NET we can just use !Any() on IEnumerable<T>
            //{
            //    get
            //    {
            //        return Count == 0;
            //    }
            //}

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IEnumerator<KType> GetEnumerator()
            {
                return new EnumeratorAnonymousClass(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private sealed class EnumeratorAnonymousClass : IEnumerator<KType>
            {
                private readonly IdentityHashSet<KType> outerInstance;

                public EnumeratorAnonymousClass(IdentityHashSet<KType> outerInstance)
                {
                    this.outerInstance = outerInstance;
                    pos = -1;
                    nextElement = FetchNext();
                }

                internal int pos;
                internal object nextElement;
                internal KType current;

                public bool MoveNext()
                {
                    object r = nextElement;
                    if (nextElement is null)
                    {
                        return false;
                    }

                    nextElement = FetchNext();
                    current = (KType)r;
                    return true;
                }

                public KType Current => current;

                object IEnumerator.Current => Current;

                private object FetchNext()
                {
                    pos++;
                    while (pos < outerInstance.keys.Length && outerInstance.keys[pos] is null)
                    {
                        pos++;
                    }

                    return (pos >= outerInstance.keys.Length ? null : outerInstance.keys[pos]);
                }

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }
        }
    }
}