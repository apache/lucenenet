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
    /// Abstract base class for input from a file in a <see cref="Directory"/>.  A
    /// random-access input stream.  Used for all Lucene index input operations.
    ///
    /// <para/><see cref="IndexInput"/> may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position). To allow
    /// multithreaded use, every <see cref="IndexInput"/> instance must be cloned before
    /// used in another thread. Subclasses must therefore implement <see cref="Clone()"/>,
    /// returning a new <see cref="IndexInput"/> which operates on the same underlying
    /// resource, but positioned independently. Lucene never closes cloned
    /// <see cref="IndexInput"/>s, it will only do this on the original one.
    /// The original instance must take care that cloned instances throw
    /// <see cref="ObjectDisposedException"/> when the original one is closed.
    /// </summary>
    /// <seealso cref="Directory"/>
    public abstract class IndexInput : DataInput, IDisposable // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private readonly string resourceDescription;

        /// <summary>
        /// <paramref name="resourceDescription"/> should be a non-null, opaque string
        /// describing this resource; it's returned from
        /// <see cref="ToString()"/>.
        /// </summary>
        protected IndexInput(string resourceDescription)
        {
            // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.resourceDescription = resourceDescription ?? throw new ArgumentNullException(nameof(resourceDescription), $"{nameof(resourceDescription)} must not be null");
        }

        /// <summary>
        /// Closes the stream to further operations. </summary>
        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the stream to further operations. </summary>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Returns the current position in this file, where the next read will
        /// occur.
        /// <para/>
        /// This was getFilePointer() in Lucene.
        /// </summary>
        /// <seealso cref="Seek(long)"/>
        public abstract long Position { get; } // LUCENENET specific: Renamed Position to match FileStream

        /// <summary>
        /// Sets current position in this file, where the next read will occur.
        /// </summary>
        /// <seealso cref="Position"/>
        public abstract void Seek(long pos);

        /// <summary>
        /// The number of bytes in the file. </summary>
        public abstract long Length { get; }

        /// <summary>
        /// Returns the resourceDescription that was passed into the constructor.
        /// </summary>
        public override string ToString()
        {
            return resourceDescription;
        }

        /// <summary>
        /// Returns a clone of this stream.
        ///
        /// <para/>Clones of a stream access the same data, and are positioned at the same
        /// point as the stream they were cloned from.
        ///
        /// <para/>Expert: Subclasses must ensure that clones may be positioned at
        /// different points in the input from each other and from the stream they
        /// were cloned from.
        /// 
        /// <para/><b>Warning:</b> Lucene never closes cloned
        /// <see cref="IndexInput"/>s, it will only do this on the original one.
        /// The original instance must take care that cloned instances throw
        /// <see cref="ObjectDisposedException"/> when the original one is closed.
        /// </summary>
        public override object Clone()
        {
            return (IndexInput)base.Clone();
        }
    }
}