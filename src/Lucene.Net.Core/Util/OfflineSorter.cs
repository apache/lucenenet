using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Store;
using Lucene.Net.Support.Compatibility;

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
    /// On-disk sorting of byte arrays. Each byte array (entry) is a composed of the following
    /// fields:
    /// <ul>
    ///   <li>(two bytes) length of the following byte array,
    ///   <li>exactly the above count of bytes for the sequence to be sorted.
    /// </ul>
    /// </summary>
    public sealed class OfflineSorter
    {
        /// <summary>
        /// Utility class to emit length-prefixed byte[] entries to an output stream for sorting.
        /// Complementary to <seealso cref="ByteSequencesReader"/>.
        /// </summary>
        public class ByteSequencesWriter : IDisposable
        {
            internal readonly DataOutput Os;

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided File </summary>
            public ByteSequencesWriter(string filePath)
                : this(new BinaryWriterDataOutput(new BinaryWriter(new FileStream(filePath, FileMode.Open))))
            {
            }

            /// <summary>
            /// Constructs a ByteSequencesWriter to the provided DataOutput </summary>
            public ByteSequencesWriter(DataOutput os)
            {
                this.Os = os;
            }

            /// <summary>
            /// Writes a BytesRef. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(BytesRef @ref)
            {
                Debug.Assert(@ref != null);
                Write(@ref.Bytes, @ref.Offset, @ref.Length);
            }

            /// <summary>
            /// Writes a byte array. </summary>
            /// <seealso cref= #write(byte[], int, int) </seealso>
            public virtual void Write(byte[] bytes)
            {
                Write(bytes, 0, bytes.Length);
            }

            /// <summary>
            /// Writes a byte array.
            /// <p>
            /// The length is written as a <code>short</code>, followed
            /// by the bytes.
            /// </summary>
            public virtual void Write(byte[] bytes, int off, int len)
            {
                Debug.Assert(bytes != null);
                Debug.Assert(off >= 0 && off + len <= bytes.Length);
                Debug.Assert(len >= 0);
                Os.WriteShort((short)len);
                Os.Write(bytes, off, len);
            }

            /// <summary>
            /// Closes the provided <seealso cref="DataOutput"/> if it is <seealso cref="IDisposable"/>.
            /// </summary>
            public void Dispose()
            {
                var os = Os as IDisposable;
                if (os != null)
                {
                    os.Dispose();
                }
            }
        }
//
//        /// <summary>
//        /// Utility class to read length-prefixed byte[] entries from an input.
//        /// Complementary to <seealso cref="ByteSequencesWriter"/>.
//        /// </summary>
//        public class ByteSequencesReader : IDisposable
//        {
//            internal readonly DataInput inputStream;
//
//            /// <summary>
//            /// Constructs a ByteSequencesReader from the provided File </summary>
//            public ByteSequencesReader(FileInfo file)
//                : this(new DataInputStream(new BufferedInputStream(new FileInputStream(file))))
//            {
//            }
//
//            /// <summary>
//            /// Constructs a ByteSequencesReader from the provided DataInput </summary>
//            public ByteSequencesReader(DataInput inputStream)
//            {
//                this.inputStream = inputStream;
//            }
//
//            /// <summary>
//            /// Reads the next entry into the provided <seealso cref="BytesRef"/>. The internal
//            /// storage is resized if needed.
//            /// </summary>
//            /// <returns> Returns <code>false</code> if EOF occurred when trying to read
//            /// the header of the next sequence. Returns <code>true</code> otherwise. </returns>
//            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
//            public virtual bool Read(BytesRef @ref)
//            {
//                short length;
//                try
//                {
//                    length = inputStream.ReadShort();
//                }
//                catch (EOFException)
//                {
//                    return false;
//                }
//
//                @ref.Grow(length);
//                @ref.Offset = 0;
//                @ref.Length = length;
//                inputStream.ReadFully(@ref.Bytes, 0, length);
//                return true;
//            }
//
//            /// <summary>
//            /// Reads the next entry and returns it if successful.
//            /// </summary>
//            /// <seealso cref= #read(BytesRef)
//            /// </seealso>
//            /// <returns> Returns <code>null</code> if EOF occurred before the next entry
//            /// could be read. </returns>
//            /// <exception cref="EOFException"> if the file ends before the full sequence is read. </exception>
//            public virtual sbyte[] Read()
//            {
//                short length;
//                try
//                {
//                    length = inputStream.ReadShort();
//                }
//                catch (EOFException e)
//                {
//                    return null;
//                }
//
//                Debug.Assert(length >= 0, "Sanity: sequence length < 0: " + length);
//                sbyte[] result = new sbyte[length];
//                inputStream.ReadFully(result);
//                return result;
//            }
//
//            /// <summary>
//            /// Closes the provided <seealso cref="DataInput"/> if it is <seealso cref="IDisposable"/>.
//            /// </summary>
//            public void Dispose()
//            {
//                var @is = inputStream as IDisposable;
//                if (@is != null)
//                {
//                    @is.Dispose();
//                }
//            }
//        }
//
//        /// <summary>
//        /// Returns the comparator in use to sort entries </summary>
//        public IComparer<BytesRef> Comparator
//        {
//            get
//            {
//                return comparator;
//            }
//        }
    }
}