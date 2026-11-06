using J2N;
using Lucene.Net.Analysis.Ko.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System.IO;
using Lucene.Net.Analysis.Ko.Dict;
using System.Collections.Generic;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Globalization;
using System.Threading;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko
{
    public sealed class KoreanTokenizer : Tokenizer
    {
        public enum KoreanTokenizerType
        {
            KNOWN,
            UNKNOWN,
            USER
        }

        public enum DecompoundMode {
            //No decomposition for compound.
            NONE,
            //Decompose compounds and discards the original form (default).
            DISCARD,
            //Decompose compounds and keeps the original form.
            MIXED
        }

        public static DecompoundMode DEFAULT_DECOMPOUND = DecompoundMode.DISCARD;

        private static bool VERBOSE = false;

        private static int MAX_UNKNOWN_WORD_LENGTH = 1024;
        private static int MAX_BACKTRACE_GAP = 1024;

        private readonly IDictionary<KoreanTokenizerType, IDictionary> dictionaryMap = new Dictionary<KoreanTokenizerType, IDictionary>();

        private TokenInfoFST fst;
        private TokenInfoDictionary dictionary;
        private UnknownDictionary unkDictionary;
        private ConnectionCosts costs;
        private UserDictionary userDictionary;
        private CharacterDefinition characterDefinition;

        private readonly FST.Arc<Int64> arc = new FST.Arc<Int64>();
        private readonly FST.BytesReader fstReader;
        private readonly Int32sRef wordIdRef = new Int32sRef();

        private FST.BytesReader userFSTReader;
        private TokenInfoFST userFST;

        private bool discardPunctuation;
        private DecompoundMode mode;
        private bool outputUnknownUnigrams;

        private RollingCharBuffer buffer = new RollingCharBuffer();

        private WrappedPositionArray positions = new WrappedPositionArray();

        // True once we've hit the EOF from the input reader:
        private bool end;

        // Last absolute position we backtraced from:
        private int lastBackTracePos;

        // Next absolute position to process:
        private int pos;

        private readonly JCG.List<Token> pending = new JCG.List<Token>();

        private readonly ICharTermAttribute termAtt;
        private readonly IOffsetAttribute offsetAtt;
        private readonly IPositionIncrementAttribute posIncAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IPartOfSpeechAttribute posAtt;
        private readonly IReadingAttribute readingAtt;

        public KoreanTokenizer
            (AttributeFactory factory, TextReader input, UserDictionary userDictionary,  DecompoundMode mode, bool outputUnknownUnigrams)
            : this (factory, input, userDictionary, mode, outputUnknownUnigrams, true) {}

        public KoreanTokenizer
            (AttributeFactory factory, TextReader input, UserDictionary userDictionary,  DecompoundMode mode, bool outputUnknownUnigrams, bool discardPunctuation)
            : base(factory, input)
        {
            this.termAtt = AddAttribute<ICharTermAttribute>();
            this.offsetAtt = AddAttribute<IOffsetAttribute>();
            this.posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            this.posLengthAtt = AddAttribute<IPositionLengthAttribute>();
            this.posAtt = AddAttribute<IPartOfSpeechAttribute>();
            this.readingAtt = AddAttribute<IReadingAttribute>();

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
            this.mode = mode;
            this.outputUnknownUnigrams = outputUnknownUnigrams;
            this.discardPunctuation = discardPunctuation;
            buffer.Reset(this.m_input);

            ResetState();

            dictionaryMap[KoreanTokenizerType.KNOWN] = dictionary;
            dictionaryMap[KoreanTokenizerType.UNKNOWN] = unkDictionary;
            dictionaryMap[KoreanTokenizerType.USER] = userDictionary;
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
            pos = 0;
            end = false;
            lastBackTracePos = 0;
            pending.Clear();

            // Add BOS:
            positions.Get(0).Add(0, 0, -1, -1, -1, -1,KoreanTokenizerType.KNOWN);
        }

        public override void End()
        {
            base.End();
            // Set final offset
            int finalOffset = CorrectOffset(pos);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        internal sealed class Position
        {

            internal int pos;

            internal int count;

            // maybe single int array * 5?
            internal int[] costs = new int[8];
            internal int[] lastRightID = new int[8];
            internal int[] backPos = new int[8];
            internal int[] backWordPos = new int[8];
            internal int[] backIndex = new int[8];
            internal int[] backID = new int[8];
            internal KoreanTokenizerType[] backType = new KoreanTokenizerType[8];

            public void Grow()
            {
                costs = ArrayUtil.Grow(costs, 1 + count);
                lastRightID = ArrayUtil.Grow(lastRightID, 1 + count);
                backPos = ArrayUtil.Grow(backPos, 1 + count);
                backWordPos = ArrayUtil.Grow(backWordPos, 1+count);
                backIndex = ArrayUtil.Grow(backIndex, 1 + count);
                backID = ArrayUtil.Grow(backID, 1 + count);

                // NOTE: sneaky: grow separately because
                // ArrayUtil.grow will otherwise pick a different
                // length than the int[]s we just grew:
                KoreanTokenizerType[] newBackType = new KoreanTokenizerType[backID.Length];
                System.Array.Copy(backType, 0, newBackType, 0, backType.Length);
                backType = newBackType;
            }

            public void Add(int cost, int lastRightID, int backPos, int backRPos, int backIndex, int backID, KoreanTokenizerType backType)
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
                this.backWordPos[count] = backRPos;
                this.backIndex[count] = backIndex;
                this.backID[count] = backID;
                this.backType[count] = backType;
                count++;
            }

            public void Reset()
            {
                count = 0;
            }
        }

        private int ComputeSpacePenalty(POS.Tag leftPOS, int numSpaces)
        {
            int spacePenalty = 0;
            if (numSpaces > 0)
            {
                switch (leftPOS.Name)
                {
                    case "E":
                    case "J":
                    case "VCP":
                    case "XSA":
                    case "XSN":
                    case "XSV":
                        spacePenalty = 3000;
                        break;
                    default:
                        break;
                }
            }

            return spacePenalty;
        }

        private void Add(IDictionary dict, Position fromPosData, int wordPos, int endPos, int wordID, KoreanTokenizerType type)
        {
            POS.Tag leftPOS = dict.GetLeftPOS(wordID);
            int wordCost = dict.GetWordCost(wordID);
            int leftID = dict.GetLeftId(wordID);
            int leastCost = int.MaxValue;
            int leastIDX = -1;
            if (Debugging.AssertsEnabled) Debugging.Assert(fromPosData.count > 0);
            for (int idx = 0; idx < fromPosData.count; idx++)
            {
                // The number of spaces before the term
                int numSpaces = wordPos - fromPosData.pos;

                // Cost is path cost so far, plus word cost (added at
                // end of loop), plus bigram cost:
                int cost = fromPosData.costs[idx] + costs.Get(fromPosData.lastRightID[idx], leftID);
                if (VERBOSE)
                {
                    Console.WriteLine("      fromIDX=" + idx + ": cost=" + cost + " (prevCost=" + fromPosData.costs[idx] + " wordCost=" + wordCost + " bgCost=" + costs.Get(fromPosData.lastRightID[idx], leftID) +
                                      " spacePenalty=" + ComputeSpacePenalty(leftPOS, numSpaces) + " leftID=" + leftID);
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

            positions.Get(endPos).Add(leastCost, dict.GetRightId(wordID), fromPosData.pos, wordPos, leastIDX, wordID, type);
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

            int length = token.GetLength;
            ClearAttributes();
            if (Debugging.AssertsEnabled) Debugging.Assert(length > 0);
            //System.out.println("off=" + token.getOffset() + " len=" + length + " vs " + token.SurfaceForm.length);
            termAtt.CopyBuffer(token.GetSurfaceForm, token.GetOffset, length);
            offsetAtt.SetOffset(CorrectOffset(token.GetStartOffset()), CorrectOffset(token.GetEndOffset() + length));
            posAtt.SetToken(token);
            readingAtt.SetToken(token);
            posIncAtt.PositionIncrement = token.GetPositionIncrement();
            posLengthAtt.PositionLength = token.GetPositionLength();

            if (VERBOSE)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ":    incToken: return token=" + token);
            }
            return true;
        }

        private void Parse()
        {
            if (VERBOSE)
            {
                Console.WriteLine("\nPARSE");
            }

            // Index of the last character of unknown word:
            int unknownWordEndIndex = -1;

            // Maximum posAhead of user word in the entire input
            int userWordMaxPosAhead = -1;

            // Advances over each position (character):
            while (buffer.Get(pos) != -1)
            {
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
                                posData2.backWordPos[0] = posData2.backWordPos[leastIDX];
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

                    if (pending.Count > 0)
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
                    int maxPosAhead = 0;
                    int outputMaxPosAhead = 0;
                    int arcFinalOutMaxPosAhead = 0;
                    for (int posAhead = posData.pos;; posAhead++)
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
                            maxPosAhead = posAhead;
                            outputMaxPosAhead = output;
                            arcFinalOutMaxPosAhead = (int)arc.NextFinalOutput;
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

                    for (int posAhead = posData.pos;; posAhead++)
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
                                Console.WriteLine(
                                    "    KNOWN word " + new string(buffer.Get(pos, posAhead - pos + 1)) + " toPos=" +
                                    (posAhead + 1) + " " + wordIdRef.Length + " wordIDs");
                            }

                            for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                            {
                                Add(
                                    dictionary,
                                    posData,
                                    posAhead,
                                    posAhead + 1,
                                    wordIdRef.Int32s[wordIdRef.Offset + ofs],
                                    KoreanTokenizerType.KNOWN);
                                anyMatches = true;
                            }
                        }
                    }
                }

                // In the case of normal mode, it doesn't process unknown word greedily.

                if (unknownWordEndIndex > posData.pos)
                {
                    pos++;
                    continue;
                }

                char firstCharacter = (char)buffer.Get(pos);
                if (!anyMatches || characterDefinition.IsInvoke(firstCharacter))
                {

                    // Find unknown match:
                    int characterId = characterDefinition.GetCharacterClass(firstCharacter);
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
                        java.lang.Character.UnicodeScript scriptCode = java.lang.Character.UnicodeScript.of(firstCharacter);
                        bool isPunct = IsPunctuation(firstCharacter);
                        bool isDigit = Character.IsDigit(firstCharacter);
                        for (int posAhead = pos + 1; unknownWordLength < MAX_UNKNOWN_WORD_LENGTH; posAhead++)
                        {
                            int next = buffer.Get(posAhead);
                            if (next == -1)
                            {
                                break;
                            }
                            char ch = (char) next;
                            UnicodeCategory chType = Character.GetType(ch);
                            java.lang.Character.UnicodeScript sc = java.lang.Character.UnicodeScript.of(next);
                            bool sameScript = IsSameScript(scriptCode, sc)
                                              // Non-spacing marks inherit the script of their base character,
                                              // following recommendations from UTR #24.
                                                || chType == UnicodeCategory.NonSpacingMark;
                            if (sameScript
                                // split on punctuation
                                && IsPunctuation(ch) == isPunct
                                // split on digit
                                && Character.IsDigit(ch) == isDigit
                                && characterDefinition.IsGroup(ch))
                            {
                                unknownWordLength++;
                            } else {
                                break;
                            }
                            // Update the script code and character class if the original script
                            // is Inherited or Common.
                            if (IsCommonOrInherited(scriptCode) && IsCommonOrInherited(sc) == false) {
                                scriptCode = sc;
                                characterId = characterDefinition.GetCharacterClass(ch);
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

                    unkDictionary.LookupWordIds(
                        characterId,
                        wordIdRef); // characters in input text are supposed to be the same
                    if (VERBOSE)
                    {
                        Console.WriteLine(
                            "    UNKNOWN word len=" + unknownWordLength + " " + wordIdRef.Length + " wordIDs");
                    }

                    for (int ofs = 0; ofs < wordIdRef.Length; ofs++)
                    {
                        Add(
                            unkDictionary,
                            posData,
                            pos,
                            pos + unknownWordLength,
                            wordIdRef.Int32s[wordIdRef.Offset + ofs],
                            KoreanTokenizerType.UNKNOWN);
                    }
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

        private void Backtrace(Position endPosData, int fromIDX)
        {
            int endPos = endPosData.pos;

            if (VERBOSE)
            {
                Console.WriteLine(
                    "\n  backtrace: endPos=" + endPos + " pos=" + this.pos + "; " + (this.pos - lastBackTracePos) +
                    " characters; last=" + lastBackTracePos + " cost=" + endPosData.costs[fromIDX]);
            }

            char[] fragment = buffer.Get(lastBackTracePos, endPos - lastBackTracePos);

            if (dotOut != null)
            {
                dotOut.OnBacktrace(this, positions, lastBackTracePos, endPosData, fromIDX, fragment, end);
            }

            int pos = endPos;
            int bestIDX = fromIDX;

            while (pos > lastBackTracePos)
            {
                //System.out.println("BT: back pos=" + pos + " bestIDX=" + bestIDX);
                Position posData = positions.Get(pos);
                if (Debugging.AssertsEnabled) Debugging.Assert(bestIDX < posData.count);

                int backPos = posData.backPos[bestIDX];
                int backWordPos = posData.backWordPos[bestIDX];
                if (Debugging.AssertsEnabled)
                    Debugging.Assert(
                        backPos >= lastBackTracePos,
                        "backPos={0} vs lastBackTracePos={1}",
                        backPos,
                        lastBackTracePos);
                int length = pos - backPos;
                KoreanTokenizerType backType = posData.backType[bestIDX];
                int backID = posData.backID[bestIDX];
                int nextBestIDX = posData.backIndex[bestIDX];
                int fragmentOffset = backWordPos - lastBackTracePos;
                if (Debugging.AssertsEnabled)
                    Debugging.Assert(fragmentOffset > 0, "fragmentOffset={0}", fragmentOffset);

                IDictionary dict = GetDict(backType);
                if (outputUnknownUnigrams && backType == KoreanTokenizerType.UNKNOWN)
                {
                    // outputUnknownUnigrams converts unknown word into unigrams:
                    for (int i = length; i >= 0; i--)
                    {
                        int charLen = 1;
                        char surrogate = fragment[fragmentOffset];
                        if (i > 0 && (surrogate >= Character.MinLowSurrogate &&
                                      surrogate < (Character.MaxLowSurrogate + 1)))
                        {
                            i--;
                            charLen = 2;
                        }

                        DictionaryToken token = new DictionaryToken(
                            KoreanTokenizerType.UNKNOWN,
                            unkDictionary,
                            CharacterDefinition.NGRAM,
                            fragment,
                            fragmentOffset + i,
                            charLen,
                            backWordPos + i,
                            backWordPos + i + charLen
                        );
                        pending.Add(token);
                        if (VERBOSE)
                        {
                            Console.WriteLine("    add token=" + pending[-1]);
                        }

                    }
                }
                else
                {
                    DictionaryToken token = new DictionaryToken(
                        backType,
                        dict,
                        backID,
                        fragment,
                        fragmentOffset,
                        length,
                        backWordPos,
                        backWordPos + length
                    );
                    if (token.GetPOSType() == POS.Type.MORPHEME || mode == DecompoundMode.NONE)
                    {
                        if (ShouldFilterToken(token) == false)
                        {
                            pending.Add(token);
                            if (VERBOSE)
                            {
                                Console.WriteLine("    add token=" + pending[-1]);
                            }
                        }
                    }
                    else
                    {
                        IDictionary.Morpheme[] morphemes = token.GetMorphemes();
                        if (morphemes == null)
                        {
                            pending.Add(token);
                            if (VERBOSE)
                            {
                                Console.WriteLine("    add token=" + pending[-1]);
                            }
                        }
                        else
                        {
                            int endOffset = backWordPos + length;
                            int posLen = 0;
                            // decompose the compound
                            for (int i = morphemes.Length - 1; i >= 0; i--)
                            {
                                IDictionary.Morpheme morpheme = morphemes[i];
                                Token compoundToken;
                                if (token.GetPOSType() == POS.Type.COMPOUND)
                                {
                                    if (Debugging.AssertsEnabled)
                                    {
                                        Debugging.Assert(endOffset - morpheme.surfaceForm.Length >= 0);
                                    }
                                    compoundToken = new DecompoundToken(
                                        morpheme.posTag,
                                        morpheme.surfaceForm,
                                        endOffset - morpheme.surfaceForm.Length,
                                        endOffset);
                                }
                                else
                                {
                                    compoundToken = new DecompoundToken(
                                        morpheme.posTag,
                                        morpheme.surfaceForm,
                                        token.GetStartOffset(),
                                        token.GetEndOffset());
                                }

                                if (i == 0 && mode == DecompoundMode.MIXED)
                                {
                                    compoundToken.SetPositionIncrement(0);
                                }

                                ++posLen;
                                endOffset -= morpheme.surfaceForm.Length;
                                pending.Add(compoundToken);
                                if (VERBOSE)
                                {
                                    Console.WriteLine("    add token=" + pending[-1]);
                                }
                            }

                            if (mode == DecompoundMode.MIXED)
                            {
                                token.SetPositionLength(Math.Max(1, posLen));
                                pending.Add(token);
                                if (VERBOSE)
                                {
                                    Console.WriteLine("    add token=" + pending[-1]);
                                }
                            }
                        }
                    }
                }

                if (discardPunctuation == false && backWordPos != backPos)
                {
                    // Add a token for whitespaces between terms
                    int offset = backPos - lastBackTracePos;
                    int len = backWordPos - backPos;
                    //System.out.println(offset + " " + fragmentOffset + " " + len + " " + backWordPos + " " + backPos);
                    unkDictionary.LookupWordIds(characterDefinition.GetCharacterClass(' '), wordIdRef);
                    DictionaryToken spaceToken = new DictionaryToken(
                        KoreanTokenizerType.UNKNOWN,
                        unkDictionary,
                        wordIdRef.Int32s[wordIdRef.Offset],
                        fragment,
                        offset,
                        len,
                        backPos,
                        backPos + len);
                    pending.Add(spaceToken);
                }

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

        internal IDictionary GetDict(KoreanTokenizerType type) {
            dictionaryMap.TryGetValue(type, out IDictionary result);
            return result;
        }

        private bool ShouldFilterToken(Token token)
        {
            return discardPunctuation && IsPunctuation(token.GetSurfaceForm[token.GetOffset]);
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

        private static bool IsCommonOrInherited(java.lang.Character.UnicodeScript script) {
            return script == java.lang.Character.UnicodeScript.INHERITED ||
                   script == java.lang.Character.UnicodeScript.COMMON;
        }

        private static bool IsSameScript(java.lang.Character.UnicodeScript scriptOne, java.lang.Character.UnicodeScript scriptTwo) {
            return scriptOne == scriptTwo
                   || IsCommonOrInherited(scriptOne)
                   || IsCommonOrInherited(scriptTwo);
        }


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

                    positions[nextWrite--].
                        Reset();
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
                        Position[] newPositions = new Position[ArrayUtil.Oversize(
                            1 + count,
                            RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                        //System.out.println("grow positions " + newPositions.length);
                        System.Array.Copy(positions, nextWrite, newPositions, 0, positions.Length - nextWrite);
                        System.Array.Copy(positions, 0, newPositions, positions.Length - nextWrite, nextWrite);
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
                    if (Debugging.AssertsEnabled)
                        Debugging.Assert(
                            positions[nextWrite].
                                count == 0);
                    positions[nextWrite++].
                        pos = nextPos++;
                    count++;
                }

                if (Debugging.AssertsEnabled) Debugging.Assert(InBounds(pos));
                int index = GetIndex(pos);
                if (Debugging.AssertsEnabled)
                    Debugging.Assert(
                        positions[index].
                            pos == pos);
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
                    positions[index].
                        Reset();
                    index++;
                }

                count -= toFree;
            }
        }
    }
}