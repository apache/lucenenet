// Lucene version compatibility level 4.8.1
using J2N;
using J2N.Numerics;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Globalization;

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
    /// Matches single or multi word synonyms in a token stream.
    /// This token stream cannot properly handle position
    /// increments != 1, ie, you should place this filter before
    /// filtering out stop words.
    /// 
    /// <para>Note that with the current implementation, parsing is
    /// greedy, so whenever multiple parses would apply, the rule
    /// starting the earliest and parsing the most tokens wins.
    /// For example if you have these rules:
    ///      
    /// <code>
    ///   a -> x
    ///   a b -> y
    ///   b c d -> z
    /// </code>
    /// 
    /// Then input <c>a b c d e</c> parses to <c>y b c
    /// d</c>, ie the 2nd rule "wins" because it started
    /// earliest and matched the most input tokens of other rules
    /// starting at that point.</para>
    /// 
    /// <para>A future improvement to this filter could allow
    /// non-greedy parsing, such that the 3rd rule would win, and
    /// also separately allow multiple parses, such that all 3
    /// rules would match, perhaps even on a rule by rule
    /// basis.</para>
    /// 
    /// <para><b>NOTE</b>: when a match occurs, the output tokens
    /// associated with the matching rule are "stacked" on top of
    /// the input stream (if the rule had
    /// <c>keepOrig=true</c>) and also on top of another
    /// matched rule's output tokens.  This is not a correct
    /// solution, as really the output should be an arbitrary
    /// graph/lattice.  For example, with the above match, you
    /// would expect an exact <see cref="Search.PhraseQuery"/> <c>"y b
    /// c"</c> to match the parsed tokens, but it will fail to
    /// do so.  This limitation is necessary because Lucene's
    /// <see cref="TokenStream"/> (and index) cannot yet represent an arbitrary
    /// graph.</para>
    /// 
    /// <para><b>NOTE</b>: If multiple incoming tokens arrive on the
    /// same position, only the first token at that position is
    /// used for parsing.  Subsequent tokens simply pass through
    /// and are not parsed.  A future improvement would be to
    /// allow these tokens to also be matched.</para>
    /// </summary>

    // TODO: maybe we should resolve token -> wordID then run
    // FST on wordIDs, for better perf?

    // TODO: a more efficient approach would be Aho/Corasick's
    // algorithm
    // http://en.wikipedia.org/wiki/Aho%E2%80%93Corasick_string_matching_algorithm
    // It improves over the current approach here
    // because it does not fully re-start matching at every
    // token.  For example if one pattern is "a b c x"
    // and another is "b c d" and the input is "a b c d", on
    // trying to parse "a b c x" but failing when you got to x,
    // rather than starting over again your really should
    // immediately recognize that "b c d" matches at the next
    // input.  I suspect this won't matter that much in
    // practice, but it's possible on some set of synonyms it
    // will.  We'd have to modify Aho/Corasick to enforce our
    // conflict resolving (eg greedy matching) because that algo
    // finds all matches.  This really amounts to adding a .*
    // closure to the FST and then determinizing it.

    public sealed class SynonymFilter : TokenFilter
    {
        public const string TYPE_SYNONYM = "SYNONYM";

        private readonly SynonymMap synonyms;

        private readonly bool ignoreCase;
        private readonly int rollBufferSize;

        private int captureCount;

        // TODO: we should set PositionLengthAttr too...

        private readonly ICharTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPositionLengthAttribute posLenAtt;
        private readonly ITypeAttribute typeAtt;
        private readonly IOffsetAttribute offsetAtt;


        // How many future input tokens have already been matched
        // to a synonym; because the matching is "greedy" we don't
        // try to do any more matching for such tokens:
        private int inputSkipCount;

        // Hold all buffered (read ahead) stacked input tokens for
        // a future position.  When multiple tokens are at the
        // same position, we only store (and match against) the
        // term for the first token at the position, but capture
        // state for (and enumerate) all other tokens at this
        // position:
        private class PendingInput
        {
            internal readonly CharsRef term = new CharsRef();
            internal AttributeSource.State state;
            internal bool keepOrig;
            internal bool matched;
            internal bool consumed = true;
            internal int startOffset;
            internal int endOffset;

            public void Reset()
            {
                state = null;
                consumed = true;
                keepOrig = false;
                matched = false;
            }
        }

        // Rolling buffer, holding pending input tokens we had to
        // clone because we needed to look ahead, indexed by
        // position:
        private readonly PendingInput[] futureInputs;

        // Holds pending output synonyms for one future position:
        private class PendingOutputs
        {
            internal CharsRef[] outputs;
            internal int[] endOffsets;
            internal int[] posLengths;
            internal int upto;
            internal int count;
            internal int posIncr = 1;
            internal int lastEndOffset;
            internal int lastPosLength;

            public PendingOutputs()
            {
                outputs = new CharsRef[1];
                endOffsets = new int[1];
                posLengths = new int[1];
            }

            public virtual void Reset()
            {
                upto = count = 0;
                posIncr = 1;
            }

            public virtual CharsRef PullNext()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(upto < count);
                lastEndOffset = endOffsets[upto];
                lastPosLength = posLengths[upto];
                CharsRef result = outputs[upto++];
                posIncr = 0;
                if (upto == count)
                {
                    Reset();
                }
                return result;
            }

            public virtual int LastEndOffset => lastEndOffset;

            public virtual int LastPosLength => lastPosLength;

            public virtual void Add(char[] output, int offset, int len, int endOffset, int posLength)
            {
                if (count == outputs.Length)
                {
                    CharsRef[] next = new CharsRef[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Arrays.Copy(outputs, 0, next, 0, count);
                    outputs = next;
                }
                if (count == endOffsets.Length)
                {
                    int[] next = new int[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_INT32)];
                    Arrays.Copy(endOffsets, 0, next, 0, count);
                    endOffsets = next;
                }
                if (count == posLengths.Length)
                {
                    int[] next = new int[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_INT32)];
                    Arrays.Copy(posLengths, 0, next, 0, count);
                    posLengths = next;
                }
                if (outputs[count] is null)
                {
                    outputs[count] = new CharsRef();
                }
                outputs[count].CopyChars(output, offset, len);
                // endOffset can be -1, in which case we should simply
                // use the endOffset of the input token, or X >= 0, in
                // which case we use X as the endOffset for this output
                endOffsets[count] = endOffset;
                posLengths[count] = posLength;
                count++;
            }
        }

        private readonly ByteArrayDataInput bytesReader = new ByteArrayDataInput();

        // Rolling buffer, holding stack of pending synonym
        // outputs, indexed by position:
        private readonly PendingOutputs[] futureOutputs;

        // Where (in rolling buffers) to write next input saved state:
        private int nextWrite;

        // Where (in rolling buffers) to read next input saved state:
        private int nextRead;

        // True once we've read last token
        private bool finished;

        private readonly FST.Arc<BytesRef> scratchArc;

        private readonly FST<BytesRef> fst;

        private readonly FST.BytesReader fstReader;


        private readonly BytesRef scratchBytes = new BytesRef();
        private readonly CharsRef scratchChars = new CharsRef();

        /// <param name="input"> input tokenstream </param>
        /// <param name="synonyms"> synonym map </param>
        /// <param name="ignoreCase"> case-folds input for matching with <see cref="Character.ToLower(int, CultureInfo)"/>
        ///                   in using <see cref="CultureInfo.InvariantCulture"/>.
        ///                   Note, if you set this to <c>true</c>, its your responsibility to lowercase
        ///                   the input entries when you create the <see cref="SynonymMap"/>.</param>
        public SynonymFilter(TokenStream input, SynonymMap synonyms, bool ignoreCase) 
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            posLenAtt = AddAttribute<IPositionLengthAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();

            this.synonyms = synonyms;
            this.ignoreCase = ignoreCase;
            this.fst = synonyms.Fst;
            if (fst is null)
            {
                throw new ArgumentNullException(nameof(synonyms.Fst), "fst must be non-null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            this.fstReader = fst.GetBytesReader();

            // Must be 1+ so that when roll buffer is at full
            // lookahead we can distinguish this full buffer from
            // the empty buffer:
            rollBufferSize = 1 + synonyms.MaxHorizontalContext;

            futureInputs = new PendingInput[rollBufferSize];
            futureOutputs = new PendingOutputs[rollBufferSize];
            for (int pos = 0; pos < rollBufferSize; pos++)
            {
                futureInputs[pos] = new PendingInput();
                futureOutputs[pos] = new PendingOutputs();
            }

            //System.out.println("FSTFilt maxH=" + synonyms.maxHorizontalContext);

            scratchArc = new FST.Arc<BytesRef>();
        }

        private void Capture()
        {
            captureCount++;
            //System.out.println("  capture slot=" + nextWrite);
            PendingInput input = futureInputs[nextWrite];

            input.state = CaptureState();
            input.consumed = false;
            input.term.CopyChars(termAtt.Buffer, 0, termAtt.Length);

            nextWrite = RollIncr(nextWrite);

            // Buffer head should never catch up to tail:
            if (Debugging.AssertsEnabled) Debugging.Assert(nextWrite != nextRead);
        }

        /*
         This is the core of this TokenFilter: it locates the
         synonym matches and buffers up the results into
         futureInputs/Outputs.

         NOTE: this calls input.incrementToken and does not
         capture the state if no further tokens were checked.  So
         caller must then forward state to our caller, or capture:
        */
        private int lastStartOffset;
        private int lastEndOffset;

        private void Parse()
        {
            //System.out.println("\nS: parse");

            if (Debugging.AssertsEnabled) Debugging.Assert(inputSkipCount == 0);

            int curNextRead = nextRead;

            // Holds the longest match we've seen so far:
            BytesRef matchOutput = null;
            int matchInputLength = 0;
            int matchEndOffset = -1;

            BytesRef pendingOutput = fst.Outputs.NoOutput;
            fst.GetFirstArc(scratchArc);

            if (Debugging.AssertsEnabled) Debugging.Assert(scratchArc.Output == fst.Outputs.NoOutput);

            int tokenCount = 0;

            while (true)
            {

                // Pull next token's chars:
                char[] buffer;
                int bufferLen;
                //System.out.println("  cycle nextRead=" + curNextRead + " nextWrite=" + nextWrite);

                int inputEndOffset = 0;

                if (curNextRead == nextWrite)
                {

                    // We used up our lookahead buffer of input tokens
                    // -- pull next real input token:

                    if (finished)
                    {
                        break;
                    }
                    else
                    {
                        //System.out.println("  input.incrToken");
                        if (Debugging.AssertsEnabled) Debugging.Assert(futureInputs[nextWrite].consumed);
                        // Not correct: a syn match whose output is longer
                        // than its input can set future inputs keepOrig
                        // to true:
                        //assert !futureInputs[nextWrite].keepOrig;
                        if (m_input.IncrementToken())
                        {
                            buffer = termAtt.Buffer;
                            bufferLen = termAtt.Length;
                            PendingInput pendingInput = futureInputs[nextWrite];
                            lastStartOffset = pendingInput.startOffset = offsetAtt.StartOffset;
                            lastEndOffset = pendingInput.endOffset = offsetAtt.EndOffset;
                            inputEndOffset = pendingInput.endOffset;
                            //System.out.println("  new token=" + new String(buffer, 0, bufferLen));
                            if (nextRead != nextWrite)
                            {
                                Capture();
                            }
                            else
                            {
                                pendingInput.consumed = false;
                            }

                        }
                        else
                        {
                            // No more input tokens
                            //System.out.println("      set end");
                            finished = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Still in our lookahead
                    buffer = futureInputs[curNextRead].term.Chars;
                    bufferLen = futureInputs[curNextRead].term.Length;
                    inputEndOffset = futureInputs[curNextRead].endOffset;
                    //System.out.println("  old token=" + new String(buffer, 0, bufferLen));
                }

                tokenCount++;

                // Run each char in this token through the FST:
                int bufUpto = 0;
                while (bufUpto < bufferLen)
                {
                    int codePoint = Character.CodePointAt(buffer, bufUpto, bufferLen);
                    if (fst.FindTargetArc(ignoreCase ? Character.ToLower(codePoint, CultureInfo.InvariantCulture) : codePoint, scratchArc, scratchArc, fstReader) is null)
                    {
                        //System.out.println("    stop");
                        goto byTokenBreak;
                    }

                    // Accum the output
                    pendingOutput = fst.Outputs.Add(pendingOutput, scratchArc.Output);
                    //System.out.println("    char=" + buffer[bufUpto] + " output=" + pendingOutput + " arc.output=" + scratchArc.output);
                    bufUpto += Character.CharCount(codePoint);
                }

                // OK, entire token matched; now see if this is a final
                // state:
                if (scratchArc.IsFinal)
                {
                    matchOutput = fst.Outputs.Add(pendingOutput, scratchArc.NextFinalOutput);
                    matchInputLength = tokenCount;
                    matchEndOffset = inputEndOffset;
                    //System.out.println("  found matchLength=" + matchInputLength + " output=" + matchOutput);
                }

                // See if the FST wants to continue matching (ie, needs to
                // see the next input token):
                if (fst.FindTargetArc(SynonymMap.WORD_SEPARATOR, scratchArc, scratchArc, fstReader) is null)
                {
                    // No further rules can match here; we're done
                    // searching for matching rules starting at the
                    // current input position.
                    break;
                }
                else
                {
                    // More matching is possible -- accum the output (if
                    // any) of the WORD_SEP arc:
                    pendingOutput = fst.Outputs.Add(pendingOutput, scratchArc.Output);
                    if (nextRead == nextWrite)
                    {
                        Capture();
                    }
                }

                curNextRead = RollIncr(curNextRead);
            }
            byTokenBreak:

            if (nextRead == nextWrite && !finished)
            {
                //System.out.println("  skip write slot=" + nextWrite);
                nextWrite = RollIncr(nextWrite);
            }

            if (matchOutput != null)
            {
                //System.out.println("  add matchLength=" + matchInputLength + " output=" + matchOutput);
                inputSkipCount = matchInputLength;
                AddOutput(matchOutput, matchInputLength, matchEndOffset);
            }
            else if (nextRead != nextWrite)
            {
                // Even though we had no match here, we set to 1
                // because we need to skip current input token before
                // trying to match again:
                inputSkipCount = 1;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(finished);
            }

            //System.out.println("  parse done inputSkipCount=" + inputSkipCount + " nextRead=" + nextRead + " nextWrite=" + nextWrite);
        }

        // Interleaves all output tokens onto the futureOutputs:
        private void AddOutput(BytesRef bytes, int matchInputLength, int matchEndOffset)
        {
            bytesReader.Reset(bytes.Bytes, bytes.Offset, bytes.Length);

            int code = bytesReader.ReadVInt32();
            bool keepOrig = (code & 0x1) == 0;
            int count = code.TripleShift(1);
            //System.out.println("  addOutput count=" + count + " keepOrig=" + keepOrig);
            for (int outputIDX = 0; outputIDX < count; outputIDX++)
            {
                synonyms.Words.Get(bytesReader.ReadVInt32(), scratchBytes);
                //System.out.println("    outIDX=" + outputIDX + " bytes=" + scratchBytes.length);
                UnicodeUtil.UTF8toUTF16(scratchBytes, scratchChars);
                int lastStart = scratchChars.Offset;
                int chEnd = lastStart + scratchChars.Length;
                int outputUpto = nextRead;
                for (int chIDX = lastStart; chIDX <= chEnd; chIDX++)
                {
                    if (chIDX == chEnd || scratchChars.Chars[chIDX] == SynonymMap.WORD_SEPARATOR)
                    {
                        int outputLen = chIDX - lastStart;
                        // Caller is not allowed to have empty string in
                        // the output:
                        if (Debugging.AssertsEnabled) Debugging.Assert(outputLen > 0, "output contains empty string: {0}", scratchChars);
                        int endOffset;
                        int posLen;
                        if (chIDX == chEnd && lastStart == scratchChars.Offset)
                        {
                            // This rule had a single output token, so, we set
                            // this output's endOffset to the current
                            // endOffset (ie, endOffset of the last input
                            // token it matched):
                            endOffset = matchEndOffset;
                            posLen = keepOrig ? matchInputLength : 1;
                        }
                        else
                        {
                            // This rule has more than one output token; we
                            // can't pick any particular endOffset for this
                            // case, so, we inherit the endOffset for the
                            // input token which this output overlaps:
                            endOffset = -1;
                            posLen = 1;
                        }
                        futureOutputs[outputUpto].Add(scratchChars.Chars, lastStart, outputLen, endOffset, posLen);
                        //System.out.println("      " + new String(scratchChars.chars, lastStart, outputLen) + " outputUpto=" + outputUpto);
                        lastStart = 1 + chIDX;
                        //System.out.println("  slot=" + outputUpto + " keepOrig=" + keepOrig);
                        outputUpto = RollIncr(outputUpto);
                        if (Debugging.AssertsEnabled) Debugging.Assert(futureOutputs[outputUpto].posIncr == 1, "outputUpto={0} vs nextWrite={1}", outputUpto, nextWrite);
                    }
                }
            }

            int upto = nextRead;
            for (int idx = 0; idx < matchInputLength; idx++)
            {
                futureInputs[upto].keepOrig |= keepOrig;
                futureInputs[upto].matched = true;
                upto = RollIncr(upto);
            }
        }

        // ++ mod rollBufferSize
        private int RollIncr(int count)
        {
            count++;
            if (count == rollBufferSize)
            {
                return 0;
            }
            else
            {
                return count;
            }
        }

        // for testing
        internal int CaptureCount => captureCount;

        public override bool IncrementToken()
        {

            //System.out.println("\nS: incrToken inputSkipCount=" + inputSkipCount + " nextRead=" + nextRead + " nextWrite=" + nextWrite);

            while (true)
            {

                // First play back any buffered future inputs/outputs
                // w/o running parsing again:
                while (inputSkipCount != 0)
                {

                    // At each position, we first output the original
                    // token

                    // TODO: maybe just a PendingState class, holding
                    // both input & outputs?
                    PendingInput input = futureInputs[nextRead];
                    PendingOutputs outputs = futureOutputs[nextRead];

                    //System.out.println("  cycle nextRead=" + nextRead + " nextWrite=" + nextWrite + " inputSkipCount="+ inputSkipCount + " input.keepOrig=" + input.keepOrig + " input.consumed=" + input.consumed + " input.state=" + input.state);

                    if (!input.consumed && (input.keepOrig || !input.matched))
                    {
                        if (input.state != null)
                        {
                            // Return a previously saved token (because we
                            // had to lookahead):
                            RestoreState(input.state);
                        }
                        else
                        {
                            // Pass-through case: return token we just pulled
                            // but didn't capture:
                            if (Debugging.AssertsEnabled) Debugging.Assert(inputSkipCount == 1, "inputSkipCount={0} nextRead={1}", inputSkipCount, nextRead);
                        }
                        input.Reset();
                        if (outputs.count > 0)
                        {
                            outputs.posIncr = 0;
                        }
                        else
                        {
                            nextRead = RollIncr(nextRead);
                            inputSkipCount--;
                        }
                        //System.out.println("  return token=" + termAtt.toString());
                        return true;
                    }
                    else if (outputs.upto < outputs.count)
                    {
                        // Still have pending outputs to replay at this
                        // position
                        input.Reset();
                        int posIncr = outputs.posIncr;
                        CharsRef output = outputs.PullNext();
                        ClearAttributes();
                        termAtt.CopyBuffer(output.Chars, output.Offset, output.Length);
                        typeAtt.Type = TYPE_SYNONYM;
                        int endOffset = outputs.LastEndOffset;
                        if (endOffset == -1)
                        {
                            endOffset = input.endOffset;
                        }
                        offsetAtt.SetOffset(input.startOffset, endOffset);
                        posIncrAtt.PositionIncrement = posIncr;
                        posLenAtt.PositionLength = outputs.LastPosLength;
                        if (outputs.count == 0)
                        {
                            // Done with the buffered input and all outputs at
                            // this position
                            nextRead = RollIncr(nextRead);
                            inputSkipCount--;
                        }
                        //System.out.println("  return token=" + termAtt.toString());
                        return true;
                    }
                    else
                    {
                        // Done with the buffered input and all outputs at
                        // this position
                        input.Reset();
                        nextRead = RollIncr(nextRead);
                        inputSkipCount--;
                    }
                }

                if (finished && nextRead == nextWrite)
                {
                    // End case: if any output syns went beyond end of
                    // input stream, enumerate them now:
                    PendingOutputs outputs = futureOutputs[nextRead];
                    if (outputs.upto < outputs.count)
                    {
                        int posIncr = outputs.posIncr;
                        CharsRef output = outputs.PullNext();
                        futureInputs[nextRead].Reset();
                        if (outputs.count == 0)
                        {
                            nextWrite = nextRead = RollIncr(nextRead);
                        }
                        ClearAttributes();
                        // Keep offset from last input token:
                        offsetAtt.SetOffset(lastStartOffset, lastEndOffset);
                        termAtt.CopyBuffer(output.Chars, output.Offset, output.Length);
                        typeAtt.Type = TYPE_SYNONYM;
                        //System.out.println("  set posIncr=" + outputs.posIncr + " outputs=" + outputs);
                        posIncrAtt.PositionIncrement = posIncr;
                        //System.out.println("  return token=" + termAtt.toString());
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                // Find new synonym matches:
                Parse();
            }
        }

        public override void Reset()
        {
            base.Reset();
            captureCount = 0;
            finished = false;
            inputSkipCount = 0;
            nextRead = nextWrite = 0;

            // In normal usage these resets would not be needed,
            // since they reset-as-they-are-consumed, but the app
            // may not consume all input tokens (or we might hit an
            // exception), in which case we have leftover state
            // here:
            foreach (PendingInput input in futureInputs)
            {
                input.Reset();
            }
            foreach (PendingOutputs output in futureOutputs)
            {
                output.Reset();
            }
        }
    }
}