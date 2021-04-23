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
    /// Subclasses of <see cref="CharFilter"/> can be chained to filter a <see cref="TextReader"/>
    /// They can be used as <see cref="TextReader"/> with additional offset
    /// correction. <see cref="Tokenizer"/>s will automatically use <see cref="CorrectOffset"/>
    /// if a <see cref="CharFilter"/> subclass is used.
    /// <para/>
    /// This class is abstract: at a minimum you must implement <see cref="TextReader.Read(char[], int, int)"/>,
    /// transforming the input in some way from <see cref="m_input"/>, and <seealso cref="Correct(int)"/>
    /// to adjust the offsets to match the originals.
    /// <para/>
    /// You can optionally provide more efficient implementations of additional methods
    /// like <see cref="TextReader.Read()"/>, but this is not required.
    /// <para/>
    /// For examples and integration with <see cref="Analyzer"/>, see the
    /// <see cref="Lucene.Net.Analysis"/> namespace documentation.
    /// </summary>
    // the way java.io.FilterReader should work!
    public abstract class CharFilter : TextReader
    {
        /// <summary>
        /// The underlying character-input stream.
        /// </summary>
        protected internal readonly TextReader m_input;

        /// <summary>
        /// Create a new <see cref="CharFilter"/> wrapping the provided reader. </summary>
        /// <param name="input"> a <see cref="TextReader"/>, can also be a <see cref="CharFilter"/> for chaining. </param>
        protected CharFilter(TextReader input) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.m_input = input;
        }

        /// <summary>
        /// Closes the underlying input stream.
        /// <para/>
        /// <b>NOTE:</b>
        /// The default implementation closes the input <see cref="TextReader"/>, so
        /// be sure to call <c>base.Dispose(disposing)</c> when overriding this method.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_input.Dispose();
            }
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
        /// <see cref="CharFilter"/>(s).
        /// </summary>
        public int CorrectOffset(int currentOff)
        {
            int corrected = Correct(currentOff);
            return (m_input is CharFilter charFilter) ? charFilter.CorrectOffset(corrected) : corrected;
        }

        // LUCENENET specific - force subclasses to implement Read(char[] buffer, int index, int count),
        // since it is required (and the .NET implementation calls Read() which would have infinite recursion
        // if it were called.
        public abstract override int Read(char[] buffer, int index, int count);

        // LUCENENET specific - need to override read, as it returns -1 by default in .NET.
        public override int Read()
        {
            var buffer = new char[1];
            int count = Read(buffer, 0, 1);
            return (count < 1) ? -1 : buffer[0];
        }

        // LUCENENET specific
        #region From Reader Class

        /// <summary>
        /// Skips characters. This method will block until some characters are available, an I/O error occurs, or the end of the stream is reached.
        /// 
        /// LUCENENET specific. Moved here from the Reader class (in Java) so it can be overridden to provide reader buffering.
        /// </summary>
        /// <param name="n">The number of characters to skip</param>
        /// <returns>The number of characters actually skipped</returns>
        public virtual long Skip(int n)
        {
            throw UnsupportedOperationException.Create("Skip() not supported");
        }

        /// <summary>
        /// LUCENENET specific. Moved here from the Reader class (in Java) so it can be overridden to provide reader buffering.
        /// </summary>
        /// <returns></returns>
        public virtual void Reset()
        {
            throw UnsupportedOperationException.Create("Reset() not supported");
        }

        /// <summary>
        /// Tells whether this stream is ready to be read.
        /// <para/>
        /// True if the next <see cref="TextReader.Read()"/> is guaranteed not to block for input, false otherwise. Note 
        /// that returning false does not guarantee that the next read will block.
        /// <para/>
        /// LUCENENET specific. Moved here from the Reader class (in Java) so it can be overridden to provide reader buffering.
        /// </summary>
        public virtual bool IsReady => false;

        /// <summary>
        /// Tells whether this stream supports the <see cref="Mark(int)"/> operation. The default implementation always 
        /// returns false. Subclasses should override this method.
        /// <para/>
        /// LUCENENET specific. Moved here from the Reader class (in Java) so it can be overridden to provide reader buffering.
        /// </summary>
        /// <returns>true if and only if this stream supports the mark operation.</returns>
        public virtual bool IsMarkSupported => false;

        /// <summary>
        /// Marks the present position in the stream. Subsequent calls to <see cref="Reset"/> will attempt to 
        /// reposition the stream to this point. Not all character-input streams support the <see cref="Mark(int)"/> operation.
        /// <para/>
        /// LUCENENET specific. Moved here from the Reader class (in Java) so it can be overridden to provide reader buffering.
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