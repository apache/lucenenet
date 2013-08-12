using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public sealed class KeywordTokenizer : Tokenizer
    {
        public const int DEFAULT_BUFFER_SIZE = 256;

        private bool done = false;
        private int finalOffset;
        private readonly ICharTermAttribute termAtt; // = AddAttribute<ICharTermAttribute>();
        private IOffsetAttribute offsetAtt; // = AddAttribute<IOffsetAttribute>();

        public KeywordTokenizer(TextReader input)
            : this(input, DEFAULT_BUFFER_SIZE)
        {
        }

        public KeywordTokenizer(TextReader input, int bufferSize)
            : base(input)
        {
            // .NET Port: can't inline this
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();

            if (bufferSize <= 0)
            {
                throw new ArgumentException("bufferSize must be > 0");
            }
            termAtt.ResizeBuffer(bufferSize);
        }

        public KeywordTokenizer(AttributeFactory factory, TextReader input, int bufferSize)
            : base(factory, input)
        {
            // .NET Port: can't inline this
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();

            if (bufferSize <= 0)
            {
                throw new ArgumentException("bufferSize must be > 0");
            }
            termAtt.ResizeBuffer(bufferSize);
        }

        public override bool IncrementToken()
        {
            if (!done)
            {
                ClearAttributes();
                done = true;
                int upto = 0;
                char[] buffer = termAtt.Buffer;
                while (true)
                {
                    int length = input.Read(buffer, upto, buffer.Length - upto);
                    if (length <= 0) break;
                    upto += length;
                    if (upto == buffer.Length)
                        buffer = termAtt.ResizeBuffer(1 + buffer.Length);
                }
                termAtt.SetLength(upto);
                finalOffset = CorrectOffset(upto);
                offsetAtt.SetOffset(CorrectOffset(0), finalOffset);
                return true;
            }
            return false;
        }

        public override void End()
        {
            // set final offset 
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            this.done = false;
        }
    }
}
