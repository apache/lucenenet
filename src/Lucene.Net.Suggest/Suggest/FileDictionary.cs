using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Search.Spell;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest
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
    /// <p/>Format allowed: 1 entry per line:<br/>
    /// An entry can be: <br/>
    /// <ul>
    /// <li>suggestion</li>
    /// <li>suggestion <code>fieldDelimiter</code> weight</li>
    /// <li>suggestion <code>fieldDelimiter</code> weight <code>fieldDelimiter</code> payload</li>
    /// </ul>
    /// where the default <code>fieldDelimiter</code> is {@value #DEFAULT_FIELD_DELIMITER}<br/>
    /// <p/>
    /// <b>NOTE:</b> 
    /// <ul>
    /// <li>In order to have payload enabled, the first entry has to have a payload</li>
    /// <li>If the weight for an entry is not specified then a value of 1 is used</li>
    /// <li>A payload cannot be specified without having the weight specified for an entry</li>
    /// <li>If the payload for an entry is not specified (assuming payload is enabled) 
    ///  then an empty payload is returned</li>
    /// <li>An entry cannot have more than two <code>fieldDelimiter</code></li>
    /// </ul>
    /// <p/>
    /// <b>Example:</b><br/>
    /// word1 word2 TAB 100 TAB payload1<br/>
    /// word3 TAB 101<br/>
    /// word4 word3 TAB 102<br/>
    /// </summary>
    public class FileDictionary : Dictionary
    {

        /// <summary>
        /// Tab-delimited fields are most common thus the default, but one can override this via the constructor
        /// </summary>
        public const string DEFAULT_FIELD_DELIMITER = "\t";
        private BufferedReader @in;
        private string line;
        private bool done = false;
        private readonly string fieldDelimiter;

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// Using <seealso cref="#DEFAULT_FIELD_DELIMITER"/> as the 
        /// field seperator in a line.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public FileDictionary(InputStream dictFile)
            : this(dictFile, DEFAULT_FIELD_DELIMITER)
        {
        }

        /// <summary>
        /// Creates a dictionary based on a reader.
        /// Using <seealso cref="#DEFAULT_FIELD_DELIMITER"/> as the 
        /// field seperator in a line.
        /// </summary>
        public FileDictionary(Reader reader)
            : this(reader, DEFAULT_FIELD_DELIMITER)
        {
        }

        /// <summary>
        /// Creates a dictionary based on a reader. 
        /// Using <code>fieldDelimiter</code> to seperate out the
        /// fields in a line.
        /// </summary>
        public FileDictionary(Reader reader, string fieldDelimiter)
        {
            @in = new BufferedReader(reader);
            this.fieldDelimiter = fieldDelimiter;
        }

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// Using <code>fieldDelimiter</code> to seperate out the
        /// fields in a line.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public FileDictionary(InputStream dictFile, string fieldDelimiter)
        {
            @in = new BufferedReader(IOUtils.GetDecodingReader(dictFile, StandardCharsets.UTF_8));
            this.fieldDelimiter = fieldDelimiter;
        }

        public virtual InputIterator EntryIterator
        {
            get
            {
                try
                {
                    return new FileIterator(this);
                }
                catch (IOException)
                {
                    throw new Exception();
                }
            }
        }

        internal sealed class FileIterator : InputIterator
        {
            private readonly FileDictionary outerInstance;

            internal long curWeight;
            internal readonly BytesRef spare = new BytesRef();
            internal BytesRef curPayload = new BytesRef();
            internal bool isFirstLine = true;
            internal bool hasPayloads = false;

            internal FileIterator(FileDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
                outerInstance.line = outerInstance.@in.readLine();
                if (outerInstance.line == null)
                {
                    outerInstance.done = true;
                    IOUtils.Close(outerInstance.@in);
                }
                else
                {
                    string[] fields = outerInstance.line.Split(outerInstance.fieldDelimiter, true);
                    if (fields.Length > 3)
                    {
                        throw new System.ArgumentException("More than 3 fields in one line");
                    } // term, weight, payload
                    else if (fields.Length == 3)
                    {
                        hasPayloads = true;
                        spare.CopyChars(fields[0]);
                        ReadWeight(fields[1]);
                        curPayload.CopyChars(fields[2]);
                    } // term, weight
                    else if (fields.Length == 2)
                    {
                        spare.CopyChars(fields[0]);
                        ReadWeight(fields[1]);
                    } // only term
                    else
                    {
                        spare.CopyChars(fields[0]);
                        curWeight = 1;
                    }
                }
            }

            public long Weight
            {
                get { return curWeight; }
            }

            public BytesRef Next()
            {
                if (outerInstance.done)
                {
                    return null;
                }
                if (isFirstLine)
                {
                    isFirstLine = false;
                    return spare;
                }
                outerInstance.line = outerInstance.@in.ReadLine();
                if (outerInstance.line != null)
                {
                    string[] fields = outerInstance.line.Split(outerInstance.fieldDelimiter, true);
                    if (fields.Length > 3)
                    {
                        throw new System.ArgumentException("More than 3 fields in one line");
                    } // term, weight and payload
                    else if (fields.Length == 3)
                    {
                        spare.CopyChars(fields[0]);
                        ReadWeight(fields[1]);
                        if (hasPayloads)
                        {
                            curPayload.CopyChars(fields[2]);
                        }
                    } // term, weight
                    else if (fields.Length == 2)
                    {
                        spare.CopyChars(fields[0]);
                        ReadWeight(fields[1]);
                        if (hasPayloads) // have an empty payload
                        {
                            curPayload = new BytesRef();
                        }
                    } // only term
                    else
                    {
                        spare.CopyChars(fields[0]);
                        curWeight = 1;
                        if (hasPayloads)
                        {
                            curPayload = new BytesRef();
                        }
                    }
                    return spare;
                }
                else
                {
                    outerInstance.done = true;
                    IOUtils.Close(outerInstance.@in);
                    return null;
                }
            }

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public BytesRef Payload
            {
                get
                {
                    {
                        return (hasPayloads) ? curPayload : null;
                    }
                }
            }

            public bool HasPayloads
            {
                get { return hasPayloads; }
            }

            internal void ReadWeight(string weight)
            {
                // keep reading floats for bw compat
                try
                {
                    curWeight = Convert.ToInt64(weight);
                }
                catch (FormatException)
                {
                    curWeight = (long)Convert.ToDouble(weight);
                }
            }

            public HashSet<BytesRef> Contexts
            {
                get { return null; }
            }

            public bool HasContexts
            {
                get { return false; }
            }
        }

    }
}