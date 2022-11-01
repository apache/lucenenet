using Lucene.Net.Support;
using System;
using System.Text;
using System.Text.RegularExpressions;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Ja.Util
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
    /// Utility class for parsing CSV text
    /// </summary>
    public sealed class CSVUtil
    {
        private const char QUOTE = '"';

        private const char COMMA = ',';

        private static readonly Regex QUOTE_REPLACE_PATTERN = new Regex("^\"([^\"]+)\"$", RegexOptions.Compiled);

        private const string ESCAPED_QUOTE = "\"\"";

        private CSVUtil() { } // no instance!!!

        /// <summary>
        /// Parse CSV line
        /// </summary>
        /// <param name="line">line containing csv-encoded data</param>
        /// <returns>Array of values</returns>
        public static string[] Parse(string line)
        {
            bool insideQuote = false;
            JCG.List<string> result = new JCG.List<string>();
            int quoteCount = 0;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == QUOTE)
                {
                    insideQuote = !insideQuote;
                    quoteCount++;
                }

                if (c == COMMA && !insideQuote)
                {
                    string value = sb.ToString();
                    value = UnQuoteUnEscape(value);
                    result.Add(value);
                    sb.Length = 0;
                    continue;
                }

                sb.Append(c);
            }

            result.Add(sb.ToString());

            // Validate
            if (quoteCount % 2 != 0)
            {
                return Arrays.Empty<string>();
            }

            return result.ToArray(/*new String[result.size()]*/);
        }

        private static string UnQuoteUnEscape(string original)
        {
            string result = original;

            // Unquote
            if (result.IndexOf('\"') >= 0)
            {
                Match m = QUOTE_REPLACE_PATTERN.Match(original);
                if (m.Success)
                {
                    result = m.Groups[1].Value;
                }

                // Unescape
                if (result.IndexOf(ESCAPED_QUOTE, StringComparison.Ordinal) >= 0)
                {
                    result = result.Replace(ESCAPED_QUOTE, "\"");
                }
            }

            return result;
        }

        /// <summary>
        /// Quote and escape input value for CSV
        /// </summary>
        public static string QuoteEscape(string original)
        {
            string result = original;

            if (result.IndexOf('\"') >= 0)
            {
                result = result.Replace("\"", ESCAPED_QUOTE); // LUCENENET 4.8.0: Applied SOLR-9413 (was fixed in Lucene 6.2/7.0)
            }
            if (result.IndexOf(COMMA) >= 0)
            {
                result = "\"" + result + "\"";
            }
            return result;
        }
    }
}
