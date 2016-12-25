using System;

namespace Lucene.Net.Store
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
    /// Abstract base class for output to a file in a Directory.  A random-access
    /// output stream.  Used for all Lucene index output operations.
    ///
    /// <p>{@code IndexOutput} may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position).
    /// </summary>
    /// <seealso cref= Directory </seealso>
    /// <seealso cref= IndexInput </seealso>
    public abstract class IndexOutput : DataOutput, IDisposable
    {
        /// <summary>
        /// Forces any buffered output to be written. </summary>
        public abstract void Flush();

        /// <summary>
        /// Closes this stream to further operations. </summary>
        public abstract void Dispose();

        /// <summary>
        /// Returns the current position in this file, where the next write will
        /// occur. </summary>
        /// <seealso cref= #seek(long) </seealso>
        public abstract long FilePointer { get; }

        /// <summary>
        /// Sets current position in this file, where the next write will occur. </summary>
        /// <seealso cref= #getFilePointer() </seealso>
        /// @deprecated (4.1) this method will be removed in Lucene 5.0
        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public abstract void Seek(long pos);

        /// <summary>
        /// Returns the current checksum of bytes written so far </summary>
        public abstract long Checksum { get; }

        /// <summary>
        /// The number of bytes in the file. </summary>
        //public abstract long Length();

        /// <summary>
        /// Set the file length. By default, this method does
        /// nothing (it's optional for a Directory to implement
        /// it).  But, certain Directory implementations (for </summary>
        /// example <see cref="FSDirectory"/>) can use this to inform the
        /// underlying IO system to pre-allocate the file to the
        /// specified size.  If the length is longer than the
        /// current file length, the bytes added to the file are
        /// undefined.  Otherwise the file is truncated.
        /// <param name="length"> file length </param>
        public virtual long Length
        {
            get;
            set;
        }
    }
}