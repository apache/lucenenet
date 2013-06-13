/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using AttributeSource = Lucene.Net.Util.AttributeSource;

namespace Lucene.Net.Analysis
{

    /// <summary> A Tokenizer is a TokenStream whose input is a Reader.
    /// <p/>
    /// This is an abstract class; subclasses must override <see cref="TokenStream.IncrementToken()" />
    /// <p/>
    /// NOTE: Subclasses overriding <see cref="TokenStream.IncrementToken()" /> must call
    /// <see cref="AttributeSource.ClearAttributes()" /> before setting attributes.
    /// </summary>
    public abstract class Tokenizer : TokenStream
    {
        /// <summary>The text source for this Tokenizer. </summary>
        protected System.IO.TextReader input;

        private bool isDisposed;

        /// <summary>Construct a token stream processing the given input. </summary>
        protected Tokenizer(System.IO.TextReader input)
        {
            this.input = input;
        }

        /// <summary>Construct a token stream processing the given input using the given AttributeFactory. </summary>
        protected internal Tokenizer(AttributeFactory factory, System.IO.TextReader input)
            : base(factory)
        {
            this.input = input;
        }
        
        protected override void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                if (input != null)
                {
                    input.Close();
                }
            }

            // LUCENE-2387: don't hold onto Reader after close, so
            // GC can reclaim
            input = null;
            isDisposed = true;
        }

        /// <summary>Return the corrected offset. If <see cref="input" /> is a <see cref="CharStream" /> subclass
        /// this method calls <see cref="CharStream.CorrectOffset" />, else returns <c>currentOff</c>.
        /// </summary>
        /// <param name="currentOff">offset as seen in the output
        /// </param>
        /// <returns> corrected offset based on the input
        /// </returns>
        /// <seealso cref="CharStream.CorrectOffset">
        /// </seealso>
        protected internal int CorrectOffset(int currentOff)
        {
            return (input is CharFilter) ? ((CharFilter) input).CorrectOffset(currentOff) : currentOff;
        }

        public System.IO.TextReader Reader
        {
            get { return input; }
            set 
            {
                //assert input != null: "input must not be null";
                input = value; 
                //assert setReaderTestPoint();
            }
        }

        // only used by assert, for testing
        private bool SetReaderTestPoint()
        {
            return true;
        }
    }
}