using Lucene.Net.Index;

namespace Lucene.Net.Documents.Extensions
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
    /// Extension methods to the <see cref="IIndexableField"/> interface.
    /// </summary>
    public static class IndexableFieldExtensions
    {
        /// <summary>
        /// Returns the field value as <see cref="byte"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static byte GetByteValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetByteValue().GetValueOrDefault();
        }

        /// <summary>
        /// Returns the field value as <see cref="short"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static short GetInt16ValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetInt16Value().GetValueOrDefault();
        }

        /// <summary>
        /// Returns the field value as <see cref="int"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static int GetInt32ValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetInt32Value().GetValueOrDefault();
        }

        /// <summary>
        /// Returns the field value as <see cref="long"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static long GetInt64ValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetInt64Value().GetValueOrDefault();
        }

        /// <summary>
        /// Returns the field value as <see cref="float"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static float GetSingleValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetSingleValue().GetValueOrDefault();
        }

        /// <summary>
        /// Returns the field value as <see cref="double"/> or <c>0</c> if the type
        /// is non-numeric.
        /// </summary>
        /// <param name="field">This <see cref="IIndexableField"/>.</param>
        /// <returns>The field value or <c>0</c> if the type is non-numeric.</returns>
        public static double GetDoubleValueOrDefault(this IIndexableField field)
        {
            if (field is null) return default;
            return field.GetDoubleValue().GetValueOrDefault();
        }
    }
}
