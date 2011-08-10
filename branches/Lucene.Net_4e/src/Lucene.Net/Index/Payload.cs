// -----------------------------------------------------------------------
// <copyright company="Apache" file="Payload.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------



namespace Lucene.Net.Index
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Support;
    using Util;

    /// <summary>
    /// TODO: port
    /// </summary>
    public class Payload : ICloneable
    {
        private byte[] data;

        /// <summary>
        /// Initializes a new instance of the <see cref="Payload"/> class. This
        /// constructor will create an empty payload with a null for the <see cref="Data"/>.
        /// </summary>
        public Payload()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Payload"/> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="limit">The limit.</param>
        public Payload(byte[] data, int offset = 0, int limit = 0)
        {
            this.SetData(data, offset, limit);
        }

        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public int Offset { get; set; }

        /// <summary>
        /// Gets or sets the length.
        /// </summary>
        /// <value>The length.</value>
        public int Length { get; protected set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public byte[] Data
        {
            get
            {
                return this.data;
            }

            set
            {
                this.SetData(value);
            }
        }

        /// <summary>
        /// Retrieves the byte at the given index. Similar to CharAt
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>An instance of <see cref="Byte"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        ///     Thrown when the specified index is less than 0 or greater than 
        ///     or equal to the <see cref="Length"/>
        /// </exception>
        public virtual byte ByteAt(int index)
        {
            if (0 <= index && index < this.Length)
                return this.Data[this.Offset + index];

            throw new IndexOutOfRangeException(
                string.Format(
                    "The index must be greater than 0 and less than the length '{0}', The index was '{1}'",
                    this.Length, 
                    index));
        }

        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public object Clone()
        {
            Payload clone = (Payload)this.MemberwiseClone();
            
            if (this.Offset == 0 && this.Length == this.Data.Length)
            {
                clone.data = new byte[this.Data.Length];
                this.CopyTo(clone.data);
            } 
            else
            {
                clone.data = this.ToByteArray();
                clone.Offset = 0;
            }

            return clone;
        }

        /// <summary>
        /// Copies the payload data to a byte array.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="offset">The offset of the target. The default is 0.</param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown when <paramref name="target"/> is null.
        /// </exception>
        /// <exception cref="IndexOutOfRangeException">
        ///     Thrown when this instances <see cref="Length"/> is greater than 
        ///     the combined target length and offset.
        /// </exception>
        public virtual void CopyTo(byte[] target, int offset = 0)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            if (this.Length > target.Length + offset)
                throw new IndexOutOfRangeException(
                    string.Format(
                        "The combined target length and offset '{0}' must be smaller the payload length '{1}' ",
                        target.Length + offset,
                        this.Length));

            Array.Copy(this.Data, this.Offset, target, offset, this.Length);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///    <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            if (!(obj is Payload))
                return false;

            Payload payload = (Payload)obj;

            if (this.Length == payload.Length)
            { 
                for (int i = 0; i < this.Length; i++)
                {
                    if (this.Data[this.Offset + i] != payload.Data[payload.Offset + i])
                        return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.Data.CreateHashCode(this.Offset, this.Offset + this.Length);
        }

        /// <summary>
        /// Sets the payload data. A reference to the passed-in aray is held but 
        /// not copied 
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="offset">The offset of the data.</param>
        /// <param name="length">The length of the data.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="offset"/> is less than 0
        /// or when the <paramref name="offset"/> and <paramref name="length"/>
        /// is greater than <paramref name="data"/>'s length.
        /// </exception>
        public void SetData(byte[] data, int offset = 0, int length = 0)
        {
            if (offset < 0 || (offset + length) > data.Length)
                throw new ArgumentException(
                    string.Format(
                        "The offset must be 0 or greater and the offset and length " +
                        "combined must be less that length of byte[]. The offset was '{0}' ",
                        offset),
                    "offset");

            this.data = data;
            this.Offset = offset;
            this.Length = length == 0 ? data.Length : length;
        }

        /// <summary>
        /// Allocates a new byte array. Then copies the payload data into the new array 
        /// and returns it.
        /// </summary>
        /// <returns>An instance of <see cref="T:System.Byte[]"/>.</returns>
        public virtual byte[] ToByteArray()
        {
            byte[] copy = new byte[this.Length];
            Array.Copy(this.Data, this.Offset, copy, 0, this.Length);

            return copy;
        }
    }
}