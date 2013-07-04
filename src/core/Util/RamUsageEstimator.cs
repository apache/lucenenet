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

using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace Lucene.Net.Util
{

    /// <summary> Estimates the size of a given Object using a given MemoryModel for primitive
    /// size information.
    /// 
    /// Resource Usage: 
    /// 
    /// Internally uses a Map to temporally hold a reference to every
    /// object seen. 
    /// 
    /// If checkIntered, all Strings checked will be interned, but those
    /// that were not already interned will be released for GC when the
    /// estimate is complete.
    /// </summary>
    public sealed class RamUsageEstimator
    {
        public static readonly string JVM_INFO_STRING;

        public const long ONE_KB = 1024L;

        public const long ONE_MB = ONE_KB * ONE_KB;

        public const long ONE_GB = ONE_KB * ONE_MB;

        /// <summary>
        /// No instantiation
        /// </summary>
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

        public static readonly int NUM_BYTES_OBJECT_REF;

        public static readonly int NUM_BYTES_OBJECT_HEADER;

        public static readonly int NUM_BYTES_ARRAY_HEADER;

        public static readonly int NUM_BYTES_OBJECT_ALIGNMENT;

        private static readonly IDictionary<Type, int> primitiveSizes;

        static RamUsageEstimator()
        {
            primitiveSizes = new HashMap<Type, int>();
            primitiveSizes[typeof(bool)] = NUM_BYTES_BOOLEAN;
            primitiveSizes[typeof(byte)] = NUM_BYTES_BYTE;
            primitiveSizes[typeof(sbyte)] = NUM_BYTES_BYTE;
            primitiveSizes[typeof(char)] = NUM_BYTES_CHAR;
            primitiveSizes[typeof(short)] = NUM_BYTES_SHORT;
            primitiveSizes[typeof(int)] = NUM_BYTES_INT;
            primitiveSizes[typeof(float)] = NUM_BYTES_FLOAT;
            primitiveSizes[typeof(double)] = NUM_BYTES_DOUBLE;
            primitiveSizes[typeof(long)] = NUM_BYTES_LONG;

            // These were copied from the Java version of lucene and may not be correct.
            // The referenceSize and objectHeader seem to be correct according to 
            // https://www.simple-talk.com/dotnet/.net-framework/object-overhead-the-hidden-.net-memory--allocation-cost/
            int referenceSize = Constants.JRE_IS_64BIT ? 8 : 4;
            int objectHeader = Constants.JRE_IS_64BIT ? 16 : 8;
            int arrayHeader = Constants.JRE_IS_64BIT ? 24 : 12;
            int objectAlignment = Constants.JRE_IS_64BIT ? 8 : 4;

            NUM_BYTES_OBJECT_REF = referenceSize;
            NUM_BYTES_OBJECT_HEADER = objectHeader;
            NUM_BYTES_ARRAY_HEADER = arrayHeader;
            NUM_BYTES_OBJECT_ALIGNMENT = objectAlignment;

            JVM_INFO_STRING = "[JVM: " +
                Constants.JVM_NAME + ", " + Constants.JVM_VERSION + ", " + Constants.JVM_VENDOR + ", " +
                Constants.JAVA_VENDOR + ", " + Constants.JAVA_VERSION + "]";
        }

        private sealed class ClassCache
        {
            public readonly long alignedShallowInstanceSize;
            public readonly FieldInfo[] referenceFields;

            public ClassCache(long alignedShallowInstanceSize, FieldInfo[] referenceFields)
            {
                this.alignedShallowInstanceSize = alignedShallowInstanceSize;
                this.referenceFields = referenceFields;
            }
        }

        public static long AlignObjectSize(long size)
        {
            size += (long)NUM_BYTES_OBJECT_ALIGNMENT - 1L;
            return size - (size % NUM_BYTES_OBJECT_ALIGNMENT);
        }

        public static long SizeOf(byte[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        public static long SizeOf(sbyte[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        public static long SizeOf(bool[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + arr.Length);
        }

        public static long SizeOf(char[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_CHAR * arr.Length);
        }

        public static long SizeOf(short[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_SHORT * arr.Length);
        }

        public static long SizeOf(int[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_INT * arr.Length);
        }

        public static long SizeOf(float[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_FLOAT * arr.Length);
        }

        public static long SizeOf(long[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_LONG * arr.Length);
        }

        public static long SizeOf(double[] arr)
        {
            return AlignObjectSize((long)NUM_BYTES_ARRAY_HEADER + (long)NUM_BYTES_DOUBLE * arr.Length);
        }

        public static long SizeOf(Object obj)
        {
            return MeasureObjectSize(obj);
        }

        public static long ShallowSizeOf(Object obj)
        {
            if (obj == null) return 0;
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

        public static long ShallowSizeOfInstance(Type clazz)
        {
            if (clazz.IsArray)
                throw new ArgumentException("This method does not work with array classes.");
            if (clazz.IsPrimitive)
                return primitiveSizes[clazz];

            long size = NUM_BYTES_OBJECT_HEADER;

            // Walk type hierarchy
            for (; clazz != null; clazz = clazz.BaseType)
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

        private static long MeasureObjectSize(Object root)
        {
            // Objects seen so far.
            HashSet<Object> seen = new HashSet<Object>();
            // Class cache with reference Field and precalculated shallow size. 
            HashMap<Type, ClassCache> classCache = new HashMap<Type, ClassCache>();
            // Stack of objects pending traversal. Recursion caused stack overflows. 
            Stack<Object> stack = new Stack<Object>();
            stack.Push(root);

            long totalSize = 0;
            while (stack.Count > 0)
            {
                Object ob = stack.Pop();

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
                                Object o = array.GetValue(i);
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
                    ClassCache cachedInfo = classCache[obClazz];
                    if (cachedInfo == null)
                    {
                        classCache[obClazz] = cachedInfo = CreateCacheEntry(obClazz);
                    }

                    foreach (FieldInfo f in cachedInfo.referenceFields)
                    {
                        // Fast path to eliminate redundancies.
                        Object o = f.GetValue(ob);
                        if (o != null && !seen.Contains(o))
                        {
                            stack.Push(o);
                        }
                    }

                    totalSize += cachedInfo.alignedShallowInstanceSize;

                }
            }

            // Help the GC (?).
            seen.Clear();
            stack.Clear();
            classCache.Clear();

            return totalSize;
        }

        private static ClassCache CreateCacheEntry(Type clazz)
        {
            ClassCache cachedInfo;
            long shallowInstanceSize = NUM_BYTES_OBJECT_HEADER;
            List<FieldInfo> referenceFields = new List<FieldInfo>(32);
            for (Type c = clazz; c != null; c = c.BaseType)
            {
                FieldInfo[] fields = c.GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo f in fields)
                {
                    shallowInstanceSize = AdjustForField(shallowInstanceSize, f);

                    if (!f.FieldType.IsPrimitive)
                    {
                        referenceFields.Add(f);
                    }
                }
            }

            cachedInfo = new ClassCache(
                AlignObjectSize(shallowInstanceSize),
                referenceFields.ToArray());
            return cachedInfo;
        }

        private static long AdjustForField(long sizeSoFar, FieldInfo f)
        {
            Type type = f.FieldType;
            int fsize = type.IsPrimitive ? primitiveSizes[type] : NUM_BYTES_OBJECT_REF;

            // TODO: there was a lot of other stuff here, not sure if needed
            return sizeSoFar + fsize;
        }
        
        public static string HumanReadableUnits(long bytes)
        {
            return HumanReadableUnits(bytes, new NumberFormatInfo() { NumberDecimalDigits = 1 });
        }

        /// <summary> Return good default units based on byte size.</summary>
        public static string HumanReadableUnits(long bytes, IFormatProvider df)
        {
            string newSizeAndUnits;

            if (bytes / ONE_GB > 0)
            {
                newSizeAndUnits = System.Convert.ToString(((float)bytes / ONE_GB), df) + " GB";
            }
            else if (bytes / ONE_MB > 0)
            {
                newSizeAndUnits = System.Convert.ToString((float)bytes / ONE_MB, df) + " MB";
            }
            else if (bytes / ONE_KB > 0)
            {
                newSizeAndUnits = System.Convert.ToString((float)bytes / ONE_KB, df) + " KB";
            }
            else
            {
                newSizeAndUnits = System.Convert.ToString(bytes) + " bytes";
            }

            return newSizeAndUnits;
        }

        //public long EstimateRamUsage(System.Object obj)
        //{
        //    long size = Size(obj);
        //    seen.Clear();
        //    return size;
        //}

        //private long Size(System.Object obj)
        //{
        //    if (obj == null)
        //    {
        //        return 0;
        //    }
        //    // interned not part of this object
        //    if (checkInterned && obj is System.String && obj == (System.Object) String.Intern(((System.String) obj)))
        //    {
        //        // interned string will be eligible
        //        // for GC on
        //        // estimateRamUsage(Object) return
        //        return 0;
        //    }

        //    // skip if we have seen before
        //    if (seen.ContainsKey(obj))
        //    {
        //        return 0;
        //    }

        //    // add to seen
        //    seen[obj] = null;

        //    System.Type clazz = obj.GetType();
        //    if (clazz.IsArray)
        //    {
        //        return SizeOfArray(obj);
        //    }

        //    long size = 0;

        //    // walk type hierarchy
        //    while (clazz != null)
        //    {
        //        System.Reflection.FieldInfo[] fields = clazz.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Static);
        //        for (int i = 0; i < fields.Length; i++)
        //        {
        //            if (fields[i].IsStatic)
        //            {
        //                continue;
        //            }

        //            if (fields[i].FieldType.IsPrimitive)
        //            {
        //                size += memoryModel.GetPrimitiveSize(fields[i].FieldType);
        //            }
        //            else
        //            {
        //                size += refSize;
        //                fields[i].GetType(); 
        //                try
        //                {
        //                    System.Object value_Renamed = fields[i].GetValue(obj);
        //                    if (value_Renamed != null)
        //                    {
        //                        size += Size(value_Renamed);
        //                    }
        //                }
        //                catch (System.UnauthorizedAccessException)
        //                {
        //                    // ignore for now?
        //                }
        //            }
        //        }
        //        clazz = clazz.BaseType;
        //    }
        //    size += classSize;
        //    return size;
        //}

        //private long SizeOfArray(System.Object obj)
        //{
        //    int len = ((System.Array) obj).Length;
        //    if (len == 0)
        //    {
        //        return 0;
        //    }
        //    long size = arraySize;
        //    System.Type arrayElementClazz = obj.GetType().GetElementType();
        //    if (arrayElementClazz.IsPrimitive)
        //    {
        //        size += len * memoryModel.GetPrimitiveSize(arrayElementClazz);
        //    }
        //    else
        //    {
        //        for (int i = 0; i < len; i++)
        //        {
        //            size += refSize + Size(((System.Array) obj).GetValue(i));
        //        }
        //    }

        //    return size;
        //}


    }
}