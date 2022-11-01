﻿using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Search.Suggest.Fst
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

    using System.Collections.Generic;

    /// <summary>
    /// Builds and iterates over sequences stored on disk.
    /// </summary>
    public class ExternalRefSorter : IBytesRefSorter, IDisposable
    {
        private readonly OfflineSorter sort;
        private OfflineSorter.ByteSequencesWriter writer;
        private FileInfo input;
        private FileInfo sorted;

        /// <summary>
        /// Will buffer all sequences to a temporary file and then sort (all on-disk).
        /// </summary>
        public ExternalRefSorter(OfflineSorter sort)
        {
            this.sort = sort;
            this.input = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            this.writer = new OfflineSorter.ByteSequencesWriter(input);
        }

        public virtual void Add(BytesRef utf8)
        {
            if (writer is null)
            {
                throw IllegalStateException.Create();
            }
            writer.Write(utf8);
        }

        public virtual IBytesRefEnumerator GetEnumerator()
        {
            if (sorted is null)
            {
                CloseWriter();

                sorted = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
                sort.Sort(input, sorted);

                input.Delete();
                input = null;
            }

            return new ByteSequenceEnumerator(new OfflineSorter.ByteSequencesReader(sorted), sort.Comparer);
        }

        private void CloseWriter()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        public IComparer<BytesRef> Comparer => sort.Comparer;

        /// <summary>
        /// Removes any written temporary files.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) // LUCENENET: Added proper dispose pattern.
        {
            if (disposing)
            {
                try
                {
                    CloseWriter();
                }
                finally
                {
                    if (input != null)
                    {
                        input.Delete();
                    }
                    if (sorted != null)
                    {
                        sorted.Delete();
                    }
                }
            }
        }

        /// <summary>
        /// Iterate over byte refs in a file.
        /// </summary>
        internal class ByteSequenceEnumerator : IBytesRefEnumerator
        {
            private readonly OfflineSorter.ByteSequencesReader reader;
            private BytesRef scratch = new BytesRef();
            private readonly IComparer<BytesRef> comparer;

            public ByteSequenceEnumerator(OfflineSorter.ByteSequencesReader reader, IComparer<BytesRef> comparer)
            {
                this.reader = reader;
                this.comparer = comparer;
            }

            public BytesRef Current => scratch;

            public bool MoveNext()
            {
                if (scratch is null)
                {
                    return false;
                }
                bool success = false;
                try
                {
                    byte[] next = reader.Read();
                    if (next != null)
                    {
                        scratch.Bytes = next;
                        scratch.Length = next.Length;
                        scratch.Offset = 0;
                        success = true;
                        return true;
                    }
                    else
                    {
                        IOUtils.Dispose(reader);
                        scratch = null;
                        success = true;
                        return false;
                    }
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(reader);
                    }
                }
            }

            public virtual IComparer<BytesRef> Comparer => comparer;
        }
    }
}
