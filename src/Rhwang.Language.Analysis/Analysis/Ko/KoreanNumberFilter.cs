using java.lang;
using java.math;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System.Collections.Generic;
using ArithmeticException = java.lang.ArithmeticException;

namespace Lucene.Net.Analysis.Ko
{
    public class KoreanNumberFilter : TokenFilter
    {
        private readonly ICharTermAttribute termAttr;
        private readonly IOffsetAttribute offsetAttr;
        private readonly IKeywordAttribute keywordAttr;
        private readonly IPositionIncrementAttribute posIncrAttr;
        private readonly IPositionLengthAttribute posLengthAttr;

        private static char NO_NUMERAL = Character.MAX_VALUE;

        private static Dictionary<char, int> numerals = new() {
            ['영'] = 0, // 영 U+C601 0
            ['일'] = 1, // 일 U+C77C 1
            ['이'] = 2, // 이 U+C774 2
            ['삼'] = 3, // 삼 U+C0BC 3
            ['사'] = 4, // 사 U+C0AC 4
            ['오'] = 5, // 오 U+C624 5
            ['육'] = 6, // 육 U+C721 6
            ['칠'] = 7, // 칠 U+CE60 7
            ['팔'] = 8, // 팔 U+D314 8
            ['구'] = 9, // 구 U+AD6C 9
        };

        private static Dictionary<char, int> exponents = new() {
            ['십'] = 1, // 십 U+C2ED 10
            ['백'] = 2, // 백 U+BC31 100
            ['천'] = 3, // 천 U+CC9C 1,000
            ['만'] = 4, // 만 U+B9CC 10,000
            ['억'] = 8, // 억 U+C5B5 100,000,000
            ['조'] = 12, // 조 U+C870 1,000,000,000,000
            ['경'] = 16, // 경 U+ACBD 10,000,000,000,000,000
            ['해'] = 20, // 해 U+D574 100,000,000,000,000,000,000
        };

        private State state;

        private StringBuilder numeral;

        private int fallThroughTokens;

        private bool exhausted = false;

        public KoreanNumberFilter(TokenStream input)
            : base(input)
        {
            termAttr = AddAttribute<ICharTermAttribute>();
            offsetAttr = AddAttribute<IOffsetAttribute>();
            keywordAttr = AddAttribute<IKeywordAttribute>();
            posIncrAttr = AddAttribute<IPositionIncrementAttribute>();
            posLengthAttr = AddAttribute<IPositionLengthAttribute>();
        }

        public override bool IncrementToken()
        {

            // Emit previously captured token we read past earlier
            if (state != null)
            {
                RestoreState(state);
                state = null;
                return true;
            }

            if (exhausted)
            {
                return false;
            }

            if (!m_input.IncrementToken())
            {
                exhausted = true;
                return false;
            }

            if (keywordAttr.IsKeyword)
            {
                return true;
            }

            if (fallThroughTokens > 0)
            {
                fallThroughTokens--;
                return true;
            }

            if (posIncrAttr.PositionIncrement == 0)
            {
                fallThroughTokens = posLengthAttr.PositionLength - 1;
                return true;
            }

            bool moreTokens = true;
            bool composedNumberToken = false;
            int startOffset = 0;
            int endOffset = 0;
            State preCompositionState = CaptureState();
            string term = termAttr.ToString();
            bool numeralTerm = IsNumeral(term);

            while (moreTokens && numeralTerm)
            {

                if (!composedNumberToken)
                {
                    startOffset = offsetAttr.StartOffset;
                    composedNumberToken = true;
                }

                endOffset = offsetAttr.EndOffset;
                moreTokens = m_input.IncrementToken();
                if (moreTokens == false)
                {
                    exhausted = true;
                }

                if (posIncrAttr.PositionIncrement == 0)
                {
                    // This token is a stacked/synonym token, capture number of tokens "under" this token,
                    // except the first token, which we will emit below after restoring state
                    fallThroughTokens = posLengthAttr.PositionLength - 1;
                    state = CaptureState();
                    RestoreState(preCompositionState);
                    return moreTokens;
                }

                numeral.append(term);

                if (moreTokens)
                {
                    term = termAttr.ToString();
                    numeralTerm = IsNumeral(term) || IsNumeralPunctuation(term);
                }
            }

            if (composedNumberToken)
            {
                if (moreTokens)
                {
                    // We have read past all numerals and there are still tokens left, so
                    // capture the state of this token and emit it on our next incrementToken()
                    state = CaptureState();
                }

                string normalizedNumber = NormalizeNumber(numeral.toString());

                termAttr.SetEmpty();
                termAttr.Append(normalizedNumber);
                offsetAttr.SetOffset(startOffset, endOffset);

                numeral = new StringBuilder();
                return true;
            }

            return moreTokens;
        }

        public override void Reset()
        {
            this.Reset();
            fallThroughTokens = 0;
            numeral = new StringBuilder();
            state = null;
            exhausted = false;
        }

        /**
   * Normalizes a Korean number
   *
   * @param number number or normalize
   * @return normalized number, or number to normalize on error (no op)
   */
        public string NormalizeNumber(string number)
        {
            try
            {
                BigDecimal normalizedNumber = ParseNumber(new NumberBuffer(number));
                if (normalizedNumber == null)
                {
                    return number;
                }

                return normalizedNumber.stripTrailingZeros().
                    toPlainString();
            }
            catch (Exception e) {
                // Return the source number in case of error, i.e. malformed input
                if (e is NumberFormatException || e is ArithmeticException)
                {
                    return number;
                }
                throw e;
            }
        }

        /**
       * Parses a Korean number
       *
       * @param buffer buffer to parse
       * @return parsed number, or null on error or end of input
       */
        private BigDecimal ParseNumber(NumberBuffer buffer)
        {
            BigDecimal sum = BigDecimal.ZERO;
            BigDecimal result = ParseLargePair(buffer);

            if (result == null)
            {
                return null;
            }

            while (result != null)
            {
                sum = sum.add(result);
                result = ParseLargePair(buffer);
            }

            return sum;
        }

        /**
       * Parses a pair of large numbers, i.e. large Hangul factor is 10,000（만）or larger
       *
       * @param buffer buffer to parse
       * @return parsed pair, or null on error or end of input
       */
        private BigDecimal ParseLargePair(NumberBuffer buffer)
        {
            BigDecimal first = ParseMediumNumber(buffer);
            BigDecimal second = ParseLargeHangulNumeral(buffer);

            if (first == null && second == null)
            {
                return null;
            }

            if (second == null)
            {
                // If there's no second factor, we return the first one
                // This can happen if we our number is smaller than 10,000 (만)
                return first;
            }

            if (first == null)
            {
                // If there's no first factor, just return the second one,
                // which is the same as multiplying by 1, i.e. with 만
                return second;
            }

            return first.multiply(second);
        }

        /**
       * Parses a "medium sized" number, typically less than 10,000（만）, but might be larger
       * due to a larger factor from {link parseBasicNumber}.
       *
       * @param buffer buffer to parse
       * @return parsed number, or null on error or end of input
       */
        private BigDecimal ParseMediumNumber(NumberBuffer buffer)
        {
            BigDecimal sum = BigDecimal.ZERO;
            BigDecimal result = ParseMediumPair(buffer);

            if (result == null)
            {
                return null;
            }

            while (result != null)
            {
                sum = sum.add(result);
                result = ParseMediumPair(buffer);
            }

            return sum;
        }

        /**
       * Parses a pair of "medium sized" numbers, i.e. large Hangul factor is at most 1,000（천）
       *
       * @param buffer buffer to parse
       * @return parsed pair, or null on error or end of input
       */
        private BigDecimal ParseMediumPair(NumberBuffer buffer)
        {
            BigDecimal first = ParseBasicNumber(buffer);
            BigDecimal second = ParseMediumHangulNumeral(buffer);

            if (first == null && second == null)
            {
                return null;
            }

            if (second == null)
            {
                // If there's no second factor, we return the first one
                // This can happen if we just have a plain number such as 오
                return first;
            }

            if (first == null)
            {
                // If there's no first factor, just return the second one,
                // which is the same as multiplying by 1, i.e. with 천
                return second;
            }

            // Return factors multiplied
            return first.multiply(second);
        }

        /**
       * Parse a basic number, which is a sequence of Arabic numbers or a sequence or 0-9 Hangul numerals (영 to 구).
       *
       * @param buffer buffer to parse
       * @return parsed number, or null on error or end of input
       */
        private BigDecimal ParseBasicNumber(NumberBuffer buffer)
        {
            StringBuilder builder = new StringBuilder();
            int i = buffer.position;

            while (i < buffer.Length())
            {
                char c = buffer.CharAt(i);

                if (IsArabicNumeral(c))
                {
                    // Arabic numerals; 0 to 9 or ０ to ９ (full-width)
                    builder.append(ArabicNumeralValue(c));
                }
                else if (IsHangulNumeral(c))
                {
                    // Hangul numerals; 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구
                    builder.append(HangulNumeralValue(c));
                }
                else if (IsDecimalPoint(c))
                {
                    builder.append(".");
                }
                else if (IsThousandSeparator(c))
                {
                    // Just skip and move to the next character
                }
                else
                {
                    // We don't have an Arabic nor Hangul numeral, nor separation or punctuation, so we'll stop.
                    break;
                }

                i++;
                buffer.Advance();
            }

            if (builder.length() == 0)
            {
                // We didn't build anything, so we don't have a number
                return null;
            }

            return new BigDecimal(builder.toString());
        }

        /**
       * Parse large Hangul numerals (ten thousands or larger)
       *
       * @param buffer buffer to parse
       * @return parsed number, or null on error or end of input
       */
        public BigDecimal ParseLargeHangulNumeral(NumberBuffer buffer)
        {
            int i = buffer.position;

            if (i >= buffer.Length())
            {
                return null;
            }

            char c = buffer.CharAt(i);
            int power = exponents[c];

            if (power > 3)
            {
                buffer.Advance();
                return BigDecimal.TEN.pow(power);
            }

            return null;
        }

        /**
       * Parse medium Hangul numerals (tens, hundreds or thousands)
       *
       * @param buffer buffer to parse
       * @return parsed number or null on error
       */
        public BigDecimal ParseMediumHangulNumeral(NumberBuffer buffer)
        {
            int i = buffer.position;

            if (i >= buffer.Length())
            {
                return null;
            }

            char c = buffer.CharAt(i);
            int power = exponents[c];

            if (1 <= power && power <= 3)
            {
                buffer.Advance();
                return BigDecimal.TEN.pow(power);
            }

            return null;
        }

        /**
       * Numeral predicate
       *
       * @param input string to test
       * @return true if and only if input is a numeral
       */
        public bool IsNumeral(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (!IsNumeral(input[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /**
       * Numeral predicate
       *
       * @param c character to test
       * @return true if and only if c is a numeral
       */
        public bool IsNumeral(char c)
        {
            return IsArabicNumeral(c) || IsHangulNumeral(c) || exponents.ContainsKey(c);
        }

        /**
       * Numeral punctuation predicate
       *
       * @param input string to test
       * @return true if and only if c is a numeral punctuation string
       */
        public bool IsNumeralPunctuation(string input)
        {
            for (int i = 0; i < input.Length; i++)
            {
                if (!IsNumeralPunctuation(input[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /**
       * Numeral punctuation predicate
       *
       * @param c character to test
       * @return true if and only if c is a numeral punctuation character
       */
        public bool IsNumeralPunctuation(char c)
        {
            return IsDecimalPoint(c) || IsThousandSeparator(c);
        }

        /**
       * Arabic numeral predicate. Both half-width and full-width characters are supported
       *
       * @param c character to test
       * @return true if and only if c is an Arabic numeral
       */
        public bool IsArabicNumeral(char c)
        {
            return IsHalfWidthArabicNumeral(c) || IsFullWidthArabicNumeral(c);
        }

        /**
       * Arabic half-width numeral predicate
       *
       * @param c character to test
       * @return true if and only if c is a half-width Arabic numeral
       */
        private bool IsHalfWidthArabicNumeral(char c)
        {
            // 0 U+0030 - 9 U+0039
            return '0' <= c && c <= '9';
        }

        /**
       * Arabic full-width numeral predicate
       *
       * @param c character to test
       * @return true if and only if c is a full-width Arabic numeral
       */
        private bool IsFullWidthArabicNumeral(char c)
        {
            // ０ U+FF10 - ９ U+FF19
            return '０' <= c && c <= '９';
        }

        /**
       * Returns the numeric value for the specified character Arabic numeral.
       * Behavior is undefined if a non-Arabic numeral is provided
       *
       * @param c arabic numeral character
       * @return numeral value
       */
        private int ArabicNumeralValue(char c)
        {
            int offset;
            if (IsHalfWidthArabicNumeral(c))
            {
                offset = '0';
            }
            else
            {
                offset = '０';
            }

            return c - offset;
        }

        /**
       * Hangul numeral predicate that tests if the provided character is one of 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구.
       * Larger number Hangul gives a false value.
       *
       * @param c character to test
       * @return true if and only is character is one of 영, 일, 이, 삼, 사, 오, 육, 칠, 팔, or 구 (0 to 9)
       */
        private bool IsHangulNumeral(char c)
        {
            return numerals.ContainsKey(c);
        }

        /**
       * Returns the value for the provided Hangul numeral. Only numeric values for the characters where
       * {link isHangulNumeral} return true are supported - behavior is undefined for other characters.
       *
       * @param c Hangul numeral character
       * @return numeral value
       * @see #isHangulNumeral(char)
       */
        private int HangulNumeralValue(char c)
        {
            return numerals[c];
        }

        /**
       * Decimal point predicate
       *
       * @param c character to test
       * @return true if and only if c is a decimal point
       */
        private bool IsDecimalPoint(char c)
        {
            return c == '.' // U+002E FULL STOP
                   || c == '．'; // U+FF0E FULLWIDTH FULL STOP
        }

        /**
       * Thousand separator predicate
       *
       * @param c character to test
       * @return true if and only if c is a thousand separator predicate
       */
        private bool IsThousandSeparator(char c)
        {
            return c == ',' // U+002C COMMA
                   || c == '，'; // U+FF0C FULLWIDTH COMMA
        }

        /**
       * Buffer that holds a Korean number string and a position index used as a parsed-to marker
       */
        public class NumberBuffer
        {

            public int position { get; private set; }

            private string str;

            public NumberBuffer(string str)
            {
                this.str = str;
                position = 0;
            }

            public char CharAt(int index)
            {
                return str[index];
            }

            public int Length()
            {
                return str.Length;
            }

            public void Advance()
            {
                position++;
            }

        }
    }
}