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

using System;
using Lucene.Net.Support;

namespace Lucene.Net.Store
{

    /// <summary>Abstract base class for input from a file in a <see cref="Directory" />.  A
    /// random-access input stream.  Used for all Lucene index input operations.
    /// </summary>
    /// <seealso cref="Directory">
    /// </seealso>
    public abstract class IndexInput : DataInput, ICloneable, IDisposable
    {
        private readonly string resourceDescription;

        protected IndexInput(string resourceDescription)
        {
            if (resourceDescription == null)
            {
                throw new ArgumentNullException("resourceDescription");
            }
            this.resourceDescription = resourceDescription;
        }
        
        /// <summary>Closes the stream to futher operations. </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        protected abstract void Dispose(bool disposing);

        /// <summary>Returns the current position in this file, where the next read will
        /// occur.
        /// </summary>
        /// <seealso cref="Seek(long)">
        /// </seealso>
        public abstract long FilePointer { get; }

        /// <summary>Sets current position in this file, where the next read will occur.</summary>
        /// <seealso cref="FilePointer">
        /// </seealso>
        public abstract void Seek(long pos);

        /// <summary>The number of bytes in the file. </summary>
        public abstract long Length();

        public override string ToString()
        {
            return resourceDescription;
        }

        /// <summary>Returns a clone of this stream.
        /// 
        /// <p/>Clones of a stream access the same data, and are positioned at the same
        /// point as the stream they were cloned from.
        /// 
        /// <p/>Expert: Subclasses must ensure that clones may be positioned at
        /// different points in the input from each other and from the stream they
        /// were cloned from.
        /// </summary>
        public virtual System.Object Clone()
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