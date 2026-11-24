// Lucene version compatibility level 4.8.1
using Lucene.Net.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

namespace Lucene.Net.Analysis
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
    /// Internal class to enable reuse of the character memory by
    /// <see cref="Analyzer.GetTokenStream(string, string)"/> or
    /// <see cref="Analyzer.GetTokenStream(string, ReadOnlyMemory{char}, bool)"/>.
    /// </summary>
    /// <remarks>
    /// This reader retains a reference to the supplied memory without copying
    /// its contents. The caller must not modify the underlying memory while
    /// the reader is consuming it.
    /// </remarks>
    internal sealed class ReusableStringReader : TextReader, ISpanTextReadable
    {
        private ReadOnlyMemory<char> s;
        private int pos;

        internal void SetValue(string value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            SetValue(value.AsMemory());
        }

        internal void SetValue(ReadOnlyMemory<char> value)
        {
            s = value;
            pos = 0;
        }

        public override int Peek()
        {
            return pos < s.Length
                ? s.Span[pos]
                : -1;
        }

        public override int Read()
        {
            return pos < s.Length
                ? s.Span[pos++]
                : -1;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index > buffer.Length - count)
                throw new ArgumentException("Offset and count exceed the bounds of the array.");

            // LUCENENET: Preserve the legacy -1 EOF behavior for compatibility.
            if (pos >= s.Length)
                return -1;

            if (count == 0)
                return 0;

            int charsToCopy = Math.Min(count, s.Length - pos);
            s.Span.Slice(pos, charsToCopy).CopyTo(buffer.AsSpan(index, charsToCopy));

            pos += charsToCopy;
            return charsToCopy;
        }

#if FEATURE_TEXTREADER_SPAN
        public override int Read(Span<char> buffer)
#else
        int ISpanTextReadable.Read(Span<char> buffer)
            => ReadCore(buffer);
        private int ReadCore(Span<char> buffer)
#endif
        {
            if (buffer.IsEmpty || pos >= s.Length)
                return 0;

            int charsToCopy = Math.Min(buffer.Length, s.Length - pos);
            s.Span.Slice(pos, charsToCopy).CopyTo(buffer);

            pos += charsToCopy;
            return charsToCopy;
        }


        // LUCENENET: This override can be removed if Read(char[], int, int)
        // is changed to return 0 instead of -1 at EOF.
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            int charsRead = Read(buffer, index, count);

            // ReadBlock follows .NET semantics even though our legacy Read()
            // overload returns -1 at EOF.
            return charsRead < 0 ? 0 : charsRead;
        }

        public override string ReadToEnd()
        {
            if (pos >= s.Length)
                return string.Empty;

            string result = s.Slice(pos).ToString();
            pos = s.Length;
            return result;
        }

#if FEATURE_TEXTREADER_SPAN
        public override int ReadBlock(Span<char> buffer)
            // Read(Span<char>) already fills the destination until it is full or EOF.
             => Read(buffer);
#else
        int ISpanTextReadable.ReadBlock(Span<char> buffer)
             // Read(Span<char>) already fills the destination until it is full or EOF.
             => ReadCore(buffer);
#endif


        // LUCENENET: This override can be removed if Read(char[], int, int)
        // is changed to return 0 instead of -1 at EOF.
        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            int charsRead = Read(buffer, index, count);

            // The async API follows .NET EOF semantics, not the legacy array Read() behavior.
            return Task.FromResult(charsRead < 0 ? 0 : charsRead);
        }

        // LUCENENET: This override can be removed if Read(char[], int, int)
        // is changed to return 0 instead of -1 at EOF.
        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            return Task.FromResult(ReadBlock(buffer, index, count));
        }

        public override Task<string?> ReadLineAsync()
        {
            return Task.FromResult<string?>(ReadLine());
        }

        public override Task<string> ReadToEndAsync()
        {
            return Task.FromResult(ReadToEnd());
        }

#if FEATURE_TEXTREADER_MEMORY
        public override ValueTask<int> ReadAsync(
            Memory<char> buffer,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<int>(cancellationToken);

            return new ValueTask<int>(Read(buffer.Span));
        }

        public override ValueTask<int> ReadBlockAsync(
            Memory<char> buffer,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<int>(cancellationToken);

            return new ValueTask<int>(ReadBlock(buffer.Span));
        }
#endif

#if FEATURE_TEXTREADER_ASYNC_CANCELLATION
        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return ValueTask.FromCanceled<string?>(cancellationToken);

            return new ValueTask<string?>(ReadLine());
        }

        public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<string>(cancellationToken);

            return Task.FromResult(ReadToEnd());
        }
#endif

        protected override void Dispose(bool disposing)
        {
            // LUCENENET: This reader is intentionally reusable after Dispose().
            // Release the reference to the previous input.
            s = default;
            pos = 0;

            base.Dispose(disposing);
        }
    }
}
