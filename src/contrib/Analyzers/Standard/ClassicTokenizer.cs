using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analysis.Standard
{
    public sealed class ClassicTokenizer : Tokenizer
    {
        private IStandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        public const int APOSTROPHE = 1;
        public const int ACRONYM = 2;
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        public const int HOST = 5;
        public const int NUM = 6;
        public const int CJ = 7;

        public const int ACRONYM_DEP = 8;

        public static readonly string[] TOKEN_TYPES = new string[] {
            "<ALPHANUM>",
            "<APOSTROPHE>",
            "<ACRONYM>",
            "<COMPANY>",
            "<EMAIL>",
            "<HOST>",
            "<NUM>",
            "<CJ>",
            "<ACRONYM_DEP>"
          };

        private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        public int MaxTokenLength
        {
            get { return maxTokenLength; }
            set { maxTokenLength = value; }
        }

        public ClassicTokenizer(Version? matchVersion, TextReader input)
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();

            Init(matchVersion);
        }

        public ClassicTokenizer(Version? matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();

            Init(matchVersion);
        }

        private void Init(Version? matchVersion)
        {
            this.scanner = new ClassicTokenizerImpl(null); // best effort NPE if you dont call reset
        }

        // this tokenizer generates three attributes:
        // term offset, positionIncrement and type
        private readonly ICharTermAttribute termAtt; // = addAttribute(CharTermAttribute.class);
        private readonly IOffsetAttribute offsetAtt; // = addAttribute(OffsetAttribute.class);
        private readonly IPositionIncrementAttribute posIncrAtt; // = addAttribute(PositionIncrementAttribute.class);
        private readonly ITypeAttribute typeAtt; // = addAttribute(TypeAttribute.class);

        public override bool IncrementToken()
        {
            ClearAttributes();
            int posIncr = 1;

            while (true)
            {
                int tokenType = scanner.GetNextToken();

                if (tokenType == StandardTokenizerInterface.YYEOF)
                {
                    return false;
                }

                if (scanner.YYLength <= maxTokenLength)
                {
                    posIncrAtt.PositionIncrement = posIncr;
                    scanner.GetText(termAtt);
                    int start = scanner.YYChar;
                    offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + termAtt.Length));

                    if (tokenType == ClassicTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[ClassicTokenizer.HOST];
                        termAtt.SetLength(termAtt.Length - 1); // remove extra '.'
                    }
                    else
                    {
                        typeAtt.Type = ClassicTokenizer.TOKEN_TYPES[tokenType];
                    }
                    return true;
                }
                else
                    // When we skip a too-long term, we still increment the
                    // position increment
                    posIncr++;
            }
        }

        public override void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(scanner.YYChar + scanner.YYLength);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset()
        {
            scanner.YYReset(input);
        }
    }
}
