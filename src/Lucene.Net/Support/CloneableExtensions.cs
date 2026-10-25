namespace Lucene.Net
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
    /// Extensions to clone types and cast to the proper derived type.
    /// </summary>
    public static class CloneableExtensions
    {
        /// <summary>
        /// Creates a clone of the specified object.
        /// </summary>
        /// <typeparam name="T">The compile-time type of the object being cloned.</typeparam>
        /// <param name="source">The object to clone.</param>
        /// <returns>
        /// A clone of <paramref name="source"/>, with the same compile-time type.
        /// </returns>
        /// <remarks>
        /// The depth of the copy and the state shared with the original depend
        /// on the specific type being cloned. See the documentation of
        /// <typeparamref name="T"/> for type-specific details.
        /// </remarks>
        public static T Clone<T>(this T source) where T : ICloneable
        {
            return (T)source.Clone();
        }

        /// <summary>
        /// Specifies that a type is cloneable and requires covariant casting
        /// via the <see cref="Clone{T}(T)"/> extension method.
        /// </summary>
        /// <remarks>
        /// This interface is nested because it has no purpose other than to provide
        /// a constraint for types to expose a strongly typed covariant return type.
        /// We also don't want it to collide with <see cref="System.ICloneable"/>.
        /// </remarks>
        public interface ICloneable
        {
            /// <summary>
            /// Creates a new object that is a copy of the current instance.
            /// </summary>
            /// <returns>The cloned instance.</returns>
            object Clone();
        }
    }
}
