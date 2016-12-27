using Lucene.Net.Search.Suggest;
using Lucene.Net.Util;
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

        private TextReader @in;

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

        public virtual IInputIterator EntryIterator
        {
            get
            {
                return new InputIteratorWrapper(new FileIterator(this));
            }
        }

        internal sealed class FileIterator : IBytesRefIterator
        {
            private readonly PlainTextDictionary outerInstance;

            public FileIterator(PlainTextDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal bool done = false;
            internal readonly BytesRef spare = new BytesRef();

            public BytesRef Next()
            {
                if (done)
                {
                    return null;
                }
                bool success = false;
                BytesRef result;
                try
                {
                    string line;
                    if ((line = outerInstance.@in.ReadLine()) != null)
                    {
                        spare.CopyChars(line);
                        result = spare;
                    }
                    else
                    {
                        done = true;
                        IOUtils.Close(outerInstance.@in);
                        result = null;
                    }
                    success = true;
                }
                finally
                {
                    if (!success)
                    {
                        IOUtils.CloseWhileHandlingException(outerInstance.@in);
                    }
                }
                return result;
            }

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }
        }
    }
}