// Lucene version compatibility level 4.8.1
// This class was sourced from the Apache Harmony project's BufferedReader
// https://svn.apache.org/repos/asf/harmony/enhanced/java/trunk/

using Lucene.Net.Analysis.CharFilters;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Util
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
    /// LUCENENET specific class to mimic Java's BufferedReader (that is, a reader that is seekable) 
    /// so it supports Mark() and Reset() (which are part of the Java Reader class), but also 
    /// provide the Correct() method of BaseCharFilter.
    /// </summary>
    public class BufferedCharFilter : BaseCharFilter
    {
        public const int DEFAULT_CHAR_BUFFER_SIZE = 8192;

        /// <summary>
        /// The object used to synchronize access to the reader.
        /// </summary>
        protected object m_lock = new object();

        private TextReader @in;

        /// <summary>
        /// The characters that can be read and refilled in bulk. We maintain three
        /// indices into this buffer:
        /// <code>
        /// { X X X X X X X X X X X X - - }
        /// ^     ^             ^
        /// |     |             |
        /// mark   pos end
        /// </code>
        /// Pos points to the next readable character.End is one greater than the
        /// last readable character.When<c> pos == end</c>, the buffer is empty and
        /// must be <see cref="FillBuf()"/> before characters can be read.
        ///
        /// <para/> Mark is the value pos will be set to on calls to 
        /// <see cref="Reset()"/>. Its value is in the range <c>[0...pos]</c>. If the mark is <c>-1</c>, the
        /// buffer cannot be reset.
        /// 
        /// <para/> MarkLimit limits the distance between the mark and the pos.When this
        /// limit is exceeded, <see cref="Reset()"/> is permitted (but not required) to
        /// throw an exception. For shorter distances, <see cref="Reset()"/> shall not throw
        /// (unless the reader is closed).
        /// </summary>
        private char[] buf;
        private int pos;
        private int end;
        private int mark = -1;
        private int markLimit = -1;

#if FEATURE_TEXTWRITER_CLOSE
        /// <summary>
        /// LUCENENET specific to throw an exception if the user calls <see cref="Close()"/> instead of <see cref="TextReader.Dispose()"/>
        /// </summary>
        private bool isDisposing = false;
#endif

        /// <summary>
        /// Creates a buffering character-input stream that uses a default-sized input buffer.
        /// </summary>
        /// <param name="in">A TextReader</param>
        public BufferedCharFilter(TextReader @in)
            : base(@in)
        {
            this.@in = @in;
            buf = new char[DEFAULT_CHAR_BUFFER_SIZE];
        }

        /// <summary>
        /// Creates a buffering character-input stream that uses an input buffer of the specified size.
        /// </summary>
        /// <param name="in">A TextReader</param>
        /// <param name="size">Input-buffer size</param>
        public BufferedCharFilter(TextReader @in, int size)
            : base(@in)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Buffer size <= 0");
            }
            this.@in = @in;
            buf = new char[size];
        }

        /// <summary>
        /// Disposes this reader. This implementation closes the buffered source reader
        /// and releases the buffer. Nothing is done if this reader has already been
        /// disposed.
        /// </summary>
        /// <param name="disposing"></param>
        /// <exception cref="IOException">if an error occurs while closing this reader.</exception>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
#if FEATURE_TEXTWRITER_CLOSE
                this.isDisposing = true;
#endif
                UninterruptableMonitor.Enter(m_lock);
                try
                {
                    if (!IsClosed)
                    {
                        @in.Dispose();
                        @in = null;
                        buf = null;
                    }
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_lock);
                }
#if FEATURE_TEXTWRITER_CLOSE
                this.isDisposing = false;
#endif
            }
            base.Dispose(disposing); // LUCENENET specific - disposable pattern requires calling the base class implementation
        }

        /// <summary>
        /// Populates the buffer with data. It is an error to call this method when
        /// the buffer still contains data; ie. if <c>pos &lt; end</c>.
        /// </summary>
        /// <returns>
        /// the number of bytes read into the buffer, or -1 if the end of the
        /// source stream has been reached.
        /// </returns>
        private int FillBuf()
        {
            // assert(pos == end);

            if (mark == -1 || (pos - mark >= markLimit))
            {
                /* mark isn't set or has exceeded its limit. use the whole buffer */
                int result = @in.Read(buf, 0, buf.Length);
                if (result > 0)
                {
                    mark = -1;
                    pos = 0;
                    end = result;
                }
                // LUCENENET specific: convert result to -1 to mimic java's reader
                return result == 0 ? -1 : result;
            }

            if (mark == 0 && markLimit > buf.Length)
            {
                /* the only way to make room when mark=0 is by growing the buffer */
                int newLength = buf.Length * 2;
                if (newLength > markLimit)
                {
                    newLength = markLimit;
                }
                char[] newbuf = new char[newLength];
                Arrays.Copy(buf, 0, newbuf, 0, buf.Length);
                buf = newbuf;
            }
            else if (mark > 0)
            {
                /* make room by shifting the buffered data to left mark positions */
                Arrays.Copy(buf, mark, buf, 0, buf.Length - mark);
                pos -= mark;
                end -= mark;
                mark = 0;
            }

            /* Set the new position and mark position */
            int count = @in.Read(buf, pos, buf.Length - pos);
            if (count > 0)
            {
                end += count;
            }
            // LUCENENET specific: convert result to -1 to mimic java's reader
            return count == 0 ? -1 : count;
        }

        /// <summary>
        /// Checks to make sure that the stream has not been closed
        /// </summary>
        private void EnsureOpen()
        {
            if (IsClosed)
            {
                throw new IOException("Reader already closed");
            }
        }

        /// <summary>
        /// Indicates whether or not this reader is closed.
        /// </summary>
        private bool IsClosed => buf is null;

        /// <summary>
        /// Sets a mark position in this reader. The parameter <paramref name="markLimit"/>
        /// indicates how many characters can be read before the mark is invalidated.
        /// Calling <see cref="Reset()"/> will reposition the reader back to the marked
        /// position if <see cref="markLimit"/> has not been surpassed.
        /// </summary>
        /// <param name="markLimit">
        /// the number of characters that can be read before the mark is
        /// invalidated.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">if <c>markLimit &lt; 0</c></exception>
        /// <exception cref="IOException">if an error occurs while setting a mark in this reader.</exception>
        public override void Mark(int markLimit)
        {
            if (markLimit < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(markLimit), "Read-ahead limit < 0");
            }
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                this.markLimit = markLimit;
                mark = pos;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Indicates whether this reader supports the <see cref="Mark(int)"/> and
        /// <see cref="Reset()"/> methods. This implementation returns <c>true</c>.
        /// </summary>
        /// <seealso cref="Mark(int)"/>
        /// <seealso cref="Reset()"/>
        public override bool IsMarkSupported => true;


        /// <summary>
        /// Reads a single character from this reader and returns it with the two
        /// higher-order bytes set to 0. If possible, <see cref="BufferedCharFilter"/> returns a
        /// character from the buffer. If there are no characters available in the
        /// buffer, it fills the buffer and then returns a character. It returns -1
        /// if there are no more characters in the source reader.
        /// </summary>
        /// <returns>The character read or -1 if the end of the source reader has been reached.</returns>
        /// <exception cref="IOException">If this reader is disposed or some other I/O error occurs.</exception>
        public override int Read()
        {
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                /* Are there buffered characters available? */
                if (pos < end || FillBuf() != -1)
                {
                    return buf[pos++];
                }
                return -1;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Reads at most <paramref name="length"/> characters from this reader and stores them
        /// at <paramref name="offset"/> in the character array <paramref name="buffer"/>. Returns the
        /// number of characters actually read or -1 if the end of the source reader
        /// has been reached. If all the buffered characters have been used, a mark
        /// has not been set and the requested number of characters is larger than
        /// this readers buffer size, BufferedReader bypasses the buffer and simply
        /// places the results directly into <paramref name="buffer"/>.
        /// </summary>
        /// <param name="buffer">the character array to store the characters read.</param>
        /// <param name="offset">the initial position in <paramref name="buffer"/> to store the bytes read from this reader.</param>
        /// <param name="length">the maximum number of characters to read, must be non-negative.</param>
        /// <returns>number of characters read or -1 if the end of the source reader has been reached.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// if <c>offset &lt; 0</c> or <c>length &lt; 0</c>, or if
        /// <c>offset + length</c> is greater than the size of
        /// <paramref name="buffer"/>.
        /// </exception>
        /// <exception cref="IOException">if this reader is disposed or some other I/O error occurs.</exception>
        public override int Read(char[] buffer, int offset, int length)
        {
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                // LUCENENT specific - refactored guard clauses to throw individual messages.
                // Note that this is the order the Apache Harmony tests expect it to be checked in.
                if (offset < 0)
                    throw new ArgumentOutOfRangeException(nameof(offset), offset, $"{nameof(offset)} must not be negative.");
                // LUCENENET specific - Added guard clause for null
                if (buffer is null)
                    throw new ArgumentNullException(nameof(buffer));
                if (offset > buffer.Length - length) // LUCENENET: Checks for int overflow
                    throw new ArgumentOutOfRangeException(nameof(length), $"{nameof(offset)} + {nameof(length)} may not be greater than the size of {nameof(buffer)}");
                if (length < 0)
                    throw new ArgumentOutOfRangeException(nameof(length), length, $"{nameof(length)} must not be negative.");

                int outstanding = length;
                while (outstanding > 0)
                {

                    /*
                     * If there are bytes in the buffer, grab those first.
                     */
                    int available = end - pos;
                    if (available > 0)
                    {
                        int count2 = available >= outstanding ? outstanding : available;
                        Arrays.Copy(buf, pos, buffer, offset, count2);
                        pos += count2;
                        offset += count2;
                        outstanding -= count2;
                    }

                    /*
                     * Before attempting to read from the underlying stream, make
                     * sure we really, really want to. We won't bother if we're
                     * done, or if we've already got some bytes and reading from the
                     * underlying stream would block.
                     */
                    // LUCENENET specific: only CharFilter derived types support IsReady
                    if (outstanding == 0 || (outstanding < length) && @in is CharFilter charFilter && !charFilter.IsReady)
                    {
                        break;
                    }

                    // assert(pos == end);

                    /*
                     * If we're unmarked and the requested size is greater than our
                     * buffer, read the bytes directly into the caller's buffer. We
                     * don't read into smaller buffers because that could result in
                     * a many reads.
                     */
                    if ((mark == -1 || (pos - mark >= markLimit))
                            && outstanding >= buf.Length)
                    {
                        int count3 = @in.Read(buffer, offset, outstanding);
                        if (count3 > 0)
                        {
                            offset += count3;
                            outstanding -= count3;
                            mark = -1;
                        }

                        break; // assume the source stream gave us all that it could
                    }

                    if (FillBuf() == -1)
                    {
                        break; // source is exhausted
                    }
                }

                int count = length - outstanding;
                return (count > 0 || count == length) ? count : 0 /*-1*/;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Returns the next line of text available from this reader. A line is
        /// represented by zero or more characters followed by <c>'\n'</c>,
        /// <c>'\r'</c>, <c>"\r\n"</c> or the end of the reader. The string does
        /// not include the newline sequence.
        /// </summary>
        /// <returns>The contents of the line or <c>null</c> if no characters were 
        /// read before the end of the reader has been reached.</returns>
        /// <exception cref="IOException">if this reader is disposed or some other I/O error occurs.</exception>
        public override string ReadLine()
        {
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                /* has the underlying stream been exhausted? */
                if (pos == end && FillBuf() == -1)
                {
                    return null;
                }
                for (int charPos = pos; charPos < end; charPos++)
                {
                    char ch = buf[charPos];
                    if (ch > '\r')
                    {
                        continue;
                    }
                    if (ch == '\n')
                    {
                        string res = new string(buf, pos, charPos - pos);
                        pos = charPos + 1;
                        return res;
                    }
                    else if (ch == '\r')
                    {
                        string res = new string(buf, pos, charPos - pos);
                        pos = charPos + 1;
                        if (((pos < end) || (FillBuf() != -1))
                                && (buf[pos] == '\n'))
                        {
                            pos++;
                        }
                        return res;
                    }
                }

                char eol = '\0';
                StringBuilder result = new StringBuilder(80);
                /* Typical Line Length */

                result.Append(buf, pos, end - pos);
                while (true)
                {
                    pos = end;

                    /* Are there buffered characters available? */
                    if (eol == '\n')
                    {
                        return result.ToString();
                    }
                    // attempt to fill buffer
                    if (FillBuf() == -1)
                    {
                        // characters or null.
                        return result.Length > 0 || eol != '\0'
                                ? result.ToString()
                                : null;
                    }
                    for (int charPos = pos; charPos < end; charPos++)
                    {
                        char c = buf[charPos];
                        if (eol == '\0')
                        {
                            if ((c == '\n' || c == '\r'))
                            {
                                eol = c;
                            }
                        }
                        else if (eol == '\r' && c == '\n')
                        {
                            if (charPos > pos)
                            {
                                result.Append(buf, pos, charPos - pos - 1);
                            }
                            pos = charPos + 1;
                            return result.ToString();
                        }
                        else
                        {
                            if (charPos > pos)
                            {
                                result.Append(buf, pos, charPos - pos - 1);
                            }
                            pos = charPos;
                            return result.ToString();
                        }
                    }
                    if (eol == '\0')
                    {
                        result.Append(buf, pos, end - pos);
                    }
                    else
                    {
                        result.Append(buf, pos, end - pos - 1);
                    }
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Indicates whether this reader is ready to be read without blocking.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this reader will not block when <see cref="Read()"/> is
        /// called, <c>false</c> if unknown or blocking will occur.
        /// </returns>
        public override bool IsReady
        {
            get
            {
                UninterruptableMonitor.Enter(m_lock);
                try
                {
                    EnsureOpen();
                    // LUCENENET specific: only CharFilter derived types support IsReady
                    return ((end - pos) > 0) || (@in is CharFilter charFilter && charFilter.IsReady);
                }
                finally
                {
                    UninterruptableMonitor.Exit(m_lock);
                }
            }
        }

        /// <summary>
        /// Resets this reader's position to the last <see cref="Mark(int)"/> location.
        /// Invocations of <see cref="Read()"/> and <see cref="Skip(int)"/> will occur from this new
        /// location.
        /// </summary>
        /// <exception cref="IOException">If this reader is disposed or no mark has been set.</exception>
        /// <seealso cref="Mark(int)"/>
        /// <seealso cref="IsMarkSupported"/>
        public override void Reset()
        {
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                if (mark < 0)
                {
                    // LUCENENET NOTE: Seems odd, but in .NET StreamReader, this is also the exception that is thrown when closed.
                    throw new IOException("Reader not marked");
                }
                pos = mark;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        /// <summary>
        /// Skips <paramref name="amount"/> characters in this reader. Subsequent
        /// <see cref="Read()"/>s will not return these characters unless <see cref="Reset()"/>
        /// is used. Skipping characters may invalidate a mark if <see cref="markLimit"/>
        /// is surpassed.
        /// </summary>
        /// <param name="amount">the maximum number of characters to skip.</param>
        /// <returns>the number of characters actually skipped.</returns>
        /// <exception cref="ArgumentOutOfRangeException">if <c>amount &lt; 0</c>.</exception>
        /// <exception cref="IOException">If this reader is disposed or some other I/O error occurs.</exception>
        /// <seealso cref="Mark(int)"/>
        /// <seealso cref="IsMarkSupported"/>
        /// <seealso cref="Reset()"/>
        public override long Skip(int amount)
        {
            if (amount < 0L)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "skip value is negative");
            }
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                if (amount < 1)
                {
                    return 0;
                }
                if (end - pos >= amount)
                {
                    pos += amount;
                    return amount;
                }

                int read = end - pos;
                pos = end;
                while (read < amount)
                {
                    if (FillBuf() == -1)
                    {
                        return read;
                    }
                    if (end - pos >= amount - read)
                    {
                        pos += amount - read;
                        return amount;
                    }
                    // Couldn't get all the characters, skip what we read
                    read += (end - pos);
                    pos = end;
                }
                return amount;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

        #region LUCENENET Specific Methods

        /// <summary>
        /// Reads a single character from this reader and returns it with the two
        /// higher-order bytes set to 0. If possible, <see cref="BufferedCharFilter"/> returns a
        /// character from the buffer. If there are no characters available in the
        /// buffer, it fills the buffer and then returns a character. It returns -1
        /// if there are no more characters in the source reader. Unlike <see cref="Read()"/>,
        /// this method does not advance the current position.
        /// </summary>
        /// <returns>The character read or -1 if the end of the source reader has been reached.</returns>
        /// <exception cref="IOException">If this reader is disposed or some other I/O error occurs.</exception>
        public override int Peek()
        {
            UninterruptableMonitor.Enter(m_lock);
            try
            {
                EnsureOpen();
                /* Are there buffered characters available? */
                if (pos < end || FillBuf() != -1)
                {
                    return buf[pos];
                }
                return -1;
            }
            finally
            {
                UninterruptableMonitor.Exit(m_lock);
            }
        }

#if FEATURE_STREAM_READ_SPAN
        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override int Read(Span<char> buffer)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override int ReadBlock(Span<char> buffer)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw UnsupportedOperationException.Create();
        }
#endif
        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override Task<string> ReadLineAsync()
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override string ReadToEnd()
        {
            throw UnsupportedOperationException.Create();
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        public override Task<string> ReadToEndAsync()
        {
            throw UnsupportedOperationException.Create();
        }
#if FEATURE_TEXTWRITER_INITIALIZELIFETIMESERVICE
        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">In all cases.</exception>
        // LUCENENET: We don't override this on .NET Core, it throws a
        // PlatformNotSupportedException, which is the behavior we want.
        public override object InitializeLifetimeService()
        {
            throw UnsupportedOperationException.Create();
        }
#endif

#if FEATURE_TEXTWRITER_CLOSE
        /// <summary>
        /// Not supported.
        /// </summary>
        /// <exception cref="NotSupportedException">The call didn't originate from within <see cref="Dispose(bool)"/>.</exception>
        public override void Close()
        {
            if (!isDisposing)
            {
                throw UnsupportedOperationException.Create("Close() is not supported. Call Dispose() instead.");
            }
        }
#endif
        #endregion LUCENENET Specific Methods
    }
}