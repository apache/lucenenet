using Lucene.Net.Index;
using System.Linq;

namespace Lucene.Net.Documents
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
    /// Extension methods to the <see cref="Document"/> class.
    /// </summary>
    public static class DocumentExtensions
    {
        /// <summary>
        /// Returns a field with the given name if any exist in this document cast to type <typeparamref name="T"/>, or
        /// <c>null</c>. If multiple fields exists with this name, this method returns the
        /// first value added.
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <exception cref="System.InvalidCastException">If the field type cannot be cast to <typeparamref name="T"/>.</exception>
        public static T GetField<T>(this Document document, string name) where T : IIndexableField
        {
            return (T)document.GetField(name);
        }

        /// <summary>
        /// Returns an array of <see cref="IIndexableField"/>s with the given name, cast to type <typeparamref name="T"/>.
        /// This method returns an empty array when there are no
        /// matching fields. It never returns <c>null</c>.
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="name"> the name of the field </param>
        /// <returns> a <see cref="T:IndexableField[]"/> array </returns>
        /// <exception cref="System.InvalidCastException">If the field type cannot be cast to <typeparam name="T"/>.</exception>
        public static T[] GetFields<T>(this Document document, string name) where T : IIndexableField
        {
            return document.GetFields(name).Cast<T>().ToArray();
        }
    }
}
