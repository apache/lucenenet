// Lucene version compatibility level 4.8.1 + LUCENE-10059 (https://github.com/apache/lucene/pull/254 only)

using J2N;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Analysis.Ja.TokenAttributes;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ja
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
    /// Tokenizer for Japanese that uses morphological analysis.
    /// </summary>
    /// <remarks>
    /// This tokenizer sets a number of additional attributes:
    /// <list type="bullet">
    ///     <item><description><see cref="IBaseFormAttribute"/> containing base form for inflected adjectives and verbs.</description></item>
    ///     <item><description><see cref="IPartOfSpeechAttribute"/> containing part-of-speech.</description></item>
    ///     <item><description><see cref="IReadingAttribute"/> containing reading and pronunciation.</description></item>
    ///     <item><description><see cref="IInflectionAttribute"/> containing additional part-of-speech information for inflected forms.</description></item>
    /// </list>
    /// <para/>
    /// This tokenizer uses a rolling Viterbi search to find the
    /// least cost segmentation (path) of the incoming characters.
    /// For tokens that appear to be compound (> length 2 for all
    /// Kanji, or > length 7 for non-Kanji), we see if there is a
    /// 2nd best segmentation of that token after applying
    /// penalties to the long tokens.  If so, and the Mode is
    /// <see cref="JapaneseTokenizerMode.SEARCH"/>, we output the alternate segmentation
    /// as well.
    /// </remarks>
    public sealed class JapaneseTokenizer : Tokenizer
    {
        // LUCENENET specific: de-nested Mode and renamed JapaneseTokenizerMode

        /// <summary>
        /// Default tokenization mode. Currently this is <see cref="JapaneseTokenizerMode.SEARCH"/>.
        /// </summary>
        public static readonly JapaneseTokenizerMode DEFAULT_MODE = JapaneseTokenizerMode.SEARCH;

        // LUCENENET specific: de-nested Type and renamed JapaneseTokenizerType


#pragma warning disable CA1802 // Use literals where appropriate
        private static readonly bool VERBOSE = false; // For debugging
#pragma warning restore CA1802 // Use literals where appropriate

        private const int SEARCH_MODE_KANJI_LENGTH = 2;

        private const int SEARCH_MODE_OTHER_LENGTH = 7; // Must be >= SEARCH_MODE_KANJI_LENGTH

        private const int SEARCH_MODE_KANJI_PENALTY = 3000;

        private const int SEARCH_MODE_OTHER_PENALTY = 1700;

        // For safety:
        private const int MAX_UNKNOWN_WORD_LENGTH = 1024;
        private const int MAX_BACKTRACE_GAP = 1024;

        private readonly IDictionary<JapaneseTokenizerType, IDictionary> dictionaryMap = new Dictionary<JapaneseTokenizerType, IDictionary>();

        private readonly TokenInfoFST fst;
        private readonly TokenInfoDictionary dictionary;
        private readonly UnknownDictionary unkDictionary;
        private readonly ConnectionCosts costs;
        private readonly UserDictionary userDictionary;
        private readonly CharacterDefinition characterDefinition;

        private readonly FST.Arc<Int64> arc = new FST.Arc<Int64>();
        private readonly FST.BytesReader fstReader;
        private readonly Int32sRef wordIdRef = new Int32sRef();

        private readonly FST.BytesReader userFSTReader;
        private readonly TokenInfoFST userFST;

        private readonly RollingCharBuffer buffer = new RollingCharBuffer();

        private readonly WrappedPositionArray positions = new WrappedPositionArray();

        private readonly bool discardPunctuation;
        private readonly bool searchMode;
        private readonly bool extendedMode;
        private readonly bool outputCompounds;

        // Index of the last character of unknown word:
        private int unknownWordEndIndex = -1;

        // True once we've hit the EOF from the input reader:
        private bool end;

        // Last absolute position we backtraced from:
        private int lastBackTracePos;

        // Position of last token we returned; we use this to
        // figure out whether to set posIncr to 0 or 1:
        private int lastTokenPos;

        // Next absolute position to process:
        private int pos;

        // Already parsed, but not yet passed to caller, tokens:
        private readonly JCG.List<Token> pending = new JCG.List<Token>();

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IBaseFormAttribute basicFormAtt;
        private readonly IPartOfSpeechAttribute posAtt;
        private readonly IReadingAttribute readingAtt;
        private readonly IInflectionAttribute inflectionAtt;

        /// <summary>
        /// Create a new JapaneseTokenizer.
        /// <para/>
        /// Uses the default AttributeFactory.
        /// </summary>
        /// <param name="input">TextReader containing text.</param>
        /// <param name="userDictionary">Optional: if non-null, user dictionary.</param>
        /// <param name="discardPunctuation"><c>true</c> if punctuation tokens should be dropped from the output.</param>
        /// <param name="mode">Tokenization mode.</param>
        public JapaneseTokenizer(TextReader input, UserDictionary userDictionary, bool discardPunctuation, JapaneseTokenizerMode mode)
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, userDictionary, discardPunctuation, mode)
        {
        }

        /// <summary>
        /// Create a new JapaneseTokenizer.
        /// </summary>
        /// <param name="factory">The AttributeFactory to use.</param>
        /// <param name="input">TextReader containing text.</param>
        /// <param name="userDictionary">Optional: if non-null, user dictionary.</param>
        /// <param name="discardPunctuation"><c>true</c> if punctuation tokens should be dropped from the output.</param>
        /// <param name="mode">Tokenization mode.</param>
        public JapaneseTokenizer
            (AttributeFactory factory, TextReader input, UserDictionary userDictionary, bool discardPunctuation, JapaneseTokenizerMode mode)
            : base(factory, input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            this.posLengthAtt = AddAttribute<IPositionLengthAttribute>();
            this.basicFormAtt = AddAttribute<IBaseFormAttribute>();
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
            this.readingAtt = AddAttribute<IReadingAttribute>();
            this.inflectionAtt = AddAttribute<IInflectionAttribute>();

            dictionary = TokenInfoDictionary.Instance;
            fst = dictionary.FST;
            unkDictionary = UnknownDictionary.Instance;
            characterDefinition = unkDictionary.CharacterDefinition;
            this.userDictionary = userDictionary;
            costs = ConnectionCosts.Instance;
            fstReader = fst.GetBytesReader();
            if (userDictionary != null)
            {
                userFST = userDictionary.FST;
                userFSTReader = userFST.GetBytesReader();
            }
            else
            {
                userFST = null;
                userFSTReader = null;
            }
            this.discardPunctuation = discardPunctuation;
            switch (mode)
            {
                case JapaneseTokenizerMode.SEARCH:
                    searchMode = true;
                    extendedMode = false;
                    outputCompounds = true;
                    break;
                case JapaneseTokenizerMode.EXTENDED:
                    searchMode = true;
                    extendedMode = true;
                    outputCompounds = false;
                    break;
                default:
                    searchMode = false;
                    extendedMode = false;
                    outputCompounds = false;
                    break;
            }
            buffer.Reset(this.m_input);

            ResetState();

            dictionaryMap[JapaneseTokenizerType.KNOWN] = dictionary;
            dictionaryMap[JapaneseTokenizerType.UNKNOWN] = unkDictionary;
            dictionaryMap[JapaneseTokenizerType.USER] = userDictionary;
        }

        private GraphvizFormatter dotOut;

        // LUCENENET specific - added getter and made into property
        // so we can set this during object initialization.

        /// <summary>
        /// Expert: set this to produce graphviz (dot) output of
        /// the Viterbi lattice
        /// </summary>
        public GraphvizFormatter GraphvizFormatter
        {
            get => this.dotOut;
            set => this.dotOut = value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                buffer.Reset(m_input);
            }
        }

        public override void Reset()
        {
            base.Reset();
            buffer.Reset(m_input);
            ResetState();
        }

        private void ResetState()
        {
            positions.Reset();
            unknownWordEndIndex = -1;
            pos = 0;
            end = false;
            lastBackTracePos = 0;
            lastTokenPos = -1;
            pending.Clear();

            // Add BOS:
            positions.Get(0).Add(0, 0, -1, -1, -1, JapaneseTokenizerType.KNOWN);
        }

        public override void End()
        {
            base.End();
            // Set final offset
            int finalOffset = CorrectOffset(pos);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        // Returns the added cost that a 2nd best segmentation is
        // allowed to have.  Ie, if we see path with cost X,
        // ending in a compound word, and this method returns
        // threshold > 0, then we will also find the 2nd best
        // segmentation and if its path score is within this
        // threshold of X, we'll include it in the output:
        private int ComputeSecondBestThreshold(int pos, int length)
        {
            // TODO: maybe we do something else here, instead of just
            // using the penalty...?  EG we can be more aggressive on
            // when to also test for 2nd best path
            return ComputePenalty(pos, length);
        }

        private int ComputePenalty(int pos, int length)
        {
            if (length > SEARCH_MODE_KANJI_LENGTH)
            {
                bool allKanji = true;
                // check if node consists of only kanji
                int endPos = pos + length;
                for (int pos2 = pos; pos2 < endPos; pos2++)
                {
                    if (!characterDefinition.IsKanji((char)buffer.Get(pos2)))
                    {
                        allKanji = false;
                        break;
                    }
                }
                if (allKanji)
                {  // Process only Kanji keywords
                    return (length - SEARCH_MODE_KANJI_LENGTH) * SEARCH_MODE_KANJI_PENALTY;
                }
                else if (length > SEARCH_MODE_OTHER_LENGTH)
                {
                    return (length - SEARCH_MODE_OTHER_LENGTH) * SEARCH_MODE_OTHER_PENALTY;
                }
            }
            return 0;
        }

        // LUCENENET specific - de-nested Position class

        private void Add(IDictionary dict, Position fromPosData, int endPos, int wordID, JapaneseTokenizerType type, bool addPenalty)
        {
            int wordCost = dict.GetWordCost(wordID);
            int leftID = dict.GetLeftId(wordID);
            int leastCost = int.MaxValue;
            int leastIDX = -1;
            if (Debugging.AssertsEnabled) Debugging.Assert(fromPosData.count > 0);
            for (int idx = 0; idx < fromPosData.count; idx++)
            {
                // Cost is path cost so far, plus word cost (added at
                // end of loop), plus bigram cost:
                int cost = fromPosData.costs[idx] + costs.Get(fromPosData.lastRightID[idx], leftID);
                if (VERBOSE)
                {
                    Console.WriteLine("      fromIDX=" + idx + ": cost=" + cost + " (prevCost=" + fromPosData.costs[idx] + " wordCost=" + wordCost + " bgCost=" + costs.Get(fromPosData.lastRightID[idx], leftID) + " leftID=" + leftID);
                }
                if (cost < leastCost)
                {
                    leastCost = cost;
                    leastIDX = idx;
                    if (VERBOSE)
                    {
                        Console.WriteLine("        **");
                    }
                }
            }

            leastCost += wordCost;

            if (VERBOSE)
            {
                Console.WriteLine("      + cost=" + leastCost + " wordID=" + wordID + " leftID=" + leftID + " leastIDX=" + leastIDX + " toPos=" + endPos + " toPos.idx=" + positions.Get(endPos).count);
            }

            if ((addPenalty || (!outputCompounds && searchMode)) && type != JapaneseTokenizerType.USER)
            {
                int penalty = ComputePenalty(fromPosData.pos, endPos - fromPosData.pos);
                if (VERBOSE)
                {
                    if (penalty > 0)
                    {
                        Console.WriteLine("        + penalty=" + penalty + " cost=" + (leastCost + penalty));
                    }
                }
                leastCost += penalty;
            }

            //positions.get(endPos).add(leastCost, dict.getRightId(wordID), fromPosData.pos, leastIDX, wordID, type);
            if (Debugging.AssertsEnabled) Debugging.Assert(leftID == dict.GetRightId(wordID));
            positions.Get(endPos).Add(leastCost, leftID, fromPosData.pos, leastIDX, wordID, type);
        }

        public override bool IncrementToken()
        {

            // parse() is able to return w/o producing any new
            // tokens, when the tokens it had produced were entirely
            // punctuation.  So we loop here until we get a real
            // token or we end:
            while (pending.Count == 0)
            {
                if (end)
                {
                    return false;
                }

                // Push Viterbi forward some more:
                Parse();
            }

            Token token = pending[pending.Count - 1]; // LUCENENET: The above loop ensures we don't get here unless we have at least 1 item
            if (token != null)
            {
                pending.Remove(token);
            }

            int position = token.Position;
            int length = token.Length;
            ClearAttributes();
            if (Debugging.AssertsEnabled) Debugging.Assert(length > 0);
            //System.out.println("off=" + token.getOffset() + " len=" + length + " vs " + token.getSurfaceForm().length);
            termAtt.CopyBuffer(token.SurfaceForm, token.Offset, length);
            offsetAtt.SetOffset(CorrectOffset(position), CorrectOffset(position + length));
            basicFormAtt.SetToken(token);
            posAtt.SetToken(token);
            readingAtt.SetToken(token);
            inflectionAtt.SetToken(token);
            if (token.Position == lastTokenPos)
            {
                posIncAtt.PositionIncrement = 0;
                posLengthAtt.PositionLength = token.PositionLength;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(token.Position > lastTokenPos);
                posIncAtt.PositionIncrement = 1;
                posLengthAtt.PositionLength = 1;
            }
            if (VERBOSE)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ":    incToken: return token=" + token);
            }
            lastTokenPos = token.Position;
            return true;
        }

        /// <summary>
        /// Incrementally parse some more characters.  This runs
        /// the viterbi search forwards "enough" so that we
        /// generate some more tokens.  How much forward depends on
        /// the chars coming in, since some chars could cause
        /// longer-lasting ambiguity in the parsing.  Once the
        /// ambiguity is resolved, then we back trace, produce
        /// the pending tokens, and return.
        /// </summary>
        private void Parse()
        {
            if (VERBOSE)
            {
                Console.WriteLine("\nPARSE");
            }

            // Advances over each position (character):
            while (true)
            {

                if (buffer.Get(pos) == -1)
                {
                    // End
                    break;
                }

                Position posData = positions.Get(pos);
                bool isFrontier = positions.GetNextPos() == pos + 1;

                if (posData.count == 0)
                {
                    // No arcs arrive here; move to next position:
                    if (VERBOSE)
                    {
                        Console.WriteLine("    no arcs in; skip pos=" + pos);
                    }
                    pos++;
                    continue;
                }

                if (pos > lastBackTracePos && posData.count == 1 && isFrontier)
                {
                    //  if (pos > lastBackTracePos && posData.count == 1 && isFrontier) {
                    // We are at a "frontier", and only one node is
                    // alive, so whatever the eventual best path is must
                    // come through this node.  So we can safely commit
                    // to the prefix of the best path at this point:
                    Backtrace(posData, 0);

                    // Re-base cost so we don't risk int overflow:
                    posData.costs[0] = 0;

                    if (pending.Count != 0)
                    {
                        return;
                    }
                    else
                    {
                        // This means the backtrace only produced
                        // punctuation tokens, so we must keep parsing.
                    }
                }

                if (pos - lastBackTracePos >= MAX_BACKTRACE_GAP)
                {
                    // Safety: if we've buffered too much, force a
                    // backtrace now.  We find the least-cost partial
                    // path, across all paths, backtrace from it, and
                    // then prune all others.  Note that this, in
                    // general, can produce the wrong result, if the
                    // total best path did not in fact back trace
                    // through this partial best path.  But it's the
                    // best we can do... (short of not having a
                    // safety!).

                    // First pass: find least cost partial path so far,
                    // including ending at future positions:
                    int leastIDX = -1;
                    int leastCost = int.MaxValue;
                    Position leastPosData = null;
                    for (int pos2 = pos; pos2 < positions.GetNextPos(); pos2++)
                    {
                        Position posData2 = positions.Get(pos2);
                        for (int idx = 0; idx < posData2.count; idx++)
                        {
                            //System.out.println("    idx=" + idx + " cost=" + cost);
                            int cost = posData2.costs[idx];
                            if (cost < leastCost)
                            {
                                leastCost = cost;
                                leastIDX = idx;
                                leastPosData = posData2;
                            }
                        }
                    }

                    // We will always have at least one live path:
                    if (Debugging.AssertsEnabled) Debugging.Assert(leastIDX != -1);

                    // Second pass: prune all but the best path:
                    for (int pos2 = pos; pos2 < positions.GetNextPos(); pos2++)
                    {
                        Position posData2 = positions.Get(pos2);
                        if (posData2 != leastPosData)
                        {
                            posData2.Reset();
                        }
                        else
                        {
                            if (leastIDX != 0)
                            {
                                posData2.costs[0] = posData2.costs[leastIDX];
                                posData2.lastRightID[0] = posData2.lastRightID[leastIDX];
                                posData2.backPos[0] = posData2.backPos[leastIDX];
                                posData2.backIndex[0] = posData2.backIndex[leastIDX];
                                posData2.backID[0] = posData2.backID[leastIDX];
                                posData2.backType[0] = posData2.backType[leastIDX];
                            }
                            posData2.count = 1;
                        }
                    }

                    Backtrace(leastPosData, 0);

                    // Re-base cost so we don't risk int overflow:
                    Arrays.Fill(leastPosData.costs, 0, leastPosData.count, 0);

                    if (pos != leastPosData.pos)
                    {
                        // We jumped into a future position:
                        if (Debugging.AssertsEnabled) Debugging.Assert(pos < leastPosData.pos);
                        pos = leastPosData.pos;
                    }

                    if (pending.Count != 0)
                    {
                        return;
                    }
                    else
                    {
                        // This means the backtrace only produced
                        // punctuation tokens, so we must keep parsing.
                        continue;
                    }
                }

                if (VERBOSE)
                {
                    Console.WriteLine("\n  extend @ pos=" + pos + " char=" + (char)buffer.Get(pos));
                }

                if (VERBOSE)
                {
                    Console.WriteLine("    " + posData.count + " arcs in");
                }

                bool anyMatches = false;

                // First try user dict:
                if (userFST != null)
                {
                    userFST.GetFirstArc(arc);
                    int output = 0;
                    for (int posAhead = posData.pos; ; posAhead++)
                    {
                        int ch = buffer.Get(posAhead);
                        if (ch == -1)
                        {
                            break;
                        }
                        if (userFST.FindTargetArc(ch, arc, arc, posAhead == posData.pos, userFSTReader) is null)
                        {
                            break;
                        }
                        output += (int)arc.Output;
                        if (arc.IsFinal)
                        {
                            if (VERBOSE)
                            {
                                Console.WriteLine("    USER word " + new string(buffer.Get(pos, posAhead - pos + 1)) + " toPos=" + (posAhead + 1));
                            }
                            Add(userDictionary, posData, posAhead + 1, output + (int)arc.NextFinalOutput, JapaneseTokenizerType.USER, false);
                            anyMatches = true;
                        }
                    }
                }

                // TODO: we can be more aggressive about user
                // matches?  if we are "under" a user match then don't
                // extend KNOWN/UNKNOWN paths?

                if (!anyMatches)
                {
                    // Next, try known dictionary matches
                    fst.GetFirstArc(arc);
                    int output = 0;

                    for (int posAhead = posData.pos; ; posAhead++)
                    {
                        int ch = buffer.Get(posAhead);
                        if (ch == -1)
                        {
                            break;
                        }
                        //System.out.println("    match " + (char) ch + " posAhead=" + posAhead);

                        if (fst.FindTargetArc(ch, arc, arc, posAhead == posData.pos, fstReader) is null)
                        {
                            break;
                        }

                        output += (int)arc.Output;

                        // Optimization: for known words that are too-long
                        // (compound), we should pre-compute the 2nd
                        // best segmentation and store it in the
                        // dictionary instead of recomputing it each time a
                        // match is found.

                        if (arc.IsFinal)
                        {
                            dictionary.LookupWordIds(output + (int)arc.NextFinalOutput, wordIdRef);
                            if (VERBOSE)
                            {
                                Console.WriteLine("    KNOWN word " + new string(buffer.Get(pos, posAhead - pos + 1)) + " toPos=" + (posAhead + 1) + " " + wordIdRef.Length + " wordIDs");
                            }
                            for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                            {
                                Add(dictionary, posData, posAhead + 1, wordIdRef.Int32s[wordIdRef.Offset + ofs], JapaneseTokenizerType.KNOWN, false);
                                anyMatches = true;
                            }
                        }
                    }
                }

                // In the case of normal mode, it doesn't process unknown word greedily.

                if (!searchMode && unknownWordEndIndex > posData.pos)
                {
                    pos++;
                    continue;
                }

                char firstCharacter = (char)buffer.Get(pos);
                if (!anyMatches || characterDefinition.IsInvoke(firstCharacter))
                {

                    // Find unknown match:
                    int characterId = characterDefinition.GetCharacterClass(firstCharacter);
                    bool isPunct = IsPunctuation(firstCharacter);

                    // NOTE: copied from UnknownDictionary.lookup:
                    int unknownWordLength;
                    if (!characterDefinition.IsGroup(firstCharacter))
                    {
                        unknownWordLength = 1;
                    }
                    else
                    {
                        // Extract unknown word. Characters with the same character class are considered to be part of unknown word
                        unknownWordLength = 1;
                        for (int posAhead = pos + 1; unknownWordLength < MAX_UNKNOWN_WORD_LENGTH; posAhead++)
                        {
                            int ch = buffer.Get(posAhead);
                            if (ch == -1)
                            {
                                break;
                            }
                            if (characterId == characterDefinition.GetCharacterClass((char)ch) &&
                                IsPunctuation((char)ch) == isPunct)
                            {
                                unknownWordLength++;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    unkDictionary.LookupWordIds(characterId, wordIdRef); // characters in input text are supposed to be the same
                    if (VERBOSE)
                    {
                        Console.WriteLine("    UNKNOWN word len=" + unknownWordLength + " " + wordIdRef.Length + " wordIDs");
                    }
                    for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                    {
                        Add(unkDictionary, posData, posData.pos + unknownWordLength, wordIdRef.Int32s[wordIdRef.Offset + ofs], JapaneseTokenizerType.UNKNOWN, false);
                    }

                    unknownWordEndIndex = posData.pos + unknownWordLength;
                }

                pos++;
            }

            end = true;

            if (pos > 0)
            {

                Position endPosData = positions.Get(pos);
                int leastCost = int.MaxValue;
                int leastIDX = -1;
                if (VERBOSE)
                {
                    Console.WriteLine("  end: " + endPosData.count + " nodes");
                }
                for (int idx = 0; idx < endPosData.count; idx++)
                {
                    // Add EOS cost:
                    int cost = endPosData.costs[idx] + costs.Get(endPosData.lastRightID[idx], 0);
                    //System.out.println("    idx=" + idx + " cost=" + cost + " (pathCost=" + endPosData.costs[idx] + " bgCost=" + costs.get(endPosData.lastRightID[idx], 0) + ") backPos=" + endPosData.backPos[idx]);
                    if (cost < leastCost)
                    {
                        leastCost = cost;
                        leastIDX = idx;
                    }
                }

                Backtrace(endPosData, leastIDX);
            }
            else
            {
                // No characters in the input string; return no tokens!
            }
        }

        // Eliminates arcs from the lattice that are compound
        // tokens (have a penalty) or are not congruent with the
        // compound token we've matched (ie, span across the
        // startPos).  This should be fairly efficient, because we
        // just keep the already intersected structure of the
        // graph, eg we don't have to consult the FSTs again:

        private void PruneAndRescore(int startPos, int endPos, int bestStartIDX)
        {
            if (VERBOSE)
            {
                Console.WriteLine("  pruneAndRescore startPos=" + startPos + " endPos=" + endPos + " bestStartIDX=" + bestStartIDX);
            }

            // First pass: walk backwards, building up the forward
            // arcs and pruning inadmissible arcs:
            for (int pos = endPos; pos > startPos; pos--)
            {
                Position posData = positions.Get(pos);
                if (VERBOSE)
                {
                    Console.WriteLine("    back pos=" + pos);
                }
                for (int arcIDX = 0; arcIDX < posData.count; arcIDX++)
                {
                    int backPos = posData.backPos[arcIDX];
                    if (backPos >= startPos)
                    {
                        // Keep this arc:
                        //System.out.println("      keep backPos=" + backPos);
                        positions.Get(backPos).AddForward(pos,
                                                          arcIDX,
                                                          posData.backID[arcIDX],
                                                          posData.backType[arcIDX]);
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("      prune");
                        }
                    }
                }
                if (pos != startPos)
                {
                    posData.count = 0;
                }
            }

            // Second pass: walk forward, re-scoring:
            for (int pos = startPos; pos < endPos; pos++)
            {
                Position posData = positions.Get(pos);
                if (VERBOSE)
                {
                    Console.WriteLine("    forward pos=" + pos + " count=" + posData.forwardCount);
                }
                if (posData.count == 0)
                {
                    // No arcs arrive here...
                    if (VERBOSE)
                    {
                        Console.WriteLine("      skip");
                    }
                    posData.forwardCount = 0;
                    continue;
                }

                if (pos == startPos)
                {
                    // On the initial position, only consider the best
                    // path so we "force congruence":  the
                    // sub-segmentation is "in context" of what the best
                    // path (compound token) had matched:
                    int rightID;
                    if (startPos == 0)
                    {
                        rightID = 0;
                    }
                    else
                    {
                        rightID = GetDict(posData.backType[bestStartIDX]).GetRightId(posData.backID[bestStartIDX]);
                    }
                    int pathCost = posData.costs[bestStartIDX];
                    for (int forwardArcIDX = 0; forwardArcIDX < posData.forwardCount; forwardArcIDX++)
                    {
                        JapaneseTokenizerType forwardType = posData.forwardType[forwardArcIDX];
                        IDictionary dict2 = GetDict(forwardType);
                        int wordID = posData.forwardID[forwardArcIDX];
                        int toPos = posData.forwardPos[forwardArcIDX];
                        int newCost = pathCost + dict2.GetWordCost(wordID) +
                          costs.Get(rightID, dict2.GetLeftId(wordID)) +
                          ComputePenalty(pos, toPos - pos);
                        if (VERBOSE)
                        {
                            Console.WriteLine("      + " + forwardType + " word " + new string(buffer.Get(pos, toPos - pos)) + " toPos=" + toPos + " cost=" + newCost + " penalty=" + ComputePenalty(pos, toPos - pos) + " toPos.idx=" + positions.Get(toPos).count);
                        }
                        positions.Get(toPos).Add(newCost,
                                                 dict2.GetRightId(wordID),
                                                 pos,
                                                 bestStartIDX,
                                                 wordID,
                                                 forwardType);
                    }
                }
                else
                {
                    // On non-initial positions, we maximize score
                    // across all arriving lastRightIDs:
                    for (int forwardArcIDX = 0; forwardArcIDX < posData.forwardCount; forwardArcIDX++)
                    {
                        JapaneseTokenizerType forwardType = posData.forwardType[forwardArcIDX];
                        int toPos = posData.forwardPos[forwardArcIDX];
                        if (VERBOSE)
                        {
                            Console.WriteLine("      + " + forwardType + " word " + new string(buffer.Get(pos, toPos - pos)) + " toPos=" + toPos);
                        }
                        Add(GetDict(forwardType),
                            posData,
                            toPos,
                            posData.forwardID[forwardArcIDX],
                            forwardType,
                            true);
                    }
                }
                posData.forwardCount = 0;
            }
        }

        // Backtrace from the provided position, back to the last
        // time we back-traced, accumulating the resulting tokens to
        // the pending list.  The pending list is then in-reverse
        // (last token should be returned first).
        private void Backtrace(Position endPosData, int fromIDX)
        {
            int endPos = endPosData.pos;

            /*
             * LUCENE-10059: If the endPos is the same as lastBackTracePos, we don't want to backtrace to
             * avoid an assertion error {@link RollingCharBuffer#get(int)} when it tries to generate an
             * empty buffer
             */
            if (endPos == lastBackTracePos)
            {
                return;
            }

            if (VERBOSE)
            {
                Console.WriteLine("\n  backtrace: endPos=" + endPos + " pos=" + this.pos + "; " + (this.pos - lastBackTracePos) + " characters; last=" + lastBackTracePos + " cost=" + endPosData.costs[fromIDX]);
            }

            char[] fragment = buffer.Get(lastBackTracePos, endPos - lastBackTracePos);

            if (dotOut != null)
            {
                dotOut.OnBacktrace(this, positions, lastBackTracePos, endPosData, fromIDX, fragment, end);
            }

            int pos = endPos;
            int bestIDX = fromIDX;
            Token altToken = null;

            // We trace backwards, so this will be the leftWordID of
            // the token after the one we are now on:
            int lastLeftWordID = -1;

            int backCount = 0;

            // TODO: sort of silly to make Token instances here; the
            // back trace has all info needed to generate the
            // token.  So, we could just directly set the attrs,
            // from the backtrace, in incrementToken w/o ever
            // creating Token; we'd have to defer calling freeBefore
            // until after the backtrace was fully "consumed" by
            // incrementToken.

            while (pos > lastBackTracePos)
            {
                //System.out.println("BT: back pos=" + pos + " bestIDX=" + bestIDX);
                Position posData = positions.Get(pos);
                if (Debugging.AssertsEnabled) Debugging.Assert(bestIDX < posData.count);

                int backPos = posData.backPos[bestIDX];
                if (Debugging.AssertsEnabled) Debugging.Assert(backPos >= lastBackTracePos,"backPos={0} vs lastBackTracePos={1}", backPos, lastBackTracePos);
                int length = pos - backPos;
                JapaneseTokenizerType backType = posData.backType[bestIDX];
                int backID = posData.backID[bestIDX];
                int nextBestIDX = posData.backIndex[bestIDX];

                if (outputCompounds && searchMode && altToken is null && backType != JapaneseTokenizerType.USER)
                {

                    // In searchMode, if best path had picked a too-long
                    // token, we use the "penalty" to compute the allowed
                    // max cost of an alternate back-trace.  If we find an
                    // alternate back trace with cost below that
                    // threshold, we pursue it instead (but also output
                    // the long token).
                    //System.out.println("    2nd best backPos=" + backPos + " pos=" + pos);

                    int penalty = ComputeSecondBestThreshold(backPos, pos - backPos);

                    if (penalty > 0)
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("  compound=" + new string(buffer.Get(backPos, pos - backPos)) + " backPos=" + backPos + " pos=" + pos + " penalty=" + penalty + " cost=" + posData.costs[bestIDX] + " bestIDX=" + bestIDX + " lastLeftID=" + lastLeftWordID);
                        }

                        // Use the penalty to set maxCost on the 2nd best
                        // segmentation:
                        int maxCost = posData.costs[bestIDX] + penalty;
                        if (lastLeftWordID != -1)
                        {
                            maxCost += costs.Get(GetDict(backType).GetRightId(backID), lastLeftWordID);
                        }

                        // Now, prune all too-long tokens from the graph:
                        PruneAndRescore(backPos, pos,
                                        posData.backIndex[bestIDX]);

                        // Finally, find 2nd best back-trace and resume
                        // backtrace there:
                        int leastCost = int.MaxValue;
                        int leastIDX = -1;
                        for (int idx = 0; idx < posData.count; idx++)
                        {
                            int cost = posData.costs[idx];
                            //System.out.println("    idx=" + idx + " prevCost=" + cost);

                            if (lastLeftWordID != -1)
                            {
                                cost += costs.Get(GetDict(posData.backType[idx]).GetRightId(posData.backID[idx]),
                                                  lastLeftWordID);
                                //System.out.println("      += bgCost=" + costs.get(getDict(posData.backType[idx]).getRightId(posData.backID[idx]),
                                //lastLeftWordID) + " -> " + cost);
                            }
                            //System.out.println("penalty " + posData.backPos[idx] + " to " + pos);
                            //cost += computePenalty(posData.backPos[idx], pos - posData.backPos[idx]);
                            if (cost < leastCost)
                            {
                                //System.out.println("      ** ");
                                leastCost = cost;
                                leastIDX = idx;
                            }
                        }
                        //System.out.println("  leastIDX=" + leastIDX);

                        if (VERBOSE)
                        {
                            Console.WriteLine("  afterPrune: " + posData.count + " arcs arriving; leastCost=" + leastCost + " vs threshold=" + maxCost + " lastLeftWordID=" + lastLeftWordID);
                        }

                        if (leastIDX != -1 && leastCost <= maxCost && posData.backPos[leastIDX] != backPos)
                        {
                            // We should have pruned the altToken from the graph:
                            if (Debugging.AssertsEnabled) Debugging.Assert(posData.backPos[leastIDX] != backPos);

                            // Save the current compound token, to output when
                            // this alternate path joins back:
                            altToken = new Token(backID,
                                                 fragment,
                                                 backPos - lastBackTracePos,
                                                 length,
                                                 backType,
                                                 backPos,
                                                 GetDict(backType));

                            // Redirect our backtrace to 2nd best:
                            bestIDX = leastIDX;
                            nextBestIDX = posData.backIndex[bestIDX];

                            backPos = posData.backPos[bestIDX];
                            length = pos - backPos;
                            backType = posData.backType[bestIDX];
                            backID = posData.backID[bestIDX];
                            backCount = 0;
                            //System.out.println("  do alt token!");

                        }
                        else
                        {
                            // I think in theory it's possible there is no
                            // 2nd best path, which is fine; in this case we
                            // only output the compound token:
                            //System.out.println("  no alt token! bestIDX=" + bestIDX);
                        }
                    }
                }

                int offset = backPos - lastBackTracePos;
                if (Debugging.AssertsEnabled) Debugging.Assert(offset >= 0);

                if (altToken != null && altToken.Position >= backPos)
                {

                    // We've backtraced to the position where the
                    // compound token starts; add it now:

                    // The pruning we did when we created the altToken
                    // ensures that the back trace will align back with
                    // the start of the altToken:
                    if (Debugging.AssertsEnabled) Debugging.Assert(altToken.Position == backPos, "{0} vs {1}", altToken.Position, backPos);

                    // NOTE: not quite right: the compound token may
                    // have had all punctuation back traced so far, but
                    // then the decompounded token at this position is
                    // not punctuation.  In this case backCount is 0,
                    // but we should maybe add the altToken anyway...?

                    if (backCount > 0)
                    {
                        backCount++;
                        altToken.PositionLength = backCount;
                        if (VERBOSE)
                        {
                            Console.WriteLine("    add altToken=" + altToken);
                        }
                        pending.Add(altToken);
                    }
                    else
                    {
                        // This means alt token was all punct tokens:
                        if (VERBOSE)
                        {
                            Console.WriteLine("    discard all-punctuation altToken=" + altToken);
                        }
                        if (Debugging.AssertsEnabled) Debugging.Assert(discardPunctuation);
                    }
                    altToken = null;
                }

                IDictionary dict = GetDict(backType);

                if (backType == JapaneseTokenizerType.USER)
                {

                    // Expand the phraseID we recorded into the actual
                    // segmentation:
                    int[] wordIDAndLength = userDictionary.LookupSegmentation(backID);
                    int wordID = wordIDAndLength[0];
                    int current = 0;
                    for (int j = 1; j < wordIDAndLength.Length; j++)
                    {
                        int len = wordIDAndLength[j];
                        //System.out.println("    add user: len=" + len);
                        pending.Add(new Token(wordID + j - 1,
                                              fragment,
                                              current + offset,
                                              len,
                                              JapaneseTokenizerType.USER,
                                              current + backPos,
                                              dict));
                        if (VERBOSE)
                        {
                            Console.WriteLine("    add USER token=" + pending[pending.Count - 1]);
                        }
                        current += len;
                    }

                    // Reverse the tokens we just added, because when we
                    // serve them up from incrementToken we serve in
                    // reverse:
                    pending.Reverse(pending.Count - (wordIDAndLength.Length - 1),
                        wordIDAndLength.Length - 1); // LUCENENET: Converted from reverse on SubList to reverse on List<T> and converted end index to length


                    backCount += wordIDAndLength.Length - 1;
                }
                else
                {

                    if (extendedMode && backType == JapaneseTokenizerType.UNKNOWN)
                    {
                        // In EXTENDED mode we convert unknown word into
                        // unigrams:
                        int unigramTokenCount = 0;
                        for (int i = length - 1; i >= 0; i--)
                        {
                            int charLen = 1;
                            if (i > 0 && char.IsLowSurrogate(fragment[offset + i]))
                            {
                                i--;
                                charLen = 2;
                            }
                            //System.out.println("    extended tok offset="
                            //+ (offset + i));
                            if (!discardPunctuation || !IsPunctuation(fragment[offset + i]))
                            {
                                pending.Add(new Token(CharacterDefinition.NGRAM,
                                                      fragment,
                                                      offset + i,
                                                      charLen,
                                                      JapaneseTokenizerType.UNKNOWN,
                                                      backPos + i,
                                                      unkDictionary));
                                unigramTokenCount++;
                            }
                        }
                        backCount += unigramTokenCount;

                    }
                    else if (!discardPunctuation || length == 0 || !IsPunctuation(fragment[offset]))
                    {
                        pending.Add(new Token(backID,
                                              fragment,
                                              offset,
                                              length,
                                              backType,
                                              backPos,
                                              dict));
                        if (VERBOSE)
                        {
                            Console.WriteLine("    add token=" + pending[pending.Count - 1]);
                        }
                        backCount++;
                    }
                    else
                    {
                        if (VERBOSE)
                        {
                            Console.WriteLine("    skip punctuation token=" + new string(fragment, offset, length));
                        }
                    }
                }

                lastLeftWordID = dict.GetLeftId(backID);
                pos = backPos;
                bestIDX = nextBestIDX;
            }

            lastBackTracePos = endPos;

            if (VERBOSE)
            {
                Console.WriteLine("  freeBefore pos=" + endPos);
            }
            // Notify the circular buffers that we are done with
            // these positions:
            buffer.FreeBefore(endPos);
            positions.FreeBefore(endPos);
        }

        internal IDictionary GetDict(JapaneseTokenizerType type)
        {
            dictionaryMap.TryGetValue(type, out IDictionary result);
            return result;
        }

        private static bool IsPunctuation(char ch)
        {
            switch (Character.GetType(ch))
            {
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.Control:
                case UnicodeCategory.Format:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.OtherPunctuation:
                case UnicodeCategory.MathSymbol:
                case UnicodeCategory.CurrencySymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.OtherSymbol:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                    return true;
                default:
                    return false;
            }
        }
    }

    // LUCENENET specific - de-nested Mode and renamed JapaneseTokenizerMode

    /// <summary>
    /// Tokenization mode: this determines how the tokenizer handles
    /// compound and unknown words.
    /// </summary>
    public enum JapaneseTokenizerMode
    {
        /// <summary>
        /// Ordinary segmentation: no decomposition for compounds,
        /// </summary>
        NORMAL,

        /// <summary>
        /// Segmentation geared towards search: this includes a
        /// decompounding process for long nouns, also including
        /// the full compound token as a synonym.
        /// </summary>
        SEARCH,

        /// <summary>
        /// Extended mode outputs unigrams for unknown words.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        EXTENDED
    }

    // LUCENENET specific: de-nested Type and renamed JapaneseTokenizerType

    /// <summary>
    /// Token type reflecting the original source of this token
    /// </summary>
    public enum JapaneseTokenizerType
    {
        /// <summary>
        /// Known words from the system dictionary.
        /// </summary>
        KNOWN,
        /// <summary>
        /// Unknown words (heuristically segmented).
        /// </summary>
        UNKNOWN,
        /// <summary>
        /// Known words from the user dictionary.
        /// </summary>
        USER
    }


    // LUCENENET specific - De-nested Position

    // Holds all back pointers arriving to this position:
    internal sealed class Position
    {

        internal int pos;

        internal int count;

        // maybe single int array * 5?
        internal int[] costs = new int[8];
        internal int[] lastRightID = new int[8];
        internal int[] backPos = new int[8];
        internal int[] backIndex = new int[8];
        internal int[] backID = new int[8];
        internal JapaneseTokenizerType[] backType = new JapaneseTokenizerType[8];

        // Only used when finding 2nd best segmentation under a
        // too-long token:
        internal int forwardCount;
        internal int[] forwardPos = new int[8];
        internal int[] forwardID = new int[8];
        internal int[] forwardIndex = new int[8];
        internal JapaneseTokenizerType[] forwardType = new JapaneseTokenizerType[8];

        public void Grow()
        {
            costs = ArrayUtil.Grow(costs, 1 + count);
            lastRightID = ArrayUtil.Grow(lastRightID, 1 + count);
            backPos = ArrayUtil.Grow(backPos, 1 + count);
            backIndex = ArrayUtil.Grow(backIndex, 1 + count);
            backID = ArrayUtil.Grow(backID, 1 + count);

            // NOTE: sneaky: grow separately because
            // ArrayUtil.grow will otherwise pick a different
            // length than the int[]s we just grew:
            JapaneseTokenizerType[] newBackType = new JapaneseTokenizerType[backID.Length];
            Arrays.Copy(backType, 0, newBackType, 0, backType.Length);
            backType = newBackType;
        }

        public void GrowForward()
        {
            forwardPos = ArrayUtil.Grow(forwardPos, 1 + forwardCount);
            forwardID = ArrayUtil.Grow(forwardID, 1 + forwardCount);
            forwardIndex = ArrayUtil.Grow(forwardIndex, 1 + forwardCount);

            // NOTE: sneaky: grow separately because
            // ArrayUtil.grow will otherwise pick a different
            // length than the int[]s we just grew:
            JapaneseTokenizerType[] newForwardType = new JapaneseTokenizerType[forwardPos.Length];
            Arrays.Copy(forwardType, 0, newForwardType, 0, forwardType.Length);
            forwardType = newForwardType;
        }

        public void Add(int cost, int lastRightID, int backPos, int backIndex, int backID, JapaneseTokenizerType backType)
        {
            // NOTE: this isn't quite a true Viterbi search,
            // because we should check if lastRightID is
            // already present here, and only update if the new
            // cost is less than the current cost, instead of
            // simply appending.  However, that will likely hurt
            // performance (usually we add a lastRightID only once),
            // and it means we actually create the full graph
            // intersection instead of a "normal" Viterbi lattice:
            if (count == costs.Length)
            {
                Grow();
            }
            this.costs[count] = cost;
            this.lastRightID[count] = lastRightID;
            this.backPos[count] = backPos;
            this.backIndex[count] = backIndex;
            this.backID[count] = backID;
            this.backType[count] = backType;
            count++;
        }

        public void AddForward(int forwardPos, int forwardIndex, int forwardID, JapaneseTokenizerType forwardType)
        {
            if (forwardCount == this.forwardID.Length)
            {
                GrowForward();
            }
            this.forwardPos[forwardCount] = forwardPos;
            this.forwardIndex[forwardCount] = forwardIndex;
            this.forwardID[forwardCount] = forwardID;
            this.forwardType[forwardCount] = forwardType;
            forwardCount++;
        }

        public void Reset()
        {
            count = 0;
            // forwardCount naturally resets after it runs:
            if (Debugging.AssertsEnabled) Debugging.Assert(forwardCount == 0,"pos={0} forwardCount={1}", pos, forwardCount);
        }
    }


    // LUCENENET specific - de-nested WrappedPositionArray

    // TODO: make generic'd version of this "circular array"?
    // It's a bit tricky because we do things to the Position
    // (eg, set .pos = N on reuse)...
    internal sealed class WrappedPositionArray
    {
        private Position[] positions = new Position[8];

        public WrappedPositionArray()
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = new Position();
            }
        }

        // Next array index to write to in positions:
        private int nextWrite;

        // Next position to write:
        private int nextPos;

        // How many valid Position instances are held in the
        // positions array:
        private int count;

        public void Reset()
        {
            nextWrite--;
            while (count > 0)
            {
                if (nextWrite == -1)
                {
                    nextWrite = positions.Length - 1;
                }
                positions[nextWrite--].Reset();
                count--;
            }
            nextWrite = 0;
            nextPos = 0;
            count = 0;
        }

        /// <summary>
        /// Get Position instance for this absolute position;
        /// this is allowed to be arbitrarily far "in the
        /// future" but cannot be before the last freeBefore.
        /// </summary>
        public Position Get(int pos)
        {
            while (pos >= nextPos)
            {
                //System.out.println("count=" + count + " vs len=" + positions.length);
                if (count == positions.Length)
                {
                    Position[] newPositions = new Position[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    //System.out.println("grow positions " + newPositions.length);
                    Arrays.Copy(positions, nextWrite, newPositions, 0, positions.Length - nextWrite);
                    Arrays.Copy(positions, 0, newPositions, positions.Length - nextWrite, nextWrite);
                    for (int i = positions.Length; i < newPositions.Length; i++)
                    {
                        newPositions[i] = new Position();
                    }
                    nextWrite = positions.Length;
                    positions = newPositions;
                }
                if (nextWrite == positions.Length)
                {
                    nextWrite = 0;
                }
                // Should have already been reset:
                if (Debugging.AssertsEnabled) Debugging.Assert(positions[nextWrite].count == 0);
                positions[nextWrite++].pos = nextPos++;
                count++;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(InBounds(pos));
            int index = GetIndex(pos);
            if (Debugging.AssertsEnabled) Debugging.Assert(positions[index].pos == pos);
            return positions[index];
        }

        public int GetNextPos()
        {
            return nextPos;
        }

        // For assert:
        private bool InBounds(int pos)
        {
            return pos < nextPos && pos >= nextPos - count;
        }

        private int GetIndex(int pos)
        {
            int index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                index += positions.Length;
            }
            return index;
        }

        public void FreeBefore(int pos)
        {
            int toFree = count - (nextPos - pos);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(toFree >= 0);
                Debugging.Assert(toFree <= count);
            }
            int index = nextWrite - count;
            if (index < 0)
            {
                index += positions.Length;
            }
            for (int i = 0; i < toFree; i++)
            {
                if (index == positions.Length)
                {
                    index = 0;
                }
                //System.out.println("  fb idx=" + index);
                positions[index].Reset();
                index++;
            }
            count -= toFree;
        }
    }
}
