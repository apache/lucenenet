using J2N.Text;
using Lucene.Net.Search.Spell;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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
    /// <para>Format allowed: 1 entry per line:</para>
    /// <para>An entry can be: </para>
    /// <list type="number">
    /// <item><description>suggestion</description></item>
    /// <item><description>suggestion <see cref="fieldDelimiter"/> weight</description></item>
    /// <item><description>suggestion <see cref="fieldDelimiter"/> weight <see cref="fieldDelimiter"/> payload</description></item>
    /// </list>
    /// where the default <see cref="fieldDelimiter"/> is <see cref="DEFAULT_FIELD_DELIMITER"/> (a tab)
    /// <para>
    /// <b>NOTE:</b> 
    /// <list type="number">
    /// <item><description>In order to have payload enabled, the first entry has to have a payload</description></item>
    /// <item><description>If the weight for an entry is not specified then a value of 1 is used</description></item>
    /// <item><description>A payload cannot be specified without having the weight specified for an entry</description></item>
    /// <item><description>If the payload for an entry is not specified (assuming payload is enabled) 
    ///  then an empty payload is returned</description></item>
    /// <item><description>An entry cannot have more than two <see cref="fieldDelimiter"/>s</description></item>
    /// </list>
    /// </para>
    /// <c>Example:</c><para/>
    /// word1 word2 TAB 100 TAB payload1<para/>
    /// word3 TAB 101<para/>
    /// word4 word3 TAB 102<para/>
    /// </summary>
    public class FileDictionary : IDictionary
    {

        /// <summary>
        /// Tab-delimited fields are most common thus the default, but one can override this via the constructor
        /// </summary>
        public const string DEFAULT_FIELD_DELIMITER = "\t";
        private readonly TextReader @in; // LUCENENET: marked readonly
        private string line;
        private bool done = false;
        private readonly string fieldDelimiter;

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// Using <see cref="DEFAULT_FIELD_DELIMITER"/> as the 
        /// field seperator in a line.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public FileDictionary(Stream dictFile)
            : this(dictFile, DEFAULT_FIELD_DELIMITER)
        {
        }

        /// <summary>
        /// Creates a dictionary based on a reader.
        /// Using <see cref="DEFAULT_FIELD_DELIMITER"/> as the 
        /// field seperator in a line.
        /// </summary>
        public FileDictionary(TextReader reader)
            : this(reader, DEFAULT_FIELD_DELIMITER)
        {
        }

        /// <summary>
        /// Creates a dictionary based on a reader. 
        /// Using <paramref name="fieldDelimiter"/> to seperate out the
        /// fields in a line.
        /// </summary>
        public FileDictionary(TextReader reader, string fieldDelimiter)
        {
            @in = reader;
            this.fieldDelimiter = fieldDelimiter;
        }

        /// <summary>
        /// Creates a dictionary based on an inputstream.
        /// Using <paramref name="fieldDelimiter"/> to seperate out the
        /// fields in a line.
        /// <para>
        /// NOTE: content is treated as UTF-8
        /// </para>
        /// </summary>
        public FileDictionary(Stream dictFile, string fieldDelimiter)
        {
            @in = IOUtils.GetDecodingReader(dictFile, Encoding.UTF8);
            this.fieldDelimiter = fieldDelimiter;
        }

        public virtual IInputEnumerator GetEntryEnumerator()
        {
            try
            {
                return new FileEnumerator(this);
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw RuntimeException.Create(e);
            }
        }

        internal sealed class FileEnumerator : IInputEnumerator
        {
            private readonly FileDictionary outerInstance;

            internal long curWeight;
            internal readonly BytesRef spare = new BytesRef();
            internal BytesRef curPayload = new BytesRef();
            internal bool isFirstLine = true;
            internal bool hasPayloads = false;
            private BytesRef current;

            internal FileEnumerator(FileDictionary outerInstance)
            {
                this.outerInstance = outerInstance;
                outerInstance.line = outerInstance.@in.ReadLine();
                if (outerInstance.line is null)
                {
                    outerInstance.done = true;
                    IOUtils.Dispose(outerInstance.@in);
                }
                else
                {
                    string[] fields = outerInstance.line.Split(new string[] { outerInstance.fieldDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length > 3)
                    {
                        throw new ArgumentException("More than 3 fields in one line");
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

            public long Weight => curWeight;

            public BytesRef Current => current;

            public bool MoveNext()
            {
                if (outerInstance.done)
                {
                    current = null;
                    return false;
                }
                if (isFirstLine)
                {
                    isFirstLine = false;
                    current = spare;
                    return true;
                }
                outerInstance.line = outerInstance.@in.ReadLine();
                if (outerInstance.line != null)
                {
                    string[] fields = outerInstance.line.Split(new string[] { outerInstance.fieldDelimiter }, StringSplitOptions.RemoveEmptyEntries);
                    if (fields.Length > 3)
                    {
                        throw new ArgumentException("More than 3 fields in one line");
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
                    current = spare;
                    return true;
                }
                else
                {
                    outerInstance.done = true;
                    IOUtils.Dispose(outerInstance.@in);
                    current = null;
                    return false;
                }
            }

            public IComparer<BytesRef> Comparer => null;

            public BytesRef Payload
                => (hasPayloads) ? curPayload : null;


            public bool HasPayloads => hasPayloads;

            internal void ReadWeight(string weight)
            {
                // LUCENENET specific - don't use exception, use TryParse
                if (!long.TryParse(weight, NumberStyles.Integer, CultureInfo.InvariantCulture, out curWeight))
                {
                    // keep reading floats for bw compat
                    curWeight = (long)double.Parse(weight, NumberStyles.Float, CultureInfo.InvariantCulture);
                }
            }

            public ICollection<BytesRef> Contexts => null;

            public bool HasContexts => false;
        }
    }
}