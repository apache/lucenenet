using System;
using System.Diagnostics;
using System.IO;
using Lucene.Net.Util;

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
    /// A Tokenizer is a TokenStream whose input is a Reader.
    ///  <p>
    ///  this is an abstract class; subclasses must override <seealso cref="#IncrementToken()"/>
    ///  <p>
    ///  NOTE: Subclasses overriding <seealso cref="#IncrementToken()"/> must
    ///  call <seealso cref="AttributeSource#ClearAttributes()"/> before
    ///  setting attributes.
    /// </summary>
    public abstract class Tokenizer : TokenStream
    {
        /// <summary>
        /// The text source for this Tokenizer. </summary>
        protected internal TextReader input = ILLEGAL_STATE_READER;

        /// <summary>
        /// Pending reader: not actually assigned to input until reset() </summary>
        private TextReader InputPending = ILLEGAL_STATE_READER;

        /// <summary>
        /// Construct a token stream processing the given input. </summary>
        protected internal Tokenizer(TextReader input)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input", "input must not be null");
            }
            this.InputPending = input;
        }

        /// <summary>
        /// Construct a token stream processing the given input using the given AttributeFactory. </summary>
        protected internal Tokenizer(AttributeFactory factory, TextReader input)
            : base(factory)
        {
            if (input == null)
            {
                throw new System.ArgumentNullException("input", "input must not be null");
            }
            this.InputPending = input;
        }

        /// <summary>
        /// {@inheritDoc}
        /// <p>
        /// <b>NOTE:</b>
        /// The default implementation closes the input Reader, so
        /// be sure to call <code>super.Dispose()</code> when overriding this method.
        /// </summary>
        public override void Dispose()
        {
            input.Dispose();
            // LUCENE-2387: don't hold onto Reader after close, so
            // GC can reclaim
            InputPending = ILLEGAL_STATE_READER;
            input = ILLEGAL_STATE_READER;
        }

        /// <summary>
        /// Return the corrected offset. If <seealso cref="#input"/> is a <seealso cref="CharFilter"/> subclass
        /// this method calls <seealso cref="CharFilter#correctOffset"/>, else returns <code>currentOff</code>. </summary>
        /// <param name="currentOff"> offset as seen in the output </param>
        /// <returns> corrected offset based on the input </returns>
        /// <seealso> cref= CharFilter#correctOffset </seealso>
        protected internal int CorrectOffset(int currentOff)
        {
            return (input is CharFilter) ? ((CharFilter)input).CorrectOffset(currentOff) : currentOff;
        }

        /// <summary>
        /// Expert: Set a new reader on the Tokenizer.  Typically, an
        ///  analyzer (in its tokenStream method) will use
        ///  this to re-use a previously created tokenizer.
        /// </summary>
        public TextReader Reader
        {
            set
            {
                if (value == null)
                {
                    throw new System.ArgumentNullException("value", "input must not be null");
                }
                else if (this.input != ILLEGAL_STATE_READER)
                {
                    throw new InvalidOperationException("TokenStream contract violation: close() call missing");
                }
                this.InputPending = value;
                Debug.Assert(SetReaderTestPoint());
            }
        }

        public override void Reset()
        {
            base.Reset();
            input = InputPending;
            InputPending = ILLEGAL_STATE_READER;
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
                throw new InvalidOperationException("TokenStream contract violation: reset()/close() call missing, " 
                    + "reset() called multiple times, or subclass does not call super.reset(). "
                    + "Please see Javadocs of TokenStream class for more information about the correct consuming workflow.");
            }

            protected override void Dispose(bool disposing)
            {
            }
        }
    }
}