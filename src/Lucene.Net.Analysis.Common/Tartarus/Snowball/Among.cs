/*

Copyright (c) 2001, Dr Martin Porter
Copyright (c) 2002, Richard Boulton
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    * this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright
    * notice, this list of conditions and the following disclaimer in the
    * documentation and/or other materials provided with the distribution.
    * Neither the name of the copyright holders nor the names of its contributors
    * may be used to endorse or promote products derived from this software
    * without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

 */

using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Lucene.Net.Tartarus.Snowball
{
    /// <summary>
    /// This is the rev 502 of the Snowball SVN trunk,
    /// but modified:
    /// made abstract and introduced abstract method stem to avoid expensive reflection in filter class.
    /// refactored StringBuffers to StringBuilder
    /// uses char[] as buffer instead of StringBuffer/StringBuilder
    /// eq_s,eq_s_b,insert,replace_s take CharSequence like eq_v and eq_v_b
    /// reflection calls (Lovins, etc) use EMPTY_ARGS/EMPTY_PARAMS
    /// </summary>
    public class Among
    {
        private readonly Type[] EMPTY_PARAMS = new Type[0];

        public Among(string s, int substring_i, int result,
            string methodname, SnowballProgram methodobject)
        {
            this.s_size = s.Length;
            this.s = s.ToCharArray();
            this.substring_i = substring_i;
            this.result = result;
            this.methodobject = methodobject;
            if (methodname.Length == 0)
            {
                this.method = null;
            }
            else
            {
                try
                {
                    this.method = methodobject.GetType().GetMethod(methodname, EMPTY_PARAMS);
                }
                catch (MissingMethodException e)
                {
                    throw new Exception(e.ToString(), e);
                }
            }
        }

        /// <summary>search string</summary>
        public int Length
        {
            get { return s_size; }
        }
        private readonly int s_size;

        /// <summary>search string</summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public char[] S
        {
            get { return s; }
        }
        private readonly char[] s;

        /// <summary>index to longest matching substring</summary>
        public int SubstringIndex
        {
            get { return substring_i; }
        }
        private readonly int substring_i;

        /// <summary>result of the lookup</summary>
        public int Result
        {
            get { return result; }
        }
        private readonly int result;

        /// <summary>method to use if substring matches</summary>
        public MethodInfo Method
        {
            get { return method; }
        }
        private readonly MethodInfo method;

        /// <summary>object to invoke method on</summary>
        public SnowballProgram MethodObject
        {
            get { return MethodObject; }
        }
        private readonly SnowballProgram methodobject;
    }
}
