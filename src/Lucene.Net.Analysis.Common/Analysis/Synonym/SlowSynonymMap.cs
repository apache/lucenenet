// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// Mapping rules for use with <see cref="SlowSynonymFilter"/>
    /// </summary>
    /// @deprecated (3.4) use <see cref="SynonymFilterFactory"/> instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
    [Obsolete("(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
    internal class SlowSynonymMap
    {
        /// <summary>
        /// @lucene.internal </summary>
        public CharArrayDictionary<SlowSynonymMap> Submap // recursive: Map<String, SynonymMap>
        {
            get => submap;
            set => submap = value;
        }
        private CharArrayDictionary<SlowSynonymMap> submap;

        /// <summary>
        /// @lucene.internal </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public Token[] Synonyms
        {
            get => synonyms;
            set => synonyms = value;
        }
        private Token[] synonyms;
        internal int flags;

        internal const int INCLUDE_ORIG = 0x01;
        internal const int IGNORE_CASE = 0x02;

        public SlowSynonymMap()
        {
        }

        public SlowSynonymMap(bool ignoreCase)
        {
            if (ignoreCase)
            {
                flags |= IGNORE_CASE;
            }
        }

        public virtual bool IncludeOrig => (flags & INCLUDE_ORIG) != 0;

        public virtual bool IgnoreCase => (flags & IGNORE_CASE) != 0;

        /// <param name="singleMatch">  <see cref="IList{String}"/>, the sequence of strings to match </param>
        /// <param name="replacement">  <see cref="IList{Token}"/> the list of tokens to use on a match </param>
        /// <param name="includeOrig">  sets a flag on this mapping signaling the generation of matched tokens in addition to the replacement tokens </param>
        /// <param name="mergeExisting"> merge the replacement tokens with any other mappings that exist </param>
        public virtual void Add(IList<string> singleMatch, IList<Token> replacement, bool includeOrig, bool mergeExisting)
        {
            var currMap = this;
            foreach (string str in singleMatch)
            {
                if (currMap.submap is null)
                {
                    // for now hardcode at 4.0, as its what the old code did.
                    // would be nice to fix, but shouldn't store a version in each submap!!!
                    currMap.submap = new CharArrayDictionary<SlowSynonymMap>(LuceneVersion.LUCENE_CURRENT, 1, IgnoreCase);
                }

                if (!currMap.submap.TryGetValue(str, out SlowSynonymMap map) || map is null)
                {
                    map = new SlowSynonymMap();
                    map.flags |= flags & IGNORE_CASE;
                    currMap.submap[str] = map;
                }

                currMap = map;
            }

            if (currMap.synonyms != null && !mergeExisting)
            {
                throw new ArgumentException("SynonymFilter: there is already a mapping for " + singleMatch);
            }
            IList<Token> superset = currMap.synonyms is null ? replacement : MergeTokens(currMap.synonyms, replacement);
            currMap.synonyms = superset.ToArray();
            if (includeOrig)
            {
                currMap.flags |= INCLUDE_ORIG;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder("<");
            if (synonyms != null)
            {
                sb.Append('[');
                for (int i = 0; i < synonyms.Length; i++)
                {
                    if (i != 0)
                    {
                        sb.Append(',');
                    }
                    sb.Append(synonyms[i]);
                }
                if ((flags & INCLUDE_ORIG) != 0)
                {
                    sb.Append(",ORIG");
                }
                sb.Append("],");
            }
            sb.Append(submap);
            sb.Append('>');
            return sb.ToString();
        }

        /// <summary>
        /// Produces a <see cref="IList{Token}"/> from a <see cref="IList{String}"/>
        /// </summary>
        public static IList<Token> MakeTokens(IList<string> strings)
        {
            IList<Token> ret = new JCG.List<Token>(strings.Count);
            foreach (string str in strings)
            {
                //Token newTok = new Token(str,0,0,"SYNONYM");
                Token newTok = new Token(str, 0, 0, "SYNONYM");
                ret.Add(newTok);
            }
            return ret;
        }


        /// <summary>
        /// Merge two lists of tokens, producing a single list with manipulated positionIncrements so that
        /// the tokens end up at the same position.
        /// 
        /// Example:  [a b] merged with [c d] produces [a/b c/d]  ('/' denotes tokens in the same position)
        /// Example:  [a,5 b,2] merged with [c d,4 e,4] produces [c a,5/d b,2 e,2]  (a,n means a has posInc=n)
        /// </summary>
        public static IList<Token> MergeTokens(IList<Token> lst1, IList<Token> lst2)
        {
            var result = new JCG.List<Token>();
            if (lst1 is null || lst2 is null)
            {
                if (lst2 != null)
                {
                    result.AddRange(lst2);
                }
                if (lst1 != null)
                {
                    result.AddRange(lst1);
                }
                return result;
            }

            int pos = 0;
            using (var iter1 = lst1.GetEnumerator())
            using (var iter2 = lst2.GetEnumerator())
            {
                var tok1 = iter1.MoveNext() ? iter1.Current : null;
                var tok2 = iter2.MoveNext() ? iter2.Current : null;
                int pos1 = tok1 != null ? tok1.PositionIncrement : 0;
                int pos2 = tok2 != null ? tok2.PositionIncrement : 0;
                while (tok1 != null || tok2 != null)
                {
                    while (tok1 != null && (pos1 <= pos2 || tok2 is null))
                    {
                        var tok = new Token(tok1.StartOffset, tok1.EndOffset, tok1.Type);
                        tok.CopyBuffer(tok1.Buffer, 0, tok1.Length);
                        tok.PositionIncrement = pos1 - pos;
                        result.Add(tok);
                        pos = pos1;
                        tok1 = iter1.MoveNext() ? iter1.Current : null;
                        pos1 += tok1 != null ? tok1.PositionIncrement : 0;
                    }
                    while (tok2 != null && (pos2 <= pos1 || tok1 is null))
                    {
                        var tok = new Token(tok2.StartOffset, tok2.EndOffset, tok2.Type);
                        tok.CopyBuffer(tok2.Buffer, 0, tok2.Length);
                        tok.PositionIncrement = pos2 - pos;
                        result.Add(tok);
                        pos = pos2;
                        tok2 = iter2.MoveNext() ? iter2.Current : null;
                        pos2 += tok2 != null ? tok2.PositionIncrement : 0;
                    }
                }
                return result;
            }
        }
    }
}