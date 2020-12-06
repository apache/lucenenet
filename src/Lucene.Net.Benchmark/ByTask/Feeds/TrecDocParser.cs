using J2N.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Parser for trec doc content, invoked on doc text excluding &lt;DOC&gt; and &lt;DOCNO&gt;
    /// which are handled in TrecContentSource. Required to be stateless and hence thread safe. 
    /// </summary>
    public abstract class TrecDocParser
    {
        /// <summary>Types of trec parse paths,</summary>
        public enum ParsePathType { GOV2, FBIS, FT, FR94, LATIMES, UNKNOWN }

        /// <summary>trec parser type used for unknown extensions</summary>
        public static readonly ParsePathType DEFAULT_PATH_TYPE = ParsePathType.GOV2;

        internal static readonly IDictionary<ParsePathType, TrecDocParser> pathType2parser = new Dictionary<ParsePathType, TrecDocParser>() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            { ParsePathType.GOV2, new TrecGov2Parser() },
            { ParsePathType.FBIS, new TrecFBISParser() },
            { ParsePathType.FR94, new TrecFR94Parser() },
            { ParsePathType.FT, new TrecFTParser() },
            { ParsePathType.LATIMES, new TrecLATimesParser() },
        };

        internal static readonly IDictionary<string, ParsePathType?> pathName2Type = LoadPathName2Type();
        private static IDictionary<string, ParsePathType?> LoadPathName2Type() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            var pathName2Type = new Dictionary<string, ParsePathType?>();
            foreach (ParsePathType ppt in Enum.GetValues(typeof(ParsePathType)))
            {
                pathName2Type[ppt.ToString().ToUpperInvariant()] = ppt;
            }
            return pathName2Type;
        }


        /// <summary>max length of walk up from file to its ancestors when looking for a known path type.</summary>
        private const int MAX_PATH_LENGTH = 10;

        /// <summary>
        /// Compute the path type of a file by inspecting name of file and its parents.
        /// </summary>
        public static ParsePathType PathType(FileInfo f)
        {
            int pathLength = 0;
            if (pathName2Type.TryGetValue(f.Name.ToUpperInvariant(), out ParsePathType? ppt) && ppt != null)
            {
                return ppt.Value;
            }
            // Walk up the directory names to find a match.
            DirectoryInfo parentDir = f.Directory;
            while (parentDir != null && ++pathLength < MAX_PATH_LENGTH)
            {
                if (pathName2Type.TryGetValue(parentDir.Name.ToUpperInvariant(), out ppt) && ppt != null)
                {
                    return ppt.Value;
                }
                parentDir = parentDir.Parent;
            }
            return DEFAULT_PATH_TYPE;
        }

        /// <summary>
        /// Parse the text prepared in docBuf into a result DocData, 
        /// no synchronization is required.
        /// </summary>
        /// <param name="docData">Reusable result.</param>
        /// <param name="name">Name that should be set to the result.</param>
        /// <param name="trecSrc">Calling trec content source.</param>
        /// <param name="docBuf">Text to parse.</param>
        /// <param name="pathType">Type of parsed file, or <see cref="ParsePathType.UNKNOWN"/> if unknown - may be used by 
        /// parsers to alter their behavior according to the file path type. </param>
        /// <returns></returns>
        public abstract DocData Parse(DocData docData, string name, TrecContentSource trecSrc,
            StringBuilder docBuf, ParsePathType pathType);

        /// <summary>
        /// strip tags from <code>buf</code>: each tag is replaced by a single blank.
        /// </summary>
        /// <returns>Text obtained when stripping all tags from <paramref name="buf"/> (input <see cref="StringBuilder"/> is unmodified).</returns>
        public static string StripTags(StringBuilder buf, int start)
        {
            return StripTags(buf.ToString(start, buf.Length - start), 0);
        }

        /// <summary>
        /// Strip tags from input.
        /// </summary>
        /// <seealso cref="StripTags(StringBuilder, int)"/>
        public static string StripTags(string buf, int start)
        {
            if (start > 0)
            {
                buf = buf.Substring(0);
            }
            return Regex.Replace(buf, "<[^>]*>", " ");
        }

        /// <summary>
        /// Extract from <paramref name="buf"/> the text of interest within specified tags.
        /// </summary>
        /// <param name="buf">Entire input text.</param>
        /// <param name="startTag">Tag marking start of text of interest.</param>
        /// <param name="endTag">Tag marking end of text of interest.</param>
        /// <param name="maxPos">if &#8805; 0 sets a limit on start of text of interest.</param>
        /// <param name="noisePrefixes">Text of interest or null if not found.</param>
        /// <returns></returns>
        public static string Extract(StringBuilder buf, string startTag, string endTag, int maxPos, string[] noisePrefixes)
        {
            int k1 = buf.IndexOf(startTag, StringComparison.Ordinal);
            if (k1 >= 0 && (maxPos < 0 || k1 < maxPos))
            {
                k1 += startTag.Length;
                int k2 = buf.IndexOf(endTag, k1, StringComparison.Ordinal);
                if (k2 >= 0 && (maxPos < 0 || k2 < maxPos))
                { // found end tag with allowed range
                    if (noisePrefixes != null)
                    {
                        foreach (string noise in noisePrefixes)
                        {
                            int k1a = buf.IndexOf(noise, k1, StringComparison.Ordinal);
                            if (k1a >= 0 && k1a < k2)
                            {
                                k1 = k1a + noise.Length;
                            }
                        }
                    }
                    return buf.ToString(k1, k2 - k1).Trim();
                }
            }
            return null;
        }

        //public static void main(String[] args) {
        //  System.out.println(stripTags("is it true that<space>2<<second space>><almost last space>1<one more space>?",0));
        //}
    }
}
