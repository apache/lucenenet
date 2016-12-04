using Lucene.Net.QueryParsers.Flexible.Messages;

namespace Lucene.Net.QueryParsers.Flexible.Core.Messages
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

    /// <summary>
    /// Flexible Query Parser message bundle class
    /// </summary>
    public class QueryParserMessages : NLS
    {
        private static readonly string BUNDLE_NAME = typeof(QueryParserMessages).Name;

        private QueryParserMessages()
        {
            // Do not instantiate
        }

        static QueryParserMessages()
        {
            // register all string ids with NLS class and initialize static string
            // values
            NLS.InitializeMessages(BUNDLE_NAME, typeof(QueryParserMessages));
        }

        // static string must match the strings in the property files.
        public static string INVALID_SYNTAX;
        public static string INVALID_SYNTAX_CANNOT_PARSE;
        public static string INVALID_SYNTAX_FUZZY_LIMITS;
        public static string INVALID_SYNTAX_FUZZY_EDITS;
        public static string INVALID_SYNTAX_ESCAPE_UNICODE_TRUNCATION;
        public static string INVALID_SYNTAX_ESCAPE_CHARACTER;
        public static string INVALID_SYNTAX_ESCAPE_NONE_HEX_UNICODE;
        public static string NODE_ACTION_NOT_SUPPORTED;
        public static string PARAMETER_VALUE_NOT_SUPPORTED;
        public static string LUCENE_QUERY_CONVERSION_ERROR;
        public static string EMPTY_MESSAGE;
        public static string WILDCARD_NOT_SUPPORTED;
        public static string TOO_MANY_BOOLEAN_CLAUSES;
        public static string LEADING_WILDCARD_NOT_ALLOWED;
        public static string COULD_NOT_PARSE_NUMBER;
        public static string NUMBER_CLASS_NOT_SUPPORTED_BY_NUMERIC_RANGE_QUERY;
        public static string UNSUPPORTED_NUMERIC_DATA_TYPE;
        public static string NUMERIC_CANNOT_BE_EMPTY;
    }
}
