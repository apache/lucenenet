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
    /// Extension of <see cref="IndexInput"/>, computing checksum as it goes.
    /// Callers can retrieve the checksum via <see cref="Checksum"/>.
    /// </summary>
    public abstract class ChecksumIndexInput : IndexInput
    {
        /// <summary>
        /// <paramref name="resourceDescription"/> should be a non-null, opaque string
        /// describing this resource; it's returned from
        /// <see cref="object.ToString()"/>.
        /// </summary>
        protected ChecksumIndexInput(string resourceDescription)
            : base(resourceDescription)
        {
        }

        /// <summary>
        /// Returns the current checksum value </summary>
        public abstract long Checksum { get; }

        /// <summary>
        /// Sets current position in this file, where the next read will occur. 
        /// <para/>
        /// <see cref="ChecksumIndexInput"/> can only seek forward and seeks are expensive
        /// since they imply to read bytes in-between the current position and the
        /// target position in order to update the checksum.
        /// </summary>
        public override void Seek(long pos)
        {
            long skip = pos - Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (skip < 0)
            {
                throw IllegalStateException.Create(this.GetType() + " cannot seek backwards");
            }
            SkipBytes(skip);
        }
    }
}