using System;

namespace Lucene.Net.Index
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

    public abstract partial class IndexReader
    {
        /// <summary>
        /// A custom listener that's invoked when the <see cref="IndexReader"/>
        /// is closed.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        [Obsolete("Use IReaderDisposedListener interface instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public interface IReaderClosedListener
        {
            /// <summary>
            /// Invoked when the <see cref="IndexReader"/> is closed. </summary>
            void OnClose(IndexReader reader);
        }

        [Obsolete("This class will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private sealed class ReaderCloseListenerWrapper : IReaderDisposedListener
        {
            private readonly IReaderClosedListener listener;
            public ReaderCloseListenerWrapper(IReaderClosedListener listener)
            {
                this.listener = listener ?? throw new ArgumentNullException(nameof(listener));
            }

            public void OnDispose(IndexReader reader) => listener.OnClose(reader);

            public override bool Equals(object obj) => listener.Equals(obj);
            public override int GetHashCode() => listener.GetHashCode();
            public override string ToString() => listener.ToString();
        }

        /// <summary>
        /// Expert: adds a <see cref="IReaderClosedListener"/>.  The
        /// provided listener will be invoked when this reader is closed.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        [Obsolete("Use AddReaderDisposedListerner method instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void AddReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerDisposedListeners.Add(new ReaderCloseListenerWrapper(listener));
        }

        /// <summary>
        /// Expert: remove a previously added <see cref="IReaderClosedListener"/>.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        [Obsolete("Use RemoveReaderDisposedListerner method instead. This method will be removed in 4.8.0 release candidate."), System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void RemoveReaderClosedListener(IReaderClosedListener listener)
        {
            EnsureOpen();
            readerDisposedListeners.Remove(new ReaderCloseListenerWrapper(listener));
        }
    }
}
