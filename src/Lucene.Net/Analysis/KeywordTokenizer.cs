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

using System;

namespace Lucene.Net.Analysis
{
	
    /// <summary> Emits the entire input as a single token.</summary>
    public class KeywordTokenizer : Tokenizer
    {
		
        private const int DEFAULT_BUFFER_SIZE = 256;
		
        private bool done;
		
        public KeywordTokenizer(System.IO.TextReader input) : this(input, DEFAULT_BUFFER_SIZE)
        {
        }
		
        public KeywordTokenizer(System.IO.TextReader input, int bufferSize) : base(input)
        {
            this.done = false;
        }

        public override Token Next(Token result)
        {
            if (!done)
            {
                done = true;
                int upto = 0;
                result.Clear();
                char[] buffer = result.TermBuffer();
                while (true)
                {
                    int length = input.Read(buffer, upto, buffer.Length - upto);
                    if (length <= 0)
                        break;
                    upto += length;
                    if (upto == buffer.Length)
                        buffer = result.ResizeTermBuffer(1 + buffer.Length);
                }
                result.termLength = upto;
                return result;
            }
            return null;
        }

        public override void Reset(System.IO.TextReader input)
        {
            base.Reset(input);
            this.done = false;
        }
    }
}