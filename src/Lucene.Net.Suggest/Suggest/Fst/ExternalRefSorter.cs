using System;
using System.IO;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest.Fst
{
    using System.Collections.Generic;

    /// <summary>
    /// Builds and iterates over sequences stored on disk.
    /// </summary>
    public class ExternalRefSorter : BytesRefSorter, IDisposable
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

        public void Add(BytesRef utf8)
        {
            if (writer == null)
            {
                throw new InvalidOperationException();
            }
            writer.Write(utf8);
        }

        public BytesRefIterator Iterator()
        {
            if (sorted == null)
            {
                CloseWriter();

                sorted = new FileInfo(Path.GetTempFileName());
                sort.Sort(input, sorted);

                input.Delete();
                input = null;
            }

            return new ByteSequenceIterator(new OfflineSorter.ByteSequencesReader(sorted), sort.Comparator);
        }

        private void CloseWriter()
        {
            if (writer != null)
            {
                writer.Dispose();
                writer = null;
            }
        }

        public IComparer<BytesRef> Comparator
        {
            get
            {
                return sort.Comparator;
            }
        }

        /// <summary>
        /// Removes any written temporary files.
        /// </summary>
        public void Dispose()
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
        internal class ByteSequenceIterator : BytesRefIterator
        {
            private readonly OfflineSorter.ByteSequencesReader reader;
            private BytesRef scratch = new BytesRef();
            private readonly IComparer<BytesRef> comparator;

            public ByteSequenceIterator(OfflineSorter.ByteSequencesReader reader, IComparer<BytesRef> comparator)
            {
                this.reader = reader;
                this.comparator = comparator;
            }

            public BytesRef Next()
            {
                if (scratch == null)
                {
                    return null;
                }
                bool success = false;
                try
                {
                    sbyte[] next = reader.Read();
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

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return comparator;
                }
            }
        }
    }
}
