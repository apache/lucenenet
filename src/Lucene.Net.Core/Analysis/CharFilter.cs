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
        /// be sure to call <code>super.close()</code> when overriding this method.
        /// </summary>
        public override void Close()
        {
            input.Close();
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
    }
}