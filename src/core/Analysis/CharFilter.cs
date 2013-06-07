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

namespace Lucene.Net.Analysis
{
	
	/// <summary> Subclasses of CharFilter can be chained to filter CharStream.
	/// They can be used as <see cref="System.IO.TextReader" /> with additional offset
	/// correction. <see cref="Tokenizer" />s will automatically use <see cref="CorrectOffset" />
	/// if a CharFilter/CharStream subclass is used.
	/// 
	/// </summary>
	/// <version>  $Id$
	/// 
	/// </version>
	public abstract class CharFilter : System.IO.TextReader
	{
        protected readonly System.IO.TextReader input;
		
		public CharFilter(System.IO.TextReader input)
		{
			this.input = input;
		}

        public override void Close()
        {
            input.Close();
            base.Close();
        }
        
		/// <summary>Subclass may want to override to correct the current offset.</summary>
		/// <param name="currentOff">current offset</param>
		/// <returns>corrected offset</returns>
        protected abstract int Correct(int currentOff);
		
		/// <summary> Chains the corrected offset through the input
		/// CharFilter.
		/// </summary>
		public int CorrectOffset(int currentOff)
		{
            int corrected = Correct(currentOff);

            var charFilter = input as CharFilter;

            return charFilter != null ? charFilter.CorrectOffset(corrected) : corrected;
		}
	}
}