// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    /// <see cref="SynonymFilter"/> handles multi-token synonyms with variable position increment offsets.
    /// <para>
    /// The matched tokens from the input stream may be optionally passed through (includeOrig=true)
    /// or discarded.  If the original tokens are included, the position increments may be modified
    /// to retain absolute positions after merging with the synonym tokenstream.
    /// </para>
    /// <para>
    /// Generated synonyms will start at the same position as the first matched source token.
    /// </para>
    /// </summary>
    /// @deprecated (3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0 
    [Obsolete("(3.4) use SynonymFilterFactory instead. only for precise index backwards compatibility. this factory will be removed in Lucene 5.0")]
    internal sealed class SlowSynonymFilter : TokenFilter
    {
        private readonly SlowSynonymMap map; // Map<String, SynonymMap>
        private IEnumerator<AttributeSource> replacement; // iterator over generated tokens

        public SlowSynonymFilter(TokenStream @in, SlowSynonymMap map) 
            : base(@in)
        {
            if (map is null)
            {
                throw new ArgumentException("map is required", nameof(map));
            }

            this.map = map;
            // just ensuring these attributes exist...
            AddAttribute<ICharTermAttribute>();
            AddAttribute<IPositionIncrementAttribute>();
            AddAttribute<IOffsetAttribute>();
            AddAttribute<ITypeAttribute>();
        }


        /*
         * Need to worry about multiple scenarios:
         *  - need to go for the longest match
         *    a b => foo      #shouldn't match if "a b" is followed by "c d"
         *    a b c d => bar
         *  - need to backtrack - retry matches for tokens already read
         *     a b c d => foo
         *       b c => bar
         *     If the input stream is "a b c x", one will consume "a b c d"
         *     trying to match the first rule... all but "a" should be
         *     pushed back so a match may be made on "b c".
         *  - don't try and match generated tokens (thus need separate queue)
         *    matching is not recursive.
         *  - handle optional generation of original tokens in all these cases,
         *    merging token streams to preserve token positions.
         *  - preserve original positionIncrement of first matched token
         */
        public override bool IncrementToken()
        {
            while (true)
            {
                // if there are any generated tokens, return them... don't try any
                // matches against them, as we specifically don't want recursion.
                if (replacement != null && replacement.MoveNext())
                {
                    Copy(this, replacement.Current);
                    return true;
                }

                // common case fast-path of first token not matching anything
                AttributeSource firstTok = NextTok();
                if (firstTok is null)
                {
                    return false;
                }
                var termAtt = firstTok.AddAttribute<ICharTermAttribute>();
                if (map.Submap is null || !map.Submap.TryGetValue(termAtt.Buffer, 0, termAtt.Length, out SlowSynonymMap result) || result is null)
                {
                    Copy(this, firstTok);
                    return true;
                }

                // fast-path failed, clone ourselves if needed
                if (firstTok == this)
                {
                    firstTok = CloneAttributes();
                }
                // OK, we matched a token, so find the longest match.

                matched = new LinkedList<AttributeSource>();

                result = Match(result);

                if (result is null)
                {
                    // no match, simply return the first token read.
                    Copy(this, firstTok);
                    return true;
                }

                // reuse, or create new one each time?
                IList<AttributeSource> generated = new JCG.List<AttributeSource>(result.Synonyms.Length + matched.Count + 1);

                //
                // there was a match... let's generate the new tokens, merging
                // in the matched tokens (position increments need adjusting)
                //
                AttributeSource lastTok = matched.Count == 0 ? firstTok : matched.Last.Value;
                bool includeOrig = result.IncludeOrig;

                AttributeSource origTok = includeOrig ? firstTok : null;
                IPositionIncrementAttribute firstPosIncAtt = firstTok.AddAttribute<IPositionIncrementAttribute>();
                int origPos = firstPosIncAtt.PositionIncrement; // position of origTok in the original stream
                int repPos = 0; // curr position in replacement token stream
                int pos = 0; // current position in merged token stream

                for (int i = 0; i < result.Synonyms.Length; i++)
                {
                    Token repTok = result.Synonyms[i];
                    AttributeSource newTok = firstTok.CloneAttributes();
                    ICharTermAttribute newTermAtt = newTok.AddAttribute<ICharTermAttribute>();
                    IOffsetAttribute newOffsetAtt = newTok.AddAttribute<IOffsetAttribute>();
                    IPositionIncrementAttribute newPosIncAtt = newTok.AddAttribute<IPositionIncrementAttribute>();

                    IOffsetAttribute lastOffsetAtt = lastTok.AddAttribute<IOffsetAttribute>();

                    newOffsetAtt.SetOffset(newOffsetAtt.StartOffset, lastOffsetAtt.EndOffset);
                    newTermAtt.CopyBuffer(repTok.Buffer, 0, repTok.Length);
                    repPos += repTok.PositionIncrement;
                    if (i == 0) // make position of first token equal to original
                    {
                        repPos = origPos;
                    }

                    // if necessary, insert original tokens and adjust position increment
                    while (origTok != null && origPos <= repPos)
                    {
                        IPositionIncrementAttribute origPosInc = origTok.AddAttribute<IPositionIncrementAttribute>();
                        origPosInc.PositionIncrement = origPos - pos;
                        generated.Add(origTok);
                        pos += origPosInc.PositionIncrement;
                        //origTok = matched.Count == 0 ? null : matched.RemoveFirst();
                        if (matched.Count == 0)
                        {
                            origTok = null;
                        }
                        else
                        {
                            origTok = matched.First.Value;
                            matched.Remove(origTok);
                        }
                        if (origTok != null)
                        {
                            origPosInc = origTok.AddAttribute<IPositionIncrementAttribute>();
                            origPos += origPosInc.PositionIncrement;
                        }
                    }

                    newPosIncAtt.PositionIncrement = repPos - pos;
                    generated.Add(newTok);
                    pos += newPosIncAtt.PositionIncrement;
                }

                // finish up any leftover original tokens
                while (origTok != null)
                {
                    IPositionIncrementAttribute origPosInc = origTok.AddAttribute<IPositionIncrementAttribute>();
                    origPosInc.PositionIncrement = origPos - pos;
                    generated.Add(origTok);
                    pos += origPosInc.PositionIncrement;
                    if (matched.Count == 0)
                    {
                        origTok = null;
                    }
                    else
                    {
                        origTok = matched.First.Value;
                        matched.Remove(origTok);
                    }
                    if (origTok != null)
                    {
                        origPosInc = origTok.AddAttribute<IPositionIncrementAttribute>();
                        origPos += origPosInc.PositionIncrement;
                    }
                }

                // what if we replaced a longer sequence with a shorter one?
                // a/0 b/5 =>  foo/0
                // should I re-create the gap on the next buffered token?

                replacement = generated.GetEnumerator();
                // Now return to the top of the loop to read and return the first
                // generated token.. The reason this is done is that we may have generated
                // nothing at all, and may need to continue with more matching logic.
            }
        }


        //
        // Defer creation of the buffer until the first time it is used to
        // optimize short fields with no matches.
        //
        private LinkedList<AttributeSource> buffer;
        private LinkedList<AttributeSource> matched;

        private bool exhausted;

        private AttributeSource NextTok()
        {
            if (buffer != null && buffer.Count > 0)
            {
                var first = buffer.First.Value;
                buffer.Remove(first);
                return first;
            }
            else
            {
                if (!exhausted && m_input.IncrementToken())
                {
                    return this;
                }
                else
                {
                    exhausted = true;
                    return null;
                }
            }
        }

        private void PushTok(AttributeSource t)
        {
            if (buffer is null)
            {
                buffer = new LinkedList<AttributeSource>();
            }
            buffer.AddFirst(t);
        }

        private SlowSynonymMap Match(SlowSynonymMap map)
        {
            SlowSynonymMap result = null;

            if (map.Submap != null)
            {
                AttributeSource tok = NextTok();
                if (tok != null)
                {
                    // clone ourselves.
                    if (tok == this)
                    {
                        tok = CloneAttributes();
                    }
                    // check for positionIncrement!=1?  if>1, should not match, if==0, check multiple at this level?
                    var termAtt = tok.GetAttribute<ICharTermAttribute>();

                    if (map.Submap.TryGetValue(termAtt.Buffer, 0, termAtt.Length, out SlowSynonymMap subMap) && subMap != null)
                    {
                        // recurse
                        result = Match(subMap);
                    }

                    if (result != null)
                    {
                        matched.AddFirst(tok);
                    }
                    else
                    {
                        // push back unmatched token
                        PushTok(tok);
                    }
                }
            }

            // if no longer sequence matched, so if this node has synonyms, it's the match.
            if (result is null && map.Synonyms != null)
            {
                result = map;
            }

            return result;
        }

        private void Copy(AttributeSource target, AttributeSource source)
        {
            if (target != source)
            {
                source.CopyTo(target);
            }
        }

        public override void Reset()
        {
            m_input.Reset();
            replacement = null;
            exhausted = false;
        }
    }
}