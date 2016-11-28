using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Parser
{
    public static class RegexpToken
    {
        /// <summary>End of File. </summary>
        public const int EOF = 0;
        /// <summary>RegularExpression Id. </summary>
        public const int _NUM_CHAR = 1;
        /// <summary>RegularExpression Id. </summary>
        public const int _ESCAPED_CHAR = 2;
        /// <summary>RegularExpression Id. </summary>
        public const int _TERM_START_CHAR = 3;
        /// <summary>RegularExpression Id. </summary>
        public const int _TERM_CHAR = 4;
        /// <summary>RegularExpression Id. </summary>
        public const int _WHITESPACE = 5;
        /// <summary>RegularExpression Id. </summary>
        public const int _QUOTED_CHAR = 6;
        /// <summary>RegularExpression Id. </summary>
        public const int AND = 8;
        /// <summary>RegularExpression Id. </summary>
        public const int OR = 9;
        /// <summary>RegularExpression Id. </summary>
        public const int NOT = 10;
        /// <summary>RegularExpression Id. </summary>
        public const int PLUS = 11;
        /// <summary>RegularExpression Id. </summary>
        public const int MINUS = 12;
        /// <summary>RegularExpression Id. </summary>
        public const int LPAREN = 13;
        /// <summary>RegularExpression Id. </summary>
        public const int RPAREN = 14;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_COLON = 15;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_EQUAL = 16;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_LESSTHAN = 17;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_LESSTHANEQ = 18;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_MORETHAN = 19;
        /// <summary>RegularExpression Id. </summary>
        public const int OP_MORETHANEQ = 20;
        /// <summary>RegularExpression Id. </summary>
        public const int CARAT = 21;
        /// <summary>RegularExpression Id. </summary>
        public const int QUOTED = 22;
        /// <summary>RegularExpression Id. </summary>
        public const int TERM = 23;
        /// <summary>RegularExpression Id. </summary>
        public const int FUZZY_SLOP = 24;
        /// <summary>RegularExpression Id. </summary>
        public const int REGEXPTERM = 25;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEIN_START = 26;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEEX_START = 27;
        /// <summary>RegularExpression Id. </summary>
        public const int NUMBER = 28;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_TO = 29;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEIN_END = 30;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEEX_END = 31;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_QUOTED = 32;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_GOOP = 33;
    }

    public static class LexicalToken
    {
        /// <summary>Lexical state.</summary>
        public const int Boost = 0;
        /// <summary>Lexical state.</summary>
        public const int Range = 1;
        /// <summary>Lexical state.</summary>
        public const int DEFAULT = 2;
    }

    public static class StandardSyntaxParserConstants
    {
        /// <summary>Literal token values.</summary>
        public static string[] TokenImage = new string[] {
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
