using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Util;
using Version = Lucene.Net.Util.Version;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Standard.Std31;
using Lucene.Net.Analysis.Standard.Std34;

namespace Lucene.Net.Analysis.Standard
{
    public sealed class StandardTokenizer : Tokenizer
    {
        private IStandardTokenizerInterface scanner;

        public const int ALPHANUM = 0;
        [Obsolete]
        public const int APOSTROPHE = 1;
        [Obsolete]
        public const int ACRONYM = 2;
        [Obsolete]
        public const int COMPANY = 3;
        public const int EMAIL = 4;
        [Obsolete]
        public const int HOST = 5;
        public const int NUM = 6;
        [Obsolete]
        public const int CJ = 7;
        [Obsolete]
        public const int ACRONYM_DEP = 8;
        public const int SOUTHEAST_ASIAN = 9;
        public const int IDEOGRAPHIC = 10;
        public const int HIRAGANA = 11;
        public const int KATAKANA = 12;
        public const int HANGUL = 13;

        public static readonly string[] TOKEN_TYPES = new string[] {
            "<ALPHANUM>",
            "<APOSTROPHE>",
            "<ACRONYM>",
            "<COMPANY>",
            "<EMAIL>",
            "<HOST>",
            "<NUM>",
            "<CJ>",
            "<ACRONYM_DEP>",
            "<SOUTHEAST_ASIAN>",
            "<IDEOGRAPHIC>",
            "<HIRAGANA>",
            "<KATAKANA>",
            "<HANGUL>"
          };

        private int maxTokenLength = StandardAnalyzer.DEFAULT_MAX_TOKEN_LENGTH;

        public int MaxTokenLength
        {
            get { return maxTokenLength; }
            set { maxTokenLength = value; }
        }

        public StandardTokenizer(Version? matchVersion, TextReader input)
            : base(input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();

            Init(matchVersion.GetValueOrDefault());
        }

        public StandardTokenizer(Version? matchVersion, AttributeFactory factory, TextReader input)
            : base(factory, input)
        {
            termAtt = AddAttribute<ICharTermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();

            Init(matchVersion.GetValueOrDefault());
        }

        private void Init(Version matchVersion)
        {
            // best effort NPE if you dont call reset
            if (matchVersion.OnOrAfter(Version.LUCENE_40))
            {
                this.scanner = new StandardTokenizerImpl(null);
            }
            else if (matchVersion.OnOrAfter(Version.LUCENE_34))
            {
                this.scanner = new StandardTokenizerImpl34(null);
            }
            else if (matchVersion.OnOrAfter(Version.LUCENE_31))
            {
                this.scanner = new StandardTokenizerImpl31(null);
            }
            else
            {
                this.scanner = new ClassicTokenizerImpl(null);
            }
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
                    // This 'if' should be removed in the next release. For now, it converts
                    // invalid acronyms to HOST. When removed, only the 'else' part should
                    // remain.
                    if (tokenType == StandardTokenizer.ACRONYM_DEP)
                    {
                        typeAtt.Type = StandardTokenizer.TOKEN_TYPES[StandardTokenizer.HOST];
                        termAtt.SetLength(termAtt.Length - 1); // remove extra '.'
                    }
                    else
                    {
                        typeAtt.Type = StandardTokenizer.TOKEN_TYPES[tokenType];
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
