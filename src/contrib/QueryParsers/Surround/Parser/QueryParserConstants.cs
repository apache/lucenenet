using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Surround.Parser
{
    public static class QueryParserConstants
    {
        /** End of File. */
        public const int EOF = 0;
        /** RegularExpression Id. */
        public const int _NUM_CHAR = 1;
        /** RegularExpression Id. */
        public const int _TERM_CHAR = 2;
        /** RegularExpression Id. */
        public const int _WHITESPACE = 3;
        /** RegularExpression Id. */
        public const int _STAR = 4;
        /** RegularExpression Id. */
        public const int _ONE_CHAR = 5;
        /** RegularExpression Id. */
        public const int _DISTOP_NUM = 6;
        /** RegularExpression Id. */
        public const int OR = 8;
        /** RegularExpression Id. */
        public const int AND = 9;
        /** RegularExpression Id. */
        public const int NOT = 10;
        /** RegularExpression Id. */
        public const int W = 11;
        /** RegularExpression Id. */
        public const int N = 12;
        /** RegularExpression Id. */
        public const int LPAREN = 13;
        /** RegularExpression Id. */
        public const int RPAREN = 14;
        /** RegularExpression Id. */
        public const int COMMA = 15;
        /** RegularExpression Id. */
        public const int COLON = 16;
        /** RegularExpression Id. */
        public const int CARAT = 17;
        /** RegularExpression Id. */
        public const int TRUNCQUOTED = 18;
        /** RegularExpression Id. */
        public const int QUOTED = 19;
        /** RegularExpression Id. */
        public const int SUFFIXTERM = 20;
        /** RegularExpression Id. */
        public const int TRUNCTERM = 21;
        /** RegularExpression Id. */
        public const int TERM = 22;
        /** RegularExpression Id. */
        public const int NUMBER = 23;

        /** Lexical state. */
        public const int Boost = 0;
        /** Lexical state. */
        public const int DEFAULT = 1;

        /** Literal token values. */
        public static readonly String[] tokenImage = {
        "<EOF>",
        "<_NUM_CHAR>",
        "<_TERM_CHAR>",
        "<_WHITESPACE>",
        "\"*\"",
        "\"?\"",
        "<_DISTOP_NUM>",
        "<token of kind 7>",
        "<OR>",
        "<AND>",
        "<NOT>",
        "<W>",
        "<N>",
        "\"(\"",
        "\")\"",
        "\",\"",
        "\":\"",
        "\"^\"",
        "<TRUNCQUOTED>",
        "<QUOTED>",
        "<SUFFIXTERM>",
        "<TRUNCTERM>",
        "<TERM>",
        "<NUMBER>",
        };
    }
}
