using System;
using System.IO;

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
    /// Subclasses of CharFilter can be chained to filter a Reader
    /// They can be used as <seealso cref="java.io.Reader"/> with additional offset
    /// correction. <seealso cref="Tokenizer"/>s will automatically use <seealso cref="#correctOffset"/>
    /// if a CharFilter subclass is used.
    /// <p>
    /// this class is abstract: at a minimum you must implement <seealso cref="#read(char[], int, int)"/>,
    /// transforming the input in some way from <seealso cref="#input"/>, and <seealso cref="#correct(int)"/>
    /// to adjust the offsets to match the originals.
    /// <p>
    /// You can optionally provide more efficient implementations of additional methods
    /// like <seealso cref="#read()"/>, <seealso cref="#read(char[])"/>, <seealso cref="#read(java.nio.CharBuffer)"/>,
    /// but this is not required.
    /// <p>
    /// For examples and integration with <seealso cref="Analyzer"/>, see the
    /// <seealso cref="Lucene.Net.Analysis Analysis package documentation"/>.
    /// </summary>
    // the way java.io.FilterReader should work!
    public abstract class CharFilter : TextReader
    {
        /// <summary>
        /// The underlying character-input stream.
        /// </summary>
        public readonly TextReader input;

        /// <summary>
        /// Create a new CharFilter wrapping the provided reader. </summary>
        /// <param name="input"> a Reader, can also be a CharFilter for chaining. </param>
        protected CharFilter(TextReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// Closes the underlying input stream.
        /// <p>
        /// <b>NOTE:</b>
        /// The default implementation closes the input Reader, so
        /// be sure to call <code>super.Dispose()</code> when overriding this method.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            input.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// Subclasses override to correct the current offset.
        /// </summary>
        /// <param name="currentOff"> current offset </param>
        /// <returns> corrected offset </returns>
        protected abstract int Correct(int currentOff);

        /// <summary>
        /// Chains the corrected offset through the input
        /// CharFilter(s).
        /// </summary>
        public int CorrectOffset(int currentOff)
        {
            int corrected = Correct(currentOff);
            return (input is CharFilter) ? ((CharFilter)input).CorrectOffset(corrected) : corrected;
        }


        #region From Reader Class

        /// <summary>
        /// Skips characters. This method will block until some characters are available, an I/O error occurs, or the end of the stream is reached.
        /// 
        /// LUCENENET specific. Moved here from the Java Reader class so it can be overridden to provide reader buffering.
        /// </summary>
        /// <param name="n">The number of characters to skip</param>
        /// <returns>The number of characters actually skipped</returns>
        public virtual long Skip(int n)
        {
            throw new NotSupportedException("Skip() not supported");
        }

        /// <summary>
        /// LUCENENET specific. Moved here from the Java Reader class so it can be overridden to provide reader buffering.
        /// </summary>
        /// <returns></returns>
        public virtual void Reset()
        {
            throw new NotSupportedException("Reset() not supported");
        }

        /// <summary>
        /// Tells whether this stream is ready to be read.
        /// 
        /// True if the next read() is guaranteed not to block for input, false otherwise. Note that returning false does not guarantee that the next read will block.
        /// 
        /// LUCENENET specific. Moved here from the Java Reader class so it can be overridden to provide reader buffering.
        /// </summary>
        public virtual bool Ready()
        {
            return false;
        }

        /// <summary>
        /// Tells whether this stream supports the mark() operation. The default implementation always returns false. Subclasses should override this method.
        /// 
        /// LUCENENET specific. Moved here from the Java Reader class so it can be overridden to provide reader buffering.
        /// </summary>
        /// <returns>true if and only if this stream supports the mark operation.</returns>
        public virtual bool IsMarkSupported
        {
            get { return false; }
        }

        /// <summary>
        /// Marks the present position in the stream. Subsequent calls to reset() will attempt to reposition the stream to this point. Not all character-input streams support the mark() operation.
        /// 
        /// LUCENENET specific. Moved here from the Java Reader class so it can be overridden to provide reader buffering.
        /// </summary>
        /// <param name="readAheadLimit">Limit on the number of characters that may be read while still preserving the mark. After 
        /// reading this many characters, attempting to reset the stream may fail.</param>
        public virtual void Mark(int readAheadLimit)
        {
            throw new IOException("Mark() not supported");
        }

        #endregion
    }
}