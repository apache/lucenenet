using System.IO;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
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
    /// An efficient implementation of JavaCC's <see cref="ICharStream"/> interface.  
    /// <para/>
    /// Note that
    /// this does not do line-number counting, but instead keeps track of the
    /// character position of the token in the input, as required by Lucene's
    /// <see cref="Token"/> API. 
    /// </summary>
    public sealed class FastCharStream : ICharStream
    {
        char[] buffer = null;

        int bufferLength = 0;          // end of valid chars
        int bufferPosition = 0;        // next char to read

        int tokenStart = 0;          // offset in buffer
        int bufferStart = 0;          // position in file of buffer

        TextReader input;            // source of chars

        /// <summary>
        /// Constructs from a <see cref="TextReader"/>.
        /// </summary>
        public FastCharStream(TextReader r)
        {
            input = r;
        }

        public char ReadChar()
        {
            if (bufferPosition >= bufferLength)
                Refill();
            return buffer[bufferPosition++];
        }

        private void Refill()
        {
            int newPosition = bufferLength - tokenStart;

            if (tokenStart == 0)
            {        // token won't fit in buffer
                if (buffer == null)
                {        // first time: alloc buffer
                    buffer = new char[2048];
                }
                else if (bufferLength == buffer.Length)
                { // grow buffer
                    char[] newBuffer = new char[buffer.Length * 2];
                    System.Array.Copy(buffer, 0, newBuffer, 0, bufferLength);
                    buffer = newBuffer;
                }
            }
            else
            {            // shift token to front
                System.Array.Copy(buffer, tokenStart, buffer, 0, newPosition);
            }

            bufferLength = newPosition;        // update state
            bufferPosition = newPosition;
            bufferStart += tokenStart;
            tokenStart = 0;

            int charsRead =          // fill space in buffer
              input.Read(buffer, newPosition, buffer.Length - newPosition);
            if (charsRead <= 0)
                throw new IOException("read past eof");
            else
                bufferLength += charsRead;
        }

        public char BeginToken()
        {
            tokenStart = bufferPosition;
            return ReadChar();
        }

        public void BackUp(int amount)
        {
            bufferPosition -= amount;
        }

        public string GetImage()
        {
            return new string(buffer, tokenStart, bufferPosition - tokenStart);
        }

        public char[] GetSuffix(int len)
        {
            char[] value = new char[len];
            System.Array.Copy(buffer, bufferPosition - len, value, 0, len);
            return value;
        }

        public void Done()
        {
            try
            {
                input.Dispose();
            }
#pragma warning disable 168
            catch (IOException e)
#pragma warning restore 168
            {
            }
        }

        public int Column
        {
            get { return bufferStart + bufferPosition; }
        }

        public int Line
        {
            get { return 1; }
        }

        public int EndColumn
        {
            get { return bufferStart + bufferPosition; }
        }

        public int EndLine
        {
            get { return 1; }
        }

        public int BeginColumn
        {
            get { return bufferStart + tokenStart; }
        }

        public int BeginLine
        {
            get { return 1; }
        }
    }
}
