using System;
using System.Diagnostics;
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
    /// A <see cref="Tokenizer"/> is a <see cref="TokenStream"/> whose input is a <see cref="TextReader"/>.
    /// <para/>
    /// This is an abstract class; subclasses must override <see cref="TokenStream.IncrementToken()"/>
    /// <para/>
    /// NOTE: Subclasses overriding <see cref="TokenStream.IncrementToken()"/> must
    /// call <see cref="Util.AttributeSource.ClearAttributes()"/> before
    /// setting attributes.
    /// </summary>
    public abstract class Tokenizer : TokenStream
    {
        /// <summary>
        /// The text source for this <see cref="Tokenizer"/>. </summary>
        protected TextReader m_input = ILLEGAL_STATE_READER;

        /// <summary>
        /// Pending reader: not actually assigned to input until <see cref="Reset()"/> </summary>
        private TextReader inputPending = ILLEGAL_STATE_READER;

        /// <summary>
        /// Construct a token stream processing the given input. </summary>
        protected internal Tokenizer(TextReader input)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input", "input must not be null");
            }
            this.inputPending = input;
        }

        /// <summary>
        /// Construct a token stream processing the given input using the given <see cref="Util.AttributeSource.AttributeFactory"/>.
        /// </summary>
        protected internal Tokenizer(AttributeFactory factory, TextReader input)
            : base(factory)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input", "input must not be null");
            }
            this.inputPending = input;
        }

        /// <summary>
        /// Releases resources associated with this stream.
        /// <para/>
        /// If you override this method, always call <c>base.Dispose(disposing)</c>, otherwise
        /// some internal state will not be correctly reset (e.g., <see cref="Tokenizer"/> will
        /// throw <see cref="InvalidOperationException"/> on reuse).
        /// </summary>
        /// <remarks>
        /// <b>NOTE:</b>
        /// The default implementation closes the input <see cref="TextReader"/>, so
        /// be sure to call <c>base.Dispose(disposing)</c> when overriding this method.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_input.Dispose();
                // LUCENE-2387: don't hold onto TextReader after close, so
                // GC can reclaim
                inputPending = ILLEGAL_STATE_READER;
                m_input = ILLEGAL_STATE_READER;
            }
        }

        /// <summary>
        /// Return the corrected offset. If <see cref="m_input"/> is a <see cref="CharFilter"/> subclass
        /// this method calls <see cref="CharFilter.CorrectOffset"/>, else returns <paramref name="currentOff"/>. </summary>
        /// <param name="currentOff"> offset as seen in the output </param>
        /// <returns> corrected offset based on the input </returns>
        /// <seealso cref="CharFilter.CorrectOffset(int)"/>
        protected internal int CorrectOffset(int currentOff)
        {
            return (m_input is CharFilter) ? ((CharFilter)m_input).CorrectOffset(currentOff) : currentOff;
        }

        /// <summary>
        /// Expert: Set a new reader on the <see cref="Tokenizer"/>. Typically, an
        /// analyzer (in its tokenStream method) will use
        /// this to re-use a previously created tokenizer.
        /// </summary>
        public void SetReader(TextReader input)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("value", "input must not be null");
            }
            else if (this.m_input != ILLEGAL_STATE_READER)
            {
                throw new InvalidOperationException("TokenStream contract violation: Close() call missing");
            }
            this.inputPending = input;
            Debug.Assert(SetReaderTestPoint());
        }

        public override void Reset()
        {
            base.Reset();
            m_input = inputPending;
            inputPending = ILLEGAL_STATE_READER;
        }

        // only used by assert, for testing
        internal virtual bool SetReaderTestPoint()
        {
            return true;
        }

        private static readonly TextReader ILLEGAL_STATE_READER = new ReaderAnonymousInnerClassHelper();

        private class ReaderAnonymousInnerClassHelper : TextReader
        {
            public override int Read(char[] cbuf, int off, int len)
            {
                throw new InvalidOperationException("TokenStream contract violation: Reset()/Dispose() call missing, " 
                    + "Reset() called multiple times, or subclass does not call base.Reset(). "
                    + "Please see Javadocs of TokenStream class for more information about the correct consuming workflow.");
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}