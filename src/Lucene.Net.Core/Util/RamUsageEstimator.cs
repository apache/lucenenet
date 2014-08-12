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
    using Lucene.Net.Support;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Linq;
    using System.Runtime.InteropServices;



    /// <summary> 
    ///  <see cref="RamUsageEstimator"/> estimate the size of memory allocation for an object, even though the size of the object
    /// cannot be accurately computed due to the barrier that higher level languages like C# and Java have. 
    /// </summary>
    /// <remarks>
    ///     <para>
    ///        References on .NET memory allocation for objects and arrays: 
    ///     </para>
    ///     <list type="">
    ///         <item>
    ///             <see href="http://atlasteit.wordpress.com/2012/07/18/advanced-c-programming-6-everything-about-memory-allocation-in-net/">
    ///              .NET Memory Allocation
    ///             </see>
    ///         </item>
    ///         
    ///         <item>
    ///             <see href="https://www.simple-talk.com/dotnet/.net-framework/object-overhead-the-hidden-.net-memory--allocation-cost/">
    ///             Hiden Object Overhead.
    ///             </see>
    ///         </item>
    ///         <item>
    ///             <see href="http://stackoverflow.com/questions/1589669/overhead-of-a-net-array">
    ///             Overhead of a .NET Array
    ///             </see>
    ///         </item>
    ///         <item>
    ///             <see href="http://blogs.msdn.com/b/microsoft_press/archive/2012/11/29/new-book-clr-via-c-fourth-edition.aspx">
    ///              Clr via C#
    ///             </see>
    ///         </item>
    ///     </list>
    /// </remarks>
    // The JVM FEATURE enum should only be ported if mono or different version of the 
    // .NET framework handle memory allocation differently.
    public sealed class RamUsageEstimator
    {
        /// <summary>
        /// The number of bytes for one killabyte. 
        /// </summary>
        public const long ONE_KB = 1024L;

        /// <summary>
        /// The number of bytes for one megabyte. 
        /// </summary>
        public const long ONE_MB = ONE_KB * ONE_KB;

        /// <summary>
        /// The number of bytes for one gigbyte. 
        /// </summary>
        public const long ONE_GB = ONE_KB * ONE_MB;

        /// <summary>
        /// The number of bytes that a <see cref="bool"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_BOOLEAN = 1;

        /// <summary>
        /// The number of bytes that an <see cref="sbyte"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_SBYTE = 1;

        /// <summary>
        /// The number of bytes that a byte <see cref="byte"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_BYTE = 1;

        /// <summary>
        /// The number of bytes that a <see cref="char"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_CHAR = 2;

        /// <summary>
        /// The number of bytes that a <see cref="char"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_SHORT = 2;

        /// <summary>
        /// The number of bytes that a <see cref="short"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_USHORT = 2;

        /// <summary>
        /// The number of bytes that a <see cref="ushort"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_INT = 4;

        /// <summary>
        /// The number of bytes that a <see cref="int"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_UINT = 4;

        /// <summary>
        /// The number of bytes that a <see cref="unit"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_FLOAT = 4;

        /// <summary>
        /// The number of bytes that a <see cref="float"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_LONG = 8;

        /// <summary>
        /// The number of bytes that a <see cref="ulong"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_ULONG = 8;

        /// <summary>
        /// The number of bytes that a <see cref="double"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_DOUBLE = 8;

        /// <summary>
        /// The number of bytes that a <see cref="decimal"/> takes up in memory.
        /// </summary>
        public const int NUM_BYTES_DECIMAL = 16;

        /// <summary>
        ///The number of bytes for that an object reference takes up in memory.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_REF;

        /// <summary>
        /// The number of bytes for that the overhead of an object takes up in memory.
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_HEADER;

        /// <summary>
        /// The number of bytes for that the overhead of an value type array takes up in memory.
        /// </summary>
        public static readonly int NUM_BYTES_VALUE_TYPE_ARRAY_HEADER;

        /// <summary>
        /// The number of bytes for that the overhead of an value type array takes up in memory.
        /// </summary>
        public static readonly int NUM_BYTES_REFERENCE_TYPE_ARRAY_HEADER;

        /// <summary>
        /// The number of bytes used to align memory for an object. 
        /// </summary>
        public static readonly int NUM_BYTES_OBJECT_ALIGNMENT;


        /// <summary>
        /// No instantiation
        /// </summary>
        private RamUsageEstimator()
        {
        }



        internal static readonly IDictionary<Type, int> PrimitiveSizes;

        static RamUsageEstimator()
        {
            PrimitiveSizes = new HashMap<Type, int>();

            // 1 
            PrimitiveSizes[typeof(bool)] = NUM_BYTES_BOOLEAN;
            PrimitiveSizes[typeof(byte)] = NUM_BYTES_BYTE;
            PrimitiveSizes[typeof(sbyte)] = NUM_BYTES_SBYTE;

            // 2
            PrimitiveSizes[typeof(char)] = NUM_BYTES_CHAR;
            PrimitiveSizes[typeof(short)] = NUM_BYTES_SHORT;
            PrimitiveSizes[typeof(ushort)] = NUM_BYTES_USHORT;

            // 4
            PrimitiveSizes[typeof(int)] = NUM_BYTES_INT;
            PrimitiveSizes[typeof(uint)] = NUM_BYTES_UINT;
            PrimitiveSizes[typeof(float)] = NUM_BYTES_FLOAT;

            // 8
            PrimitiveSizes[typeof(long)] = NUM_BYTES_LONG;
            PrimitiveSizes[typeof(ulong)] = NUM_BYTES_ULONG;
            PrimitiveSizes[typeof(double)] = NUM_BYTES_DOUBLE;

            // 16
            PrimitiveSizes[typeof(decimal)] = NUM_BYTES_DECIMAL;

            // The Java Version references "sun.misc.Unsafe", the closest class to have one or two of the
            // methods that Unsafe has is System.Runtime.InteropServices.Marshal

            // The logic below is written different than the Java Version so
            // a developer can visually see how the number of bytes are actually
            // added up.

            int typeObjectPointer = 4; // 4 bytes  32 bit
            int syncBlock = 4;
            int arrayLength = 4;
            int elementReferenceMethodTable = 4;
            int memoryAlignmentSize = 4;
            int referenceTypeSize = 4;

            if (Constants.KRE_IS_64BIT)
            {
                referenceTypeSize = 8;
                typeObjectPointer = 8; // 8 bytes  64 bit
                syncBlock = 8; // 8 bytes  64 bit
                arrayLength = 8; // 8 bytes 64 bit
                elementReferenceMethodTable = 8; // 8 bytes 64 bit
                memoryAlignmentSize = 8; // 8 bytes 64 bit
            }

            

            int objectHeader = typeObjectPointer + syncBlock;
            int valueTypeArrayHeader = typeObjectPointer + syncBlock + arrayLength;
            int referenceTypeArrayHeader = typeObjectPointer + syncBlock + arrayLength + elementReferenceMethodTable;

            NUM_BYTES_OBJECT_REF = referenceTypeSize;
            NUM_BYTES_OBJECT_HEADER = objectHeader;
            NUM_BYTES_OBJECT_ALIGNMENT = memoryAlignmentSize;

            // .NET has a different overhead for arrays of value types than
            // it does for arrays of reference types.

            NUM_BYTES_VALUE_TYPE_ARRAY_HEADER = valueTypeArrayHeader;
            NUM_BYTES_REFERENCE_TYPE_ARRAY_HEADER = referenceTypeArrayHeader;
        }

        public static long AdjustForField(long sizeSoFar, FieldInfo f)
        {
            var typeInfo = f.FieldType.GetTypeInfo();
            int fsize = typeInfo.IsPrimitive ? PrimitiveSizes[f.FieldType] : NUM_BYTES_OBJECT_REF;

            if (f.DeclaringType != null && f.DeclaringType.GetTypeInfo().IsValueType)
            {
                try
                {
                    // this is the closest thing that .NET has to getting the FieldOffset.
                    // objectFieldOffsetMethod

                    // here is a .NET Fiddle that shows what the code in Java is attempting to account for
                    // https://dotnetfiddle.net/7fSZ5b

                    // the alternative would be to create an express to use Marshal.OffsetOf<T>(fieldName).
#pragma warning disable 0618
                    var offset = Marshal.OffsetOf(f.DeclaringType, f.Name).ToInt64() + fsize;
#pragma warning restore 0618
                    Math.Max(sizeSoFar, offset);
                }
                catch
                {
                    throw;
                }
            }

            return sizeSoFar + fsize;
        }


        /// <summary>
        /// Estimates the aligned memory size of the object.
        /// </summary>
        /// <param name="size">The current estimated size of the object.</param>
        /// <returns>The size of the object after its alignment. </returns>
        public static long AlignObjectSize(long size)
        {
            size += (long)NUM_BYTES_OBJECT_ALIGNMENT - 1L;
            return size - (size % NUM_BYTES_OBJECT_ALIGNMENT);
        }


        /// <summary> 
        /// Returns the memory size in the human readable units. (GB, MB, KB or bytes).
        /// </summary>
        public static string HumanReadableUnits(long bytes)
        {
            return HumanReadableUnits(bytes, new NumberFormatInfo() { NumberDecimalDigits = 1 });
        }

        /// <summary> 
        /// Returns the memory size in the human readable units. (GB, MB, KB or bytes).
        /// </summary>
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

        /// <summary>
        /// Returns the shallow size of the array's memory allocation in bytes. Use this 
        /// method instead of <see cref="ShallowSizeOf(object)"/> to avoid reflection for 
        /// arrays.
        /// </summary>
        /// <param name="array">The object array.</param>
        /// <returns>The size of the array in bytes.</returns>
        public static long ShallowSizeOf(Object[] array)
        {

            var bytes = (long)NUM_BYTES_OBJECT_REF;
            long size = NUM_BYTES_REFERENCE_TYPE_ARRAY_HEADER + bytes * array.Length;

            return AlignObjectSize(size);
        }


        /// <summary>
        /// Returns the "shallow" size of memory allocation in bytes for the given object. For arrays, this will be the
        /// memory taken by array storage(no subreferences will be followed). For objects, this
        /// will be the memory taken by the fields.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The size of the object in bytes.</returns>
        public static long ShallowSizeOf(Object obj)
        {
            if (obj == null)
                return 0;

            Type type = obj.GetType();

            if (type.IsArray)
            {
                return ShallowSizeOfArray((Array)obj);
            }
            else
            {
                return ShallowSizeOfInstance(type);
            }
        }

        /// <summary>
        /// Returns the shallow size of memory allocation in bytes of the memory allocated that an object
        /// of the specified type could occupy.
        /// </summary>
        /// <param name="instanceType">The type information used to estimate memory allocation.</param>
        /// <returns>The size of the object in bytes.</returns>
        /// <exception cref="ArgumentException">Throws when the <paramref name="instanceType"/> is an <see cref="System.Array"/>.</exception>
        public static long ShallowSizeOfInstance(Type instanceType)
        {
            // typeInfo = clazz.
            var type = instanceType;
            var typeInfo = instanceType.GetTypeInfo();

            if (typeInfo.IsArray)
                throw new ArgumentException("This method does not work with Arrays.");

            if (typeInfo.IsPrimitive)
                return PrimitiveSizes[typeInfo.AsType()];

            long size = NUM_BYTES_OBJECT_HEADER;

            // GetRuntimeFields includes inherited fields. 
            var fields = type.GetRuntimeFields().Where(o => !o.IsStatic);
            foreach (FieldInfo f in fields)
            {
                size = AdjustForField(size, f);
            }

            return AlignObjectSize(size);
        }

        private static long ShallowSizeOfArray(Array array)
        {
            long size = 0L;
            int length = array.Length;
            if (length > 0)
            {
                Type arrayElementType = array.GetType().GetElementType();
                var arrayElementTypeInfo = arrayElementType.GetTypeInfo();

                if (arrayElementTypeInfo.IsPrimitive)
                {
                    size = NUM_BYTES_VALUE_TYPE_ARRAY_HEADER;
                    size += (long)length * PrimitiveSizes[arrayElementType];
                }
      
                else
                {
                    size = NUM_BYTES_REFERENCE_TYPE_ARRAY_HEADER;
                    size += (long)NUM_BYTES_OBJECT_REF * length;
                }
            }

            return AlignObjectSize(size);
        }

        /// <summary>
        /// Retrns the size of the memory allocation for a string.
        /// </summary>
        /// <param name="array">The string.</param>
        /// <returns>The size of the memory allocation.</returns>
        public static long SizeOf(string array)
        {
            return SizeOf(array.ToCharArray());
        }

        /// <summary>
        /// Returns the size of the memory allocation for <typeparamref name="T"/>[]. If the
        /// array is not a primitive type, it defers the array to <see cref="ShallowSizeOfArray(Array)"/> 
        /// </summary>
        /// <typeparam name="T">The element type of the array</typeparam>
        /// <param name="array">The array of <typeparamref name="T"/>.</param>
        /// <returns>The size of the memory allocation.</returns>
        public static long SizeOf<T>(T[] array) where T : struct
        {
            var type = typeof(T);
            if (!PrimitiveSizes.ContainsKey(type))
                return ShallowSizeOfArray(array);

            int bytes = PrimitiveSizes[type];
          
            var size = (long)NUM_BYTES_VALUE_TYPE_ARRAY_HEADER + (long)bytes * array.Length;

            return AlignObjectSize(size);
        }

        /// <summary>
        /// Return the size of the provided array of <see cref="IAccountable"/> by summing
        /// up the shallow size of the array and the <see cref="IAccountable.RamBytesUsed"/> reported
        /// by each <see cref="IAccountable"/>
        /// </summary>
        /// <param name="accountables">The array of <see cref="IAccountable"/></param>
        /// <returns>The memory allocation size.</returns>
        public static long SizeOf(IAccountable[] accountables)
        {
            var size = ShallowSizeOf(accountables);
            foreach (var accountable in accountables)
            {
                size += accountable.RamBytesUsed;
            }

            return size;
        }
    }
}