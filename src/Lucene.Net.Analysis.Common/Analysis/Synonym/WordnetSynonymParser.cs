// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Synonym
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
    /// Parser for wordnet prolog format
    /// <para>
    /// See http://wordnet.princeton.edu/man/prologdb.5WN.html for a description of the format.
    /// @lucene.experimental
    /// </para>
    /// </summary>
    // TODO: allow you to specify syntactic categories (e.g. just nouns, etc)
    public class WordnetSynonymParser : SynonymMap.Parser
    {
        private readonly bool expand;

        public WordnetSynonymParser(bool dedup, bool expand, Analyzer analyzer) 
            : base(dedup, analyzer)
        {
            this.expand = expand;
        }

        public override void Parse(TextReader @in)
        {
            int lineNumber = 0;
            TextReader br = @in;
            try
            {
                string line = null;
                string lastSynSetID = "";
                CharsRef[] synset = new CharsRef[8];
                int synsetSize = 0;


                while ((line = br.ReadLine()) != null)
                {
                    lineNumber++;
                    string synSetID = line.Substring(2, 9);

                    if (!synSetID.Equals(lastSynSetID, StringComparison.Ordinal))
                    {
                        AddInternal(synset, synsetSize);
                        synsetSize = 0;
                    }

                    if (synset.Length <= synsetSize + 1)
                    {
                        CharsRef[] larger = new CharsRef[synset.Length * 2];
                        Arrays.Copy(synset, 0, larger, 0, synsetSize);
                        synset = larger;
                    }

                    synset[synsetSize] = ParseSynonym(line, synset[synsetSize]);
                    synsetSize++;
                    lastSynSetID = synSetID;
                }

                // final synset in the file
                AddInternal(synset, synsetSize);
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                throw new ParseException("Invalid synonym rule at line " + lineNumber, lineNumber, e);
            }
            finally
            {
                br.Dispose();
            }
        }

        private CharsRef ParseSynonym(string line, CharsRef reuse)
        {
            if (reuse is null)
            {
                reuse = new CharsRef(8);
            }

            int start = line.IndexOf('\'') + 1;
            int end = line.LastIndexOf('\'');

            string text = line.Substring(start, end - start).Replace("''", "'");
            return Analyze(text, reuse);
        }

        private void AddInternal(CharsRef[] synset, int size)
        {
            if (size <= 1)
            {
                return; // nothing to do
            }

            if (expand)
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        Add(synset[i], synset[j], false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    Add(synset[i], synset[0], false);
                }
            }
        }
    }
}