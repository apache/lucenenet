using System;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Randomized;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;

namespace Lucene.Net.Analysis
{
    /**
     * Tokenizer for testing.
     * <p>
     * This tokenizer is a replacement for {@link #WHITESPACE}, {@link #SIMPLE}, and {@link #KEYWORD}
     * tokenizers. If you are writing a component such as a TokenFilter, its a great idea to test
     * it wrapping this tokenizer instead for extra checks. This tokenizer has the following behavior:
     * <ul>
     *   <li>An internal state-machine is used for checking consumer consistency. These checks can
     *       be disabled with {@link #setEnableChecks(boolean)}.
     *   <li>For convenience, optionally lowercases terms that it outputs.
     * </ul>
     */
    public class MockTokenizer : Tokenizer
    {
        /** Acts Similar to WhitespaceTokenizer */
        public static CharacterRunAutomaton WHITESPACE =
          new CharacterRunAutomaton(new RegExp("[^ \t\r\n]+").ToAutomaton());
        /** Acts Similar to KeywordTokenizer.
         * TODO: Keyword returns an "empty" token for an empty reader... 
         */
        public static CharacterRunAutomaton KEYWORD =
          new CharacterRunAutomaton(new RegExp(".*").ToAutomaton());
        /** Acts like LetterTokenizer. */
        // the ugly regex below is incomplete Unicode 5.2 [:Letter:]
        public static CharacterRunAutomaton SIMPLE =
          new CharacterRunAutomaton(new RegExp("[A-Za-zªµºÀ-ÖØ-öø-ˁ一-鿌]+").ToAutomaton());

        private CharacterRunAutomaton runAutomaton;
        private bool lowerCase;
        private int maxTokenLength;
        public static int DEFAULT_MAX_TOKEN_LENGTH = int.MaxValue;
        private int state;

        private readonly CharTermAttribute termAtt;
        private readonly OffsetAttribute offsetAtt;
        int off = 0;

        // TODO: "register" with LuceneTestCase to ensure all streams are closed() ?
        // currently, we can only check that the lifecycle is correct if someone is reusing,
        // but not for "one-offs".
        private enum State
        {
            SETREADER,       // consumer set a reader input either via ctor or via reset(Reader)
            RESET,           // consumer has called reset()
            INCREMENT,       // consumer is consuming, has called incrementToken() == true
            INCREMENT_FALSE, // consumer has called incrementToken() which returned false
            END,             // consumer has called end() to perform end of stream operations
            CLOSE            // consumer has called close() to release any resources
        };

        private State streamState = State.CLOSE;
        private int lastOffset = 0; // only for asserting
        private bool enableChecks = true;

        // evil: but we don't change the behavior with this random, we only switch up how we read
        private Random random = new Random(/*RandomizedContext.Current.getRandom().nextLong()*/);

        public MockTokenizer(AttributeSource.AttributeFactory factory, System.IO.TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase, int maxTokenLength)
            : base(factory, input)
        {
            this.runAutomaton = runAutomaton;
            this.lowerCase = lowerCase;
            this.state = runAutomaton.InitialState;
            this.streamState = State.SETREADER;
            this.maxTokenLength = maxTokenLength;

            termAtt = AddAttribute<CharTermAttribute>();
            offsetAtt = AddAttribute<OffsetAttribute>();
        }

        public MockTokenizer(System.IO.TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase, int maxTokenLength) :
            this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, runAutomaton, lowerCase, maxTokenLength)
        {
        }

        public MockTokenizer(System.IO.TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase) :
            this(input, runAutomaton, lowerCase, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }
        /** Calls {@link #MockTokenizer(Reader, CharacterRunAutomaton, boolean) MockTokenizer(Reader, WHITESPACE, true)} */
        public MockTokenizer(System.IO.TextReader input) :
            this(input, WHITESPACE, true)
        {
        }

        public MockTokenizer(AttributeFactory factory, System.IO.TextReader input, CharacterRunAutomaton runAutomaton, bool lowerCase) :
            this(factory, input, runAutomaton, lowerCase, DEFAULT_MAX_TOKEN_LENGTH)
        {
        }

        /** Calls {@link #MockTokenizer(org.apache.lucene.util.AttributeSource.AttributeFactory,Reader,CharacterRunAutomaton,boolean)
         *                MockTokenizer(AttributeFactory, Reader, WHITESPACE, true)} */

        public MockTokenizer(AttributeFactory factory, System.IO.TextReader input) :
            this(input, WHITESPACE, true)
        {

        }

        public override bool IncrementToken()
        {
            //    assert !enableChecks || (streamState == State.RESET || streamState == State.INCREMENT) 
            //                            : "incrementToken() called while in wrong state: " + streamState;
            ClearAttributes();
            for (; ; )
            {
                int startOffset = off;
                int cp = readCodePoint();
                if (cp < 0)
                {
                    break;
                }
                else if (isTokenChar(cp))
                {
                    int endOffset;
                    do
                    {
                        char[] chars = Character.ToChars(Normalize(cp));
                        for (int i = 0; i < chars.Length; i++)
                            termAtt.Append(chars[i]);
                        endOffset = off;
                        if (termAtt.Length >= maxTokenLength)
                        {
                            break;
                        }
                        cp = readCodePoint();
                    } while (cp >= 0 && isTokenChar(cp));

                    int correctedStartOffset = CorrectOffset(startOffset);
                    int correctedEndOffset = CorrectOffset(endOffset);
                    //        assert correctedStartOffset >= 0;
                    //        assert correctedEndOffset >= 0;
                    //        assert correctedStartOffset >= lastOffset;
                    lastOffset = correctedStartOffset;
                    //        assert correctedEndOffset >= correctedStartOffset;
                    offsetAtt.SetOffset(correctedStartOffset, correctedEndOffset);
                    streamState = State.INCREMENT;
                    return true;
                }
            }
            streamState = State.INCREMENT_FALSE;
            return false;
        }

        protected int readCodePoint()
        {
            int ch = ReadChar();
            if (ch < 0)
            {
                return ch;
            }
            else
            {
                //assert !Character.isLowSurrogate((char) ch) : "unpaired low surrogate: " + Integer.toHexString(ch);
                off++;
                if (Character.IsHighSurrogate((char)ch))
                {
                    int ch2 = ReadChar();
                    if (ch2 >= 0)
                    {
                        off++;
                        //assert Character.isLowSurrogate((char) ch2) : "unpaired high surrogate: " + Integer.toHexString(ch) + ", followed by: " + Integer.toHexString(ch2);
                        return Character.ToCodePoint((char)ch, (char)ch2);
                    }
                    else
                    {
                        //assert false : "stream ends with unpaired high surrogate: " + Integer.toHexString(ch);
                    }
                }
                return ch;
            }
        }

        protected int ReadChar()
        {
            switch (random.Next(0, 10))
            {
                case 0:
                    {
                        // read(char[])
                        char[] c = new char[1];
                        int ret = input.Read(c, 0, c.Length);
                        return ret < 0 ? ret : c[0];
                    }
                case 1:
                    {
                        // read(char[], int, int)
                        char[] c = new char[2];
                        int ret = input.Read(c, 1, 1);
                        return ret < 0 ? ret : c[1];
                    }
                //      case 2: {
                //        // read(CharBuffer)
                //        char[] c = new char[1];
                //        CharBuffer cb = CharBuffer.wrap(c);
                //        int ret = input.Read(cb);
                //        return ret < 0 ? ret : c[0];
                //      }
                default:
                    // read()
                    return input.Read();
            }
        }

        protected bool isTokenChar(int c)
        {
            state = runAutomaton.Step(state, c);
            if (state < 0)
            {
                state = runAutomaton.InitialState;
                return false;
            }
            else
            {
                return true;
            }
        }

        protected int Normalize(int c)
        {
            return lowerCase ? Character.ToLowerCase(c) : c;
        }

        public override void Reset()
        {
            base.Reset();
            state = runAutomaton.InitialState;
            lastOffset = off = 0;
            //assert !enableChecks || streamState != State.RESET : "double reset()";
            streamState = State.RESET;
        }

        protected virtual void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            // in some exceptional cases (e.g. TestIndexWriterExceptions) a test can prematurely close()
            // these tests should disable this check, by default we check the normal workflow.
            // TODO: investigate the CachingTokenFilter "double-close"... for now we ignore this
            //assert !enableChecks || streamState == State.END || streamState == State.CLOSE : "close() called in wrong state: " + streamState;
            streamState = State.CLOSE;
        }

        bool setReaderTestPoint()
        {
            //assert !enableChecks || streamState == State.CLOSE : "setReader() called in wrong state: " + streamState;
            streamState = State.SETREADER;
            return true;
        }

        public override void End()
        {
            int finalOffset = CorrectOffset(off);
            offsetAtt.SetOffset(finalOffset, finalOffset);
            // some tokenizers, such as limiting tokenizers, call end() before incrementToken() returns false.
            // these tests should disable this check (in general you should consume the entire stream)
            try
            {
                //assert !enableChecks || streamState == State.INCREMENT_FALSE : "end() called before incrementToken() returned false!";
            }
            finally
            {
                streamState = State.END;
            }
        }

        /** 
         * Toggle consumer workflow checking: if your test consumes tokenstreams normally you
         * should leave this enabled.
         */
        public void setEnableChecks(bool enableChecks)
        {
            this.enableChecks = enableChecks;
        }
    }

}
