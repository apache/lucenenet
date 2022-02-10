using J2N.Threading.Atomic;
using System;
using System.Runtime.CompilerServices;
#if FEATURE_SERIALIZABLE_EXCEPTIONS
using System.Runtime.Serialization;
#endif

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
    /// A convenient class which offers a semi-immutable object wrapper
    /// implementation which allows one to set the value of an object exactly once,
    /// and retrieve it many times. If <see cref="Set(T)"/> is called more than once,
    /// <see cref="AlreadySetException"/> is thrown and the operation
    /// will fail.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class SetOnce<T> // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
        where T : class // LUCENENET specific - added class constraint so we don't accept value types (which cannot be volatile)
    {
        private volatile T obj = default;
        private readonly AtomicBoolean set;

        /// <summary>
        /// A default constructor which does not set the internal object, and allows
        /// setting it by calling <see cref="Set(T)"/>.
        /// </summary>
        public SetOnce()
        {
            set = new AtomicBoolean(false);
        }

        /// <summary>
        /// Creates a new instance with the internal object set to the given object.
        /// Note that any calls to <see cref="Set(T)"/> afterwards will result in
        /// <see cref="AlreadySetException"/>
        /// </summary>
        /// <exception cref="AlreadySetException"> if called more than once </exception>
        /// <seealso cref="Set(T)"/>
        public SetOnce(T obj)
        {
            this.obj = obj;
            set = new AtomicBoolean(true);
        }

        /// <summary>
        /// Sets the given object. If the object has already been set, an exception is thrown. </summary>
        public void Set(T obj)
        {
            if (set.CompareAndSet(false, true))
            {
                this.obj = obj;
            }
            else
            {
                throw new AlreadySetException();
            }
        }

        /// <summary>
        /// Returns the object set by <see cref="Set(T)"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get()
        {
            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object Clone()
        {
            return obj is null ? new SetOnce<T>() : new SetOnce<T>(obj);
        }
    }

    /// <summary>
    /// Thrown when <see cref="SetOnce{T}.Set(T)"/> is called more than once. </summary>
    // LUCENENET specific - de-nested the class from SetOnce<T> to allow the test
    // framework to serialize it without the generic type.
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    public sealed class AlreadySetException : InvalidOperationException
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AlreadySetException"/>.
        /// </summary>
        public AlreadySetException()
            : base("The object cannot be set twice!")
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        private AlreadySetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif
    }
}