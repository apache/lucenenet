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
    /// Abstract base class for input from a file in a <seealso cref="Directory"/>.  A
    /// random-access input stream.  Used for all Lucene index input operations.
    ///
    /// <p>{@code IndexInput} may only be used from one thread, because it is not
    /// thread safe (it keeps internal state like file position). To allow
    /// multithreaded use, every {@code IndexInput} instance must be cloned before
    /// used in another thread. Subclasses must therefore implement <seealso cref="#clone()"/>,
    /// returning a new {@code IndexInput} which operates on the same underlying
    /// resource, but positioned independently. Lucene never closes cloned
    /// {@code IndexInput}s, it will only do this on the original one.
    /// The original instance must take care that cloned instances throw
    /// <seealso cref="AlreadyClosedException"/> when the original one is closed.
    /// </summary>
    /// <seealso cref= Directory </seealso>
    public abstract class IndexInput : DataInput, IDisposable
    {
        private readonly string ResourceDescription;

        /// <summary>
        /// resourceDescription should be a non-null, opaque string
        ///  describing this resource; it's returned from
        ///  <seealso cref="#toString"/>.
        /// </summary>
        protected internal IndexInput(string resourceDescription)
        {
            if (resourceDescription == null)
            {
                throw new System.ArgumentException("resourceDescription must not be null");
            }
            this.ResourceDescription = resourceDescription;
        }

        /// <summary>
        /// Closes the stream to further operations. </summary>
        public abstract void Dispose();

        /// <summary>
        /// Returns the current position in this file, where the next read will
        /// occur. </summary>
        /// <seealso cref= #seek(long) </seealso>
        public abstract long FilePointer { get; }

        /// <summary>
        /// Sets current position in this file, where the next read will occur. </summary>
        /// <seealso cref= #getFilePointer() </seealso>
        public abstract void Seek(long pos);

        /// <summary>
        /// The number of bytes in the file. </summary>
        public abstract long Length();

        public override string ToString()
        {
            return ResourceDescription;
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p><b>Warning:</b> Lucene never closes cloned
        /// {@code IndexInput}s, it will only do this on the original one.
        /// The original instance must take care that cloned instances throw
        /// <seealso cref="AlreadyClosedException"/> when the original one is closed.
        /// </summary>
        public override object Clone()
        {
            IndexInput clone = null;
            try
            {
                clone = (IndexInput)base.MemberwiseClone();
            }
            catch (System.Exception)
            {
            }

            return clone;
        }
    }
}