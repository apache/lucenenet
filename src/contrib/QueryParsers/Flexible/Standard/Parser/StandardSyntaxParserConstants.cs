using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public static class StandardSyntaxParserConstants
    {
        /** End of File. */
        public const int EOF = 0;
        /** RegularExpression Id. */
        public const int _NUM_CHAR = 1;
        /** RegularExpression Id. */
        public const int _ESCAPED_CHAR = 2;
        /** RegularExpression Id. */
        public const int _TERM_START_CHAR = 3;
        /** RegularExpression Id. */
        public const int _TERM_CHAR = 4;
        /** RegularExpression Id. */
        public const int _WHITESPACE = 5;
        /** RegularExpression Id. */
        public const int _QUOTED_CHAR = 6;
        /** RegularExpression Id. */
        public const int AND = 8;
        /** RegularExpression Id. */
        public const int OR = 9;
        /** RegularExpression Id. */
        public const int NOT = 10;
        /** RegularExpression Id. */
        public const int PLUS = 11;
        /** RegularExpression Id. */
        public const int MINUS = 12;
        /** RegularExpression Id. */
        public const int LPAREN = 13;
        /** RegularExpression Id. */
        public const int RPAREN = 14;
        /** RegularExpression Id. */
        public const int OP_COLON = 15;
        /** RegularExpression Id. */
        public const int OP_EQUAL = 16;
        /** RegularExpression Id. */
        public const int OP_LESSTHAN = 17;
        /** RegularExpression Id. */
        public const int OP_LESSTHANEQ = 18;
        /** RegularExpression Id. */
        public const int OP_MORETHAN = 19;
        /** RegularExpression Id. */
        public const int OP_MORETHANEQ = 20;
        /** RegularExpression Id. */
        public const int CARAT = 21;
        /** RegularExpression Id. */
        public const int QUOTED = 22;
        /** RegularExpression Id. */
        public const int TERM = 23;
        /** RegularExpression Id. */
        public const int FUZZY_SLOP = 24;
        /** RegularExpression Id. */
        public const int REGEXPTERM = 25;
        /** RegularExpression Id. */
        public const int RANGEIN_START = 26;
        /** RegularExpression Id. */
        public const int RANGEEX_START = 27;
        /** RegularExpression Id. */
        public const int NUMBER = 28;
        /** RegularExpression Id. */
        public const int RANGE_TO = 29;
        /** RegularExpression Id. */
        public const int RANGEIN_END = 30;
        /** RegularExpression Id. */
        public const int RANGEEX_END = 31;
        /** RegularExpression Id. */
        public const int RANGE_QUOTED = 32;
        /** RegularExpression Id. */
        public const int RANGE_GOOP = 33;

        /** Lexical state. */
        public const int Boost = 0;
        /** Lexical state. */
        public const int Range = 1;
        /** Lexical state. */
        public const int DEFAULT = 2;

        /** Literal token values. */
        public static String[] tokenImage = {
            "<EOF>",
            "<_NUM_CHAR>",
            "<_ESCAPED_CHAR>",
            "<_TERM_START_CHAR>",
            "<_TERM_CHAR>",
            "<_WHITESPACE>",
            "<_QUOTED_CHAR>",
            "<token of kind 7>",
            "<AND>",
            "<OR>",
            "<NOT>",
            "\"+\"",
            "\"-\"",
            "\"(\"",
            "\")\"",
            "\":\"",
            "\"=\"",
            "\"<\"",
            "\"<=\"",
            "\">\"",
            "\">=\"",
            "\"^\"",
            "<QUOTED>",
            "<TERM>",
            "<FUZZY_SLOP>",
            "<REGEXPTERM>",
            "\"[\"",
            "\"{\"",
            "<NUMBER>",
            "\"TO\"",
            "\"]\"",
            "\"}\"",
            "<RANGE_QUOTED>",
            "<RANGE_GOOP>",
          };

    }
}
