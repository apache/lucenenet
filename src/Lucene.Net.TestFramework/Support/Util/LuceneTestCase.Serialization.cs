#if FEATURE_SERIALIZABLE
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

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

    public abstract partial class LuceneTestCase
    {
#pragma warning disable SYSLIB0011 // Type or member is obsolete (BinaryFormatter)
        /// <summary>
        /// Serializes <paramref name="source"/> using <see cref="BinaryFormatter"/>
        /// and returns a <see cref="MemoryStream"/> with the result of the serialzation.
        /// This method is not meant to scale, it is only for quick serialization verification
        /// in tests.
        /// </summary>
        /// <param name="source">The object to serialize. It must be serializable.</param>
        /// <returns>A <see cref="MemoryStream"/> containing the serialized result.</returns>
        internal static MemoryStream Serialize(object source)
        {
            IFormatter formatter = new BinaryFormatter();
            MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, source);
            return stream;
        }

        /// <summary>
        /// Deserializes the given <paramref name="stream"/> into an object instance
        /// of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to cast to after serialization.</typeparam>
        /// <param name="stream">The stream to deserialize.</param>
        /// <returns>A deserialized object instance.</returns>
        internal static T Deserialize<T>(Stream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Position = 0;
            return (T)formatter.Deserialize(stream);
        }
#pragma warning restore SYSLIB0011 // Type or member is obsolete (BinaryFormatter)

        /// <summary>
        /// Uses <see cref="BinaryFormatter"/> and a <see cref="MemoryStream"/>
        /// to serialize and then deserialize <paramref name="source"/>. If the
        /// operation succeeds it means that serialization didn't fail, but extra
        /// checks should be made on the members of the object to ensure they survived
        /// the trip.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="source"/>.</typeparam>
        /// <param name="source">An object to clone. It must be serializable.</param>
        /// <returns>The deserialized object instance (a copy of <paramref name="source"/>).</returns>
        internal static T Clone<T>(T source)
        {
            return Deserialize<T>(Serialize(source));
        }

        /// <summary>
        /// Does a check to ensure a type can serialize and deserialize.
        /// <para/>
        /// NOTE: This only checks errors. It does not verify that the members survived
        /// intact. To make that check, use <see cref="Clone{T}(T)"/> instead.
        /// </summary>
        /// <typeparam name="T">The type of <paramref name="source"/>.</typeparam>
        /// <param name="source">An object to serialize. It must be serializable.</param>
        /// <param name="exception">The output <see cref="SerializationException"/> throw if serialization fails.</param>
        /// <returns><c>true</c> if the serialization operation was successful; otherwise <c>false</c>.</returns>
        internal static bool TypeCanSerialize<T>(T source, out SerializationException exception)
        {
            try
            {
                Clone(source);
            }
            catch (SerializationException e)
            {
                exception = e;
                return false;
            }

            exception = null;
            return true;
        }
    }
}
#endif