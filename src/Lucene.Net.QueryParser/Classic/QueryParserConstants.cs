namespace Lucene.Net.QueryParsers.Classic
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    public static class RegexpToken
    {
        /// <summary>End of File. </summary>
        public const int EOF = 0;
        /// <summary>RegularExpression Id. </summary>
        public const int NUM_CHAR = 1; // LUCENENET specific: removed leading underscore to make CLS compliant
        /// <summary>RegularExpression Id. </summary>
        public const int ESCAPED_CHAR = 2; // LUCENENET specific: removed leading underscore to make CLS compliant
        /// <summary>RegularExpression Id. </summary>
        public const int TERM_START_CHAR = 3; // LUCENENET specific: removed leading underscore to make CLS compliant
        /// <summary>RegularExpression Id. </summary>
        public const int TERM_CHAR = 4; // LUCENENET specific: removed leading underscore to make CLS compliant
        /// <summary>RegularExpression Id. </summary>
        public const int WHITESPACE = 5; // LUCENENET specific: removed leading underscore to make CLS compliant
        /// <summary>RegularExpression Id. </summary>
        public const int QUOTED_CHAR = 6; // LUCENENET specific: removed leading underscore to make CLS compliant
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
        public const int BAREOPER = 13;
        /// <summary>RegularExpression Id. </summary>
        public const int LPAREN = 14;
        /// <summary>RegularExpression Id. </summary>
        public const int RPAREN = 15;
        /// <summary>RegularExpression Id. </summary>
        public const int COLON = 16;
        /// <summary>RegularExpression Id. </summary>
        public const int STAR = 17;
        /// <summary>RegularExpression Id. </summary>
        public const int CARAT = 18;
        /// <summary>RegularExpression Id. </summary>
        public const int QUOTED = 19;
        /// <summary>RegularExpression Id. </summary>
        public const int TERM = 20;
        /// <summary>RegularExpression Id. </summary>
        public const int FUZZY_SLOP = 21;
        /// <summary>RegularExpression Id. </summary>
        public const int PREFIXTERM = 22;
        /// <summary>RegularExpression Id. </summary>
        public const int WILDTERM = 23;
        /// <summary>RegularExpression Id. </summary>
        public const int REGEXPTERM = 24;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEIN_START = 25;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEEX_START = 26;
        /// <summary>RegularExpression Id. </summary>
        public const int NUMBER = 27;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_TO = 28;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEIN_END = 29;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGEEX_END = 30;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_QUOTED = 31;
        /// <summary>RegularExpression Id. </summary>
        public const int RANGE_GOOP = 32;
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

    // LUCENENET NOTE: In Java, this was an interface. However, in 
    // .NET we cannot define constants in an interface.
    // So, instead we are making it a static class so it 
    // can be shared between classes with different base classes.

    // public interface QueryParserConstants

    /// <summary> Token literal values and constants.
    /// Generated by org.javacc.parser.OtherFilesGen#start()
    /// </summary>
    public static class QueryParserConstants
    {
        /// <summary>Literal token values. </summary>
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
            "<BAREOPER>",
            "\"(\"", 
            "\")\"", 
            "\":\"", 
            "\"*\"", 
            "\"^\"", 
            "<QUOTED>", 
            "<TERM>", 
            "<FUZZY_SLOP>", 
            "<PREFIXTERM>", 
            "<WILDTERM>", 
            "<REGEXPTERM>",
            "\"[\"", 
            "\"{\"", 
            "<NUMBER>", 
            "\"TO\"", 
            "\"]\"", 
            "<RANGEIN_QUOTED>", 
            "<RANGEIN_GOOP>", 
            "\"TO\"", 
            "\"}\"", 
            "<RANGE_QUOTED>",
            "<RANGE_GOOP>"
        };
    }
}