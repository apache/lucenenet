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

namespace Lucene.Net.Util
{
    using System;
    using Lucene.Net.Support;

  

    /// <summary>
    /// A convenient class which offers a semi-immutable object wrapper
    /// implementation which allows one to set the value of an object exactly once,
    /// and retrieve it many times. 
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    public class SetOnce<T> : Lucene.Net.Support.ICloneable
    {
        private T value;
        private volatile bool isSet;

        /// <summary>
        /// Initializes a new instance of <see cref="SetOnce{T}"/>
        /// </summary>
        public SetOnce()
        {
            this.isSet = false;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SetOnce{T}"/> with the 
        /// specified <paramref name="value"/>/
        /// </summary>
        /// <param name="value">The value to set.</param>
        public SetOnce(T value)
        {
            this.Set(value);
        }

        /// <summary>
        /// Gets whether or not the value is already set.
        /// </summary>
        public bool ValueIsSet
        {
            get { return this.isSet; }
        }

        /// <summary>
        /// Gets or sets the value once.
        /// </summary>
        public T Value
        {
            get { return this.value; }
            set { this.Set(value); }
        }

        /// <summary>
        /// Sets the value.
        /// </summary>
        /// <param name="value">The value to be set.</param>
        /// <exception cref="SetOnce.AlreadySetException">Thrown when the value has already been set.</exception>
        protected void Set(T value)
        {
            if (!Object.ReferenceEquals(this.value, value) && isSet)
                throw new AlreadySetException("value has already been set");

            this.isSet = true;
            this.value = value;
        }

        [Serializable]
        public class AlreadySetException : Exception
        {
            public AlreadySetException() : this("The object cannot be set more than once.") { }
            public AlreadySetException(string message) : base(message) { }
            public AlreadySetException(string message, Exception inner) : base(message, inner) { }

        }

        public object Clone(bool deepClone)
        {
            if (deepClone)
                throw new DeepCloneNotSupportedException();

            return this.MemberwiseClone();
        }
    }
}
