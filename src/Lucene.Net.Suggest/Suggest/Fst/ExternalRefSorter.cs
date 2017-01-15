using Lucene.Net.Util;
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
            this.input = new FileInfo(Path.GetTempFileName());
            this.writer = new OfflineSorter.ByteSequencesWriter(input);
        }

        public virtual void Add(BytesRef utf8)
        {
            if (writer == null)
            {
                throw new InvalidOperationException();
            }
            writer.Write(utf8);
        }

        public virtual IBytesRefIterator GetEnumerator()
        {
            if (sorted == null)
            {
                CloseWriter();

                sorted = new FileInfo(Path.GetTempFileName());
                sort.Sort(input, sorted);

                input.Delete();
                input = null;
            }

            return new ByteSequenceIterator(new OfflineSorter.ByteSequencesReader(sorted), sort.Comparer);
        }

        private void CloseWriter()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        public IComparer<BytesRef> Comparer
        {
            get
            {
                return sort.Comparer;
            }
        }

        /// <summary>
        /// Removes any written temporary files.
        /// </summary>
        public virtual void Dispose()
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

        /// <summary>
        /// Iterate over byte refs in a file.
        /// </summary>
        internal class ByteSequenceIterator : IBytesRefIterator
        {
            private readonly OfflineSorter.ByteSequencesReader reader;
            private BytesRef scratch = new BytesRef();
            private readonly IComparer<BytesRef> comparator;

            public ByteSequenceIterator(OfflineSorter.ByteSequencesReader reader, IComparer<BytesRef> comparator)
            {
                this.reader = reader;
                this.comparator = comparator;
            }

            public virtual BytesRef Next()
            {
                if (scratch == null)
                {
                    return null;
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
                    }
                    else
                    {
                        IOUtils.Close(reader);
                        scratch = null;
                    }
                    success = true;
                    return scratch;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(reader);
                    }
                }
            }

            public virtual IComparer<BytesRef> Comparer
            {
                get
                {
                    return comparator;
                }
            }
        }
    }
}
