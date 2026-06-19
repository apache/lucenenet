namespace Lucene.Net.Support
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
    /// LUCENENET specific: a reclaimer that defers the unmap of a memory-mapped
    /// region until every in-flight reader has drained, so a concurrent close can
    /// never unmap a view out from under a reader mid-dereference (the #1013
    /// <c>AccessViolationException</c>) without the per-access native refcount that
    /// caused #1151.
    /// <para/>
    /// One reclaimer is owned by one mapping and shared by every reader that views
    /// it (a root <see cref="Store.IndexInput"/>, its clones, and any slices). Each
    /// reader calls <see cref="Register"/> once to obtain an opaque token, then
    /// brackets each pointer dereference with <see cref="Enter"/>/<see cref="Exit"/>.
    /// <see cref="Close"/> runs the supplied unmap action once all in-flight readers
    /// have drained.
    /// </summary>
    internal interface IMMapReclaimer
    {
        /// <summary>
        /// Opaque per-reader token returned by <see cref="Register"/> and passed
        /// back to <see cref="Enter"/>/<see cref="Exit"/>.
        /// </summary>
        internal interface IReaderToken;

        /// <summary>
        /// Register a reader (a root input, a clone, or a slice) and return its
        /// token. Each reader registers exactly once, on construction.
        /// </summary>
        IReaderToken Register(object input);

        /// <summary>
        /// Mark the start of a dereference through <paramref name="token"/>. Throws
        /// <see cref="AlreadyClosedException"/> if the mapping has already been
        /// closed (the caller must not dereference in that case).
        /// </summary>
        void Enter(IReaderToken token);

        /// <summary>
        /// Mark the end of a dereference through <paramref name="token"/>. If this
        /// drains the last in-flight reader after a <see cref="Close"/>, performs
        /// the deferred unmap.
        /// </summary>
        void Exit(IReaderToken token);

        /// <summary>
        /// Close the mapping. New <see cref="Enter"/> calls throw from now on; the
        /// supplied <paramref name="unmap"/> action runs once all in-flight readers
        /// have drained (immediately if none are active).
        /// </summary>
        void Close(System.Action unmap);

        /// <summary>
        /// Whether <see cref="Close"/> has been called.
        /// </summary>
        bool IsClosed { get; }
    }
}
