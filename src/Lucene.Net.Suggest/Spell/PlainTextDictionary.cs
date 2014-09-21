using System.Collections.Generic;
using System.IO;
using Lucene.Net.Search.Suggest;
using Lucene.Net.Util;

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
    /// <p/>Format allowed: 1 word per line:<br/>
    /// word1<br/>
    /// word2<br/>
    /// word3<br/>
    /// </summary>
    public class PlainTextDictionary : Dictionary
    {

        private BufferedReader @in;

        /// <summary>
        /// Creates a dictionary based on a File.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public PlainTextDictionary(File file)
        {
            @in = new BufferedReader(IOUtils.getDecodingReader(file, StandardCharsets.UTF_8));
        }

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public PlainTextDictionary(InputStream dictFile)
        {
            @in = new BufferedReader(IOUtils.getDecodingReader(dictFile, StandardCharsets.UTF_8));
        }

        /// <summary>
        /// Creates a dictionary based on a reader.
        /// </summary>
        public PlainTextDictionary(Reader reader)
        {
            @in = new BufferedReader(reader);
        }

        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: @Override public org.apache.lucene.search.suggest.InputIterator getEntryIterator() throws IOException
        public virtual InputIterator EntryIterator
        {
            get
            {
                return new InputIteratorWrapper(new FileIterator(this));
            }
        }

        internal sealed class FileIterator : BytesRefIterator
        {
            private readonly PlainTextDictionary outerInstance;

            public FileIterator(PlainTextDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal bool done = false;
            internal readonly BytesRef spare = new BytesRef();
            //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
            //ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef next() throws IOException
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