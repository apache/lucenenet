using Lucene.Net.Search.Suggest;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lucene.Net.Search.Spell
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
    /// Dictionary represented by a text file.
    /// 
    /// <para/>Format allowed: 1 word per line:<para/>
    /// word1<para/>
    /// word2<para/>
    /// word3<para/>
    /// </summary>
    public class PlainTextDictionary : IDictionary
    {

        private readonly TextReader @in;

        /// <summary>
        /// Creates a dictionary based on a File.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public PlainTextDictionary(FileInfo file)
        {
            @in = IOUtils.GetDecodingReader(file, Encoding.UTF8);
        }

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public PlainTextDictionary(Stream dictFile)
        {
            @in = IOUtils.GetDecodingReader(dictFile, Encoding.UTF8);
        }

        /// <summary>
        /// Creates a dictionary based on a reader.
        /// </summary>
        public PlainTextDictionary(TextReader reader)
        {
            @in = reader;
        }

        public virtual IInputEnumerator GetEntryEnumerator()
        {
            return new InputEnumeratorWrapper(new FileEnumerator(this));
        }

        internal sealed class FileEnumerator : IBytesRefEnumerator
        {
            private readonly PlainTextDictionary outerInstance;

            public FileEnumerator(PlainTextDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal bool done = false;
            internal readonly BytesRef spare = new BytesRef();
            private BytesRef current;

            public BytesRef Current => current;

            public bool MoveNext()
            {
                if (done)
                    return false;

                bool success = false;
                bool hasNext = true;
                try
                {
                    string line;
                    if ((line = outerInstance.@in.ReadLine()) != null)
                    {
                        spare.CopyChars(line);
                        current = spare;
                    }
                    else
                    {
                        done = true;
                        IOUtils.Dispose(outerInstance.@in);
                        current = null;
                        hasNext = false;
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.DisposeWhileHandlingException(outerInstance.@in);
                    }
                }
                return hasNext;
            }

            public IComparer<BytesRef> Comparer => null;
        }
    }
}