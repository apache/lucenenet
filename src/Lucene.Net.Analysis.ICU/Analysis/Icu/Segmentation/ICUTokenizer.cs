// LUCENENET TODO: Port issues - missing dependencies

//using Lucene.Net.Analysis.ICU.TokenAttributes;
//using Lucene.Net.Analysis.TokenAttributes;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.Segmentation
//{
//    /// <summary>
//    /// Breaks text into words according to UAX #29: Unicode Text Segmentation
//    /// (http://www.unicode.org/reports/tr29/)
//    /// <para/>
//    /// Words are broken across script boundaries, then segmented according to
//    /// the BreakIterator and typing provided by the <see cref="ICUTokenizerConfig"/>
//    /// <para/>
//    /// @lucene.experimental
//    /// </summary>
//    /// <seealso cref="ICUTokenizerConfig"/>
//    public sealed class ICUTokenizer : Tokenizer
//    {
//        private static readonly int IOBUFFER = 4096;
//        private readonly char[] buffer = new char[IOBUFFER];
//        /** true length of text in the buffer */
//        private int length = 0;
//        /** length in buffer that can be evaluated safely, up to a safe end point */
//        private int usableLength = 0;
//        /** accumulated offset of previous buffers for this reader, for offsetAtt */
//        private int offset = 0;

//        private readonly CompositeBreakIterator breaker; /* tokenizes a char[] of text */
//        private readonly ICUTokenizerConfig config;
//        private readonly IOffsetAttribute offsetAtt;
//        private readonly ICharTermAttribute termAtt;
//        private readonly ITypeAttribute typeAtt;
//        private readonly IScriptAttribute scriptAtt;

//        /**
//        * Construct a new ICUTokenizer that breaks text into words from the given
//        * Reader.
//        * <p>
//        * The default script-specific handling is used.
//        * <p>
//        * The default attribute factory is used.
//        * 
//        * @param input Reader containing text to tokenize.
//        * @see DefaultICUTokenizerConfig
//        */
//        public ICUTokenizer(TextReader input)
//            : this(input, new DefaultICUTokenizerConfig(true))
//        {
//        }

//        /**
//         * Construct a new ICUTokenizer that breaks text into words from the given
//         * Reader, using a tailored BreakIterator configuration.
//         * <p>
//         * The default attribute factory is used.
//         *
//         * @param input Reader containing text to tokenize.
//         * @param config Tailored BreakIterator configuration 
//         */
//        public ICUTokenizer(TextReader input, ICUTokenizerConfig config)
//            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY, input, config)
//        {
//        }

//        /**
//         * Construct a new ICUTokenizer that breaks text into words from the given
//         * Reader, using a tailored BreakIterator configuration.
//         *
//         * @param factory AttributeFactory to use
//         * @param input Reader containing text to tokenize.
//         * @param config Tailored BreakIterator configuration 
//         */
//        public ICUTokenizer(AttributeFactory factory, TextReader input, ICUTokenizerConfig config)
//            : base(factory, input)
//        {
//            this.config = config;
//            breaker = new CompositeBreakIterator(config);

//            this.offsetAtt = AddAttribute<IOffsetAttribute>();
//            this.termAtt = AddAttribute<ICharTermAttribute>();
//            this.typeAtt = AddAttribute<ITypeAttribute>();
//            this.scriptAtt = AddAttribute<IScriptAttribute>();
//        }


//        public override bool IncrementToken()
//        {
//            ClearAttributes();
//            if (length == 0)
//                Refill();
//            while (!IncrementTokenBuffer())
//            {
//                Refill();
//                if (length <= 0) // no more bytes to read;
//                    return false;
//            }
//            return true;
//        }


//        public override void Reset()
//        {
//            base.Reset();
//            breaker.SetText(buffer, 0, 0);
//            length = usableLength = offset = 0;
//        }

//        public override void End()
//        {
//            base.End();
//            int finalOffset = (length < 0) ? offset : offset + length;
//            offsetAtt.SetOffset(CorrectOffset(finalOffset), CorrectOffset(finalOffset));
//        }

//        /*
//         * This tokenizes text based upon the longest matching rule, and because of 
//         * this, isn't friendly to a Reader.
//         * 
//         * Text is read from the input stream in 4kB chunks. Within a 4kB chunk of
//         * text, the last unambiguous break point is found (in this implementation:
//         * white space character) Any remaining characters represent possible partial
//         * words, so are appended to the front of the next chunk.
//         * 
//         * There is the possibility that there are no unambiguous break points within
//         * an entire 4kB chunk of text (binary data). So there is a maximum word limit
//         * of 4kB since it will not try to grow the buffer in this case.
//         */

//        /**
//         * Returns the last unambiguous break position in the text.
//         * 
//         * @return position of character, or -1 if one does not exist
//         */
//        private int FindSafeEnd()
//        {
//            for (int i = length - 1; i >= 0; i--)
//                if (char.IsWhiteSpace(buffer[i]))
//                    return i + 1;
//            return -1;
//        }

//        /**
//         * Refill the buffer, accumulating the offset and setting usableLength to the
//         * last unambiguous break position
//         * 
//         * @throws IOException If there is a low-level I/O error.
//         */
//        private void Refill()
//        {
//            offset += usableLength;
//            int leftover = length - usableLength;
//            System.Array.Copy(buffer, usableLength, buffer, 0, leftover);
//            int requested = buffer.Length - leftover;
//            int returned = Read(m_input, buffer, leftover, requested);
//            length = returned + leftover;
//            if (returned < requested) /* reader has been emptied, process the rest */
//                usableLength = length;
//            else
//            { /* still more data to be read, find a safe-stopping place */
//                usableLength = FindSafeEnd();
//                if (usableLength < 0)
//                    usableLength = length; /*
//                                * more than IOBUFFER of text without space,
//                                * gonna possibly truncate tokens
//                                */
//            }

//            breaker.SetText(buffer, 0, Math.Max(0, usableLength));
//        }

//        // TODO: refactor to a shared readFully somewhere
//        // (NGramTokenizer does this too):
//        /** commons-io's readFully, but without bugs if offset != 0 */
//        private static int Read(TextReader input, char[] buffer, int offset, int length)
//        {
//            Debug.Assert(length >= 0, "length must not be negative: " + length);

//            int remaining = length;
//            while (remaining > 0)
//            {
//                int location = length - remaining;
//                int count = input.Read(buffer, offset + location, remaining);
//                if (-1 == count)
//                { // EOF
//                    break;
//                }
//                remaining -= count;
//            }
//            return length - remaining;
//        }

//        /*
//         * return true if there is a token from the buffer, or null if it is
//         * exhausted.
//         */
//        private bool IncrementTokenBuffer()
//        {
//            int start = breaker.Current;
//            if (start == Support.BreakIterator.DONE)
//                return false; // BreakIterator exhausted

//            // find the next set of boundaries, skipping over non-tokens (rule status 0)
//            int end = breaker.Next();
//            while (start != Support.BreakIterator.DONE && breaker.GetRuleStatus() == 0)
//            {
//                start = end;
//                end = breaker.Next();
//            }

//            if (start == Support.BreakIterator.DONE)
//                return false; // BreakIterator exhausted

//            termAtt.CopyBuffer(buffer, start, end - start);
//            offsetAtt.SetOffset(CorrectOffset(offset + start), CorrectOffset(offset + end));
//            typeAtt.Type = config.GetType(breaker.GetScriptCode(), breaker.GetRuleStatus());
//            scriptAtt.Code = breaker.GetScriptCode();

//            return true;
//        }
//    }
//}
