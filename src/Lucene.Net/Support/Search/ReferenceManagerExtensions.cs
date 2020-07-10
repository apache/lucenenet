namespace Lucene.Net.Search
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

    public static class ReferenceManagerExtensions
    {
        /// <summary>
        /// Obtain the current reference.
        /// <para/>
        /// Like <see cref="ReferenceManager{G}.Acquire()"/>, but intended for use in a using
        /// block so calling <see cref="ReferenceManager{G}.Release(G)"/> happens implicitly.
        /// For example:
        /// <para/>
        /// <code>
        /// var searcherManager = new SearcherManager(indexWriter, true, null);
        /// using (var context = searcherManager.GetContext())
        /// {
        ///     IndexSearcher searcher = context.Reference;
        ///     
        ///     // use searcher...
        /// }
        /// </code>
        /// </summary>
        /// <typeparam name="T">The reference type</typeparam>
        /// <param name="referenceManager">this <see cref="ReferenceManager{G}"/></param>
        /// <returns>A <see cref="ReferenceContext{T}"/> instance that holds the 
        /// <see cref="ReferenceContext{T}.Reference"/> and ensures it is released properly 
        /// when <see cref="ReferenceContext{T}.Dispose()"/> is called.</returns>
        public static ReferenceContext<T> GetContext<T>(this ReferenceManager<T> referenceManager) where T : class
        {
            return new ReferenceContext<T>(referenceManager);
        }
    }
}
