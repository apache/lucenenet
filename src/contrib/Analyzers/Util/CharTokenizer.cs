using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Util
{
    public abstract class CharTokenizer : Tokenizer
    {
        public CharTokenizer(Version? matchVersion, TextReader input)
            : base(input)
        {
            charUtils = CharacterUtils.GetInstance(matchVersion);
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public CharTokenizer(Version? matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            charUtils = CharacterUtils.GetInstance(matchVersion);
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        // note: bufferIndex is -1 here to best-effort AIOOBE consumers that don't call reset()
        private int offset = 0, bufferIndex = -1, dataLen = 0, finalOffset = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 4096;

        private readonly ICharTermAttribute termAtt; // = addAttribute(CharTermAttribute.class);
        private readonly IOffsetAttribute offsetAtt; // = addAttribute(OffsetAttribute.class);

        private readonly CharacterUtils charUtils;
        private readonly CharacterUtils.CharacterBuffer ioBuffer = CharacterUtils.NewCharacterBuffer(IO_BUFFER_SIZE);

        protected abstract bool IsTokenChar(int c);

        protected virtual int Normalize(int c)
        {
            return c;
        }

        public override bool IncrementToken()
        {
            ClearAttributes();
            int length = 0;
            int start = -1; // this variable is always initialized
            int end = -1;
            char[] buffer = termAtt.Buffer;
            while (true)
            {
                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    if (!charUtils.Fill(ioBuffer, input))
                    { // read supplementary char aware with CharacterUtils
                        dataLen = 0; // so next offset += dataLen won't decrement offset
                        if (length > 0)
                        {
                            break;
                        }
                        else
                        {
                            finalOffset = CorrectOffset(offset);
                            return false;
                        }
                    }
                    dataLen = ioBuffer.Length;
                    bufferIndex = 0;
                }
                // use CharacterUtils here to support < 3.1 UTF-16 code unit behavior if the char based methods are gone
                int c = charUtils.CodePointAt(ioBuffer.Buffer, bufferIndex);
                int charCount = Character.CharCount(c);
                bufferIndex += charCount;

                if (IsTokenChar(c))
                {               // if it's a token char
                    if (length == 0)
                    {                // start of token
                        //assert start == -1;
                        start = offset + bufferIndex - charCount;
                        end = start;
                    }
                    else if (length >= buffer.Length - 1)
                    { // check if a supplementary could run out of bounds
                        buffer = termAtt.ResizeBuffer(2 + length); // make sure a supplementary fits in the buffer
                    }
                    end += charCount;
                    length += Character.ToChars(Normalize(c), buffer, length); // buffer it, normalized
                    if (length >= MAX_WORD_LEN) // buffer overflow! make sure to check for >= surrogate pair could break == test
                        break;
                }
                else if (length > 0)             // at non-Letter w/ chars
                    break;                           // return 'em
            }

            termAtt.SetLength(length);
            //assert start != -1;
            offsetAtt.SetOffset(CorrectOffset(start), finalOffset = CorrectOffset(end));
            return true;

        }

        public override void End()
        {
            // set final offset
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
            finalOffset = 0;
            ioBuffer.Reset(); // make sure to reset the IO buffer!!
        }
    }
}
