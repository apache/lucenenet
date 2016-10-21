using Lucene.Net.Analysis.CharFilters;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Analysis.Util
{
    /// <summary>
    /// LUCENENET specific class to mimic Java's BufferedReader (that is, a reader that is seekable) 
    /// so it supports Mark() and Reset() (which are part of the Java Reader class), but also 
    /// provide the Correct() method of BaseCharFilter.
    /// 
    /// At some point we might be able to make some readers accept streams (that are seekable) 
    /// so this functionality can be .NET-ified.
    /// </summary>
    public class BufferedCharFilter : BaseCharFilter
    {
        private TextReader @in;

        private char[] cb;
        private int nChars, nextChar;

        private static readonly int INVALIDATED = -2;
        private static readonly int UNMARKED = -1;
        private int markedChar = UNMARKED;
        private int readAheadLimit = 0; /* Valid only when markedChar > 0 */

        /// <summary>
        /// If the next character is a line feed, skip it
        /// </summary>
        private bool skipLF = false;

        /// <summary>
        /// The skipLF flag when the mark was set
        /// </summary>
        private bool markedSkipLF = false;

        internal static int defaultCharBufferSize = 8192;
        private static int defaultExpectedLineLength = 80;

        /// <summary>
        /// LUCENENET specific to throw an exception if the user calls Close() instead of Dispose()
        /// </summary>
        private bool isDisposing = false;

        /// <summary>
        /// Creates a buffering character-input stream that uses an input buffer of the specified size.
        /// </summary>
        /// <param name="in">A Reader</param>
        /// <param name="sz">Input-buffer size</param>
        public BufferedCharFilter(TextReader @in, int sz)
            : base(@in)
        {
            if (sz <= 0)
                throw new ArgumentOutOfRangeException("Buffer size <= 0");
            this.@in = @in;
            cb = new char[sz];
            nextChar = nChars = 0;
        }

        /// <summary>
        /// Creates a buffering character-input stream that uses a default-sized input buffer.
        /// </summary>
        /// <param name="in">A Reader</param>
        public BufferedCharFilter(TextReader @in)
            : this(@in, defaultCharBufferSize)
        {
        }

        /// <summary>
        /// Checks to make sure that the stream has not been closed
        /// </summary>
        private void EnsureOpen()
        {
            if (@in == null)
                throw new IOException("Stream closed");
        }

        /// <summary>
        /// Fills the input buffer, taking the mark into account if it is valid.
        /// </summary>
        private void Fill()
        {
            int dst;
            if (markedChar <= UNMARKED)
            {
                /* No mark */
                dst = 0;
            }
            else
            {
                /* Marked */
                int delta = nextChar - markedChar;
                if (delta >= readAheadLimit)
                {
                    /* Gone past read-ahead limit: Invalidate mark */
                    markedChar = INVALIDATED;
                    readAheadLimit = 0;
                    dst = 0;
                }
                else
                {
                    if (readAheadLimit <= cb.Length)
                    {
                        /* Shuffle in the current buffer */
                        System.Array.Copy(cb, markedChar, cb, 0, delta);
                        markedChar = 0;
                        dst = delta;
                    }
                    else
                    {
                        /* Reallocate buffer to accommodate read-ahead limit */
                        char[] ncb = new char[readAheadLimit];
                        System.Array.Copy(cb, markedChar, ncb, 0, delta);
                        cb = ncb;
                        markedChar = 0;
                        dst = delta;
                    }
                    nextChar = nChars = delta;
                }
            }

            int n = @in.Read(cb, dst, cb.Length - dst);
            // LUCENENET: .NET readers always return 0 when they are finished
            // so there is nothing to do here but remove this loop.
            //do
            //{
            //    n = @in.Read(cb, dst, cb.Length - dst);
            //} while (n == 0);
            if (n > 0)
            {
                nChars = dst + n;
                nextChar = dst;
            }
        }

        /// <summary>
        /// Reads a single character.
        /// </summary>
        /// <returns>The character read, as an integer in the range 0 to 65535 (0x00-0xffff), or -1 if the end of the stream has been reached</returns>
        /// <exception cref="IOException">If an I/O error occurs</exception>
        public override int Read()
        {
            lock (this)
            {
                EnsureOpen();
                for (;;)
                {
                    if (nextChar >= nChars)
                    {
                        Fill();
                        if (nextChar >= nChars)
                            return -1;
                    }
                    if (skipLF)
                    {
                        skipLF = false;
                        if (cb[nextChar] == '\n')
                        {
                            nextChar++;
                            continue;
                        }
                    }
                    return cb[nextChar++];
                }
            }
        }

        /// <summary>
        /// Reads characters into a portion of an array.
        /// This method implements the general contract of the corresponding read method of the Reader class. 
        /// As an additional convenience, it attempts to read as many characters as possible by repeatedly 
        /// invoking the read method of the underlying stream.This iterated read continues until one of the 
        /// following conditions becomes true:
        /// 
        /// <list type="bullet">
        /// <item>The specified number of characters have been read,</item>
        /// <item>The read method of the underlying stream returns -1, indicating end-of-file, or</item>
        /// <item>The ready method of the underlying stream returns false, indicating that further input requests would block.</item>
        /// </list>
        /// If the first read on the underlying stream returns -1 to indicate end-of-file then this method returns -1. 
        /// Otherwise this method returns the number of characters actually read.
        /// Subclasses of this class are encouraged, but not required, to attempt to read as many characters 
        /// as possible in the same fashion. Ordinarily this method takes characters from this stream's character 
        /// buffer, filling it from the underlying stream as necessary. If, however, the buffer is empty, the mark 
        /// is not valid, and the requested length is at least as large as the buffer, then this method will read 
        /// characters directly from the underlying stream into the given array. Thus redundant BufferedReaders 
        /// will not copy data unnecessarily.
        /// </summary>
        /// <param name="buffer">Destination buffer</param>
        /// <param name="index">Offset at which to start storing characters</param>
        /// <param name="count">Maximum number of characters to read</param>
        /// <returns></returns>
        public override int Read(char[] buffer, int index, int count)
        {
            if (nextChar >= nChars)
            {
                /* If the requested length is at least as large as the buffer, and
               if there is no mark/reset activity, and if line feeds are not
               being skipped, do not bother to copy the characters into the
               local buffer.  In this way buffered streams will cascade
               harmlessly. */
                if (count >= cb.Length && markedChar <= UNMARKED && !skipLF)
                {
                    return @in.Read(buffer, index, count);
                }
                Fill();
            }
            if (nextChar >= nChars) return -1;
            if (skipLF)
            {
                skipLF = false;
                if (cb[nextChar] == '\n')
                {
                    nextChar++;
                    if (nextChar >= nChars)
                        Fill();
                    if (nextChar >= nChars)
                        return -1;
                }
            }
            int n = Math.Min(count, nChars - nextChar);
            System.Array.Copy(cb, nextChar, buffer, index, n);
            nextChar += n;
            return n;
        }

        /// <summary>
        /// Reads a line of text. A line is considered to be terminated by any one of a line feed ('\n'), a carriage return 
        /// ('\r'), or a carriage return followed immediately by a linefeed.
        /// </summary>
        /// <returns>A String containing the contents of the line, not including any line-termination characters, 
        /// or null if the end of the stream has been reached</returns>
        /// <exception cref="IOException">If an I/O error occurs</exception>
        public override string ReadLine()
        {
            StringBuilder s = null;
            int startChar;

            lock (this)
            {
                EnsureOpen();
                bool omitLF = skipLF;

                for (;;)
                {
                    if (nextChar >= nChars)
                    {
                        Fill();
                    }
                    if (nextChar >= nChars)
                    { /* EOF */
                        if (s != null && s.Length > 0)
                            return s.ToString();
                        else
                            return null;
                    }
                    bool eol = false;
                    char c = (char)0;
                    int i;

                    /* Skip a leftover '\n', if necessary */
                    if (omitLF && (cb[nextChar] == '\n'))
                        nextChar++;
                    skipLF = false;
                    omitLF = false;

                    for (i = nextChar; i < nChars; i++)
                    {
                        c = cb[i];
                        if ((c == '\n') || (c == '\r'))
                        {
                            eol = true;
                            break;
                        }
                    }

                    startChar = nextChar;
                    nextChar = i;
                    if (eol)
                    {
                        string str;
                        if (s == null)
                        {
                            str = new string(cb, startChar, i - startChar);
                        }
                        else
                        {
                            s.Append(cb, startChar, i - startChar);
                            str = s.ToString();
                        }
                        nextChar++;
                        if (c == '\r')
                        {
                            skipLF = true;
                        }
                        return str;
                    }

                    if (s == null)
                        s = new StringBuilder(defaultExpectedLineLength);
                    s.Append(cb, startChar, i - startChar);
                }
            }
        }

        public override long Skip(int n)
        {
            if (n < 0L)
            {
                throw new ArgumentOutOfRangeException("skip value is negative");
            }
            lock (this)
            {
                EnsureOpen();
                int r = n;
                while (r > 0)
                {
                    if (nextChar >= nChars)
                        Fill();
                    if (nextChar >= nChars) /* EOF */
                        break;
                    if (skipLF)
                    {
                        skipLF = false;
                        if (cb[nextChar] == '\n')
                        {
                            nextChar++;
                        }
                    }
                    int d = nChars - nextChar;
                    if (r <= d)
                    {
                        nextChar += r;
                        r = 0;
                        break;
                    }
                    else
                    {
                        r -= d;
                        nextChar = nChars;
                    }
                }
                return n - r;
            }
        }

        /// <summary>
        /// Tells whether this stream is ready to be read. A buffered character stream is ready if the buffer is not empty, or if the underlying character stream is ready.
        /// </summary>
        /// <returns></returns>
        public override bool Ready()
        {
            lock (this)
            {
                EnsureOpen();

                // If newline needs to be skipped and the next char to be read
                // is a newline character, then just skip it right away.
                if (skipLF)
                {
                    // Note that in.ready() will return true if and only if the next
                    // read on the stream will not block.
                    if (nextChar >= nChars /* && @in.Ready() */)
                    {
                        Fill();
                    }
                    if (nextChar < nChars)
                    {
                        if (cb[nextChar] == '\n')
                            nextChar++;
                        skipLF = false;
                    }
                }
                return (nextChar < nChars) /* || @in.Ready() */;
            }
        }

        /// <summary>
        /// Tells whether this stream supports the mark() operation, which it does.
        /// </summary>
        public override bool IsMarkSupported
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Marks the present position in the stream. Subsequent calls to reset() will attempt to reposition the stream to this point.
        /// </summary>
        /// <param name="readAheadLimit">Limit on the number of characters that may be read while still preserving the mark. An attempt 
        /// to reset the stream after reading characters up to this limit or beyond may fail. A limit value larger than the size of the 
        /// input buffer will cause a new buffer to be allocated whose size is no smaller than limit. Therefore large values should be 
        /// used with care.</param>
        public override void Mark(int readAheadLimit)
        {
            if (readAheadLimit < 0)
            {
                throw new ArgumentOutOfRangeException("Read-ahead limit < 0");
            }
            lock (this)
            {
                EnsureOpen();
                this.readAheadLimit = readAheadLimit;
                markedChar = nextChar;
                markedSkipLF = skipLF;
            }
        }

        /// <summary>
        /// Resets the stream to the most recent mark.
        /// </summary>
        /// <exception cref="IOException">If the stream has never been marked, or if the mark has been invalidated</exception>
        public override void Reset()
        {
            lock (this)
            {
                EnsureOpen();
                if (markedChar < 0)
                    throw new IOException((markedChar == INVALIDATED) ? "Mark invalid" : "Stream not marked");
                nextChar = markedChar;
                skipLF = markedSkipLF;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.isDisposing = true;
                lock (this)
                {
                    if (@in == null)
                        return;
                    @in.Dispose();
                    @in = null;
                    cb = null;
                }
                this.isDisposing = false;
            }
        }

        #region LUCENENET Specific Methods

        public override int Peek()
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override Task<string> ReadLineAsync()
        {
            throw new NotImplementedException();
        }

        public override string ReadToEnd()
        {
            throw new NotImplementedException();
        }

        public override Task<string> ReadToEndAsync()
        {
            throw new NotImplementedException();
        }

#if !NETSTANDARD

        public override void Close()
        {
            if (!isDisposing)
            {
                throw new NotSupportedException("Close() is not supported. Call Dispose() instead.");
            }
        }

        public override object InitializeLifetimeService()
        {
            throw new NotImplementedException();
        }
#endif
        #endregion
    }
}
